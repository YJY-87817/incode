<script setup>
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute } from 'vue-router';
import axios from 'axios';
import * as signalR from "@microsoft/signalr";
import { useChatStore } from '@/stores/chatmessagestores';
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

// ★ 新增：在記憶體中暫存 Session ID，不再存 LocalStorage
const currentSessionId = ref(0);

const API_BASE_URL = "https://localhost:7190";


// 設定「不顯示按鈕」的頁面黑名單
const excludedPaths = [
    '/login', 
    '/register', 
    '/identity/account/login', 
    '/identity/account/register'
];

// ==========================================
// ★ 2. 核心：檢查登入 & 初始化
// ==========================================
const checkLoginAndInit = async () => {
    const currentPath = route.path.toLowerCase();

    // 如果在登入/註冊頁，隱藏聊天室並斷線
    if (excludedPaths.includes(currentPath)) {
        isLoggedIn.value = false;
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
            // 登入成功後，若尚未連線，則載入歷史並連線
            if (!connection.value) {
                // 先載入歷史 (內部會呼叫 GetOrCreateSession 取得 ID)
                await loadHistory(); 
                // 再啟動 SignalR
                await initSignalR();
            }
        } else {
            isLoggedIn.value = false;
            currentSessionId.value = 0;
            chatStore.setSessionId(0);
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

// ==========================================
// ★ 3. 監聽路由變化
// ==========================================
watch(
  () => route.path,
  async () => {
    await checkLoginAndInit();
  }
);

const formatTime = (dateInput) => {
    const date = dateInput ? new Date(dateInput) : new Date();
    return date.toLocaleTimeString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit',  hour: '2-digit', minute: '2-digit', hour1: false });
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

watch(() => chatStore.messages.length, () => scrollToBottom());

// ==========================================
// ★ 4. 關鍵修改：取得或建立 Session (API)
// ==========================================
const ensureSession = async () => {
    // 如果記憶體已經有 ID，直接回傳 (避免重複呼叫 API)
    if (currentSessionId.value > 0) {
        return { sessionId: currentSessionId.value, isNew: false };
    }

    try {
        // 呼叫後端新的 API: GetOrCreateSession
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/GetOrCreateSession`, {}, { withCredentials: true });
        const sid = res.data.sessionId;
        const isNew = res.data.isNew;

        // 更新狀態
        currentSessionId.value = sid;
        chatStore.setSessionId(sid);
        
        return { sessionId: sid, isNew: isNew };

    } catch (error) {
        console.error("取得對話 Session 失敗:", error);
        return { sessionId: 0, isNew: true };
    }
};

const endChat = async () => {
    console.log("1. [Debug] 點擊結束對話按鈕");
    
    // 雖然理論上要有 HumanMode 才能點，但為了測試先註解掉或是單純 Log
    if (!isHumanMode.value) {
        console.log("2. [Debug] 目前判定不是真人模式，取消執行");
        return;
    }

    isSending.value = true;

    try {
        const sid = currentSessionId.value;
        console.log("3. [Debug] 目前 Session ID 為:", sid);

        // ★★★ 關鍵修改：移除了 if (!sid) return ★★★
        // 就算 sid 是 0，也要發送給後端，讓後端去資料庫找正確的 Session
        
        const payload = { sessionId: sid, ishumanmode: false, connectionId: connectionId.value };
        
        console.log("4. [Debug] 呼叫 API payload:", payload);

        // 呼叫 API
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/SwitchMode`, payload, { withCredentials: true });
        
        console.log("5. [Debug] API 回傳成功:", res.data);

        if (res.data.success) {
            isHumanMode.value = false; // 切換回 AI
            
            // 如果後端回傳了正確的 ID (例如從 0 變成了 55)，更新它
            if (res.data.sessionId && res.data.sessionId !== currentSessionId.value) {
                console.log("6. [Debug] 更新正確 ID:", res.data.sessionId);
                currentSessionId.value = res.data.sessionId;
                chatStore.setSessionId(res.data.sessionId);
            }
        }

    } catch (error) { 
        console.error("7. [Error] 結束對話發生錯誤:", error);
    } finally {
        isSending.value = false;
    }
};

// ==========================================
// ★ 5. 關鍵修改：載入歷史
// ==========================================
const loadHistory = async () => {
    // 1. 先確保取得 Session ID
    const { sessionId, isNew } = await ensureSession();

    if (!sessionId || sessionId === 0) return;

    // 2. 如果是新建立的對話 (isNew = true)，清空 Store，顯示預設歡迎詞
    if (isNew) {
        chatStore.clearMessages();
        return;
    }

    // 3. 如果是舊對話，呼叫 API 撈取歷史紀錄
    try {
        const res = await axios.get(`${API_BASE_URL}/api/ChatMessages/GetHistory/${sessionId}`, { withCredentials: true });
        const data = res.data;
        
        chatStore.clearMessages(); // 先清空，避免重複
        
        if (Array.isArray(data)) {
            data.forEach(msg => {
                const isUser = msg.role === 'user';
                let content = msg.body;
                let imgUrl = null;
                
                // 處理圖片標籤 (簡單解析)
                if (content && content.includes('<img')) {
                    const match = content.match(/src='([^']*)'/);
                    if (match) imgUrl = getFullImageUrl(match[1]);
                    content = ''; // 若是圖片，文字設為空
                } else if (!isUser) {
                    // 若是 AI 文字，解析 Markdown
                    content = renderMarkdown(content);
                }

                chatStore.addMessage({
                    role: isUser ? 'user' : 'ai',
                    text: content,
                    imageUrl: imgUrl,
                    time: new Date(msg.createdAt),
                    // 如果 role 是 assistant 且包含特定標記或後端有欄位，可設為 true (此處為簡易判斷)
                    isHuman: msg.role === 'assistant' && (msg.body.includes("[真人]") || isHumanMode.value) 
                });
            });
        }
    } catch (error) {
        console.error("載入歷史失敗:", error);
    }
};


// SignalR 初始化
const initSignalR = async () => {
    if (connection.value) return;

    if (isConnecting.value) return;

    isConnecting.value = true;
    try {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/chatHub`)
            .withAutomaticReconnect()
            .build();

        // --- 監聽訊息 ---
        newConnection.on("ReceiveMessage", (type, message) => {
            if (type === 'User') {
                return;
            }
            // 1. 系統訊息
            if (type === 'System') {
                // 如果系統通知切換模式
                if (message.includes("人工客服")) isHumanMode.value = true;
                if (message.includes("AI助手")) isHumanMode.value = false;
                
                // 顯示系統小字
                chatStore.addMessage({ role: 'ai', text: `<i>${message}</i>`, isSystem: true, time: new Date() });
                return;
            }
            
            // 2. 真人客服訊息
            if (type === 'Live') {
                chatStore.addMessage({ role: 'ai', text: message, imageUrl: null, isHuman: true, time: new Date() });
                isHumanMode.value = true;
                isSending.value = false;
                return;
            }

            // 3. 一般 AI 或 圖片
            let imgUrl = null;
            let textContent = message;
            let isMessageHuman = false; // 預設 AI

            if (type === 'AI_Image' || type === 'User_Image') {
                if (message.startsWith('<img') || message.includes('src=')) {
                     const match = message.match(/src='([^']*)'/);
                     if(match) imgUrl = getFullImageUrl(match[1]);
                } else {
                     imgUrl = getFullImageUrl(message);
                }
                textContent = '';
                // 自己的圖片在發送時已經 addMessage 過了，這裡如果是 User_Image 不重複顯示
                if (type === 'User_Image') return; 
            } else {
                textContent = renderMarkdown(message);
            }
            
            chatStore.addMessage({ role: 'ai', text: textContent, imageUrl: imgUrl, isHuman: isMessageHuman, time: new Date() });
            isSending.value = false;
        });

        newConnection.on("UpdateModeState", (sid, isHuman) => {
            console.log(`[SignalR] 前台收到狀態變更 -> SessionID: ${sid}, IsHuman: ${isHuman}`);
            
            // 確保只更新當前使用者的 Session 狀態
            // 注意：後端傳來的 ID 是 int，前端的 currentSessionId.value 也是 int
            if (sid == currentSessionId.value) {
                isHumanMode.value = isHuman;
                console.log(`[SignalR] UI 模式已更新為: ${isHuman ? '真人' : 'AI'}`);
            }
        });

        await newConnection.start();
        console.log("SignalR 連線成功"); // 建議加這行 Log 觀察是否只出現一次

        connectionId.value = newConnection.connectionId;
        connection.value = newConnection;

        // 加入群組
        const { sessionId } = await ensureSession();
        if (sessionId > 0) {
            await newConnection.invoke("JoinSessionGroup", sessionId.toString());
        }

    } catch (err) {
        console.error("SignalR 連線錯誤", err);
        // 失敗時，清空 connection 讓下次重試可以執行
        connection.value = null; 
        setTimeout(initSignalR, 5000);
    } finally {
        isConnecting.value = false;
    }
};

// --- 操作邏輯 ---

const triggerTransfer = async () => {
    if (isSending.value || isHumanMode.value) return;
    try {
        isSending.value = true;
        const sid = currentSessionId.value; 
        

        const payload = { sessionId: sid, ishumanmode: true, connectionId: connectionId.value };
        
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/SwitchMode`, payload, { withCredentials: true });
        
        // ★ 關鍵：更新 Session ID
        if (res.data.sessionId && res.data.sessionId !== currentSessionId.value) {
             console.log("切換客服後取得 Session ID:", res.data.sessionId);
             currentSessionId.value = res.data.sessionId;
             chatStore.setSessionId(res.data.sessionId);
        }

    } catch (error) { isSending.value = false; }
};

const triggerFileUpload = () => fileInput.value.click();
const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    selectedFile.value = file;
    previewImage.value = URL.createObjectURL(file);
    event.target.value = '';
};
const removePreview = () => { selectedFile.value = null; previewImage.value = null; };
const handleSend = async () => {
    if (selectedFile.value) { await sendImageWithText(selectedFile.value, userInput.value); }
    else if (userInput.value.trim()) { await sendMessage(userInput.value); }
// 強制將游標移動到文字輸入框
nextTick(() => {
        if (textInput.value) {
            textInput.value.focus();
        }
    });
};

const sendImageWithText = async (file, text) => {
    if (!connection.value) await initSignalR();
    
    // 如果是第一次使用，這裡是 0，後端會自動處理
    const sid = currentSessionId.value;

    chatStore.addMessage({ role: 'user', text: text, imageUrl: previewImage.value, time: new Date() });
    
    const formData = new FormData();
    formData.append("SessionId", sid);
    formData.append("Image", file);
    formData.append("ConnectionId", connectionId.value);
    // ★ 記得補上 UserContent，不然圖片描述會是空的
    formData.append("UserContent", text);
    
    removePreview();
    userInput.value = '';
    isSending.value = true;

    try {
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/UploadImage`, formData, { 
            headers: { 'Content-Type': 'multipart/form-data' },
            withCredentials: true 
        });

        // ★ 關鍵：如果後端幫我們建立了新房間 (或找到了舊房間)，更新前端的 ID
        if (res.data.sessionId && res.data.sessionId !== currentSessionId.value) {
            console.log("圖片上傳後取得 Session ID:", res.data.sessionId);
            currentSessionId.value = res.data.sessionId;
            chatStore.setSessionId(res.data.sessionId);
        }

    } catch (error) { 
        isSending.value = false; 
    }
};

const sendMessage = async (text) => {
    if (isSending.value) return;
    if (!connection.value) await initSignalR();
    
    // 立即顯示使用者訊息
    chatStore.addMessage({ role: 'user', text: text, time: new Date() });
    userInput.value = '';
    isSending.value = true;
    
    try {
        const sid = currentSessionId.value;
        const payload = { sessionId: sid, userContent: text, connectionId: connectionId.value };
        
        // 呼叫 API
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/SendMessage`, payload, { withCredentials: true });
        
        // ★ 關鍵：更新 Session ID
        // 如果原本是 0 (新使用者)，後端會回傳新 ID，這裡接住它，
        // 這樣下一則訊息就會帶上正確的 ID，接續同一個房間
        if (res.data.sessionId && res.data.sessionId !== currentSessionId.value) {
            console.log("文字對話後取得 Session ID:", res.data.sessionId);
            currentSessionId.value = res.data.sessionId;
            chatStore.setSessionId(res.data.sessionId);
        }

    } catch (error) { 
        isSending.value = false;
        chatStore.addMessage({ role: 'ai', text: `<span style="color:red">發送失敗，請檢查網路連線。</span>`, isSystem: true, time: new Date() });
    }
};

onMounted(async () => {
    await checkLoginAndInit();
});

onUnmounted(() => {
    if (connection.value) {
        connection.value.stop();
        connection.value = null; // ★ 關鍵：一定要設為 null
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
               <div class="bubble">你好！我是電影 AI 助手。你可以問我電影問題，或傳海報給我辨識喔！</div>
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
            <button v-else class="chip-btn danger" @click="endChat" :disabled="isSending">結束對話</button>
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
    position: fixed; bottom: 15px; right: 20px;
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