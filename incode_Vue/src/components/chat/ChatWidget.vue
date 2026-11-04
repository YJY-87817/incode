<script setup>
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute } from 'vue-router';
import axios from 'axios';
import * as signalR from "@microsoft/signalr";
import { useChatStore } from '@/stores/chatStore';
import { marked } from 'marked';
import aiAvatarGif from '@/assets/gif/1.gif';

const route = useRoute();
const chatStore = useChatStore();

// UI 狀態
const isOpen = ref(false);
const userInput = ref('');
const messagesContainer = ref(null);
const fileInput = ref(null);
const isSending = ref(false);
const connection = ref(null);
const connectionId = ref('');
const isConnecting = ref(false);
const textInput = ref(null);

const isLoggedIn = ref(false);
const isHumanMode = ref(false);
const previewImage = ref(null);
const selectedFile = ref(null);

// 在記憶體中暫存 Session ID
const currentSessionId = ref(0);

const API_BASE_URL = import.meta.env.VITE_BASE_URL; // 後端網址 (.env)

// 設定「不顯示按鈕」的頁面黑名單
const excludedPaths = [
    '/login',
    '/register',
    '/identity/account/login',
    '/identity/account/register'
];

// ==========================================
// 輔助函式
// ==========================================
const formatTime = (dateInput) => {
    const date = dateInput ? new Date(dateInput) : new Date();
    return date.toLocaleTimeString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour1: false });
};

const renderMarkdown = (text) => {
    if (!text) return '';
    return marked.parse(text);
};

const getFullImageUrl = (url) => {
    if (!url) return '';
    if (url.startsWith('http') || url.startsWith('data:')) return url;
    return `${API_BASE_URL}${url}`;
};

const toggleChat = () => {
    isOpen.value = !isOpen.value;
    if (isOpen.value) scrollToBottom();
};

const scrollToBottom = async () => {
    await nextTick();
    if (messagesContainer.value) {
        messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
    }
};

const removePreview = () => { selectedFile.value = null; previewImage.value = null; };

const ensureSession = async () => {
    if (currentSessionId.value > 0) {
        return { sessionId: currentSessionId.value, isNew: false };
    }

    try {
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/GetOrCreateSession`, {}, { withCredentials: true });
        const sid = res.data.sessionId;
        const isNew = res.data.isNew;

        currentSessionId.value = sid;
        chatStore.setSessionId(sid);
        
        return { sessionId: sid, isNew: isNew };

    } catch (error) {
        console.error("取得對話 Session 失敗:", error);
        return { sessionId: 0, isNew: true };
    }
};

const loadHistory = async () => {
    const { sessionId, isNew } = await ensureSession();

    if (!sessionId || sessionId === 0) return;

    if (isNew) {
        chatStore.clearMessages();
        return;
    }

    try {
        // 時間戳記（?_t=），強迫瀏覽器每次都去後端問最新狀態
        const res = await axios.get(`${API_BASE_URL}/api/ChatMessages/GetHistory/${sessionId}`, { withCredentials: true });
        const data = res.data;
        
        chatStore.clearMessages();
        
        if (Array.isArray(data)) {
            data.forEach(msg => {
                const isUser = msg.role === 'user';
                let content = msg.body;
                let imgUrl = null;
                
                if (content && content.includes('<img')) {
                    const match = content.match(/src='([^']*)'/);
                    if (match) imgUrl = getFullImageUrl(match[1]);
                    content = ''; 
                } else if (!isUser) {
                    content = renderMarkdown(content);
                }

                chatStore.addMessage({
                    role: isUser ? 'user' : 'ai',
                    text: content,
                    imageUrl: imgUrl,
                    time: new Date(msg.createdAt),
                    isHuman: msg.role === 'assistant' && (msg.body.includes("[真人]") || isHumanMode.value) 
                });
            });
        }
    } catch (error) {
        console.error("載入歷史失敗:", error);
    }
};

const initSignalR = async () => {
    if (connection.value?.state === signalR.HubConnectionState.Connected) return;
    if (isConnecting.value) return;

    isConnecting.value = true;

    try {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/chatHub`)
            .withAutomaticReconnect()
            .build();

        // ★★★ 關鍵修改：SignalR 監聽器 ★★★
        newConnection.on("ReceiveMessage", (type, message) => {
            // User 訊息我們自己在前端已經先顯示了，所以這裡可以選擇過濾掉，
            // 或者是保留它來確保多裝置同步 (這裡選擇保留，但要注意可能會有極短暫的重複閃爍，通常不影響體驗)
            // if (type === 'User') return; 

            // 系統訊息
            if (type === 'System') {
                if (message.includes("人工客服")) isHumanMode.value = true;
                if (message.includes("AI助手")) isHumanMode.value = false;
                chatStore.addMessage({ role: 'ai', text: `<i>${message}</i>`, isSystem: true, time: new Date() });
                return;
            }
            
            // 真人客服訊息
            if (type === 'Live') {
                chatStore.addMessage({ role: 'ai', text: message, imageUrl: null, isHuman: true, time: new Date() });
                isHumanMode.value = true;
                isSending.value = false; // 收到訊息就關閉 loading
                return;
            }

            // AI 訊息 (包含純文字與 Markdown)
            let imgUrl = null;
            let textContent = message;

            if (type === 'AI' || type === 'AI_Image') {
                 // 解析 Markdown (包含後端回傳的圖片語法 ![img](url))
                 textContent = renderMarkdown(message);
                 
                 chatStore.addMessage({ role: 'ai', text: textContent, imageUrl: null, isHuman: false, time: new Date() });
                 
                 // ★★★ 關鍵：收到 SignalR 就代表 AI 回應了，立刻關閉 Loading ★★★
                 isSending.value = false;
                 return;
            }

            // 使用者傳圖片的回音
            if (type === 'User_Image') {
                // 如果您希望上傳圖片後，也要靠 SignalR 顯示圖片，可以在這裡處理
                // 但目前的邏輯是前端自己先顯示 previewImage，所以這裡可以忽略
                return; 
            }
        });

        newConnection.on("UpdateModeState", (sid, isHuman) => {
            if (sid == currentSessionId.value) {
                isHumanMode.value = isHuman;
            }
        });

        console.log("正在連接 SignalR...");
        await newConnection.start();
        console.log("SignalR 連線成功");

        connectionId.value = newConnection.connectionId;
        connection.value = newConnection;

        const { sessionId } = await ensureSession();
        if (sessionId > 0) {
            await newConnection.invoke("JoinSessionGroup", sessionId.toString());
        }

    } catch (err) {
        console.error("SignalR 連線錯誤", err);
        connection.value = null; 
        setTimeout(initSignalR, 5000);
    } finally {
        isConnecting.value = false;
    }
};

const checkLoginAndInit = async () => {
    const currentPath = route.path.toLowerCase();

    // 1. 如果在黑名單頁面 (登入/註冊頁)
    if (excludedPaths.includes(currentPath)) {
        isLoggedIn.value = false;
        
        // ★★★ 補上這三行，徹底清空前一個帳號的記憶 ★★★
        currentSessionId.value = 0; 
        chatStore.setSessionId(0);
        chatStore.clearMessages(); 
        
        if (connection.value) {
            connection.value.stop();
            connection.value = null;
        }
        return;
    }
    
    if (isLoggedIn.value && connection.value) {
        return;
    }

    try {
        const res = await axios.get(`${API_BASE_URL}/api/ChatMessages/CheckLogin`, {
            withCredentials: true 
        });
        
        if (res.data.isLoggedIn) {
            isLoggedIn.value = true;
            if (!connection.value) {
                await loadHistory(); 
                await initSignalR();
            }
        } else {
            // 2. 如果 API 回傳未登入 (例如 Cookie 過期)
            isLoggedIn.value = false;
            
            // 這裡你原本就有寫，維持不變
            currentSessionId.value = 0;
            chatStore.setSessionId(0);
            chatStore.clearMessages(); // ★ 建議多加這行清空畫面
            
            if (connection.value) {
                connection.value.stop();
                connection.value = null;
            }
        }
    } catch (error) {
        console.error("檢查登入失敗:", error);
        isLoggedIn.value = false;
    }
};

// ★★★ 關鍵修改：sendMessage 改為「射後不理」，不等待 response ★★★
const sendMessage = async (text) => {
    if (isSending.value) return;

    // 1. 檢查連線
    if (!connection.value || connection.value.state !== signalR.HubConnectionState.Connected) {
        await initSignalR();
        if (!connectionId.value) {
            alert("連線中斷，請稍後再試。");
            return;
        }
    }

    if (!currentSessionId.value || currentSessionId.value === 0) {
        // ... (建立 Session 邏輯保持不變) ...
        console.log("偵測到新對話，正在建立 Session...");
        try {
            const { sessionId } = await ensureSession(); 
            if (sessionId > 0 && connection.value) {
                await connection.value.invoke("JoinSessionGroup", sessionId.toString());
            }
        } catch (e) {
            console.error("建立新對話失敗", e);
            return;
        }
    }

    // UI 顯示
    // 為了讓使用者感覺「秒發」，我們在前端先顯示這則訊息
    // (注意：因為後端也會廣播 'User' 訊息，如果您發現訊息出現兩次，請把 SignalR 裡的 if (type === 'User') return; 打開)
    chatStore.addMessage({ role: 'user', text: text, time: new Date() });
    
    userInput.value = '';
    isSending.value = true; // 開啟 Loading

    try {
        const sid = currentSessionId.value;
        const payload = { 
            sessionId: sid, 
            userContent: text, 
            connectionId: connectionId.value 
        };
        
        // ★ 這裡只管發送，不需要拿 res.data 來顯示訊息
        // ★ 因為顯示訊息的工作已經全權交給 SignalR 了
        await axios.post(`${API_BASE_URL}/api/ChatMessages/SendMessage`, payload, { withCredentials: true });
        
        // 注意：isSending.value = false 不能寫在這裡！
        // 因為 API 回傳只代表「伺服器收到了」，不代表「AI 想好了」
        // 我們要在 SignalR 收到 'AI' 訊息時才把 isSending 關掉 (看 initSignalR 裡面的修改)

    } catch (error) {
        isSending.value = false; // 失敗才手動關閉
        chatStore.addMessage({ role: 'ai', text: `<span style="color:red">發送失敗。</span>`, isSystem: true, time: new Date() });
    }
};

const sendImageWithText = async (file, text) => {
    if (!connection.value) await initSignalR();

    if (!currentSessionId.value || currentSessionId.value === 0) {
        try {
            const { sessionId } = await ensureSession();
            if (sessionId > 0 && connection.value) {
                await connection.value.invoke("JoinSessionGroup", sessionId.toString());
            }
        } catch (e) { return; }
    }
    
    const sid = currentSessionId.value;

    chatStore.addMessage({ role: 'user', text: text, imageUrl: previewImage.value, time: new Date() });
    
    const formData = new FormData();
    formData.append("SessionId", sid);
    formData.append("Image", file);
    formData.append("ConnectionId", connectionId.value);
    formData.append("UserContent", text);
    
    removePreview();
    userInput.value = '';
    isSending.value = true;

    try {
        // 同樣改為「射後不理」
        await axios.post(`${API_BASE_URL}/api/ChatMessages/UploadImage`, formData, { 
            headers: { 'Content-Type': 'multipart/form-data' },
            withCredentials: true 
        });
        
    } catch (error) { 
        isSending.value = false; 
        chatStore.addMessage({ role: 'ai', text: `<span style="color:red">圖片上傳失敗。</span>`, isSystem: true, time: new Date() });
    }
};

const triggerTransfer = async () => {
    if (isSending.value || isHumanMode.value) return;

    try {
        isSending.value = true;
        const sid = currentSessionId.value;
        
        if (!connectionId.value) {
             await initSignalR();
        }

        const payload = { 
            sessionId: sid, 
            ishumanmode: true, 
            connectionId: connectionId.value 
        };
        
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/SwitchMode`, payload, { withCredentials: true });
        
        if (res.data.sessionId && res.data.sessionId !== currentSessionId.value) {
             currentSessionId.value = res.data.sessionId;
             chatStore.setSessionId(res.data.sessionId);
        }


    } catch (error) { 
        console.error("切換失敗", error);
        alert("呼叫客服失敗，請檢查網路連線");
    } finally {
        isSending.value = false; 
    }
};

const endChat = async () => {
    if (!isHumanMode.value) return;

    isSending.value = true;

    try {
        const sid = currentSessionId.value;
        const payload = { sessionId: sid, ishumanmode: false, connectionId: connectionId.value };
        
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/SwitchMode`, payload, { withCredentials: true });
        
        if (res.data.success) {
            isHumanMode.value = false;
            if (res.data.sessionId && res.data.sessionId !== currentSessionId.value) {
                currentSessionId.value = res.data.sessionId;
                chatStore.setSessionId(res.data.sessionId);
            }
        }

    } catch (error) { 
        console.error("結束對話發生錯誤:", error);
    } finally {
        isSending.value = false;
    }
};

// 監聽與生命週期
const handleSend = async () => {
    if (selectedFile.value) { await sendImageWithText(selectedFile.value, userInput.value); }
    else if (userInput.value.trim()) { await sendMessage(userInput.value); }
    nextTick(() => {
        if (textInput.value) {
            textInput.value.focus();
        }
    });
};

const triggerFileUpload = () => fileInput.value.click();
const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    selectedFile.value = file;
    previewImage.value = URL.createObjectURL(file);
    event.target.value = '';
};

watch(() => route.path, async () => {
    await checkLoginAndInit();
});

watch(() => chatStore.messages.length, () => scrollToBottom());

onMounted(async () => {
    await checkLoginAndInit();
});

onUnmounted(() => {
    if (connection.value) {
        connection.value.stop();
        connection.value = null;
    }
});
</script>

<template>
  <div v-if="isLoggedIn" class="chat-widget-wrapper">
    
    <button 
      v-if="!isOpen" 
      class="side-hidden-btn" 
      @click="toggleChat"
      title="打開 AI 助手"
    >
      <img :src="aiAvatarGif" class="side-btn-gif" alt="AI" />
      <span class="btn-text">AI助手</span>
    </button>

    <transition name="popup-anim">
      <div v-if="isOpen" class="custom-chat-window">
        
        <div class="chat-header" :class="{ 'human-mode': isHumanMode }">
          <div class="header-title">
            {{ isHumanMode ? '人工客服' : 'AI助手' }}
          </div>
          <button class="header-close-btn" @click="isOpen = false">×</button>
        </div>

        <div class="messages-area" ref="messagesContainer">
          <div v-if="chatStore.messages.length === 0" class="message-row ai">
             <div class="avatar-col">
               <div class="avatar">🤖</div>
             </div>
             <div class="bubble-content">
               <div class="bubble">你好！我是AI助手。你可以問我問題，或傳圖片給我辨識喔！</div>
             </div>
          </div>
          
          <div v-for="(msg, index) in chatStore.messages" :key="index" class="message-row" :class="msg.role === 'user' ? 'user' : 'ai'">
            <div v-if="msg.role === 'ai'" class="avatar-col">
              <span class="avatar-name">{{ msg.isHuman ? '客服' : 'AI' }}</span>
              <div class="avatar">{{ msg.isHuman ? '👨‍💼' : '🤖' }}</div>
            </div>

            <div class="bubble-content">
              <div class="bubble" :class="{ 'system-msg': msg.isSystem }">
                <img v-if="msg.imageUrl" :src="msg.imageUrl" class="msg-image" @load="scrollToBottom" />
                <span v-if="msg.text" class="msg-text markdown-body" v-html="msg.text"></span>
              </div>
              <div class="msg-time">{{ formatTime(msg.time) }}</div>
            </div>
          </div>

          <div v-if="isSending" class="message-row ai">
            <div class="avatar-col">
              <div class="avatar">{{ isHumanMode ? '👨‍💼' : '🤖' }}</div>
            </div>
            <div class="bubble typing">● ● ●</div>
          </div>
        </div>

        <div class="input-area">
          <div class="action-toolbar">
          <button v-if="!isHumanMode" class="chip-btn" @click="triggerTransfer" :disabled="isSending">呼叫客服</button>
          <button v-else class="chip-btn danger" @click="endChat">結束對話</button>
        </div>
          
          <div class="input-row-container">
            <div v-if="previewImage" class="preview-box">
              <img :src="previewImage" />
              <button class="preview-close" @click="removePreview">×</button>
            </div>
            
            <div class="input-row">
              <input type="file" ref="fileInput" @change="handleFileChange" accept="image/*" style="display: none" />
              
              <div class="input-field-wrapper">
                <input 
                  v-model="userInput" 
                  @keyup.enter="handleSend" 
                  :placeholder="isHumanMode ? '傳送給客服人員...' : '輸入訊息...'" 
                  type="text" 
                  ref="textInput"
                />
                <button class="img-upload-icon" @click="triggerFileUpload" type="button" title="上傳圖片辨識">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>
                  </svg>
                </button>
              </div>

              <button class="submit-send-btn" @click="handleSend" :disabled="!userInput.trim() && !selectedFile">
                <svg viewBox="0 0 24 24" fill="currentColor">
                  <path d="m21.426 11.095-17-8A.999.999 0 0 0 3.03 4.242L4.969 12 3.03 19.758a.998.998 0 0 0 1.396 1.147l17-8a1 1 0 0 0 0-1.81z"/>
                </svg>
              </button>
            </div>
          </div>
        </div>

      </div>
    </transition>
  </div>
</template>

<style scoped>
/* 側邊掛式圓形按鈕樣式 */
.side-hidden-btn {
  position: fixed;
  top: 90%;
  right: 0;
  transform: translateY(-50%) translateX(68px); 
  background: linear-gradient(135deg, #4398a5 0%, #3a5f54 100%);
  color: white;
  border: none;
  width: 120px; 
  height: 40px;
  border-radius: 30px 0 0 30px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: flex-start; /* 靠左 */
  padding-left: 10px; /* 左邊留白 */
  gap: 4px; /* 圖片跟字的距離 */
  box-shadow: -4px 0 15px rgba(67, 152, 165, 0.4);
  z-index: 9998;
  transition: transform 0.3s ease-out, box-shadow 0.3s ease;
}

/* 懸停效果 */
.side-hidden-btn:hover {
  transform: translateY(-50%) translateX(0); /* 滑鼠移上去完全滑出 */
  box-shadow: -6px 0 20px rgba(67, 152, 165, 0.6);
}
.side-btn-gif {
  width: 33px;
  height: 33px;
  border-radius: 50%;
  background: transparent;
  object-fit: cover;
  transform: none; 
  z-index: 1;
}
.btn-text {
  display: block; 
  font-size: 16px; 
  font-weight: bold;
  color: white;
  white-space: nowrap; 
  margin-left: 6px;
}

/* 視窗主體 */
.custom-chat-window {
    position: fixed; bottom: 15px; right: 15px;
    width: 380px; height: 550px; /* 固定寬高避免擠壓 */
    background: linear-gradient(90deg, rgb(239, 242, 247) 0%, rgb(235, 239, 248) 50%, rgb(208, 224, 247) 100%) !important;
    box-shadow: 0 12px 30px rgba(0,0,0,0.15);
    border-radius: 20px;
    display: flex; flex-direction: column; z-index: 9999; overflow: hidden;
}

.chat-header {
    background: linear-gradient(135deg, #4398a5, #3a5f54);
    color: white; padding: 12px 15px; display: flex; justify-content: space-between; align-items: center;
}
.chat-header.human-mode { background: linear-gradient(135deg, #e67e22, #d35400); }
.header-close-btn { background: none; border: none; color: white; font-size: 20px; cursor: pointer; }

/* 訊息區 */
.messages-area { flex: 1; padding: 15px; overflow-y: auto; background: transparent; }
.message-row { display: flex; gap: 8px; margin-bottom: 12px; }
.message-row.user { flex-direction: row-reverse; }
.bubble { 
    width: fit-content;
    padding: 10px; 
    border-radius: 12px; 
    max-width: 85%; 
    font-size: 15px; 
    line-height: 1.2;
    /* 強制垂直排列 */
    display: flex;
    flex-direction: column;
    gap: 5px; /* 圖片和文字之間的間距 */
    /* 確保長文字會自動換行，不會撐爆 */
    word-wrap: break-word;
    word-break: break-word;
}
:deep(.ai .bubble .markdown-body),
:deep(.ai .bubble p),
:deep(.ai .bubble ul),
:deep(.ai .bubble ol),
:deep(.ai .bubble li) {
    font-size: 15px !important;
    line-height: 1.2 !important;
    margin: 0 !important; 
    padding-bottom: 0 !important;
}
:deep(.markdown-body img) {
    max-width: 100%;       /* 寬度不超過氣泡範圍 */
    height: auto;          /* 高度自動維持比例 */
    border-radius: 8px;    /* 圓角好看一點 */
    margin-top: 10px;      /* 圖片跟文字分開一點 */
    box-shadow: 0 2px 8px rgba(0,0,0,0.1); /* 微微陰影 */
    display: block;        /* 讓圖片自己佔一行 */
}

/* 確保列表有正確縮排，但沒有多餘垂直間距 */
:deep(.ai .bubble ul),
:deep(.ai .bubble ol) {
    padding-left: 20px !important;
}
.ai .bubble { background: white; border-radius: 12px; }
.user .bubble { background: #3578F6; color: white; }
.msg-time { font-size: 10px; color: #999; margin-top: 4px; }
.msg-image { 
    max-width: 100%; 
    height: auto; 
    border-radius: 4px; 
    display: block; 
}
.msg-text {
    display: block;
    word-wrap: break-word; /* 防止長文字撐爆 */
}
.message-row.user .bubble-content {
    /*讓氣泡和時間垂直排列時，靠右對齊*/
    align-items: flex-end; 
    display: flex;
    flex-direction: column;
}

.message-row.user .msg-time {
    /*讓時間文字靠右*/
    text-align: right;
}

/* 確保 AI 的內容靠左*/
.message-row:not(.user) .bubble-content {
    align-items: flex-start;
    display: flex;
    flex-direction: column;
}

/* 輸入區修復關鍵 */
.input-area {
     background: rgba(255,255,255,0.4); border-top: 1px solid rgba(0,0,0,0.05); padding: 12px 10px 15px; /* 底部加深 padding 避免按鈕靠太近 */
}
.action-toolbar { display: flex; gap: 10px; margin-bottom: 10px; }
.chip-btn { padding: 4px 12px; border-radius: 20px; border: 1px solid #ddd; background: #fff; font-size: 12px; cursor: pointer; }
.chip-btn:hover {  border-color:#888; background: #fff5f5; }
.chip-btn.danger { color: #d9534f !important; border-color: #d9534f !important; }
.chip-btn.danger:hover {
    background: #fff5f5 !important; 
    border-color: #c9302c !important; 
    box-shadow: 0 2px 4px rgba(217, 83, 79, 0.1) !important; 
}

.input-row-container { position: relative; }
.input-row { display: flex; align-items: center; gap: 10px; }

.input-field-wrapper {
    position: relative; flex: 1; display: flex; align-items: center;
}
.input-field-wrapper input {
    width: 100%; padding: 10px 40px 6px 15px; border-radius: 20px;
    border: 1px solid #ddd; outline: none; background: #f9f9f9;
}
.img-upload-icon {
    position: absolute; right: 10px; color: #3578F6; background:transparent; border: none; cursor: pointer;
}
.image-btn:hover { color: #3578F6 !important; background: #eee !important; }
:deep(.img-upload-icon:hover) {
    color: #3578F6 !important;
    background-color: rgba(53, 120, 246, 0.1) !important;
    opacity: 1 !important;
    border-radius: 30% !important;
}

.submit-send-btn {
    width: 40px; height: 40px; border-radius: 50%; background: #3578F6;
    border: none; color: white; display: flex; align-items: center; justify-content: center; cursor: pointer;
}
.submit-send-btn:disabled { background: linear-gradient(135deg, #3578F6, #205AE6); }
.submit-send-btn svg { width: 18px; height: 18px; }

.preview-box {
    position: absolute; bottom: 50px; left: 0; background: #fff;
    padding: 5px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.1);
}
.preview-box img { height: 60px; width: auto; border-radius: 8px; display: block; }
.preview-close { position: absolute; top: -5px; right: -5px; background: red; color: #fff; border: none; border-radius: 50%; width: 18px; height: 18px; cursor: pointer; font-size: 12px; }

/* 側邊滾動條 */
.messages-area::-webkit-scrollbar { width: 5px; }
.messages-area::-webkit-scrollbar-track { background: linear-gradient(90deg, rgb(239, 242, 247) 0%, rgb(235, 239, 248) 50%, rgb(208, 224, 247) 100%) !important; }
.messages-area::-webkit-scrollbar-thumb { background-color: rgba(0, 0, 0, 0.1); border-radius: 10px; }
/* 動畫 */
.popup-anim-enter-active { transition: all 0.4s cubic-bezier(0.68, -0.55, 0.27, 1.55); }
.popup-anim-enter-from { transform: translateY(100px) scale(0.8); opacity: 0; }

/* 頭像樣式 */
.avatar-col { display: flex; flex-direction: column; align-items: center; gap: 4px; margin-right: 2px; }
.avatar-name { font-size: 10px; color: #888; line-height: 1; white-space: nowrap; }
.message-row.ai .avatar { 
    width: 32px; 
    height: 32px; 
    background: #90afe4; 
    border-radius: 50%; 
    display: flex; 
    align-items: center; 
    justify-content: center; 
    font-size: 18px; 
    flex-shrink: 0; 
}
</style>