<script setup>
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import axios from 'axios';
import * as signalR from "@microsoft/signalr";
import { useChatStore } from '@/stores/chatmessagestores';
import { marked } from 'marked';
import aiAvatarGif from '@/assets/gif/1.gif'; // 請確認路徑

const chatStore = useChatStore();
const isOpen = ref(false);
const userInput = ref('');
const messagesContainer = ref(null);
const fileInput = ref(null);
const isSending = ref(false);
const connection = ref(null);
const connectionId = ref('');
const isConnecting = ref(false);

// ★ 核心狀態：是否為真人模式
const isHumanMode = ref(false);

// 預覽與檔案狀態
const previewImage = ref(null);
const selectedFile = ref(null);

// ★ 後端 API 網址
const API_BASE_URL = "https://localhost:7190";

// 時間格式化
const formatTime = (dateInput) => {
    const date = dateInput ? new Date(dateInput) : new Date();
    return date.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false });
};

// Markdown 解析
const renderMarkdown = (text) => {
    if (!text) return '';
    return marked.parse(text);
};

// 處理圖片完整路徑
const getFullImageUrl = (url) => {
    if (!url) return '';
    if (url.startsWith('http') || url.startsWith('data:')) return url;
    return `${API_BASE_URL}${url}`;
};

// 切換視窗
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


const ensureSession = async () => {
    let sid = localStorage.getItem('chatSessionId');

    // ★★★ 修改重點：增加檢查 '0', 'undefined', 'null' ★★★
    // 如果偵測到 ID 無效，強制重新建立
    if (!sid || sid === '0' || sid === 'undefined' || sid === 'null') {
        console.log("偵測到無效 Session ID (0 或空)，正在向後端申請新 ID...");
        try {
            const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/CreateSession`, {
            userId: 1
        });
            sid = res.data.sessionId; // 取得新 ID (例如 15)
            
            if (!sid) throw new Error("後端回傳 SessionId 為空");

            console.log(`建立新 Session 成功: ${sid}`);
            localStorage.setItem('chatSessionId', sid);
            chatStore.setSessionId(sid);

        } catch (error) {
            console.error("建立對話失敗:", error);
            // 如果連建立都失敗，回傳 0 讓後續邏輯擋下
            return 0;
        }
    } else {
        // 如果 ID 有效，直接存入 Store
        chatStore.setSessionId(parseInt(sid));
    }

    const finalId = parseInt(sid);

    // 補救措施：如果 SignalR 已連線，確保加入正確的群組
    if (connection.value?.state === signalR.HubConnectionState.Connected && finalId > 0) {
        connection.value.invoke("JoinSessionGroup", finalId.toString())
            .catch(err => console.error("加入群組失敗(補):", err));
    }

    return finalId;
};

// ==========================================
// ★ 1. 載入歷史紀錄
// ==========================================
const loadHistory = async () => {
    const sid = await ensureSession();
    if (!sid || sid === 0) return; // 防呆

    try {
        const res = await axios.get(`${API_BASE_URL}/api/ChatMessages/GetHistory/${sid}`);
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
                    isHuman: msg.isHuman || false
                });
            });
        }
    } catch (error) {
        console.error("載入歷史紀錄失敗:", error);
    }
};

// ==========================================
// ★ 2. SignalR 初始化
// ==========================================
const initSignalR = async () => {
    if (connection.value?.state === signalR.HubConnectionState.Connected) return;
    isConnecting.value = true;

    try {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/chatHub`)
            .withAutomaticReconnect()
            .build();

        newConnection.on("ReceiveMessage", (type, message) => {
            console.log(`[SignalR] ${type}:`, message);

            // 1. 系統通知
            if (type === 'System') {
                chatStore.addMessage({
                    role: 'ai',
                    text: `<i>${message}</i>`,
                    isSystem: true,
                    time: new Date()
                });
                if (message.includes("真人")) isHumanMode.value = true;
                if (message.includes("AI")) isHumanMode.value = false;
                return;
            }

            // 2. 真人客服回覆
            if (type === 'Live') {
                chatStore.addMessage({
                    role: 'ai',
                    text: message,
                    imageUrl: null,
                    isHuman: true,
                    time: new Date()
                });
                isHumanMode.value = true;
                isSending.value = false;
                return;
            }

            // 3. AI 回覆
            let imgUrl = null;
            let textContent = message;
            let isMessageHuman = false;

            if (type === 'AI_Image' || type === 'User_Image') {
                if (message.startsWith('<img') || message.includes('src=')) {
                     const match = message.match(/src='([^']*)'/);
                     if(match) imgUrl = getFullImageUrl(match[1]);
                } else {
                     imgUrl = getFullImageUrl(message);
                }
                textContent = '';
                if (type === 'User_Image') return;
            } else {
                textContent = renderMarkdown(message);
                isMessageHuman = false;
            }

            chatStore.addMessage({
                role: 'ai',
                text: textContent,
                imageUrl: imgUrl,
                isHuman: isMessageHuman,
                time: new Date()
            });

            isSending.value = false;
        });

        await newConnection.start();
        console.log("SignalR Connected ID:", newConnection.connectionId);
        
        connectionId.value = newConnection.connectionId;
        connection.value = newConnection;

        // 加入群組 (確保使用 store 裡的正確 ID)
        if (chatStore.sessionId && chatStore.sessionId > 0) {
            await newConnection.invoke("JoinSessionGroup", chatStore.sessionId.toString());
        }

    } catch (err) {
        console.error("SignalR 連線失敗:", err);
        setTimeout(initSignalR, 5000);
    } finally {
        isConnecting.value = false;
    }
};

const triggerTransfer = async () => {
    if (isSending.value || isHumanMode.value) return;

    try {
        isSending.value = true;
        
        // 1. 先嘗試取得目前的 Session (這步通常會拿到舊的或是重新申請的)
        const sid = await ensureSession();
        
        // 防呆
        if (!sid || sid <= 0) {
            chatStore.addMessage({ role: 'ai', text: "⚠️ 連線異常，請重新整理網頁。", time: new Date() });
            isSending.value = false;
            return;
        }

        // 2. 準備 Payload
        const payload = {
            sessionId: sid,
            ishumanmode: true,
            connectionId: connectionId.value
        };
        
        // 3. 發送請求，並接收回傳結果
        const res = await axios.post(`${API_BASE_URL}/api/ChatMessages/SwitchMode`, payload);
        
        // ★★★ 新增這段：檢查後端是否幫我們建立了全新的 ID ★★★
        if (res.data && res.data.sessionId) {
            // 如果後端回傳的 ID 跟我們原本的不一樣 (或是單純為了確保同步)
            // 更新本地 LocalStorage 和 Store
            console.log("後端自動建立了新 Session (同步更新):", res.data.sessionId);
            localStorage.setItem('chatSessionId', res.data.sessionId);
            chatStore.setSessionId(res.data.sessionId);
        }
        
    } catch (error) {
        console.error("轉接失敗:", error);
        chatStore.addMessage({ role: 'ai', text: "轉接失敗，請稍後再試。", time: new Date() });
        isSending.value = false;
    }
};

// ==========================================
// ★ 4. 結束真人對話 (User -> AI)
// ==========================================
const endChat = async () => {
    if (!isHumanMode.value) return;

    try {
        const sid = await ensureSession();
        // 防呆
        if (!sid || sid <= 0) return;

        const payload = {
            sessionId: sid,
            ishumanmode: false,
            connectionId: connectionId.value
        };

        await axios.post(`${API_BASE_URL}/api/ChatMessages/SwitchMode`, payload);
    } catch (error) {
        console.error("結束對話失敗:", error);
    }
};

// ==========================================
// 5. 傳送邏輯 (圖片/文字)
// ==========================================
const triggerFileUpload = () => fileInput.value.click();

const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    selectedFile.value = file;
    previewImage.value = URL.createObjectURL(file);
    event.target.value = '';
};

const removePreview = () => {
    selectedFile.value = null;
    previewImage.value = null;
};

const handleSend = async () => {
    if (selectedFile.value) {
        await sendImageWithText(selectedFile.value, userInput.value);
    } else if (userInput.value.trim()) {
        await sendMessage(userInput.value);
    }
};

const sendImageWithText = async (file, text) => {
    if (!connection.value) await initSignalR();

    // 確保 ID 有效
    const sid = await ensureSession();
    if (!sid || sid <= 0) {
        alert("連線建立中，請稍後再試...");
        return;
    }

    chatStore.addMessage({
        role: 'user',
        text: text,
        imageUrl: previewImage.value,
        time: new Date()
    });

    const formData = new FormData();
    formData.append("SessionId", sid); // 使用確認過的 sid
    formData.append("Image", file);
    formData.append("ConnectionId", connectionId.value);

    removePreview();
    userInput.value = '';
    isSending.value = true;

    try {
        await axios.post(`${API_BASE_URL}/api/ChatMessages/UploadImage`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
    } catch (error) {
        console.error("圖片上傳失敗:", error);
        isSending.value = false;
        chatStore.addMessage({ role: 'ai', text: "❌ 圖片上傳失敗。", time: new Date() });
    }
};

const sendMessage = async (text) => {
    if (isSending.value) return;
    if (!connection.value) await initSignalR();

    chatStore.addMessage({ role: 'user', text: text, time: new Date() });
    userInput.value = '';
    isSending.value = true;

    try {
        const sid = await ensureSession();
        // 防呆
        if (!sid || sid <= 0) {
            throw new Error("Invalid Session ID");
        }

        const payload = {
            sessionId: sid,
            userContent: text,
            connectionId: connectionId.value
        };

        await axios.post(`${API_BASE_URL}/api/ChatMessages/SendMessage`, payload);
    } catch (error) {
        console.error("發送失敗:", error);
        isSending.value = false;
        chatStore.addMessage({ role: 'ai', text: "❌ 訊息發送失敗。", time: new Date() });
    }
};

onMounted(async () => {
    await initSignalR();
    await loadHistory();
});

onUnmounted(() => {
    if (connection.value) connection.value.stop();
});
</script>


<template>
  <div class="chat-widget-wrapper">
    <transition name="slide-up">
      <div v-show="isOpen" class="custom-chat-window">
       
        <div class="chat-header" :class="{ 'human-mode': isHumanMode }">
            <div class="header-title">
                {{ isHumanMode ? '👨‍💼 真人客服支援' : '🤖 AI 智慧助理' }}
            </div>
            <div class="header-status">
                <span class="status-dot" :class="{ 'online': connectionId, 'offline': !connectionId }"></span>
            </div>
        </div>


        <div class="messages-area" ref="messagesContainer">
            <div class="message-row ai" v-if="chatStore.messages.length === 0">
                <div class="avatar">🤖</div>
                <div class="bubble-content">
                    <div class="bubble">
                        <span>你好！我是AI小幫手。你可以問我電影問題，或傳海報給我辨識喔！</span>
                    </div>
                    <div class="msg-time">{{ formatTime(new Date()) }}</div>
                </div>
            </div>


            <div v-for="(msg, index) in chatStore.messages" :key="index" class="message-row" :class="msg.role === 'user' ? 'user' : 'ai'">
               
                <div v-if="msg.role === 'ai'" class="avatar">
                    {{ msg.isHuman ? '👨‍💼' : '🤖' }}
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
                <div class="avatar">{{ isHumanMode ? '👨‍💼' : '🤖' }}</div>
                <div class="bubble typing">● ● ●</div>
            </div>
        </div>


        <div class="input-area">
           
            <div class="action-toolbar">
                <button v-if="!isHumanMode"
                        class="chip-btn action-chip"
                        @click="triggerTransfer"
                        :disabled="isSending"
                        title="轉接真人客服">
                    <span>呼叫人工客服</span>
                </button>


                <button v-else
                        class="chip-btn action-chip danger"
                        @click="endChat"
                        :disabled="isSending"
                        title="結束人工支援">
                    <span>結束對話</span>
                </button>
            </div>


            <div class="input-row">
                <div v-if="previewImage" class="preview-popup">
                    <img :src="previewImage" />
                    <button class="close-btn" @click="removePreview">×</button>
                </div>


                <input type="file" ref="fileInput" @change="handleFileChange" accept="image/*" style="display: none" />


                <div class="input-wrapper">
                    <input
                        v-model="userInput"
                        @keyup.enter="handleSend"
                        :placeholder="isHumanMode ? '傳送給真人客服...' : '輸入訊息 或 「生圖 貓」'"
                        type="text"
                    />
                    <button class="icon-btn image-btn" @click="triggerFileUpload" title="上傳圖片">
                         <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>
                    </button>
                </div>


                <button class="send-btn" @click="handleSend" :disabled="!userInput.trim() && !selectedFile">
                    <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="m21.426 11.095-17-8A.999.999 0 0 0 3.03 4.242L4.969 12 3.03 19.758a.998.998 0 0 0 1.396 1.147l17-8a1 1 0 0 0 0-1.81zM5.481 18.197l.839-3.357L12 12 6.32 9.16l-.839-3.357L18.651 12l-13.17 6.197z"/></svg>
                </button>
            </div>
        </div>
      </div>
    </transition>


    <button class="floating-btn" @click="toggleChat" :class="{ 'is-open': isOpen }">
      <div class="icon-container">
        <img v-if="!isOpen" :src="aiAvatarGif" class="icon-chat-gif" alt="AI Helper"/>
        <svg v-else class="icon-close" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
      </div>
    </button>
  </div>
</template>


<style scoped>
:deep(.markdown-body) ul, :deep(.markdown-body) ol { padding-left: 20px; margin: 5px 0; }
:deep(.markdown-body) p { margin-bottom: 5px; }
:deep(.markdown-body) strong { font-weight: bold; color: #2c3e50; }


.chat-header.human-mode {
    background: linear-gradient(135deg, #e67e22, #d35400);
}


.system-msg {
    background-color: #f1f1f1 !important;
    color: #666 !important;
    font-size: 13px !important;
    text-align: center;
    border: none !important;
    font-style: italic;
}


.chip-btn.danger { color: #c0392b; border-color: #c0392b; }
.chip-btn.danger:hover { background: #fff5f5; }
.custom-chat-window { width: 420px; height: 520px; background: linear-gradient(90deg, rgb(239, 242, 247) 0%, rgb(235, 239, 248) 50%, rgb(208, 224, 247) 100%); border-radius: 20px; box-shadow: 0 12px 40px rgba(0, 0, 0, 0.15); display: flex; flex-direction: column; overflow: hidden; margin-bottom: 20px; font-family: 'Segoe UI', sans-serif; border: 1px solid rgba(255,255,255,0.5); }
.chat-header { background: linear-gradient(135deg, #4398a5, #3a5f54); color: white; padding: 15px 17px; flex-shrink: 0; display: flex; align-items: center; }
.header-title { font-size: 18px; font-weight: bold; }
.messages-area { flex: 1; padding: 20px; overflow-y: auto; display: flex; flex-direction: column; gap: 15px; }
.messages-area::-webkit-scrollbar { width: 6px; }
.messages-area::-webkit-scrollbar-track { background: transparent; }
.messages-area::-webkit-scrollbar-thumb { background-color: rgba(0, 0, 0, 0.1); border-radius: 10px; }
.message-row { display: flex; align-items: flex-end; gap: 8px; max-width: 100%; }
.bubble { padding: 10px 14px; border-radius: 18px; font-size: 14px; line-height: 1.5; word-break: break-word; display: flex; flex-direction: column; gap: 8px; }
.message-row.ai { align-self: flex-start; }
.message-row.ai .avatar { width: 32px; height: 32px; background: #90afe4; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 18px; flex-shrink: 0; }
.message-row.ai .bubble { background-color: white; color: #333; border-bottom-left-radius: 4px; }
.message-row.user { align-self: flex-end; flex-direction: row-reverse; }
.message-row.user .bubble { background: linear-gradient(135deg, #3578F6, #205AE6); color: white; border-bottom-right-radius: 4px; }
.typing { color: #888; letter-spacing: 2px; font-size: 12px; }
.bubble-content { display: flex; flex-direction: column; max-width: 80%; }
.msg-time { font-size: 11px; color: #999; margin-top: 4px; margin-left: 6px; }
.message-row.user .msg-time { text-align: right; margin-right: 6px; }
.msg-image { max-width: 100%; border-radius: 8px; display: block; }
.input-area { padding: 15px; background: rgba(255,255,255,0.4); border-top: 1px solid rgba(0,0,0,0.05); display: flex; flex-direction: column; gap: 10px; position: relative; }
.action-toolbar { display: flex; gap: 8px; padding-left: 2px; min-height: 28px; }
.chip-btn { display: flex; align-items: center; gap: 6px; padding: 6px 12px; background: rgba(255, 255, 255, 0.7); border: 1px solid rgba(0, 0, 0, 0.1); border-radius: 16px; font-size: 12px; color: #555; cursor: pointer; transition: all 0.2s; }
.chip-btn:hover { background: #fff; box-shadow: 0 2px 6px rgba(0,0,0,0.05); }
.chip-btn.action-chip:hover { color: #e67e22; background: #fff8f0; border-color: #e67e22; }
.chip-btn:disabled { opacity: 0.5; cursor: not-allowed; background: #f0f0f0; color: #bbb; border-color: transparent; box-shadow: none; }
.input-row { display: flex; align-items: flex-end; gap: 10px; width: 100%; }
.input-wrapper { position: relative; flex: 1; display: flex; align-items: center; }
.input-wrapper input { width: 100%; padding: 12px 15px; padding-right: 45px; border-radius: 24px; border: 1px solid #e0e0e0; font-size: 15px; outline: none; background: #f9f9f9; transition: all 0.3s; }
.input-wrapper input:focus { border-color: #3578F6; background: #fff; }
.image-btn { position: absolute; right: 8px; background: transparent; border: none; color: #888; cursor: pointer; padding: 5px; border-radius: 50%; }
.image-btn:hover { color: #3578F6; background: #eee; }
.image-btn svg { width: 20px; height: 20px; }
.send-btn { width: 46px; height: 46px; border-radius: 50%; background: linear-gradient(135deg, #3578F6, #205AE6); border: none; color: white; display: flex; align-items: center; justify-content: center; cursor: pointer; flex-shrink: 0; transition: transform 0.2s; box-shadow: 0 4px 10px rgba(53, 120, 246, 0.3); }
.send-btn:hover { transform: scale(1.05); }
.send-btn:disabled { background: #ccc; cursor: not-allowed; box-shadow: none; }
.send-btn svg { width: 20px; height: 20px; fill: white; margin-left: 2px; }
.preview-popup { position: absolute; bottom: 60px; left: 0; background: white; padding: 6px; border-radius: 12px; box-shadow: 0 5px 15px rgba(0,0,0,0.15); z-index: 100; }
.preview-popup img { height: 70px; width: auto; border-radius: 8px; display: block; }
.close-btn { position: absolute; top: -8px; right: -8px; width: 22px; height: 22px; background: #ff5e57; color: white; border: none; border-radius: 50%; cursor: pointer; display: flex; justify-content: center; align-items: center; }
.chat-widget-wrapper { position: fixed; bottom: 30px; right: 30px; z-index: 9999; display: flex; flex-direction: column; align-items: flex-end; }
.floating-btn { width: 60px; height: 60px; border-radius: 50%; background: transparent; border: none; cursor: pointer; box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12); position: relative; overflow: hidden; transition: all 0.3s; }
.floating-btn:hover { transform: scale(1.08); }
.floating-btn.is-open { transform: rotate(90deg); background: #333; }
.icon-chat-gif { width: 100%; height: 100%; object-fit: cover; transform: scale(1.0); }
.icon-close { width: 24px; height: 24px; color: white; }
.header-status { margin-left: auto; display: flex; align-items: center; }
.status-dot { width: 8px; height: 8px; border-radius: 50%; background: #ccc; margin-left: 5px; }
.status-dot.online { background: #2ecc71; box-shadow: 0 0 5px #2ecc71; }
</style>

