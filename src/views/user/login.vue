<script setup>
import { ref, onMounted } from "vue";
import axios from "axios";
import { useRouter } from "vue-router"; // 引入路由以便登入後跳轉
const router = useRouter();
const visible = ref(false);
import { useAuthStore } from "@/stores/user/auth";
const authStore = useAuthStore();
import setMaterialInput from "@/assets/js/material-input";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import Header from "@/examples/Header.vue";
import Register from "@/views/user/Register.vue";
import UserInput from "@/views/user/components/UserInput.vue";
import UserSwitch from "@/views/user/components/UserSwitch.vue";
import UserButton from "@/views/user/components/UserButton.vue";

import { shallowRef } from "vue";
const dialog = shallowRef(false);
const dialog2 = shallowRef(false);
const dialog3 = shallowRef(false);

onMounted(() => {
  setMaterialInput();
  authStore.resetForm(); // 進入登入頁面時，最好重置一下 Store 裡的殘留資料
});

// 定義登入按鈕的動作
const handleLogin = async () => {
  const success = await authStore.login(); // 呼叫 Store 的 login 方法
  if (success) {
    router.push({ path: "/userindex" }); // 登入成功後跳轉
  }
};
</script>
<template>
  <div>
    <v-img class="mx-auto my-6" max-width="228" src="" alt="網站logo"
      >網站logo</v-img
    >
    <v-card
      title="會員登入"
      class="mx-auto pa-12 pb-8"
      elevation="8"
      max-width="448"
      rounded="lg"
    >
      <div class="text-subtitle-1 text-medium-emphasis mt-3">帳號</div>

      <v-text-field
        density="compact"
        placeholder="Email"
        prepend-inner-icon="mdi-email-outline"
        variant="outlined"
      ></v-text-field>

      <div
        class="text-subtitle-1 text-medium-emphasis d-flex align-center justify-space-between"
      >
        密碼
      </div>
      <v-text-field
        :append-inner-icon="visible ? 'mdi-eye-off' : 'mdi-eye'"
        :type="visible ? 'text' : 'password'"
        density="compact"
        placeholder="password"
        prepend-inner-icon="mdi-lock-outline"
        variant="outlined"
        @click:append-inner="visible = !visible"
      >
      </v-text-field>
      <v-checkbox v-model="terms" color="secondary" label="記住我"></v-checkbox>
      <v-btn class="mb-2" color="blue" size="large" variant="tonal" block>
        登入
      </v-btn>
      <v-card-text class="text-center">
        <router-link :to="{ name: 'Register' }">註冊</router-link>
        <!-- <a class="text-blue text-decoration-none" href="#" rel="noopener noreferrer" target="_blank">
           <v-icon icon="mdi-chevron-right"></v-icon>
        </a> -->
      </v-card-text>
      <v-card class="mb-3" color="surface-variant" variant="tonal">
        <v-card-text class="text-medium-emphasis text-caption">
          備註:之後做忘記密碼
        </v-card-text>
      </v-card>
    </v-card>
  </div>
</template>
