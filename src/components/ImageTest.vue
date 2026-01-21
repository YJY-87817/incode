<script setup>
import { ref } from "vue";

const selectedFile = ref(null); // 儲存原始檔案物件
const previewUrl = ref(null); // 儲存本地預覽網址
const resultUrl = ref(null); // 儲存伺服器回傳的網址
const isUploading = ref(false); // 上傳狀態
const errorMessage = ref(""); // 錯誤訊息

// 當使用者選擇檔案時觸發
const handleFileChange = (event) => {
  const file = event.target.files[0];
  if (!file) return;

  selectedFile.value = file;
  // 建立本地預覽連結
  previewUrl.value = URL.createObjectURL(file);
  // 清除舊的結果
  resultUrl.value = null;
  errorMessage.value = "";
};

// 執行上傳動作
const uploadImage = async () => {
  if (!selectedFile.value) return;

  isUploading.value = true;
  errorMessage.value = "";

  // 使用 FormData 封裝檔案，這對應後端的 IFormFile
  const formData = new FormData();
  formData.append("file", selectedFile.value); // [注意]：'file' 必須與 API 參數名一致

  try {
    // [可自訂]：請將此處置換成你 API 的實際 Port 號
    const response = await fetch("https://localhost:7190/api/image", {
      method: "POST",
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`伺服器回應錯誤: ${response.status}`);
    }

    const data = await response.json();

    // 假設後端回傳格式為 { url: "/uploads/xxx.jpg" }
    // 如果後端只回傳相對路徑，我們補上 API 的基底路徑
    resultUrl.value = `https://localhost:7190${data.url}`;
  } catch (error) {
    errorMessage.value = error.message;
    console.error("Upload Error:", error);
  } finally {
    isUploading.value = false;
  }
};
</script>

<template>
  <div class="upload-container">
    <h2>圖片上傳測試</h2>

    <input type="file" @change="handleFileChange" accept="image/*" />

    <div v-if="previewUrl" class="preview-box">
      <p>預覽圖：</p>
      <img :src="previewUrl" style="max-width: 200px; border: 1px solid #ccc" />
    </div>

    <button @click="uploadImage" :disabled="!selectedFile || isUploading">
      {{ isUploading ? "上傳中..." : "開始上傳" }}
    </button>

    <hr />

    <div v-if="resultUrl">
      <p style="color: green">上傳成功！伺服器路徑：</p>
      <code>{{ resultUrl }}</code>
      <br />
      <img
        :src="resultUrl"
        alt="Server Image"
        style="max-width: 300px; margin-top: 10px"
      />
    </div>

    <div v-if="errorMessage" style="color: red">
      <p>錯誤：{{ errorMessage }}</p>
    </div>
  </div>
</template>

<style scoped>
.upload-container {
  padding: 20px;
  border: 1px dashed #aaa;
  border-radius: 8px;
  max-width: 400px;
  margin: 20px auto;
  font-family: sans-serif;
}
.preview-box {
  margin: 15px 0;
}
button {
  padding: 8px 16px;
  cursor: pointer;
}
</style>
