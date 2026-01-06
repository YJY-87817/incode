<script setup>
import { ref } from 'vue';
import 'deep-chat';
import { useChatStore } from '@/stores/chatmessagestores'; 


const chatStore = useChatStore();
const isOpen = ref(false);
const unreadCount = ref(1);
const toggleChat = () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) unreadCount.value = 0;
};

const chatRequest = {
  handler: async (body, signals) => {
    try {
      
      const aiReply = await chatStore.sendUserMessage(body.text);
      
     
      signals.onResponse({ text: aiReply });
      
    } catch (error) {
      console.error("Chat Error:", error);
      signals.onResponse({ error: "連線失敗，請檢查後端是否啟動。" });
    }
  }
};


const textInputStyle = {
  styles: {
    container: {
      borderRadius: '20px',
      border: 'none',
      width: '80%',
      marginLeft: '-20px',
      marginBottom: '15px',
      backgroundColor: '#ffffff',
      boxShadow: '0px 0.3px 0.9px rgba(0, 0, 0, 0.12), 0px 1.6px 3.6px rgba(0, 0, 0, 0.16)'
    },
    text: { padding: '10px 34px 10px 15px', fontSize: '14px' }
  },
  placeholder: { text: 'Ask me...', style: { color: '#606060' } }
};

const messageBubblesStyle = {
  default: {
    shared: { bubble: { backgroundColor: 'unset', marginTop: '8px', marginBottom: '8px', maxWidth: '85%', boxShadow: '0px 1px 2px rgba(0, 0, 0, 0.1)' } },
    user: { bubble: { background: 'linear-gradient(130deg, #2870EA 20%, #1B4AEF 77.5%)', color: 'white' } },
    ai: { bubble: { background: 'rgba(255,255,255,0.9)', color: '#333' } }
  }
};

const microphoneStyle = {
  button: {
    default: { container: { default: { bottom: '0.9em', right: '0.5em', borderRadius: '50%', width: '1.8em', height: '1.8em' } }, svg: { styles: { default: { width: '1em', height: '1em', bottom: '0.7em', left: '0.4em' } } } },
    position: 'inside-end'
  }
};

const submitButtonStyle = {
  position: 'outside-end',
  submit: {
    container: { default: { bottom: '0.8em', borderRadius: '50%', padding: '6px', backgroundColor: 'transparent' }, hover: { backgroundColor: 'rgba(176, 222, 255, 0.3)' }, click: { backgroundColor: 'rgba(176, 222, 255, 0.7)' } },
    svg: { content: `<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="m21.426 11.095-17-8A.999.999 0 0 0 3.03 4.242L4.969 12 3.03 19.758a.998.998 0 0 0 1.396 1.147l17-8a1 1 0 0 0 0-1.81zM5.481 18.197l.839-3.357L12 12 6.32 9.16l-.839-3.357L18.651 12l-13.17 6.197z"/></svg>`, styles: { default: { width: '1.4em', filter: 'brightness(0) saturate(100%) invert(26%) sepia(96%) saturate(3048%) hue-rotate(212deg) brightness(97%) contrast(93%)' } } }
  },
  loading: { svg: { styles: { default: { filter: 'brightness(0) saturate(100%) invert(72%) sepia(0%) saturate(3044%) hue-rotate(322deg) brightness(100%) contrast(96%)' } } } },
  stop: { container: { hover: { backgroundColor: '#ededed94' } }, svg: { styles: { default: { filter: 'brightness(0) saturate(100%) invert(72%) sepia(0%) saturate(3044%) hue-rotate(322deg) brightness(100%) contrast(96%)' } } } }
};

</script>

<template>
  <div class="chat-widget-container">
    <transition name="slide-up">
      <deep-chat
        v-show="isOpen"
        class="styled-chat"
        :textInput="textInputStyle"
        :messageStyles="messageBubblesStyle"
        :microphone="microphoneStyle"
        :submitButtonStyles="submitButtonStyle"
        :history="chatStore.messages" 
        :request="chatRequest"
        :introMessage="{ text: '你好!我是AI小幫手，有什麼可以幫你的嗎？' }"
      ></deep-chat>
    </transition>

    <button
      class="floating-btn"
      @click="toggleChat"
      :class="{ 'is-open': isOpen }"
    >
      <span v-if="!isOpen && unreadCount > 0" class="unread-badge">{{ unreadCount }}</span>
      <div class="icon-container">
        <svg 
          v-if="!isOpen"
          class="icon-chat"
          xmlns="http://www.w3.org/2000/svg" 
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <line x1="7" y1="4" x2="7" y2="7" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"></line>
          <circle cx="7" cy="3" r="1" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"></circle>
          <path d="m13.475,7.439l-1.414-1.414c-.122-.123-.3-.172-.468-.133-.169.04-.305.164-.359.329l-.707,2.121c-.06.18-.013.378.121.512.095.095.223.146.354.146.053,0,.106-.008.158-.026l2.121-.707c.165-.055.289-.19.329-.359.04-.168-.011-.346-.133-.468Z" fill="currentColor" stroke-width="0"></path>
          <path d="m8.328,7.028c-.003-.009-.504-.019-.507-.028h-1.822c-1.657,0-3,1.343-3,3v4c0,1.657,1.343,3,3,3h8c1.657,0,3-1.343,3-3v-3.24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"></path>
          <circle cx="14.5" cy="5" r="3" fill="currentColor" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"></circle>
          <circle cx="7" cy="11.5" r="1" stroke-width="0" fill="currentColor"></circle>
          <circle cx="13" cy="11.5" r="1" stroke-width="0" fill="currentColor"></circle>
          <path d="m9,13h2c.276,0,.5.224.5.5h0c0,.828-.672,1.5-1.5,1.5h0c-.828,0-1.5-.672-1.5-1.5h0c0-.276.224-.5.5-.5Z" stroke-width="0" fill="currentColor"></path>
        </svg>
        <svg v-else class="icon-close" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
      </div>
    </button>
  </div>
</template>

<style scoped>
.chat-widget-container {
  position: fixed;
  bottom: 30px;
  right: 30px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.styled-chat {
  width: 360px;
  height: 520px;
  margin-bottom: 20px;
  border-radius: 12px;
  border: none;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
  background: linear-gradient(90deg, rgb(239, 242, 247) 0%, 7.60286%, rgb(237, 240, 249) 15.2057%, 20.7513%, rgb(235, 239, 248) 26.297%, 27.6386%, rgb(235, 239, 248) 28.9803%, 38.2826%, rgb(231, 237, 249) 47.585%, 48.1216%, rgb(230, 236, 250) 48.6583%, 53.1306%, rgb(228, 236, 249) 57.6029%, 61.5385%, rgb(227, 234, 250) 65.4741%, 68.7835%, rgb(222, 234, 250) 72.093%, 75.7603%, rgb(219, 230, 248) 79.4275%, 82.8265%, rgb(216, 229, 248) 86.2254%, 87.8354%, rgb(213, 228, 249) 89.4454%, 91.8605%, rgb(210, 226, 249) 94.2755%, 95.4383%, rgb(209, 225, 248) 96.6011%, 98.3005%, rgb(208, 224, 247) 100%);
}
.floating-btn {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: #1a1a1a;
  border: none;
  cursor: pointer;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.4);
  color: white;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
}
.floating-btn:hover {
  transform: scale(1.05);
  background: #000000;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.6);
}
.floating-btn:active {
  transform: scale(0.95);
}
.floating-btn.is-open {
  transform: rotate(90deg);
}
.unread-badge {
  position: absolute;
  top: -2px;
  right: -2px;
  background-color: #ff3b30;
  color: white;
  font-size: 12px;
  font-weight: bold;
  height: 22px;
  min-width: 22px;
  padding: 0 6px;
  border-radius: 11px;
  border: 2px solid white;
  display: flex;
  align-items: center;
  justify-content: center;
}
.icon-chat, .icon-close {
  width: 28px;
  height: 28px;
}
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
}
.slide-up-enter-from,
.slide-up-leave-to {
  transform: translateY(20px) scale(0.95);
  opacity: 0;
}
</style>