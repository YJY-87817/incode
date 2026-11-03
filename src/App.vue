<script setup>
/*
  =========================================================
  * Vue Material Kit 2 - v2.0.0
  =========================================================
  
  * Product Page: https://www.creative-tim.com/product/vue-material-kit
  * Copyright 2022 Creative Tim (https://www.creative-tim.com)
  
  Coded by www.creative-tim.com
  
   =========================================================
  
  * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
  */

/*ertrtre*/
import { RouterView } from "vue-router";
import ChatWidget from "@/components/chat/ChatWidget.vue";
import Cart from "@/components/pointshop/Cart.vue";
import { onMounted } from "vue";
import { useAuthStore } from "@/stores/users/auth";

const authStore = useAuthStore();

  onMounted(async () => {
    console.log("App 啟動，檢查登入狀態...");
    await authStore.checkAuth();
  });
</script>

<template>
  <router-view />
  <Cart />
  <chat-widget />
</template>