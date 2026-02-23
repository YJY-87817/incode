import { createApp } from "vue";
import { createPinia } from "pinia";
import piniaPluginPersistedstate from "pinia-plugin-persistedstate";
import App from "./App.vue";

import "@fortawesome/fontawesome-free/css/all.css";

import router from "./router";

//owen
import "vuetify/styles";
import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import "@mdi/font/css/materialdesignicons.css";

const vuetify = createVuetify({
  components,
  directives,
});

// Nucleo Icons
import "./assets/css/nucleo-icons.css";
import "./assets/css/nucleo-svg.css";

//Bootstrap
import { createBootstrap } from "bootstrap-vue-next";
import "bootstrap/dist/css/bootstrap.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "bootstrap-vue-next/dist/bootstrap-vue-next.css";
import "bootstrap-icons/font/bootstrap-icons.css";

import materialKit from "./material-kit";
import axios from "axios";
axios.defaults.withCredentials = true;
axios.defaults.baseURL = import.meta.env.VITE_API_BASE_URL;

// Axios 攔截器(避免Pinia 和 Cookie 的「資訊不對稱」
axios.interceptors.response.use(
  (response) => {
    // 正常回傳 200 等成功狀態，直接放行
    return response;
  },
  (error) => {
    // 檢查是否有回傳錯誤，並且狀態碼是 401 (Cookie 過期或未登入)
    if (error.response && error.response.status === 401) {

      const authStore = useAuthStore();

      // 強制清空 Pinia 裡的登入狀態
      authStore.user = null;
      authStore.isAuthenticated = false;

      Swal.fire({
        icon: "warning",
        title: "登入已逾時",
        text: "為了您的帳號安全，請重新登入",
        timer: 2000,
        showConfirmButton: false,
      });

      router.push({ name: "Login" });
    }

    // 將錯誤繼續往外拋，讓元件自己的 catch 也能抓到 (如果需要的話)
    return Promise.reject(error);
  }
);

const app = createApp(App);

const pinia = createPinia();

pinia.use(piniaPluginPersistedstate); //piniaPluginPersist舊版

app.use(pinia);
app.use(router);
app.use(materialKit);
app.use(vuetify);
app.use(createBootstrap());
app.mount("#app");
