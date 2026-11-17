<script setup>
  // Email 驗證信的連結：/verify-email?email=…&token=…
  import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
  import { onMounted, ref } from "vue";
  import { useRoute } from "vue-router";
  import axios from "axios";

  const route = useRoute();
  const status = ref("loading"); // loading | success | error
  const message = ref("");

  onMounted(async () => {
    const { email, token } = route.query;
    if (!email || !token) {
      status.value = "error";
      message.value = "驗證連結不完整，請從會員中心重寄驗證信。";
      return;
    }
    try {
      const res = await axios.post("/AuthApi/verify-email", { email, token });
      status.value = "success";
      message.value = res.data?.message || "Email 驗證成功";
    } catch (error) {
      status.value = "error";
      message.value = error.response?.data?.message || "驗證失敗，請稍後再試";
    }
  });
</script>

<template>
  <DefaultNavbar light />
  <v-container class="mt-8">
    <v-card class="mx-auto pa-8 text-center" elevation="0" max-width="450" rounded="xl" border>
      <h2 class="text-h5 font-weight-bold mb-4 text-primary">Email 驗證</h2>

      <v-progress-circular v-if="status === 'loading'" indeterminate color="primary"></v-progress-circular>

      <template v-else>
        <v-icon :color="status === 'success' ? 'success' : 'grey'" size="56" class="mb-3">
          {{ status === "success" ? "mdi-check-circle" : "mdi-email-outline" }}
        </v-icon>
        <p class="text-body-1 mb-6">{{ message }}</p>
        <div class="d-flex justify-center ga-2">
          <v-btn variant="flat" color="#145369" class="text-white" rounded="pill" :to="{ name: 'UserIndex' }">
            前往會員中心
          </v-btn>
          <v-btn variant="text" rounded="pill" to="/">回首頁</v-btn>
        </div>
      </template>
    </v-card>
  </v-container>
</template>
