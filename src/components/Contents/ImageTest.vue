<script setup>
import { ref } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import ImageCarousel from "./ImageCarousel.vue";
import { uploadMultipleImages } from "@/api/imageapi";

// 多圖上傳相關
const selectedFiles = ref([]); // 儲存選中的多個檔案
const previewUrls = ref([]); // 本地預覽 URL 陣列
const uploadedImages = ref([]); // 已上傳的圖片 URL 陣列
const isUploading = ref(false); // 上傳狀態
const errorMessage = ref(""); // 錯誤訊息
const uploadProgress = ref(0); // 上傳進度

// 當使用者選擇多個檔案時觸發
const handleFileChange = (event) => {
  const files = event.target.files;
  if (!files || files.length === 0) return;

  selectedFiles.value = Array.from(files);

  // 建立本地預覽連結
  previewUrls.value = selectedFiles.value.map((file) =>
    URL.createObjectURL(file)
  );

  // 清除舊的錯誤訊息
  errorMessage.value = "";
};

// 執行批量上傳
const uploadImages = async () => {
  if (selectedFiles.value.length === 0) return;

  isUploading.value = true;
  errorMessage.value = "";
  uploadProgress.value = 0;

  try {
    // 使用 API 模組上傳多張圖片
    const urls = await uploadMultipleImages(selectedFiles.value);

    // 將新上傳的圖片加入已上傳列表
    uploadedImages.value = [...uploadedImages.value, ...urls];

    uploadProgress.value = 100;

    // 清空選擇的檔案和預覽
    selectedFiles.value = [];
    previewUrls.value = [];

    // 重置 file input
    const fileInput = document.querySelector('input[type="file"]');
    if (fileInput) fileInput.value = "";
  } catch (error) {
    errorMessage.value = error.message || "上傳失敗，請稍後再試";
    console.error("Upload Error:", error);
  } finally {
    isUploading.value = false;
  }
};

// 移除預覽圖片
const removePreview = (index) => {
  selectedFiles.value.splice(index, 1);
  previewUrls.value.splice(index, 1);
};

// 移除已上傳的圖片
const removeUploadedImage = (index) => {
  uploadedImages.value.splice(index, 1);
};

// 清空所有上傳的圖片
const clearAllImages = () => {
  uploadedImages.value = [];
};
</script>

<template>
  <DefaultNavbar />

  <div class="container mt-5">
    <div class="row justify-content-center">
      <div class="col-lg-8">
        <!-- 上傳區塊 -->
        <div class="card shadow-sm mb-4">
          <div class="card-header bg-gradient-primary">
            <h4 class="text-white mb-0">
              <i class="fas fa-images me-2"></i>多圖片上傳測試
            </h4>
          </div>

          <div class="card-body">
            <!-- 檔案選擇 -->
            <div class="mb-3">
              <label class="form-label fw-bold">選擇圖片（可多選）</label>
              <input
                type="file"
                class="form-control"
                @change="handleFileChange"
                accept="image/*"
                multiple
              />
              <small class="text-muted"
                >支援 jpg, png, gif 等格式，可一次選擇多張</small
              >
            </div>

            <!-- 預覽區 -->
            <div v-if="previewUrls.length > 0" class="preview-section mb-3">
              <label class="form-label fw-bold"
                >預覽圖（{{ previewUrls.length }} 張）</label
              >
              <div class="preview-grid">
                <div
                  v-for="(url, index) in previewUrls"
                  :key="index"
                  class="preview-item"
                >
                  <img :src="url" alt="預覽圖" />
                  <button
                    class="remove-btn"
                    @click="removePreview(index)"
                    title="移除此圖"
                  >
                    <i class="fas fa-times"></i>
                  </button>
                </div>
              </div>
            </div>

            <!-- 上傳按鈕 -->
            <button
              class="btn btn-primary w-100"
              @click="uploadImages"
              :disabled="selectedFiles.length === 0 || isUploading"
            >
              <i class="fas fa-cloud-upload-alt me-2"></i>
              {{
                isUploading
                  ? `上傳中... ${uploadProgress}%`
                  : `開始上傳 (${selectedFiles.length} 張)`
              }}
            </button>

            <!-- 錯誤訊息 -->
            <div v-if="errorMessage" class="alert alert-danger mt-3 mb-0">
              <i class="fas fa-exclamation-circle me-2"></i>{{ errorMessage }}
            </div>
          </div>
        </div>

        <!-- 已上傳圖片展示區 -->
        <div v-if="uploadedImages.length > 0" class="card shadow-sm">
          <div
            class="card-header bg-gradient-success d-flex justify-content-between align-items-center"
          >
            <h5 class="text-white mb-0">
              <i class="fas fa-check-circle me-2"></i>已上傳的圖片 ({{
                uploadedImages.length
              }}
              張)
            </h5>
            <button
              class="btn btn-sm btn-outline-light"
              @click="clearAllImages"
            >
              <i class="fas fa-trash me-1"></i>清空全部
            </button>
          </div>

          <div class="card-body">
            <!-- 使用輪播組件顯示圖片 -->
            <ImageCarousel
              :images="uploadedImages"
              :showThumbnails="true"
              :loop="true"
              baseUrl="https://localhost:7190"
            />

            <!-- 圖片列表 -->
            <div class="image-list mt-4">
              <h6 class="mb-3">圖片列表：</h6>
              <div class="list-group">
                <div
                  v-for="(url, index) in uploadedImages"
                  :key="index"
                  class="list-group-item d-flex justify-content-between align-items-center"
                >
                  <div class="d-flex align-items-center">
                    <span class="badge bg-primary me-2">{{ index + 1 }}</span>
                    <code class="text-truncate" style="max-width: 400px">{{
                      url
                    }}</code>
                  </div>
                  <button
                    class="btn btn-sm btn-outline-danger"
                    @click="removeUploadedImage(index)"
                  >
                    <i class="fas fa-trash"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 使用說明 -->
        <div class="card shadow-sm mt-4">
          <div class="card-header bg-gradient-info">
            <h6 class="text-white mb-0">
              <i class="fas fa-info-circle me-2"></i>使用說明
            </h6>
          </div>
          <div class="card-body">
            <ul class="mb-0">
              <li>點擊「選擇圖片」按鈕可一次選擇多張圖片</li>
              <li>選擇後會顯示預覽，可以移除不需要的圖片</li>
              <li>點擊「開始上傳」會將所有圖片上傳到伺服器</li>
              <li>上傳成功後，圖片會顯示在下方，可以左右滑動查看</li>
              <li>點擊縮圖可快速切換到指定圖片</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.container {
  max-width: 1200px;
}

.card {
  border: none;
  border-radius: 12px;
  overflow: hidden;
}

.card-header {
  padding: 1.25rem 1.5rem;
}

.bg-gradient-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.bg-gradient-success {
  background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
}

.bg-gradient-info {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.preview-section {
  padding: 1rem;
  background: #f8f9fa;
  border-radius: 8px;
}

.preview-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 15px;
  margin-top: 10px;
}

.preview-item {
  position: relative;
  aspect-ratio: 1;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: transform 0.2s;
}

.preview-item:hover {
  transform: scale(1.05);
}

.preview-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.remove-btn {
  position: absolute;
  top: 5px;
  right: 5px;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: rgba(220, 53, 69, 0.9);
  color: white;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  transition: all 0.2s;
}

.remove-btn:hover {
  background: rgba(220, 53, 69, 1);
  transform: scale(1.1);
}

.image-list .list-group-item {
  padding: 0.75rem 1rem;
  border-left: 3px solid transparent;
  transition: all 0.2s;
}

.image-list .list-group-item:hover {
  border-left-color: #667eea;
  background-color: #f8f9fa;
}

code {
  background: #f1f3f5;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.875rem;
}

@media (max-width: 768px) {
  .preview-grid {
    grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
    gap: 10px;
  }
}
</style>
