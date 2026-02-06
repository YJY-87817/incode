<script setup>
import { computed } from "vue";
import UserPreview from "@/views/user/components/UserPreview.vue";

const props = defineProps({
  userId: { type: [Number, String], default: null },
  username: { type: String, default: "" },
  avatarUrl: { type: String, default: "" },
  size: { type: [Number, String], default: 24 },
});

const displayName = computed(() => props.username || "User");

const avatarSrc = computed(() => {
  return props.avatarUrl || "https://cdn.vuetifyjs.com/images/john.jpg";
});

const numericSize = computed(() => {
  const parsed = Number(props.size);
  return Number.isNaN(parsed) || parsed <= 0 ? 24 : parsed;
});

const avatarStyle = computed(() => ({
  width: `${numericSize.value}px`,
  height: `${numericSize.value}px`,
}));
</script>

<template>
  <span class="user-hover-proxy" @click.stop>
    <img class="user-avatar-img" :src="avatarSrc" :style="avatarStyle" alt="" />
    <span>{{ displayName }}</span>
    <UserPreview
      v-if="userId"
      :user-id="userId"
      :username="displayName"
      :avatar-url="avatarSrc"
      :size="numericSize"
    />
  </span>
</template>

<style scoped>
.user-hover-proxy {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.user-avatar-img {
  border-radius: 999px;
  object-fit: cover;
}

.user-hover-proxy :deep(.v-avatar) {
  position: absolute;
  inset: 0;
  width: 100% !important;
  height: 100% !important;
  opacity: 0;
  cursor: pointer;
}
</style>
