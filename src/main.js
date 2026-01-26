import { createApp } from "vue";
import { createPinia } from "pinia";
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import App from "./App.vue";
import '@fortawesome/fontawesome-free/css/all.css'
import '@fortawesome/fontawesome-free/js/all.js'
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
import materialKit from "./material-kit";
import axios from 'axios';
axios.defaults.withCredentials = true;

const app = createApp(App);
const pinia = createPinia();

pinia.use(piniaPluginPersistedstate); //piniaPluginPersist舊版

app.use(pinia);
app.use(router);
app.use(materialKit);
app.use(vuetify)
app.mount("#app");
