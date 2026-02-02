<script setup>
  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import { ref } from "vue";
  import { useRouter } from "vue-router";
  import { useAuthStore } from "@/stores/user/auth";
  import { useAlertStore } from "@/stores/alert";

  const alertStore = useAlertStore();
  const router = useRouter();
  const authStore = useAuthStore();
  const isLoading = ref(false);
  const errorMessage = ref("");
  const rememberMe = ref(false);
  const visible = ref(false);
  const formValid = ref(false);
  const email = ref("");
  const password = ref("");

  const rules = {
    required: (value) => !!value || "此欄位必填",
    email: (value) => {
      const pattern = /^[^@]+@[^@]+\.[^@]+$/;
      return pattern.test(value) || "Email 格式不正確";
    },
  };

  const handleLogin = async () => {
    if (!formValid.value) return;
    isLoading.value = true;
    errorMessage.value = "";
    try {
      const loginData = {
        email: email.value,
        password: password.value,
        rememberMe: rememberMe.value,
      };
      //console.log("正在嘗試登入...", loginData);
      const success = await authStore.login(loginData);
      if (success) {
        alertStore.showSuccess("登入成功，歡迎回來！");
        //console.log("準備跳轉頁面...");
        await router.push("/userindex");
      }
    } catch (error) {
      //console.error("登入失敗:", error);
      let msg = "";
      if (error.response && error.response.data) {
        msg = error.response.data.message || "帳號或密碼錯誤";
      } else {
        msg = "伺服器無回應，請稍後再試";
      }
      alertStore.showError(msg);
    } finally {
      isLoading.value = false;
    }
  };
</script>

<template>
  <DefaultNavbar />
  <v-container fluid class="fill-height bg-grey-lighten-5">
    <div class="d-flex flex-column align-center w-100">
      <div class="text-center mb-4">
        <v-avatar color="#86B0BD" size="60" class="mb-2">
          <span class="text-h5 text-white font-weight-bold">In</span>
        </v-avatar>
        <h1 class="text-h5 font-weight-bold text-grey-darken-2">歡迎回來</h1>
      </div>

      <v-card class="mx-auto pa-8 pa-md-10" elevation="0" max-width="450" width="100%" rounded="xl" border>
        <h2 class="text-h5 font-weight-bold mb-4 text-primary text-center">
          會員登入
        </h2>

        <v-form v-model="formValid" @submit.prevent="handleLogin">
          <div class="mb-4">
            <v-text-field v-model="email" label="帳號 (Email)" placeholder="請輸入您的 Email"
              prepend-inner-icon="mdi-email-outline" variant="outlined" density="comfortable" color="primary"
              :rules="[rules.required, rules.email]" autocomplete="username"></v-text-field>
          </div>

          <div class="mb-2">
            <v-text-field v-model="password" label="密碼" placeholder="請輸入密碼" :type="visible ? 'text' : 'password'"
              :append-inner-icon="visible ? 'mdi-eye-off' : 'mdi-eye'" prepend-inner-icon="mdi-lock-outline"
              variant="outlined" density="comfortable" color="primary" :rules="[rules.required]"
              @click:append-inner="visible = !visible" autocomplete="current-password"></v-text-field>
          </div>

          <div class="d-flex align-center justify-space-between mb-4">
            <v-checkbox v-model="rememberMe" label="記住我" color="primary" hide-details density="compact"
              class="ma-0 pa-0"></v-checkbox>

            <a href="#" class="text-caption text-primary text-decoration-none font-weight-bold">
              忘記密碼?
            </a>
          </div>
          <v-alert v-if="errorMessage" type="error" variant="tonal" density="compact" class="mb-4 text-caption" closable
            @click:close="errorMessage = ''">
            {{ errorMessage }}
          </v-alert>
          <v-hover v-slot="{ isHovering, props }">
            <v-btn v-bind="props" type="submit" size="large" block rounded="lg" variant="flat"
              class="text-body-1 font-weight-bold mb-4" :loading="isLoading" :color="isHovering ? '#FFFFFF' : '#145369'"
              :style="{ 
      color: isHovering ? '#145369 !important' : '#FFFFFF !important',
      border: '1px solid #145369'
    }">
              登入
            </v-btn>
          </v-hover>
          <div class="text-center">
            <span class="text-caption text-grey">還沒有帳號嗎？</span>
            <router-link :to="{ name: 'Register' }"
              class="text-caption font-weight-bold text-primary text-decoration-none ms-1">
              立即註冊
            </router-link>
          </div>
        </v-form>
      </v-card>

      <div class="mt-4 text-caption text-grey-lighten-1">
        &copy; 2024 Incode Inc.
      </div>
    </div>
  </v-container>
</template>

<style scoped>
  :deep(.v-label) {
    font-size: 0.875rem;
    color: #666;
  }
</style>