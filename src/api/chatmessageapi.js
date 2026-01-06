import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'https://localhost:7190', 
  headers: { 'Content-Type': 'application/json' }
});

export default {
  getSessionMessages(sessionId) {
    return apiClient.get(`/api/ChatMessages/Session/${sessionId}`);
  },

  sendMessage(userContent) {
    return apiClient.post('/ChatMessages/SendMessage', { 
      UserContent: userContent 
    });
  }
};