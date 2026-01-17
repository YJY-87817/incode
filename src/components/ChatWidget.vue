<script setup>
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import axios from 'axios';
import * as signalR from "@microsoft/signalr";
import { useChatStore } from '@/stores/chatmessagestores'; 
import aiAvatarGif from '@/assets/gif/1.gif';

const chatStore = useChatStore();
const isOpen = ref(false);
const userInput = ref('');
const messagesContainer = ref(null);
const fileInput = ref(null);
const isSending = ref(false);

const connection = ref(null);
const isConnecting = ref(false);

// 預覽與檔案狀態
const previewImage = ref(null); 
const selectedFile = ref(null); 

// 時間格式化
const formatTime = (dateInput) => {
  const date = dateInput ? new Date(dateInput) : new Date();
  return date.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false });
};

// 切換視窗
const toggleChat = () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) scrollToBottom();
};

// 捲動到底部
const scrollToBottom = async () => {
  await nextTick();
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
};

watch(() => chatStore.messages.length, () => scrollToBottom());

// SignalR 初始化
const initSignalR = async () => {
  if (connection.value?.state === signalR.HubConnectionState.Connected || isConnecting.value) return;
  isConnecting.value = true;
  try {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7190/chatHub") 
      .withAutomaticReconnect()
      .build();

    newConnection.on("ReceiveMessage", (sender, message) => {
        // 若需處理後端廣播，可在此處加入邏輯
        console.log(`📩 [SignalR] ${sender}: ${message}`);
    });

    await newConnection.start();
    chatStore.setConnectionId(newConnection.connectionId);
    connection.value = newConnection;
  } catch (err) {
    console.error("❌ SignalR error:", err);
  } finally {
    isConnecting.value = false;
  }
};

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
  if (previewImage.value) {
    previewImage.value = null;
  }
};

// ★★★ 核心修改：統一發送入口 ★★★
const handleSend = async () => {
    // 狀況一：有圖片 (包含「純圖片」或「圖片+文字」)
    if (selectedFile.value) {
        await sendImageWithText(selectedFile.value, userInput.value);
    } 
    // 狀況二：只有文字
    else if (userInput.value.trim()) {
        await sendMessage(userInput.value);
    }
};

// ★★★ 核心修改：發送圖片 (可帶文字) ★★★
const sendImageWithText = async (file, text) => {
  if (!connection.value) { await initSignalR(); }
  
  // 1. 本地先顯示 (Optimistic UI)
  chatStore.addMessage({ 
      role: 'user', 
      text: text,           // 文字內容
      imageUrl: previewImage.value, // 圖片預覽網址
      time: new Date() 
  }); 

  // 暫存變數以便發送
  const fileToSend = file;
  const textToSend = text;

  // 清空輸入區與預覽
  removePreview();
  userInput.value = '';

  try {
    let formData = new FormData();
    formData.append("SessionId", chatStore.sessionId || 0);
    formData.append("Image", fileToSend);
    // ★ 關鍵：把文字也塞進去 (需確認後端 API 支援接收 UserContent)
    formData.append("UserContent", textToSend); 
    formData.append("ConnectionId", chatStore.connectionId);

    isSending.value = true;
    const res = await axios.post('https://localhost:7190/api/ChatMessages/UploadImage', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
    });

    if (res.data.success) {
       chatStore.addMessage({ role: 'ai', text: res.data.reply, time: new Date() });
    }
  } catch (error) {
    console.error(error);
    chatStore.addMessage({ role: 'ai', text: "發送失敗。", time: new Date() });
  } finally {
    isSending.value = false;
  }
};

// 發送純文字
const sendMessage = async (text) => {
  if (isSending.value) return;

  chatStore.addMessage({ role: 'user', text: text, time: new Date() });
  userInput.value = ''; 
  isSending.value = true;

  if (!connection.value) { await initSignalR(); }

  try {
    const payload = {
        SessionId: chatStore.sessionId || 0,
        UserContent: text,
        ConnectionId: chatStore.connectionId
    };
    const res = await axios.post('https://localhost:7190/api/ChatMessages/SendMessage', payload);

    if (res.data.success) {
        if (res.data.sessionId) chatStore.setSessionId(res.data.sessionId);
        chatStore.addMessage({ role: 'ai', text: res.data.data.body, time: new Date() });
    }
  } catch (error) {
    console.error(error);
    chatStore.addMessage({ role: 'ai', text: "伺服器連線失敗。", time: new Date() });
  } finally {
    isSending.value = false;
  }
};

onMounted(() => initSignalR());
onUnmounted(() => { if (connection.value) connection.value.stop(); });
</script>

<template>
  <div class="chat-widget-wrapper">
    <transition name="slide-up">
      <div v-show="isOpen" class="custom-chat-window">
        
        <div class="chat-header">
            <div class="header-title">AI小幫手</div>
        </div>

        <div class="messages-area" ref="messagesContainer">
            <div class="message-row ai">
                <div class="avatar">🤖</div>
                <div class="bubble-content">
                    <div class="bubble">
                        <span>你好！我是AI小幫手，有什麼可以幫你的嗎？</span>
                    </div>
                    <div class="msg-time">{{ formatTime(new Date()) }}</div>
                </div>
            </div>

            <div 
              v-for="(msg, index) in chatStore.messages" 
              :key="index" 
              class="message-row"
              :class="msg.role === 'user' ? 'user' : 'ai'"
            >
                <div v-if="msg.role === 'ai'" class="avatar">🤖</div>
                
                <div class="bubble-content">
                    <div class="bubble">
                        <img v-if="msg.imageUrl" :src="msg.imageUrl" class="msg-image" @load="scrollToBottom" />

                        <span v-if="msg.text" class="msg-text">{{ msg.text }}</span>
                    </div>
                    <div class="msg-time">{{ formatTime(msg.time) }}</div>
                </div>
            </div>
            
            <div v-if="isSending" class="message-row ai">
                <div class="avatar">🤖</div>
                <div class="bubble typing">● ● ●</div>
            </div>
        </div>

        <div class="input-area">
            <div v-if="previewImage" class="preview-popup">
                <img :src="previewImage" />
                <button class="close-btn" @click="removePreview">×</button>
            </div>

            <input type="file" ref="fileInput" @change="handleFileChange" accept=".jpg,.jpeg,.png,.gif" style="display: none" />

            <div class="input-wrapper">
                <input 
                    v-model="userInput" 
                    @keyup.enter="handleSend"
                    placeholder="請輸入訊息..." 
                    type="text" 
                />
                
                <button class="icon-btn image-btn" @click="triggerFileUpload" title="上傳圖片">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                        <circle cx="8.5" cy="8.5" r="1.5"></circle>
                        <polyline points="21 15 16 10 5 21"></polyline>
                    </svg>
                </button>
            </div>

            <button class="send-btn" @click="handleSend" :disabled="!userInput.trim() && !selectedFile">
                <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="currentColor">
                    <path d="m21.426 11.095-17-8A.999.999 0 0 0 3.03 4.242L4.969 12 3.03 19.758a.998.998 0 0 0 1.396 1.147l17-8a1 1 0 0 0 0-1.81zM5.481 18.197l.839-3.357L12 12 6.32 9.16l-.839-3.357L18.651 12l-13.17 6.197z"/>
                </svg>
            </button>
        </div>

      </div>
    </transition>

    <button class="floating-btn" @click="toggleChat" :class="{ 'is-open': isOpen }">
      <div class="icon-container">
        <img v-if="!isOpen" :src="aiAvatarGif" class="icon-chat-gif" alt="AI Helper" style="width: 40px; height: 40px;"/>
        <svg v-else class="icon-close" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
      </div>
    </button>
  </div>
</template>

<style scoped>
/* =========================================
   嚴格保留您原本所有的 CSS 設定 (完全不動)
   包含漸層背景、陰影、按鈕樣式
   ========================================= */

.custom-chat-window {
    width: 420px; height: 520px;
    background: linear-gradient(90deg, rgb(239, 242, 247) 0%, 7.60286%, rgb(237, 240, 249) 15.2057%, 20.7513%, rgb(235, 239, 248) 26.297%, 27.6386%, rgb(235, 239, 248) 28.9803%, 38.2826%, rgb(231, 237, 249) 47.585%, 48.1216%, rgb(230, 236, 250) 48.6583%, 53.1306%, rgb(228, 236, 249) 57.6029%, 61.5385%, rgb(227, 234, 250) 65.4741%, 68.7835%, rgb(222, 234, 250) 72.093%, 75.7603%, rgb(219, 230, 248) 79.4275%, 82.8265%, rgb(216, 229, 248) 86.2254%, 87.8354%, rgb(213, 228, 249) 89.4454%, 91.8605%, rgb(210, 226, 249) 94.2755%, 95.4383%, rgb(209, 225, 248) 96.6011%, 98.3005%, rgb(208, 224, 247) 100%);
    border-radius: 20px; border: none;
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.15);
    display: flex; flex-direction: column; overflow: hidden;
    margin-bottom: 20px; font-family: 'Segoe UI', sans-serif;
    border: 1px solid transparent;
}

.chat-header {
    background: linear-gradient(135deg, #4398a5, #3a5f54);
    color: white; padding: 15px 17px; flex-shrink: 0;
    border: none; border-bottom: none; box-shadow: none;
}
.header-title { font-size: 18px; font-weight: bold; }

.messages-area {
    flex: 1; padding: 20px; overflow-y: auto;
    display: flex; flex-direction: column; gap: 15px; background: transparent;
}
.messages-area::-webkit-scrollbar { width: 6px; }
.messages-area::-webkit-scrollbar-track { background: transparent; }
.messages-area::-webkit-scrollbar-thumb { background-color: rgba(0, 0, 0, 0.1); border-radius: 10px; }
.messages-area::-webkit-scrollbar-thumb:hover { background-color: rgba(0, 0, 0, 0.2); }

/* 訊息氣泡通用樣式 */
.message-row { display: flex; align-items: flex-end; gap: 8px; max-width: 100%; }

/* 加入 flex-direction: column 讓圖片和文字可以垂直排列 */
.bubble {
    padding: 12px 16px;
    border-radius: 18px;
    font-size: 15px;
    line-height: 1.5;
    word-break: break-word;
    display: flex;       /* 新增 */
    flex-direction: column; /* 新增 */
    gap: 8px;            /* 新增：圖文間距 */
}

/* AI 樣式 */
.message-row.ai { align-self: flex-start; }
.message-row.ai .avatar {
    width: 32px; height: 32px; background: #90afe4; border-radius: 50%;
    display: flex; align-items: center; justify-content: center; font-size: 18px; flex-shrink: 0;
}
.message-row.ai .bubble {
    background-color: white; color: #333; border-bottom-left-radius: 4px;
}

/* User 樣式 */
.message-row.user { align-self: flex-end; flex-direction: row-reverse; }
.message-row.user .bubble {
    background: linear-gradient(135deg, #3578F6, #205AE6);
    color: white; border-bottom-right-radius: 4px;
}

.typing { color: #888; letter-spacing: 2px; font-size: 12px; }

/* 輸入區 (保持原本長串漸層) */
.input-area {
    padding: 15px;
    background: linear-gradient(90deg, rgb(239, 242, 247) 0%, 7.60286%, rgb(237, 240, 249) 15.2057%, 20.7513%, rgb(235, 239, 248) 26.297%, 27.6386%, rgb(235, 239, 248) 28.9803%, 38.2826%, rgb(231, 237, 249) 47.585%, 48.1216%, rgb(230, 236, 250) 48.6583%, 53.1306%, rgb(228, 236, 249) 57.6029%, 61.5385%, rgb(227, 234, 250) 65.4741%, 68.7835%, rgb(222, 234, 250) 72.093%, 75.7603%, rgb(219, 230, 248) 79.4275%, 82.8265%, rgb(216, 229, 248) 86.2254%, 87.8354%, rgb(213, 228, 249) 89.4454%, 91.8605%, rgb(210, 226, 249) 94.2755%, 95.4383%, rgb(209, 225, 248) 96.6011%, 98.3005%, rgb(208, 224, 247) 100%);
    background-color: transparent;
    border-top: none;
    display: flex; align-items: flex-end; gap: 10px;
    position: relative; 
}

.input-wrapper { position: relative; flex: 1; display: flex; align-items: center; }
.input-wrapper input {
    width: 100%; padding: 12px 15px; padding-right: 45px;
    border-radius: 24px; border: 1px solid #e0e0e0;
    font-size: 15px; outline: none; background: #f9f9f9;
    transition: all 0.3s; position: relative; z-index: 1;
}
.input-wrapper input:focus {
    border-color: #3578F6; background: #fff; box-shadow: 0 0 0 2px rgba(53, 120, 246, 0.1);
}
.image-btn {
    position: absolute; right: 8px; background: transparent; border: none;
    color: #888; cursor: pointer; padding: 5px; border-radius: 50%;
    display: flex; transition: color 0.3s; z-index: 10;
}
.image-btn:hover { color: #3578F6; background: #eee; }
.image-btn svg { width: 20px; height: 20px; }

.send-btn {
    width: 46px; height: 50px; border-radius: 50%;
    background: linear-gradient(135deg, #3578F6, #205AE6);
    border: none; color: white;
    display: flex; align-items: center; justify-content: center;
    cursor: pointer; flex-shrink: 0;
    transition: transform 0.2s, box-shadow 0.2s;
    box-shadow: 0 4px 10px rgba(53, 120, 246, 0.3); z-index: 10;
}
.send-btn:hover { transform: scale(1.05); }
.send-btn:active { transform: scale(0.95); }
.send-btn:disabled { background: #ccc; cursor: not-allowed; box-shadow: none; }
.send-btn svg { width: 20px; height: 20px; fill: white; margin-left: 2px; }

.chat-widget-wrapper {
    position: fixed; bottom: 30px; right: 30px; z-index: 9999;
    display: flex; flex-direction: column; align-items: flex-end;
}
.floating-btn {
    width: 60px; height: 60px; border-radius: 50%;
    background: transparent; border: none; cursor: pointer;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12);
    position: relative; overflow: hidden; transition: all 0.3s;
}
.floating-btn:hover { transform: scale(1.08); }
.floating-btn.is-open { transform: rotate(90deg); background: #333; }
.icon-chat-gif { width: 100%; height: 100%; object-fit: cover; transform: scale(1.5); }
.icon-close { width: 24px; height: 24px; color: white; }

/* =========================================
   新增的樣式 (僅用於支援功能，不影響主外觀)
   ========================================= */

/* 時間標籤容器 */
.bubble-content { display: flex; flex-direction: column; max-width: 80%; }

/* 時間文字 */
.msg-time { font-size: 11px; color: #999; margin-top: 4px; margin-left: 6px; }
.message-row.user .msg-time { text-align: right; margin-right: 6px; margin-left: 0; }

/* 聊天室內圖片樣式 */
.msg-image { max-width: 100%; border-radius: 8px; display: block; }
/* 若同時有文字，調整圖片下方間距，避免太擠 (非必要，可選) */
.msg-image + .msg-text { margin-top: 4px; display: block; }

/* 預覽視窗 (絕對定位在輸入框上方) */
.preview-popup {
    position: absolute; bottom: 75px; left: 20px;
    background: white; padding: 6px; border-radius: 12px;
    box-shadow: 0 5px 15px rgba(0,0,0,0.15); z-index: 100;
    display: flex; align-items: start; animation: popIn 0.2s ease-out;
}
.preview-popup img { height: 70px; width: auto; border-radius: 8px; display: block; }
.close-btn {
    position: absolute; top: -8px; right: -8px;
    width: 22px; height: 22px; background: #ff5e57;
    color: white; border: none; border-radius: 50%;
    font-size: 16px; line-height: 1; cursor: pointer;
    box-shadow: 0 2px 5px rgba(0,0,0,0.2);
    display: flex; justify-content: center; align-items: center;
}
@keyframes popIn {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: translateY(0); }
}
</style>