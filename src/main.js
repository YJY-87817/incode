import { createApp } from "vue";
import { createPinia } from "pinia";
import App from "./App.vue";

import '@fortawesome/fontawesome-free/css/all.css'
import '@fortawesome/fontawesome-free/js/all.js'

import router from "./router";

// Nucleo Icons
import "./assets/css/nucleo-icons.css";
import "./assets/css/nucleo-svg.css";

import materialKit from "./material-kit";
import axios from 'axios';
axios.defaults.withCredentials = true;

const app = createApp(App);


app.use(createPinia());
app.use(router);
app.use(materialKit);
app.mount("#app");
