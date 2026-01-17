import { defineStore } from 'pinia';
import { ref } from 'vue';
import chatApi from '@/api/chatmessageapi'; // 請確認路徑是否正確
import axios from 'axios';

export const useChatStore = defineStore('chat', () => {
  
  // 1. 記錄目前的 Session ID
  const sessionId = ref(0); // 我將變數名稱統一改為 sessionId 配合你的元件使用習慣
  
  // 2. 記錄 SignalR 的 Connection ID
  const connectionId = ref(""); // 變數名稱改為 connectionId 較簡潔

  const loading = ref(false);
  const messages = ref([]);

  // --- 關鍵修正：補上 addMessage 函式 ---
  const addMessage = (message) => {
    // message 格式範例: { role: 'user', text: '你好' }
    messages.value.push(message);
  };

  // 重置對話
  const resetSession = () => {
    sessionId.value = 0;
    messages.value = [];
  };

  // 設定 Connection ID
  const setConnectionId = (id) => {
    console.log("Store 更新 ConnectionId:", id);
    connectionId.value = id;
  };
  
  // 設定 Session ID
  const setSessionId = (id) => {
    sessionId.value = id;
  };

  // --- 功能 A: 發送文字 (透過 Store 發送，或由元件直接呼叫 API 皆可) ---
  const sendUserMessage = async (text) => {
    if (loading.value) return; 
    
    // UI 更新
    addMessage({ text: text, role: 'user' });
    loading.value = true;

    try {
      const payload = {
        sessionId: sessionId.value,
        userContent: text,
        connectionId: connectionId.value || "" // 允許空值
      };

      // 發送請求 (假設 chatApi 已定義)
      // 如果你習慣在元件層發送，這段可以保留作為備用
      const response = await chatApi.sendMessage(payload);
      const resData = response.data;

      if (resData.success) {
        if (sessionId.value === 0 || sessionId.value !== resData.sessionId) {
          sessionId.value = resData.sessionId;
        }
        const aiReply = resData.data ? resData.data.body : "無回應";
        return aiReply; 
      } else {
        throw new Error('Server returned success: false');
      }

    } catch (error) {
      console.error('API Error:', error);
      const errorMsg = "抱歉，連線發生錯誤。";
      addMessage({ text: errorMsg, role: 'ai' });
      return errorMsg;
    } finally {
      loading.value = false;
    }
  };

  // --- 功能 B: 上傳圖片 ---
  const uploadImage = async (file) => {
    if (loading.value) return;
    loading.value = true;

    try {
      const formData = new FormData();
      formData.append('Image', file);
      formData.append('SessionId', sessionId.value);
      formData.append('ConnectionId', connectionId.value || ""); 

      const apiUrl = 'https://localhost:7190/api/ChatMessages/UploadImage'; 
      
      const response = await axios.post(apiUrl, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });

      const resData = response.data; 

      if (resData.success) {
        // 更新 Store
        addMessage({ type: 'image', src: `https://localhost:7190${resData.url}`, role: 'user' });
        addMessage({ text: resData.reply, role: 'ai' });

        return {
          url: `https://localhost:7190${resData.url}`,
          reply: resData.reply
        };
      }
    } catch (error) {
      console.error('Upload Error:', error);
      throw error;
    } finally {
      loading.value = false;
    }
  };

  // ★★★ 重要：必須 return 出去，外部才看得到！ ★★★
  return {
    messages,
    sessionId,      // 對應元件中的 chatStore.sessionId
    connectionId,   // 對應元件中的 chatStore.connectionId
    loading,
    
    addMessage,     // ★ 這裡一定要匯出
    setConnectionId,
    setSessionId,
    sendUserMessage,
    uploadImage,
    resetSession
  };
});