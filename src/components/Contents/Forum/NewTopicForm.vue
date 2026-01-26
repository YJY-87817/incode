<!-- NewTopicForm.vue -->
<!-- 
  新主題發表表單組件
  職責: 管理新主題的標題、內容、分類、圖片上傳和提交
  Props:
    - categories: 分類列表
    - windowWidth: 視窗寬度 (用於響應式調整)
    - isSubmitting: 是否提交中
    - isUploadingImages: 是否上傳圖片中
  Emits:
    - submit: 提交表單 (emit { title, body, categoryId })
    - cancel: 取消編輯
    - images-uploaded: 圖片上傳完成 (emit imageUrls)
    - category-change: 分類變更
-->

<script setup>
import { reactive, computed, watch, onMounted } from "vue";
import { uploadImage } from "@/api/imageapi.js";
import { useForumStore } from "@/stores/forumStore";

const forumStore = useForumStore();

const props = defineProps({
  categories: {
    type: Array,
    default: () => [],
  },
  windowWidth: {
    type: Number,
    default: 1024,
  },
  isSubmitting: {
    type: Boolean,
    default: false,
  },
  isUploadingImages: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  "submit",
  "cancel",
  "images-uploaded",
  "category-change",
]);

// 表單狀態
const form = reactive({
  title: "",
  body: "",
  categoryId: null,
});

// 初始化時同步 store 的選中分類
onMounted(() => {
  if (forumStore.selectedCategory) {
    form.categoryId = forumStore.selectedCategory;
  }
});

// 監聽 store 的 selectedCategory 變化並同步到表單
watch(
  () => forumStore.selectedCategory,
  (newCategory) => {
    form.categoryId = newCategory;
  }
);

/**
 * 處理圖片上傳
 * 將上傳的圖片URL附加到內容中
 */
const handleImageUpload = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;

  try {
    const uploaded = await uploadImage(files);
    const urls = Array.isArray(uploaded) ? uploaded : [uploaded];
    const chunk = urls.filter(Boolean).join("\n");

    if (chunk) {
      form.body = [form.body.trim(), chunk].filter(Boolean).join("\n");
      emit("images-uploaded", urls);
    }
  } catch (error) {
    console.error("圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    if (event.target) event.target.value = "";
  }
};

/**
 * 驗證表單
 */
const isFormValid = computed(() => {
  return form.title.trim() && form.body.trim() && form.categoryId;
});

/**
 * 提交表單
 */
const handleSubmit = () => {
  if (!isFormValid.value) {
    alert("請填寫所有必填欄位");
    return;
  }
  emit("submit", {
    title: form.title.trim(),
    body: form.body.trim(),
    categoryId: form.categoryId,
  });
};

/**
 * 取消編輯
 */
const handleCancel = () => {
  form.title = "";
  form.body = "";
  form.categoryId = null;
  emit("cancel");
};

/**
 * 分類變更時通知父組件
 */
const handleCategoryChange = () => {
  emit("category-change", form.categoryId);
};
</script>

<template>
  <div class="card shadow-sm mb-4">
    <div class="card-header bg-white py-3">
      <h5 class="mb-0 fs-6 fs-md-5">✍️ 發表新主題</h5>
    </div>

    <div class="card-body p-3 border-bottom">
      <!-- 分類選擇 -->
      <div class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-folder-open"></i> 分類
          <span class="text-danger">*</span>
        </label>
        <select
          v-model="form.categoryId"
          class="form-select"
          @change="handleCategoryChange"
        >
          <option :value="null" disabled>選擇分類</option>
          <option
            v-for="category in categories"
            :key="category.id"
            :value="category.id"
          >
            {{ category.name }}
          </option>
        </select>
        <small class="text-muted">選擇合適的分類便於他人找到你的主題</small>
      </div>

      <!-- 標題輸入 -->
      <div class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-heading"></i> 標題 <span class="text-danger">*</span>
        </label>
        <input
          v-model="form.title"
          type="text"
          class="form-control"
          placeholder="輸入主題標題 (簡潔明瞭)"
          maxlength="200"
        />
        <small class="text-muted d-block mt-1">
          {{ form.title.length }}/200
        </small>
      </div>

      <!-- 內容輸入 -->
      <div class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-pen"></i> 內容 <span class="text-danger">*</span>
        </label>
        <textarea
          v-model="form.body"
          class="form-control"
          :rows="windowWidth < 768 ? 4 : 5"
          placeholder="撰寫主題內容... (可在內容中貼上圖片URL或使用下方上傳功能)"
        ></textarea>
      </div>

      <!-- 圖片上傳 -->
      <div class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-image"></i> 附加圖片
        </label>
        <div class="d-flex flex-column flex-sm-row gap-2 align-items-start">
          <input
            type="file"
            accept="image/*"
            multiple
            class="form-control form-control-sm"
            @change="handleImageUpload"
            :disabled="isUploadingImages"
          />
          <small v-if="isUploadingImages" class="text-warning">
            <i class="fas fa-spinner fa-spin"></i> 上傳中...
          </small>
          <small v-else class="text-muted">
            上傳後會自動把圖片網址加入內容
          </small>
        </div>
      </div>
    </div>

    <!-- 操作按鈕 -->
    <div class="card-footer bg-white py-3">
      <div class="d-flex flex-wrap gap-2">
        <button
          class="btn btn-forum-primary"
          @click="handleSubmit"
          :disabled="isSubmitting || !isFormValid"
        >
          <i class="fas fa-paper-plane me-2"></i>
          {{ isSubmitting ? "發送中..." : "發表" }}
        </button>
        <button
          class="btn btn-forum-secondary"
          type="button"
          @click="handleCancel"
          :disabled="isSubmitting"
        >
          取消
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 按鈕樣式 */
.btn-forum-primary {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover:not(:disabled) {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #432f28;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(67, 47, 40, 0.3);
}

.btn-forum-primary:disabled {
  background-color: #d1d3d4;
  border-color: #d1d3d4;
  color: #fff;
  opacity: 0.6;
}

.btn-forum-secondary {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #432f28;
  transition: all 0.3s ease;
}

.btn-forum-secondary:hover:not(:disabled) {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}

/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

.card-header {
  background-color: #fff0dd !important;
  border-bottom-color: #d1d3d4;
}

.card-header h5 {
  color: #432f28;
}

.card-footer {
  border-top-color: #d1d3d4;
}

/* 表單樣式 */
.form-control,
.form-select {
  border-color: #d1d3d4;
}

.form-control:focus,
.form-select:focus {
  border-color: #432f28;
  box-shadow: 0 0 0 0.2rem rgba(67, 47, 40, 0.25);
}

/* 必填標記 */
.text-danger {
  color: #dc3545;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .card-body,
  .card-footer {
    padding: 1rem !important;
  }

  .btn {
    width: 100%;
  }
}
</style>
