import { createApp } from "vue";
import { createPinia } from "pinia";
import piniaPluginPersistedstate from "pinia-plugin-persistedstate";
import App from "./App.vue";

import "@fortawesome/fontawesome-free/css/all.css";
import "@fortawesome/fontawesome-free/js/all.js";

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
import "bootstrap-vue-next/dist/bootstrap-vue-next.css";
import "bootstrap-icons/font/bootstrap-icons.css";

import materialKit from "./material-kit";
import axios from "axios";
axios.defaults.withCredentials = true;
axios.defaults.baseURL = "https://localhost:7190/api";

const app = createApp(App);

const pinia = createPinia();

pinia.use(piniaPluginPersistedstate); //piniaPluginPersist舊版

app.use(pinia);
app.use(router);
app.use(materialKit);
app.use(vuetify);
app.use(createBootstrap());
app.mount("#app");
