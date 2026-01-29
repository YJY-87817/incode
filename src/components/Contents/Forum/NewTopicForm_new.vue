<!-- NewTopicForm.vue -->
<!-- 
  新主題發表表單組件 (Pinia版)
  職責: 管理新主題的標題、內容、分類、圖片上傳和提交
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useForumStore } from "@/stores/forumStore";
import { uploadImage } from "@/api/imageapi.js";
import { appendUrls } from "./forumUtils";

const forumStore = useForumStore();

const parentCategoryId = ref(null);
const selectedSubCategoryId = ref(null);
const hasSyncedCategory = ref(false);

const getCategoryIdValue = (cat) =>
  cat?.id ?? cat?.content_category_id ?? cat?.categoryId ?? cat?.category_id ?? null;

const getParentCategoryIdValue = (cat) =>
  cat?.parentCategoryId ??
  cat?.parent_category_id ??
  cat?.parentCategoryID ??
  cat?.parent_id ??
  null;

const getCategoryName = (cat) =>
  cat?.name ?? cat?.categoryName ?? cat?.category_name ?? "";

const toCategoryKey = (value) =>
  value === null || value === undefined ? null : String(value);

const rootCategories = computed(() => {
  const categories = forumStore.categories || [];
  return categories.filter((cat) => !getParentCategoryIdValue(cat));
});

const childCategories = computed(() => {
  if (!parentCategoryId.value) return [];
  const parentKey = toCategoryKey(parentCategoryId.value);
  return (forumStore.categories || []).filter(
    (cat) => toCategoryKey(getParentCategoryIdValue(cat)) === parentKey
  );
});

const effectiveCategoryId = computed(
  () => selectedSubCategoryId.value ?? parentCategoryId.value ?? null
);

const syncCategorySelection = (categoryId) => {
  if (!categoryId) {
    parentCategoryId.value = null;
    selectedSubCategoryId.value = null;
    return;
  }

  const categories = forumStore.categories || [];
  const targetKey = toCategoryKey(categoryId);
  const target = categories.find(
    (cat) => toCategoryKey(getCategoryIdValue(cat)) === targetKey
  );

  if (!target) {
    parentCategoryId.value = categoryId;
    selectedSubCategoryId.value = null;
    return;
  }

  const parentId = getParentCategoryIdValue(target);
  if (parentId) {
    parentCategoryId.value = parentId;
    selectedSubCategoryId.value = getCategoryIdValue(target);
  } else {
    parentCategoryId.value = getCategoryIdValue(target);
    selectedSubCategoryId.value = null;
  }
};

onMounted(() => {
  const initialCategory = forumStore.newTopic.categoryId ?? forumStore.selectedCategory;
  if (initialCategory) {
    syncCategorySelection(initialCategory);
    hasSyncedCategory.value = true;
  }
});

watch(
  () => forumStore.categories,
  (categories) => {
    if (!categories?.length || hasSyncedCategory.value) return;
    const initialCategory =
      forumStore.newTopic.categoryId ?? forumStore.selectedCategory;
    if (initialCategory) {
      syncCategorySelection(initialCategory);
      hasSyncedCategory.value = true;
    }
  }
);

watch(
  () => parentCategoryId.value,
  () => {
    selectedSubCategoryId.value = null;
  }
);

watch(
  () => effectiveCategoryId.value,
  (value) => {
    forumStore.newTopic.categoryId = value;
  }
);

/**
 * 處理圖片上傳
 */
const handleImageUpload = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;

  forumStore.isUploadingNewTopicImages = true;
  try {
    const uploaded = await uploadImage(files);
    forumStore.newTopic.body = appendUrls(forumStore.newTopic.body, uploaded);
  } catch (error) {
    console.error("圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    forumStore.isUploadingNewTopicImages = false;
    if (event.target) event.target.value = "";
  }
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
        <select v-model="parentCategoryId" class="form-select">
          <option :value="null" disabled>選擇分類</option>
          <option
            v-for="category in rootCategories"
            :key="getCategoryIdValue(category)"
            :value="getCategoryIdValue(category)"
          >
            {{ getCategoryName(category) }}
          </option>
        </select>
        <small class="text-muted">選擇合適的分類便於他人找到你的主題</small>
      </div>

      <div v-if="childCategories.length" class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-sitemap"></i> 子分類
        </label>
        <select v-model="selectedSubCategoryId" class="form-select">
          <option :value="null">請選擇子分類（可選）</option>
          <option
            v-for="child in childCategories"
            :key="getCategoryIdValue(child)"
            :value="getCategoryIdValue(child)"
          >
            {{ getCategoryName(child) }}
          </option>
        </select>
        <small class="text-muted">若有子分類，建議選擇更精確的分類</small>
      </div>

      <!-- 標題輸入 -->
      <div class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-heading"></i> 標題 <span class="text-danger">*</span>
        </label>
        <input
          v-model="forumStore.newTopic.title"
          type="text"
          class="form-control"
          placeholder="輸入主題標題 (簡潔明瞭)"
          maxlength="200"
        />
        <small class="text-muted d-block mt-1">
          {{ forumStore.newTopic.title.length }}/200
        </small>
      </div>

      <!-- 內容輸入 -->
      <div class="mb-3">
        <label class="form-label small text-muted">
          <i class="fas fa-pen"></i> 內容 <span class="text-danger">*</span>
        </label>
        <textarea
          v-model="forumStore.newTopic.body"
          class="form-control"
          :rows="forumStore.windowWidth < 768 ? 4 : 5"
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
            :disabled="forumStore.isUploadingNewTopicImages"
          />
          <small
            v-if="forumStore.isUploadingNewTopicImages"
            class="text-warning"
          >
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
          @click="forumStore.submitNewTopic()"
          :disabled="
            forumStore.isSubmittingNewTopic ||
            !forumStore.newTopic.title.trim() ||
            !forumStore.newTopic.body.trim() ||
            !effectiveCategoryId
          "
        >
          <i class="fas fa-paper-plane me-2"></i>
          {{ forumStore.isSubmittingNewTopic ? "發送中..." : "發表" }}
        </button>
        <button
          class="btn btn-forum-secondary"
          type="button"
          @click="forumStore.cancelNewTopic()"
          :disabled="forumStore.isSubmittingNewTopic"
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
