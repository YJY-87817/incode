<script setup>
import { ref } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import ImageCarousel from "./ImageCarousel.vue";
import { uploadMultipleImages } from "@/api/imageapi";
import RichTextEditor from "@/components/RichTextEditor.vue";

// 多圖上傳相關
const selectedFiles = ref([]); // 儲存選中的多個檔案
const previewUrls = ref([]); // 本地預覽 URL 陣列（只在前端顯示）
const previewFileMap = ref({}); // 預覽圖與檔案的對應關係
const savedImages = ref([]); // 已保存到資料庫的圖片 URL 陣列
const isSaving = ref(false); // 保存狀態
const errorMessage = ref(""); // 錯誤訊息
const uploadProgress = ref(0); // 上傳進度
const successMessage = ref(""); // 成功訊息
const rteContent = ref("<p>在這裡測試 Tiptap</p>");

// 當使用者選擇多個檔案時觸發
const handleFileChange = (event) => {
  const files = event.target.files;
  if (!files || files.length === 0) return;

  selectedFiles.value = Array.from(files);

  // 建立本地預覽連結（只在前端）
  previewUrls.value = selectedFiles.value.map((file) => {
    const objectUrl = URL.createObjectURL(file);
    previewFileMap.value[objectUrl] = file; // 記錄預覽 URL 與檔案的對應
    return objectUrl;
  });

  // 清除舊的錯誤訊息
  errorMessage.value = "";
  successMessage.value = "";
};

// 移除預覽圖片
const removePreview = (index) => {
  const urlToRemove = previewUrls.value[index];
  // 釋放 ObjectURL 資源
  if (urlToRemove) {
    URL.revokeObjectURL(urlToRemove);
    delete previewFileMap.value[urlToRemove];
  }
  selectedFiles.value.splice(index, 1);
  previewUrls.value.splice(index, 1);
};

// 保存預覽的圖片到資料庫
const saveImages = async () => {
  if (selectedFiles.value.length === 0) {
    errorMessage.value = "沒有圖片需要保存";
    return;
  }

  isSaving.value = true;
  errorMessage.value = "";
  successMessage.value = "";
  uploadProgress.value = 0;

  try {
    // 使用 API 模組上傳多張圖片到伺服器
    const urls = await uploadMultipleImages(selectedFiles.value);

    // 將新保存的圖片加入已保存列表
    savedImages.value = [...savedImages.value, ...urls];

    uploadProgress.value = 100;
    successMessage.value = `成功保存 ${urls.length} 張圖片到資料庫`;

    // 清理預覽
    clearPreview();

    // 重置 file input
    const fileInput = document.querySelector('input[type="file"]');
    if (fileInput) fileInput.value = "";

    // 3 秒後清除成功訊息
    setTimeout(() => {
      successMessage.value = "";
    }, 3000);
  } catch (error) {
    errorMessage.value = error.message || "保存失敗，請稍後再試";
    console.error("Save Error:", error);
  } finally {
    isSaving.value = false;
  }
};

// 清空預覽區
const clearPreview = () => {
  // 釋放所有 ObjectURL 資源
  previewUrls.value.forEach((url) => {
    URL.revokeObjectURL(url);
    delete previewFileMap.value[url];
  });
  selectedFiles.value = [];
  previewUrls.value = [];
};

// 移除已保存的圖片
const removeSavedImage = (index) => {
  savedImages.value.splice(index, 1);
};

// 清空所有已保存的圖片
const clearAllImages = () => {
  savedImages.value = [];
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
            <div class="btn-group w-100 gap-2" role="group">
              <button
                class="btn btn-primary flex-fill"
                @click="saveImages"
                :disabled="selectedFiles.length === 0 || isSaving"
              >
                <i class="fas fa-save me-2"></i>
                {{
                  isSaving
                    ? `保存中... ${uploadProgress}%`
                    : `保存圖片 (${selectedFiles.length} 張)`
                }}
              </button>
              <button
                class="btn btn-outline-secondary flex-fill"
                @click="clearPreview"
                :disabled="selectedFiles.length === 0"
              >
                <i class="fas fa-times me-2"></i>取消
              </button>
            </div>

            <!-- 成功訊息 -->
            <div v-if="successMessage" class="alert alert-success mt-3 mb-0">
              <i class="fas fa-check-circle me-2"></i>{{ successMessage }}
            </div>

            <!-- 錯誤訊息 -->
            <div v-if="errorMessage" class="alert alert-danger mt-3 mb-0">
              <i class="fas fa-exclamation-circle me-2"></i>{{ errorMessage }}
            </div>
          </div>
        </div>

        <!-- 已保存圖片展示區 -->
        <div v-if="savedImages.length > 0" class="card shadow-sm">
          <div
            class="card-header bg-gradient-success d-flex justify-content-between align-items-center"
          >
            <h5 class="text-white mb-0">
              <i class="fas fa-check-circle me-2"></i>已保存的圖片 ({{
                savedImages.length
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
              :images="savedImages"
              :showThumbnails="true"
              :loop="true"
              baseUrl="https://localhost:7190"
            />

            <!-- 圖片列表 -->
            <div class="image-list mt-4">
              <h6 class="mb-3">圖片列表：</h6>
              <div class="list-group">
                <div
                  v-for="(url, index) in savedImages"
                  :key="index"
                  class="list-group-item d-flex justify-content-between align-items-center"
                >
                  <div class="d-flex align-items-center">
                    <span class="badge bg-success me-2">{{ index + 1 }}</span>
                    <code class="text-truncate" style="max-width: 400px">{{
                      url
                    }}</code>
                  </div>
                  <button
                    class="btn btn-sm btn-outline-danger"
                    @click="removeSavedImage(index)"
                  >
                    <i class="fas fa-trash"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="card shadow-sm mt-4">
          <div class="card-header bg-gradient-info">
            <h5 class="text-white mb-0">Tiptap 富文字編輯器測試</h5>
          </div>
          <div class="card-body">
            <RichTextEditor
              v-model="rteContent"
              placeholder="在此輸入測試內容..."
            />
            <div class="mt-3">
              <h6 class="mb-2">即時預覽</h6>
              <div class="border rounded p-3 bg-light" v-html="rteContent"></div>
            </div>
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
