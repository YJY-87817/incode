<script setup>
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";
const router = useRouter();

import setMaterialInput from "@/assets/js/material-input";

import { useAuthStore } from "@/stores/user/auth";
const authStore = useAuthStore();

const formData = ref({
  email: "",
  password: "",
  confirmPassword: "",
  name: "",
});

const showWelcomeDialog = ref(false);
const handleRegister = async () => {
  try {
    const res = await axios.post("/api/AuthApi/Register", formData.value);

    if (res.status === 200) {
      console.log("註冊成功！"); //跳出下面視窗;
      showWelcomeDialog.value = true;
    }
  } catch (error) {
    console.error("註冊失敗", error);
    alert("註冊發生錯誤");
  }
};

onMounted(() => {
  setMaterialInput();
});
</script>

<template>
  <v-img class="mx-auto my-6" max-width="228" src="" alt="網站logo"
    >網站logo</v-img
  >
  <v-card
    title="註冊成為會員"
    class="mx-auto pa-12 pb-8"
    elevation="8"
    max-width="800"
    rounded="lg"
  >
    <v-row>
      <v-col cols="6" class="mt-2">
        <v-form>
          <!--響應式 md="4" sm="6" -->
          <v-text-field label="帳號" placeholder="Email"></v-text-field>
          <v-text-field
            label="暱稱"
            placeholder="Name"
            variant="solo"
          ></v-text-field>
          <v-text-field
            label="生日"
            placeholder="Birthday"
            variant="solo"
          ></v-text-field>
          <v-text-field
            label="密碼"
            placeholder="Password"
            variant="filled"
          ></v-text-field>
          <v-text-field
            label="確認密碼"
            placeholder="ComparePassword"
            variant="filled"
          ></v-text-field>
          <v-text-field
            label="自我介紹"
            placeholder="Introduce"
            variant="underlined"
          ></v-text-field>
        </v-form>
      </v-col>
      <v-col cols="6">
        <v-card class="mx-auto pa-4" max-width="500" elevation="4">
          <v-card-title>上傳大頭貼</v-card-title>
          <v-card-text>
            <div class="d-flex justify-center mb-4">
              <v-avatar size="150" color="grey-lighten-2">
                <v-img v-if="previewUrl" :src="previewUrl" cover></v-img>
                <v-icon v-else size="50" color="grey">mdi-account</v-icon>
              </v-avatar>
            </div>
            <v-file-input
              v-model="selectedFile"
              label="選擇圖片"
              accept="image/png, image/jpeg, image/bmp"
              prepend-icon="mdi-check"
              variant="outlined"
              show-size
              @update:model-value="handleFileChange"
              :error-messages="errorMessage"
            ></v-file-input>
          </v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn
              color="primary"
              variant="elevated"
              :loading="isUploading"
              :disabled="!selectedFile"
              @click="uploadImage"
            >
              確認上傳
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
    <v-btn class="mb-2" color="blue" size="large" variant="tonal" block>
      註冊
      <RouterLink
        v-if="authStore.isLoggedIn"
        to="/UserIndex"
        class="btn btn-sm bg-gradient-success mb-0"
      >
      </RouterLink>
    </v-btn>
  </v-card>

  <!-- 註冊成功跳出視窗 -->
  <v-dialog v-model="showWelcomeDialog" max-width="800">
    <v-sheet
      class="d-flex align-center justify-center flex-wrap text-center mx-auto px-4"
      elevation="4"
      height="250"
      max-width="800"
      width="100%"
      rounded
    >
      <div>
        <h2 class="text-h4 font-weight-black text-orange">
          歡迎成為incode會員!
        </h2>
        <p class="text-body-2 mb-4">
          趕快加入討論的行列吧 <br />
          也可以開始做任務累積點數哦
        </p>
        <v-btn color="orange" variant="text">討論區</v-btn>
        <v-btn color="orange" variant="text">任務區</v-btn>
      </div>
    </v-sheet>
  </v-dialog>
</template>
