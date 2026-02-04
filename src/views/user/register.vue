<script setup>
  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import { ref, onMounted } from "vue";
  import { useRouter } from "vue-router";
  import axios from "axios";
  import { useAuthStore } from "@/stores/user/auth";
  import Swal from "sweetalert2";

  const router = useRouter();
  const authStore = useAuthStore();

  const form = ref(null);
  const showPassword = ref(false);
  const showConfirmPassword = ref(false);

  // 表單資料
  const formData = ref({
    email: "",
    password: "",
    confirmPassword: "",
    name: "",
    birth: "",
    intro: "",
  });

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
    (v) => v === formData.value.password || "兩次輸入的密碼不一致",
  ];

  // 檔案上傳相關
  const selectedFile = ref(null);
  const previewUrl = ref(null);
  const isUploading = ref(false);
  const errorMessage = ref("");
  const showWelcomeDialog = ref(false);

  // 處理檔案選擇與預覽
  const handleFileChange = (files) => {
    if (files && files.length > 0) {
      const file = files[0]; // Vuetify 3 的 file-input 回傳的是陣列
      if (file.size > 2 * 1024 * 1024) {
        // alertStore.open({
        //   type: "warning",
        //   title: "檔案過大",
        //   text: "請選擇小於 2MB 的圖片",
        // });
        // 清空選擇
        selectedFile.value = null;
        previewUrl.value = null;
        return;
      }
      selectedFile.value = file;
      // 建立預覽網址
      previewUrl.value = URL.createObjectURL(file);
      errorMessage.value = "";
    } else {
      selectedFile.value = null;
      previewUrl.value = null;
    }
  };

  // 模擬上傳圖片 (或是您的實際邏輯)
  const uploadImage = async () => {
    if (!selectedFile.value)
      Swal.fire({
        icon: "info",
        text: "請先選擇一張圖片",
      });
    return;
    isUploading.value = true;
    // 這裡放入您的上傳 API 邏輯
    setTimeout(() => {
      isUploading.value = false;
      Swal.fire({
        title: "圖片上傳成功！",
        icon: "success",
        draggable: true
      });
    }, 1000);
  };

  const handleRegister = async () => {
    // 先驗證表單
    const { valid } = await form.value.validate();
    if (!valid) return;

    try {
      const success = await authStore.register(formData.value);
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
    }
  };
</script>

<template>
  <DefaultNavbar light />
  <v-container fluid class="fill-height bg-grey-lighten-5 mt-4">
    <v-row justify="center" align="center">
      <v-col cols="12" sm="10" md="10" lg="8">
        <div class="text-center mb-4">
          <h1 class="text-h5 font-weight-bold text-grey-darken-2">加入我們</h1>
        </div>

        <v-card class="mx-auto pa-8 pa-md-10" max-width="1100" rounded="xl" elevation="0" border>
          <v-row>
            <v-col cols="12" md="7" class="pe-md-6">
              <h2 class="text-h5 font-weight-bold mb-4 text-primary">
                註冊帳號
              </h2>

              <v-form ref="form" @submit.prevent="handleRegister">
                <v-row dense>
                  <v-col cols="12">
                    <v-text-field v-model="formData.email" :rules="emailRules" label="*帳號 (Email)*"
                      placeholder="name@example.com" variant="outlined" density="comfortable" color="primary"
                      prepend-inner-icon="mdi-email-outline"></v-text-field>
                  </v-col>

                  <v-col cols="12">
                    <v-text-field v-model="formData.username" label="暱稱" placeholder="怎麼稱呼您?" variant="outlined"
                      density="comfortable" color="primary" prepend-inner-icon="mdi-account-outline"></v-text-field>
                  </v-col>

                  <v-col cols="12">
                    <v-text-field v-model="formData.birth" label="生日" placeholder="YYYY-MM-DD" variant="outlined"
                      density="comfortable" color="primary"
                      prepend-inner-icon="mdi-cake-variant-outline"></v-text-field>
                  </v-col>

                  <v-col cols="12">
                    <v-text-field v-model="formData.password" :rules="passwordRules" label="*密碼*"
                      :type="showPassword ? 'text' : 'password'" :append-inner-icon="
                        showPassword ? 'mdi-eye' : 'mdi-eye-off'
                      " @click:append-inner="showPassword = !showPassword" variant="outlined" density="comfortable"
                      color="primary" prepend-inner-icon="mdi-lock-outline"></v-text-field>
                  </v-col>

                  <v-col cols="12">
                    <v-text-field v-model="formData.confirmPassword" :rules="confirmPasswordRules" label="*確認密碼*"
                      :type="showConfirmPassword ? 'text' : 'password'" :append-inner-icon="
                        showConfirmPassword ? 'mdi-eye' : 'mdi-eye-off'
                      " @click:append-inner="
                        showConfirmPassword = !showConfirmPassword
                      " variant="outlined" density="comfortable" color="primary"
                      prepend-inner-icon="mdi-lock-check-outline"></v-text-field>
                  </v-col>
                </v-row>
              </v-form>
            </v-col>

            <v-col cols="12" md="5" class="d-flex flex-column align-center justify-center ps-md-4 border-start-md">
              <div class="text-center w-100">
                <h3 class="text-subtitle-1 font-weight-bold mb-4 text-grey-darken-1">
                  設定個人大頭貼
                </h3>

                <v-card class="mx-auto mb-6 d-flex align-center justify-center rounded-circle" width="180" height="180"
                  elevation="0" color="grey-lighten-4" style="border: 2px dashed #ccc; overflow: hidden">
                  <v-img v-if="previewUrl" :src="previewUrl" cover class="w-100 h-100"></v-img>
                  <v-icon v-else size="60" color="grey-lighten-1">mdi-camera-plus</v-icon>
                </v-card>

                <v-file-input v-model="selectedFile" accept="image/*" label="點擊選擇圖片" variant="outlined"
                  density="compact" prepend-icon="" prepend-inner-icon="mdi-image" hide-details class="mb-3"
                  @update:model-value="handleFileChange"></v-file-input>

                <v-btn v-if="selectedFile" variant="tonal" color="primary" size="small" block :loading="isUploading"
                  @click="uploadImage" class="mb-6">
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
              <v-btn v-bind="props" size="large" block rounded="lg" variant="flat" class="text-body-1 font-weight-bold"
                elevation="2" @click="handleRegister" :color="isHovering ? '#FFFFFF' : '#145369'" :style="{
                  color: isHovering
                    ? '#145369 !important'
                    : '#FFFFFF !important',
                  border: '1px solid #145369',
                }">
                立即註冊
              </v-btn>
            </v-hover>
            <div class="text-center mt-2">
              <span class="text-caption text-grey">已經有帳號了嗎？</span>
              <router-link to="/login" class="text-caption font-weight-bold text-primary text-decoration-none ms-1">
                登入
              </router-link>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>
    <v-dialog v-model="showWelcomeDialog" max-width="500" persistent>
      <v-card rounded="xl" class="text-center pa-6">
        <div class="d-flex align-center justify-center mb-2">
          <v-icon size="60" color="success" class="ml-4">
            mdi-check-circle
          </v-icon>
          <h2 class="text-h5 font-weight-bold">歡迎加入 Incode!</h2>
        </div>
        <p class="text-body-2 text-grey mb-4">
          您的帳號已建立成功，現在您可以開始探索論壇或執行任務了。
        </p>

        <div class="d-flex justify-center">
          <div class="btn-grid-container">
            <v-btn class="custom-hover-btn" rounded="lg" block to="/">前往首頁</v-btn>
            <v-btn class="custom-hover-btn" rounded="lg" block to="/userindex">前往會員中心</v-btn>
          </div>
        </div>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
  /* 排版設定：建立 2欄 x 2列 的網格，中間有間距 */
  .btn-grid-container {
    display: grid;
    grid-template-columns: 1fr 1fr;
    /* 兩個等寬的欄位 */
    gap: 16px;
    /* 按鈕之間的間距，可依需求調整 */
    width: 100%;
    max-width: 400px;
    /* 限制最大寬度，避免在大螢幕太開 */
  }

  /* 按鈕樣式設定 */
  .custom-hover-btn {
    /* 預設狀態 */
    background-color: #145369 !important;
    /* 指定顏色 */
    color: #ffffff !important;
    /* 文字白色 */
    border: 1px solid #145369 !important;
    /* 預設邊框同色 */
    transition: all 0.3s ease;
    /* 平滑過渡效果 */

    /* 確保高度一致，如果有需要可以強制設定 height */
    height: 48px !important;
    font-weight: bold;
    letter-spacing: 1px;
  }

  /* 滑鼠懸停 (Hover) 效果 */
  .custom-hover-btn:hover {
    background-color: #ffffff !important;
    /* 背景變白 */
    color: #145369 !important;
    /* 文字變回深色 */
    border: 1px solid #145369 !important;
    /* 保留邊框 */
  }
</style>