<template>
  <h1>測試</h1>
  <Profile />
</template>

<script setup>
import { onMounted, onUnmounted, ref } from "vue";
import Profile from "@/views/user/Sections/userProfile.vue";
import axios from "axios";
const authStore = useAuthStore();
const API_BASE_URL = "https://localhost:7190/api";
</script>

<!-- <Posts />
  <Information />
  <Profile />
   <AboutTeam /> 
   import Information from "@/views/user/Sections/userInformation.vue";

import Posts from "@/views/user/Sections/userPosts.vue";
   -->
