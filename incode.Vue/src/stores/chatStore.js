import { defineStore } from 'pinia';
import { ref } from 'vue';
import axios from 'axios';


export const useChatStore = defineStore('chat', () => {
 
  // ================= State (資料) =================
  const sessionId = ref(0);
  const connectionId = ref("");
  const loading = ref(false);
  const messages = ref([]);


  // ================= Actions (方法) =================


  // 1. 新增訊息 (推入陣列)
  const addMessage = (message) => {
    // message 格式範例: { role: 'user', text: '你好', time: Date, imageUrl: ... }
    messages.value.push(message);
  };


  // 2. ★★★ 補上缺少的清除訊息方法 (ChatWidget.vue 需要用到) ★★★
  const clearMessages = () => {
    messages.value = [];
  };


  // 3. 設定 Connection ID
  const setConnectionId = (id) => {
    console.log("Store 更新 ConnectionId:", id);
    connectionId.value = id;
  };
 
  // 4. 設定 Session ID (並同步到 LocalStorage)
  const setSessionId = (id) => {
    sessionId.value = id;
    // ★ 關鍵：同步寫入 LocalStorage，這樣重整後 ChatWidget 才能讀取
    if (id) {
        localStorage.setItem('chatSessionId', id);
    }
  };


  // 5. 重置對話 (登出或清除時用)
  const resetSession = () => {
    sessionId.value = 0;
    messages.value = [];
    localStorage.removeItem('chatSessionId'); // 順便清掉紀錄
  };


  // --- (選用功能) HTTP 發送邏輯 ---
  // 註：因為您的 ChatWidget.vue 現在改用 SignalR (invoke) 發送，
  // 下面這兩個函式目前可能不會被用到，但保留著也沒關係。
  const sendUserMessage = async (text) => {
    // ... (保留您原本的邏輯，若未來需要 fallback API 可用)
  };


  const uploadImage = async (file) => {
    // ... (保留您原本的邏輯)
  };


  // ================= Return (匯出) =================
  // ★★★ 重要：請確認 clearMessages 有在列表裡 ★★★
  return {
    messages,
    sessionId,      
    connectionId,  
    loading,
   
    addMessage,
    clearMessages,  // ★ 這裡一定要匯出！
    setConnectionId,
    setSessionId,
    resetSession,
   
    // 如果您元件主要用 SignalR，這兩個可以不用匯出，或留著備用
    sendUserMessage,
    uploadImage
  };
});

