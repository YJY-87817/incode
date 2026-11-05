import { createApp } from "vue";
import { createPinia } from "pinia";
import piniaPluginPersistedstate from "pinia-plugin-persistedstate";
import App from "@/App.vue";

import "@fortawesome/fontawesome-free/css/all.css";

import router from "@/router";

import "vuetify/styles";
import { createVuetify } from "vuetify";
import { iconsOptions } from "@/plugins/vuetifyIcons.js";

// 元件 / 指令由 vite-plugin-vuetify 依模板實際使用自動匯入（不要再 import * 全部元件）；
// 圖示改用 SVG，只打包用到的（見 plugins/vuetifyIcons.js）
const vuetify = createVuetify({ icons: iconsOptions });

// Nucleo Icons
import "@/assets/css/nucleo-icons.css";
import "@/assets/css/nucleo-svg.css";

//Bootstrap
import "bootstrap/dist/css/bootstrap.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "bootstrap-icons/font/bootstrap-icons.css";

import materialKit from "@/material-kit";
import axios from "axios";
import { installAuthInterceptor } from "@/api/authInterceptor.js";
axios.defaults.withCredentials = true;
axios.defaults.baseURL = import.meta.env.VITE_API_BASE_URL;

// 401 處理（登入逾時時清除 Pinia 狀態並導向登入頁），apiClient / pointClient 也使用同一個攔截器
installAuthInterceptor(axios, { redirectWhenAnonymous: true });

const app = createApp(App);

const pinia = createPinia();

pinia.use(piniaPluginPersistedstate); //piniaPluginPersist舊版

app.use(pinia);
app.use(router);
app.use(materialKit);
app.use(vuetify);
app.mount("#app");
