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
import ChatWidget from "@/components/ChatWidget.vue";
// 網頁重新整理時，執行一次身分恢復
import { onMounted } from "vue";
import { useAuthStore } from "@/stores/user/auth";

const authStore = useAuthStore();

onMounted(() => {
  authStore.checkLoginStatus();
});
</script>

<template>
  <router-view />
  <chat-widget />
</template>
