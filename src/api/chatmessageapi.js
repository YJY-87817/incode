import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'http://localhost:5187', 
  headers: { 'Content-Type': 'application/json' }
});

export default {
  getSessionMessages(sessionId) {
    return apiClient.get(`/api/ChatMessages/Session/${sessionId}`);
  },

  sendMessage(userContent) {
    return apiClient.post('/api/ChatMessages/send', { 
      UserContent: userContent 
    });
  }
};