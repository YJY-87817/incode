<script setup>
  import { ref, onMounted } from "vue";
  import { useRouter } from "vue-router";
  import { useAuthStore } from "@/stores/user/auth";

  const router = useRouter();
  const authStore = useAuthStore();

  // 定義響應式變數 (原本的程式碼漏定義了這些 ref，這裡補上)
  const formValid = ref(false);
  const email = ref("");
  const password = ref("");
  const rememberMe = ref(false);
  const visible = ref(false);
  const isLoading = ref(false);

  const rules = {
    required: (value) => !!value || "此欄位必填",
    email: (value) => {
      const pattern = /^[^@]+@[^@]+\.[^@]+$/;
      return pattern.test(value) || "Email 格式不正確";
    },
  };

  const handleLogin = async () => {
    // 觸發表單驗證
    if (!formValid.value) return;

    isLoading.value = true;

    try {
      console.log("登入資料:", email.value, password.value);

      // 模擬 API 請求延遲
      await new Promise(resolve => setTimeout(resolve, 1500));

      // 這裡呼叫您的實際 API
      // await authStore.login({ email: email.value, password: password.value });

      // 成功後跳轉 (假設)
      alert("登入成功 (前端模擬)");
      // router.push({ name: 'UserIndex' });

    } catch (error) {
      console.error("登入失敗", error);
    } finally {
      isLoading.value = false;
    }
  };

  onMounted(() => {
    // 如果有引入 material-input js 可保留，但在 Vuetify 3 通常不需要
    // setMaterialInput(); 
    authStore.resetForm();
  });
</script>

<template>
  <v-container fluid class="fill-height bg-grey-lighten-5">

    <div class="d-flex flex-column align-center w-100">

      <div class="text-center mb-6">
        <v-avatar color="primary" size="60" class="mb-2">
          <span class="text-h5 text-white font-weight-bold">In</span>
        </v-avatar>
        <h1 class="text-h5 font-weight-bold text-grey-darken-2">歡迎回來</h1>
      </div>

      <v-card class="mx-auto pa-8 pa-md-10" elevation="0" max-width="450" width="100%" rounded="xl" border>
        <h2 class="text-h5 font-weight-bold mb-6 text-primary text-center">會員登入</h2>

        <v-form v-model="formValid" @submit.prevent="handleLogin">

          <div class="mb-4">
            <v-text-field v-model="email" label="帳號 (Email)" placeholder="請輸入您的 Email"
              prepend-inner-icon="mdi-email-outline" variant="outlined" density="comfortable" color="primary"
              :rules="[rules.required, rules.email]"></v-text-field>
          </div>

          <div class="mb-2">
            <v-text-field v-model="password" label="密碼" placeholder="請輸入密碼" :type="visible ? 'text' : 'password'"
              :append-inner-icon="visible ? 'mdi-eye-off' : 'mdi-eye'" prepend-inner-icon="mdi-lock-outline"
              variant="outlined" density="comfortable" color="primary" :rules="[rules.required]"
              @click:append-inner="visible = !visible"></v-text-field>
          </div>

          <div class="d-flex align-center justify-space-between mb-6">
            <v-checkbox v-model="rememberMe" label="記住我" color="primary" hide-details density="compact"
              class="ma-0 pa-0"></v-checkbox>

            <a href="#" class="text-caption text-primary text-decoration-none font-weight-bold">
              忘記密碼?
            </a>
          </div>

          <v-btn type="submit" color="primary" size="large" variant="flat" block rounded="lg"
            class="text-body-1 font-weight-bold mb-4" :loading="isLoading">
            登入
          </v-btn>

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
  /* 讓 checkbox 的 label 文字稍微小一點，更精緻 */
  :deep(.v-label) {
    font-size: 0.875rem;
    color: #666;
  }
</style>