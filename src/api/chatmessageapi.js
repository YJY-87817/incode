import axios from 'axios';

// ★ 請確認這裡的 Port 是您後端 HTTPS 的 Port
const API_URL = 'https://localhost:7190/api/ChatMessages';

export default {
  // 1. 發送文字訊息
  sendMessage(payload) {
    // payload 格式: { SessionId, UserContent, ConnectionId }
    return axios.post(`${API_URL}/SendMessage`, payload, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },

  // 2. 上傳圖片 (這是你可能誤刪的部分)
  uploadImage(formData) {
    // formData 內含: SessionId, Image, ConnectionId
    return axios.post(`${API_URL}/UploadImage`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data' // ★ 重要：上傳檔案必須用這個 Header
      }
    });
  },
  
  // 3. 取得歷史訊息 (如果需要讀取舊對話)
  getSessionMessages(sessionId) {
    return axios.get(`${API_URL}/Session/${sessionId}`);
  }
};