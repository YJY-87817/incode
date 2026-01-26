<script setup>
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";
import { useAuthStore } from "@/stores/user/auth";

const router = useRouter();
const authStore = useAuthStore();

// 表單資料
const formData = ref({
  email: "",
  password: "",
  confirmPassword: "",
  name: "",
  birth: "",
  intro: "",
});

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
  if (!selectedFile.value) return;
  isUploading.value = true;
  // 這裡放入您的上傳 API 邏輯
  setTimeout(() => {
    isUploading.value = false;
    alert("圖片已選擇 (需串接API)");
  }, 1000);
};

const handleRegister = async () => {
  try {
    // 這裡建議先做簡單的驗證 (例如密碼是否相同)
    if (formData.value.password !== formData.value.confirmPassword) {
      alert("兩次密碼輸入不一致");
      return;
    }

    const payload = {
      email: formData.value.email,
      password: formData.value.password,
      confirmpassword: formData.value.confirmpassword,
      username: formData.value.username,
      // 處理生日：如果有值就傳送，如果是空字串就傳送 null (或不傳)
      // 注意：請確認後端 DTO 的 Birth 允許 null (DateTime?)
      birth: formData.value.birth ? formData.value.birth : null,
    };

    console.log("準備傳送的資料:", payload); // 在 Console 檢查一下資料對不對

    // 3. 發送請求 (使用 payload 而不是 formData.value)
    const res = await axios.post(
      "https://localhost:7190/api/AuthApi/register",
      payload,
    );

    if (res.status === 200) {
      console.log("註冊成功！");
      showWelcomeDialog.value = true;
    }
  } catch (error) {
    console.error("註冊失敗", error);
    alert("註冊發生錯誤");
  }
};
</script>

<template>
  <v-container fluid class="fill-height bg-grey-lighten-5">
    <div class="text-center mb-6">
      <v-avatar color="primary" size="60" class="mb-2">
        <span class="text-h5 text-white font-weight-bold">In</span>
      </v-avatar>
      <h1 class="text-h5 font-weight-bold text-grey-darken-2">加入我們</h1>
    </div>

    <v-card
      class="mx-auto pa-8 pa-md-10"
      max-width="900"
      rounded="xl"
      elevation="0"
      border
    >
      <v-row>
        <v-col cols="12" md="7">
          <h2 class="text-h5 font-weight-bold mb-6 text-primary">註冊帳號</h2>

          <v-form @submit.prevent="handleRegister">
            <v-row dense>
              <v-col cols="12">
                <v-text-field
                  v-model="formData.email"
                  label="帳號 (Email)"
                  placeholder="name@example.com"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  prepend-inner-icon="mdi-email-outline"
                ></v-text-field>
              </v-col>

              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="formData.username"
                  label="暱稱"
                  placeholder="怎麼稱呼您?"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  prepend-inner-icon="mdi-account-outline"
                ></v-text-field>
              </v-col>

              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="formData.birth"
                  label="生日"
                  placeholder="YYYY-MM-DD"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  prepend-inner-icon="mdi-cake-variant-outline"
                ></v-text-field>
              </v-col>

              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="formData.password"
                  label="密碼"
                  type="password"
                  variant="outlined"
                  density="comfortable"
                  color="primary"
                  prepend-inner-icon="mdi-lock-outline"
                ></v-text-field>
              </v-col>

              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="formData.confirmPassword"
                  label="確認密碼"
                  type="password"
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
          class="d-flex flex-column align-center justify-center ps-md-8 border-start-md"
        >
          <div class="text-center w-100">
            <h3
              class="text-subtitle-1 font-weight-bold mb-4 text-grey-darken-1"
            >
              設定個人頭像
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

      <v-divider class="my-6"></v-divider>

      <div class="d-flex flex-column gap-2">
        <v-btn
          color="primary"
          size="large"
          variant="flat"
          block
          rounded="lg"
          class="text-body-1 font-weight-bold"
          elevation="2"
          @click="handleRegister"
        >
          立即註冊
        </v-btn>

        <div class="text-center mt-4">
          <span class="text-caption text-grey">已經有帳號了嗎？</span>
          <router-link
            to="/login"
            class="text-caption font-weight-bold text-primary text-decoration-none ms-1"
          >
            登入
          </router-link>
        </div>
      </div>
    </v-card>

    <v-dialog v-model="showWelcomeDialog" max-width="500">
      <v-card rounded="xl" class="text-center pa-6">
        <v-icon size="60" color="success" class="mb-4">mdi-check-circle</v-icon>
        <h2 class="text-h5 font-weight-bold mb-2">歡迎加入 Incode!</h2>
        <p class="text-body-2 text-grey mb-6">
          您的帳號已建立成功，現在您可以開始探索論壇或執行任務了。
        </p>

        <div class="d-flex justify-center gap-4">
          <v-btn color="primary" variant="flat" rounded="lg" to="/forum"
            >前往討論區</v-btn
          >
          <v-btn color="secondary" variant="tonal" rounded="lg" to="/mytask"
            >前往任務區</v-btn
          >
        </div>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
/* 如果有需要微調的地方可以寫在這裡，但目前主要依賴 Vuetify class */
.gap-2 {
  gap: 8px;
}

.gap-4 {
  gap: 16px;
}
</style>
