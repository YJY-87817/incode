<script setup>
import { onMounted, onUnmounted } from "vue";
import axios from "axios";
import { useRouter } from "vue-router";
import { useUserStore } from "@/stores/user/userStore";
import Posts from "@/views/user/Sections/userPosts.vue";
import Profile from "@/views/user/Sections/UserProfile.vue";
import UserHeader from "@/views/user/components/UserHeader.vue";

const userStore = useUserStore();
const router = useRouter();

onMounted(() => {
  userStore.fetchUserProfile();
});
</script>

<template></template>
