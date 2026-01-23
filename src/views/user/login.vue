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
import Register from "@/views/user/Register.vue";

const rules = {
  required: (value) => !!value || "此欄位必填",

  // Email 格式規則 (簡易版 regex)
  email: (value) => {
    const pattern = /^[^@]+@[^@]+\.[^@]+$/;
    return pattern.test(value) || "Email 格式不正確";
  },
};

const handleLogin = () => {
  if (!formValid.value) return;
  console.log("登入資料:", email.value, password.value);
  // 這裡呼叫你的 API
};

onMounted(() => {
  setMaterialInput();
  authStore.resetForm(); // 進入登入頁面時重置清掉 Store 裡的殘留資料
});
</script>
<template>
  <v-form v-model="formValid" @submit.prevent="handleLogin">
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
        v-model="email"
        density="compact"
        placeholder="Email"
        prepend-inner-icon="mdi-email-outline"
        variant="outlined"
        :rules="[rules.required, rules.email]"
      ></v-text-field>

      <div
        class="text-subtitle-1 text-medium-emphasis d-flex align-center justify-space-between"
      >
        密碼
      </div>
      <v-text-field
        v-model="password"
        :append-inner-icon="visible ? 'mdi-eye-off' : 'mdi-eye'"
        :type="visible ? 'text' : 'password'"
        density="compact"
        placeholder="password"
        prepend-inner-icon="mdi-lock-outline"
        variant="outlined"
        @click:append-inner="visible = !visible"
        :rules="[rules.required]"
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
  </v-form>
</template>
