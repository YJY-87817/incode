import { createApp } from "vue";
import { createPinia } from "pinia";
import piniaPluginPersistedstate from "pinia-plugin-persistedstate";
import App from "./App.vue";

import "@fortawesome/fontawesome-free/css/all.css";
import "@fortawesome/fontawesome-free/js/all.js";

import router from "./router";

//owen
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'

const vuetify = createVuetify({
  components,
  directives,
})

// Nucleo Icons
import "./assets/css/nucleo-icons.css";
import "./assets/css/nucleo-svg.css";

//Bootstrap
import { createBootstrap } from "bootstrap-vue-next";
import "bootstrap/dist/css/bootstrap.css";
import "bootstrap-vue-next/dist/bootstrap-vue-next.css";
import 'bootstrap-icons/font/bootstrap-icons.css'

import materialKit from "./material-kit";
import axios from "axios";
axios.defaults.withCredentials = true;

const app = createApp(App);

const pinia = createPinia();

pinia.use(piniaPluginPersistedstate); //piniaPluginPersist舊版

app.use(pinia);
app.use(router);
app.use(materialKit);
app.use(vuetify)
app.use(createBootstrap());

// 在 app 掛載前暴露 pinia 實例到 window，用於調試
const appInstance = app.mount("#app");

// 調試工具 - 檢查 Forum Store 和 Modal 狀態
import { useForumStore } from './stores/forumStore';

window.debugForum = () => {
  const forumStore = useForumStore();
  
  console.log('=== Forum Store Debug ===');
  console.log('showNewTopicModal:', forumStore.showNewTopicModal);
  console.log('threadDetail:', forumStore.threadDetail);
  console.log('newTopic:', forumStore.newTopic);
  console.log('newTopic.categoryId:', forumStore.newTopic.categoryId);
  console.log('newTopic.parentContentId:', forumStore.newTopic.parentContentId);
  console.log('newTopic.categorySearch:', forumStore.newTopic.categorySearch);
  console.log('selectedCategory:', forumStore.selectedCategory);
  console.log('categories:', forumStore.categories);
  console.log('===============');
};

window.checkReplyModal = () => {
  console.log('=== Checking Reply Modal State ===');
  window.debugForum();
  const modalElement = document.querySelector('[v-if="forumStore.showNewTopicModal"]');
  console.log('Modal element visible:', modalElement ? modalElement.offsetHeight > 0 : 'not found');
  const replyInput = document.querySelector('input[placeholder="輸入或搜尋主題..."]');
  console.log('Reply input found:', replyInput ? replyInput.value : 'not found');
  const categoryInput = document.querySelector('input[placeholder="輸入或搜尋分類..."]');
  console.log('Category input found:', categoryInput ? categoryInput.value : 'not found');
};