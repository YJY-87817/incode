import { defineStore } from 'pinia';
import { ref } from 'vue';
import chatApi from '@/api/chatmessageapi';

export const useChatStore = defineStore('chat', () => {
  

  const messages = ref([]);
  const sendUserMessage = async (text) => {
    messages.value.push({ text: text, role: 'user' });

    try {
      const response = await chatApi.sendMessage(text);
      const aiReply = response.data.reply;

      messages.value.push({ text: aiReply, role: 'ai' });
      
      return aiReply; 
    } catch (error) {
      console.error('API Error:', error);
      const errorMsg = "抱歉，連線發生錯誤。";
      messages.value.push({ text: errorMsg, role: 'ai' });
      return errorMsg;
    }
  };

  return {
    messages,
    sendUserMessage
  };
});