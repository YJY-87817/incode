<script setup>
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import { ref, computed, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "@/stores/users/auth";
import Swal from "sweetalert2";
import axios from "axios";

const router = useRouter();
const route = useRoute(); // 用來抓取 redirect 參數
const authStore = useAuthStore();
const isLoading = ref(false);

// 0 = 登入, 1 = 註冊
const step = ref(0);

// 動態調整卡片寬度：登入時窄一點，註冊時因為有兩欄所以寬一點
const cardMaxWidth = computed(() => (step.value === 1 ? 800 : 450));

const loginFormValid = ref(false);
const rememberMe = ref(false);
const loginVisible = ref(false); // 控制登入密碼顯示
const loginEmail = ref("");
const loginPassword = ref("");

const loginRules = {
  required: (value) => !!value || "此欄位必填",
  email: (value) => {
    const pattern = /^[^@]+@[^@]+\.[^@]+$/;
    return pattern.test(value) || "Email 格式不正確";
  },
};

const handleGoogleLogin = () => {
  // 這裡不能用 axios，必須直接讓瀏覽器跳轉
  window.location.href = "https://localhost:7190/api/AuthApi/login-google";
};

const handleLogin = async () => {
  if (!loginFormValid.value) return;

  isLoading.value = true;

  try {
    const loginData = {
      email: loginEmail.value,
      password: loginPassword.value,
      rememberMe: rememberMe.value,
    };

    const success = await authStore.login(loginData);

    if (success) {
      Swal.fire({
        title: "登入成功!",
        icon: "success",
        timer: 1500,
        showConfirmButton: false,
      });

      // 檢查是否有「原本想去但被擋下的頁面」，沒有就回 userindex
      const redirectPath = route.query.redirect || "/userindex";
      await router.push(redirectPath);
    }
  } catch (error) {
    let msg = "";
    if (error.response && error.response.data) {
      msg = error.response.data.message || "帳號或密碼錯誤";
    } else {
      msg = "伺服器無回應，請稍後再試";
    }
    Swal.fire({
      icon: "error",
      title: "登入失敗",
      text: msg,
    });
  } finally {
    isLoading.value = false;
  }
};

const regFormRef = ref(null); // 對應 form ref
const showRegPassword = ref(false);
const showRegConfirmPassword = ref(false);
const showWelcomeDialog = ref(false);

// 註冊表單資料
const regFormData = ref({
  email: "",
  password: "",
  confirmPassword: "",
  username: "",
  birth: "",
  imageUrl: "",
});

// 檔案上傳相關
const selectedFile = ref(null);
const previewUrl = ref(null);
const isUploading = ref(false); // 圖片上傳 loading

const emailRules = [
  (v) => !!v || "Email 為必填欄位",
  (v) => /.+@.+\..+/.test(v) || "Email 格式不正確",
];
const passwordRules = [
  (v) => !!v || "密碼為必填欄位",
  (v) => (v && v.length >= 6) || "密碼長度需至少 6 個字元",
];
const confirmPasswordRules = [
  (v) => !!v || "確認密碼為必填欄位",
  (v) => v === regFormData.value.password || "兩次輸入的密碼不一致",
];

// 處理檔案選擇與預覽
const handleFileChange = (files) => {
  // Vuetify 3 file-input 有時回傳陣列，有時是 Event，需注意版本
  // 這裡假設是直接綁定 modelValue (陣列)
  const file = Array.isArray(files) ? files[0] : files;

  if (file) {
    if (file.size > 2 * 1024 * 1024) {
      Swal.fire({
        icon: "warning",
        title: "檔案過大",
        text: "請選擇小於 2MB 的圖片",
      });
      selectedFile.value = null;
      previewUrl.value = null;
      return;
    }
    selectedFile.value = file;
    previewUrl.value = URL.createObjectURL(file);
  } else {
    selectedFile.value = null;
    previewUrl.value = null;
  }
};

const uploadImage = async () => {
  if (!selectedFile.value) {
    Swal.fire({
      icon: "info",
      text: "請先選擇一張圖片",
    });
    return;
  }

  isUploading.value = true;

  // 建立 FormData 把檔案包起來
  const formData = new FormData();
  formData.append("files", selectedFile.value); // 這裡的 'files' 必須對應後端接收的參數名

  try {
    const response = await axios.post("/User/images", formData);

    // 將後端回傳的圖片網址存到表單資料中，準備跟著註冊一起送出
    if (
      response.data &&
      response.data.imageUrls &&
      response.data.imageUrls.length > 0
    ) {
      regFormData.value.imageUrl = response.data.imageUrls[0];

      Swal.fire({
        title: "圖片上傳成功！",
        icon: "success",
        timer: 1500,
        showConfirmButton: false,
      });
    }
  } catch (error) {
    console.error("圖片上傳失敗", error);
    Swal.fire({
      icon: "error",
      title: "上傳失敗",
      text: error.response?.data?.message || "請檢查網路狀態或圖片大小",
    });
  } finally {
    isUploading.value = false;
  }
};

const handleRegister = async () => {
  // 先驗證表單
  const { valid } = await regFormRef.value.validate();
  if (!valid) return;

  isLoading.value = true; // 使用共用的 loading
  try {
    const success = await authStore.register(regFormData.value);
    if (success) {
      showWelcomeDialog.value = true;
    }
  } catch (error) {
    const errorMsg =
      error.response?.data?.message || "註冊發生錯誤，請稍後再試";
    Swal.fire({
      icon: "error",
      title: "註冊失敗",
      text: errorMsg,
    });
  } finally {
    isLoading.value = false;
  }
};

// 1. 新增忘記密碼的表單資料
const forgotPasswordEmail = ref("");
const forgotPasswordLoading = ref(false);

// 2. 處理發送重設信的邏輯
const handleForgotPassword = async () => {
  // 簡單驗證
  if (!forgotPasswordEmail.value) {
    Swal.fire({ icon: "warning", text: "請輸入 Email" });
    return;
  }
  const emailPattern = /^[^@]+@[^@]+\.[^@]+$/;
  if (!emailPattern.test(forgotPasswordEmail.value)) {
    Swal.fire({ icon: "warning", text: "Email 格式不正確" });
    return;
  }

  forgotPasswordLoading.value = true;
  try {
    await authStore.forgotPassword(forgotPasswordEmail.value);

    Swal.fire({
      icon: "success",
      title: "郵件已發送",
      text: "請前往信箱查收重設密碼連結",
      timer: 2000,
      showConfirmButton: false,
    });

    // 發送成功後，自動切換回登入頁 (step 0)
    step.value = 0;
    forgotPasswordEmail.value = ""; // 清空欄位
  } catch (error) {
    let errorMsg = "請稍後再試";
    if (error.response && error.response.data && error.response.data.message) {
      errorMsg = error.response.data.message;
    }
    Swal.fire({
      icon: "error",
      title: "發送失敗",
      text: errorMsg,
    });
  } finally {
    forgotPasswordLoading.value = false;
  }
};

onMounted(() => {
  // 檢查網址參數，如果是 'register' 就切換到註冊分頁 (step 1)
  if (route.query.tab === "register") {
    step.value = 1;
  }
});
</script>

<template>
  <DefaultNavbar light />

  <v-container fluid class="fill-height bg-grey-lighten-5 mt-4">
    <div class="d-flex flex-column align-center w-100">
      <div class="text-center mb-4">
        <h1 class="text-h5 font-weight-bold text-grey-darken-2">
          {{ step === 0 ? "歡迎回來" : "加入我們" }}
        </h1>
      </div>

      <v-card
        class="mx-auto pa-8 pa-md-10 trans-width"
        elevation="0"
        :max-width="cardMaxWidth"
        width="100%"
        rounded="xl"
        border
      >
        <v-window v-model="step">
          <v-window-item :value="0">
            <h2 class="text-h5 font-weight-bold mb-4 text-primary text-center">
              會員登入
            </h2>

            <v-form v-model="loginFormValid" @submit.prevent="handleLogin">
              <div class="mb-4">
                <v-text-field
                  v-model="loginEmail"
                  label="帳號 (Email)"
                  placeholder="請輸入您的 Email"
                  prepend-inner-icon="mdi-email-outline"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  :rules="[loginRules.required, loginRules.email]"
                  autocomplete="username"
                ></v-text-field>
              </div>

              <div class="mb-2">
                <v-text-field
                  v-model="loginPassword"
                  label="密碼"
                  placeholder="請輸入密碼"
                  :type="loginVisible ? 'text' : 'password'"
                  :append-inner-icon="loginVisible ? 'mdi-eye-off' : 'mdi-eye'"
                  prepend-inner-icon="mdi-lock-outline"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  :rules="[loginRules.required]"
                  @click:append-inner="loginVisible = !loginVisible"
                  autocomplete="current-password"
                ></v-text-field>
              </div>

              <div class="d-flex align-center justify-space-between mb-4">
                <v-checkbox
                  v-model="rememberMe"
                  label="記住我"
                  color="primary"
                  hide-details
                  density="compact"
                  class="ma-0 pa-0"
                ></v-checkbox>

                <a
                  href="#"
                  class="text-caption text-primary text-decoration-none font-weight-bold"
                  @click.prevent="step = 2"
                >
                  忘記密碼?
                </a>
              </div>

              <v-hover v-slot="{ isHovering, props }">
                <v-btn
                  v-bind="props"
                  type="submit"
                  size="large"
                  block
                  rounded="lg"
                  variant="flat"
                  class="text-body-1 font-weight-bold mb-4"
                  :loading="isLoading"
                  :color="isHovering ? '#FFFFFF' : '#145369'"
                  :style="{
                    color: isHovering
                      ? '#145369 !important'
                      : '#FFFFFF !important',
                    border: '1px solid #145369',
                  }"
                >
                  登入
                </v-btn>
              </v-hover>

              <v-btn
                block
                variant="outlined"
                color="#DB4437"
                class="mb-4 text-body-1 font-weight-bold"
                prepend-icon="mdi-google"
                @click="handleGoogleLogin"
              >
                使用 Google 帳號登入
              </v-btn>

              <div class="text-center">
                <span class="text-caption text-grey">還沒有帳號嗎？</span>
                <a
                  href="#"
                  @click.prevent="step = 1"
                  class="text-caption font-weight-bold text-primary text-decoration-none ms-1"
                >
                  立即註冊
                </a>
              </div>
            </v-form>
          </v-window-item>

          <v-window-item :value="1">
            <v-row>
              <v-col cols="12" md="7" class="pe-md-6">
                <h2 class="text-h5 font-weight-bold mb-4 text-primary">
                  註冊帳號
                </h2>
                <v-form ref="regFormRef" @submit.prevent="handleRegister">
                  <v-row dense>
                    <v-col cols="12">
                      <v-text-field
                        v-model="regFormData.email"
                        :rules="emailRules"
                        label="*帳號 (Email)*"
                        placeholder="name@example.com"
                        variant="outlined"
                        density="comfortable"
                        color="primary"
                        prepend-inner-icon="mdi-email-outline"
                      ></v-text-field>
                    </v-col>

                    <v-col cols="12">
                      <v-text-field
                        v-model="regFormData.username"
                        label="暱稱"
                        placeholder="怎麼稱呼您?"
                        variant="outlined"
                        density="comfortable"
                        color="primary"
                        prepend-inner-icon="mdi-account-outline"
                      ></v-text-field>
                    </v-col>

                    <v-col cols="12">
                      <v-text-field
                        v-model="regFormData.birth"
                        label="生日"
                        placeholder="YYYY-MM-DD"
                        variant="outlined"
                        density="comfortable"
                        color="primary"
                        prepend-inner-icon="mdi-cake-variant-outline"
                      ></v-text-field>
                    </v-col>

                    <v-col cols="12">
                      <v-text-field
                        v-model="regFormData.password"
                        :rules="passwordRules"
                        label="*密碼*"
                        :type="showRegPassword ? 'text' : 'password'"
                        :append-inner-icon="
                          showRegPassword ? 'mdi-eye' : 'mdi-eye-off'
                        "
                        @click:append-inner="showRegPassword = !showRegPassword"
                        variant="outlined"
                        density="comfortable"
                        color="primary"
                        prepend-inner-icon="mdi-lock-outline"
                      ></v-text-field>
                    </v-col>

                    <v-col cols="12">
                      <v-text-field
                        v-model="regFormData.confirmPassword"
                        :rules="confirmPasswordRules"
                        label="*確認密碼*"
                        :type="showRegConfirmPassword ? 'text' : 'password'"
                        :append-inner-icon="
                          showRegConfirmPassword ? 'mdi-eye' : 'mdi-eye-off'
                        "
                        @click:append-inner="
                          showRegConfirmPassword = !showRegConfirmPassword
                        "
                        variant="outlined"
                        density="comfortable"
                        color="primary"
                        prepend-inner-icon="mdi-lock-check-outline"
                      ></v-text-field>
                    </v-col>
                  </v-row>
                </v-form>
              </v-col>

              <v-col
                cols="12"
                md="5"
                class="d-flex flex-column align-center justify-center ps-md-4 border-start-md"
              >
                <div class="text-center w-100">
                  <h3
                    class="text-subtitle-1 font-weight-bold mb-4 text-grey-darken-1"
                  >
                    設定個人大頭貼
                  </h3>

                  <v-card
                    class="mx-auto mb-6 d-flex align-center justify-center rounded-circle"
                    width="180"
                    height="180"
                    elevation="0"
                    color="grey-lighten-4"
                    style="border: 2px dashed #ccc; overflow: hidden"
                  >
                    <v-img
                      v-if="previewUrl"
                      :src="previewUrl"
                      cover
                      class="w-100 h-100"
                    ></v-img>
                    <v-icon v-else size="60" color="grey-lighten-1"
                      >mdi-camera-plus</v-icon
                    >
                  </v-card>

                  <v-file-input
                    v-model="selectedFile"
                    accept="image/*"
                    label="點擊選擇圖片"
                    variant="outlined"
                    density="compact"
                    prepend-icon=""
                    prepend-inner-icon="mdi-image"
                    hide-details
                    class="mb-3"
                    @update:model-value="handleFileChange"
                  ></v-file-input>

                  <v-btn
                    v-if="selectedFile"
                    variant="tonal"
                    color="primary"
                    size="small"
                    block
                    :loading="isUploading"
                    @click="uploadImage"
                    class="mb-6"
                  >
                    <v-icon start>mdi-cloud-upload</v-icon>
                    確認上傳頭像
                  </v-btn>

                  <p class="text-caption text-grey">
                    建議尺寸: 500x500px <br />
                    支援格式: JPG, PNG
                  </p>
                </div>
              </v-col>
            </v-row>

            <v-divider class="my-4"></v-divider>

            <div class="d-flex flex-column gap-2">
              <v-hover v-slot="{ isHovering, props }">
                <v-btn
                  v-bind="props"
                  size="large"
                  block
                  rounded="lg"
                  variant="flat"
                  class="text-body-1 font-weight-bold"
                  elevation="2"
                  @click="handleRegister"
                  :loading="isLoading"
                  :color="isHovering ? '#FFFFFF' : '#145369'"
                  :style="{
                    color: isHovering
                      ? '#145369 !important'
                      : '#FFFFFF !important',
                    border: '1px solid #145369',
                  }"
                >
                  立即註冊
                </v-btn>
              </v-hover>

              <div class="text-center mt-2">
                <span class="text-caption text-grey">已經有帳號了嗎？</span>
                <a
                  href="#"
                  @click.prevent="step = 0"
                  class="text-caption font-weight-bold text-primary text-decoration-none ms-1"
                >
                  登入
                </a>
              </div>
            </div>
          </v-window-item>
          <v-window-item :value="2">
            <v-card-text class="pa-8">
              <div class="text-center mb-6">
                <h2 class="text-h5 font-weight-bold text-grey-darken-3">
                  重設密碼
                </h2>
                <p class="text-subtitle-1 text-grey">
                  輸入您的 Email 以接收重設連結
                </p>
              </div>

              <v-form @submit.prevent="handleForgotPassword">
                <v-text-field
                  v-model="forgotPasswordEmail"
                  label="Email"
                  placeholder="請輸入註冊時的 Email"
                  prepend-inner-icon="mdi-email-outline"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  class="mb-4"
                  :rules="[loginRules.required, loginRules.email]"
                ></v-text-field>

                <v-hover v-slot="{ isHovering, props }">
                  <v-btn
                    v-bind="props"
                    type="submit"
                    size="large"
                    block
                    rounded="lg"
                    variant="flat"
                    class="text-body-1 font-weight-bold"
                    :loading="forgotPasswordLoading"
                    :color="isHovering ? '#FFFFFF' : '#145369'"
                    :style="{
                      color: isHovering
                        ? '#145369 !important'
                        : '#FFFFFF !important',
                      border: '1px solid #145369',
                    }"
                  >
                    發送重設信
                  </v-btn>
                </v-hover>
              </v-form>

              <div class="text-center mt-6">
                <v-btn
                  variant="text"
                  size="small"
                  color="grey-darken-1"
                  prepend-icon="mdi-arrow-left"
                  @click="step = 0"
                >
                  返回登入
                </v-btn>
              </div>
            </v-card-text>
          </v-window-item>
        </v-window>
      </v-card>

      <div class="mt-4 text-caption text-grey-lighten-1">
        &copy; 2024 Incode Inc.
      </div>
    </div>

    <v-dialog v-model="showWelcomeDialog" max-width="500" persistent>
      <v-card rounded="xl" class="text-center pa-6">
        <div class="d-flex align-center justify-center mb-2">
          <v-icon size="60" color="success" class="ml-4">
            mdi-check-circle
          </v-icon>
          <h2 class="text-h5 font-weight-bold">歡迎加入 Incode!</h2>
        </div>
        <p class="text-body-2 text-grey mb-4">
          您的帳號已建立成功，還可以去討論區或任務大廳看看哦。
        </p>

        <div class="d-flex justify-center">
          <div class="btn-grid-container">
            <v-btn class="custom-hover-btn" rounded="lg" block to="/"
              >前往首頁</v-btn
            >
            <v-btn class="custom-hover-btn" rounded="lg" block to="/userindex"
              >前往會員中心</v-btn
            >
          </div>
        </div>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
/* 讓卡片寬度變化時有平滑的動畫 */
.trans-width {
  transition:
    max-width 0.3s ease-in-out,
    width 0.3s ease-in-out;
}

:deep(.v-label) {
  font-size: 0.875rem;
  color: #666;
}

/* 整合註冊頁面的 CSS */
.btn-grid-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  width: 100%;
  max-width: 400px;
}

.custom-hover-btn {
  background-color: #145369 !important;
  color: #ffffff !important;
  border: 1px solid #145369 !important;
  transition: all 0.3s ease;
  height: 48px !important;
  font-weight: bold;
  letter-spacing: 1px;
}

.custom-hover-btn:hover {
  background-color: #ffffff !important;
  color: #145369 !important;
  border: 1px solid #145369 !important;
}
</style>
