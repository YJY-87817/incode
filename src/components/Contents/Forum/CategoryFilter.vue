<!-- CategoryFilter.vue -->
<!-- 
  分類篩選組件 (Pinia版)
  職責: 顯示分類列表、提供篩選功能、展示分類統計
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { useForumStore } from "@/stores/forumStore";

const forumStore = useForumStore();
</script>

<template>
  <div class="card shadow-sm mb-4">
    <!-- 卡片標題 -->
    <div class="card-header bg-white py-3 d-flex flex-column gap-2">
      <div
        class="d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center gap-2"
      >
        <h5 class="mb-0 fs-6 fs-md-5">📁 討論分類</h5>
        <small class="text-muted">點擊分類篩選主題</small>
      </div>
    </div>

    <!-- 分類內容 -->
    <div class="p-2 p-md-3">
      <!-- 載入中提示 -->
      <div
        v-if="forumStore.isLoading && forumStore.categories.length === 0"
        class="text-center py-3"
      >
        <div
          class="spinner-border spinner-border-sm text-primary"
          role="status"
        >
          <span class="visually-hidden">載入中...</span>
        </div>
      </div>

      <!-- 分類列表 -->
      <div
        v-else
        class="d-flex flex-wrap align-content-start p-0 category-container"
      >
        <!-- 分類卡片 -->
        <div
          v-for="category in forumStore.categories"
          :key="category.id"
          class="m-1"
        >
          <div
            class="category-card p-2 border rounded cursor-pointer"
            :class="{
              'border-primary bg-light':
                forumStore.selectedCategory === category.id,
              'border-light': forumStore.selectedCategory !== category.id,
            }"
            @click="forumStore.selectCategory(category.id)"
            :title="category.description || category.name"
            role="button"
            tabindex="0"
            @keydown.enter="forumStore.selectCategory(category.id)"
            @keydown.space.prevent="forumStore.selectCategory(category.id)"
          >
            <!-- 分類名稱 -->
            <h6 class="mb-2 fw-bold text-truncate">{{ category.name }}</h6>

            <!-- 分類統計標籤 -->
            <div
              class="category-tags small"
              :class="{
                'text-primary': forumStore.selectedCategory === category.id,
              }"
            >
              <span class="d-inline-block me-2"
                >📝 {{ category.topicCount ?? 0 }}</span
              >
              <span class="d-inline-block"
                >💬 {{ category.postCount ?? 0 }}</span
              >
            </div>
          </div>
        </div>

        <!-- 空狀態提示 -->
        <div
          v-if="!forumStore.isLoading && forumStore.categories.length === 0"
          class="w-100 text-center text-muted py-3 small"
        >
          <p>目前沒有分類</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
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

/* 分類卡片樣式 */
.category-card {
  transition: all 0.2s ease;
  border-width: 1px;
  font-size: 0.75rem;
  min-height: 60px;
  cursor: pointer;
  background-color: #ffffff;
}

/**
 * 未選中狀態
 * 懸停時展示視覺反饋
 */
.category-card:not(.border-primary):hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  border-color: #86b0bd !important;
}

/**
 * 選中狀態
 * 使用亮色底色和深色邊框標示
 */
.category-card.border-primary {
  background: #fff0dd;
  border-color: #432f28 !important;
  border-width: 2px;
  box-shadow: 0 0 0 3px rgba(134, 176, 189, 0.2);
}

/* 分類名稱樣式 */
.category-card h6 {
  color: #432f28;
  font-size: 0.85rem;
  line-height: 1.2;
}

/* 分類標籤樣式 */
.category-tags {
  color: #666;
  line-height: 1.4;
}

.category-card.border-primary .category-tags {
  color: #432f28;
  font-weight: 500;
}

/* 分類容器滾動 */
.category-container {
  max-height: 400px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: #d1d3d4 #f0f0f0;
}

.category-container::-webkit-scrollbar {
  width: 6px;
}

.category-container::-webkit-scrollbar-track {
  background: #f0f0f0;
  border-radius: 3px;
}

.category-container::-webkit-scrollbar-thumb {
  background: #d1d3d4;
  border-radius: 3px;
}

.category-container::-webkit-scrollbar-thumb:hover {
  background: #aaa;
}

/* 載入動畫顏色 */
.spinner-border {
  color: #432f28 !important;
}

/* 鍵盤焦點樣式 (無障礙) */
.category-card:focus-visible {
  outline: 2px solid #432f28;
  outline-offset: 2px;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .category-container {
    max-height: 250px;
  }

  .category-card {
    font-size: 0.7rem;
    min-height: 50px;
    padding: 0.5rem !important;
  }

  .category-card h6 {
    font-size: 0.8rem;
  }

  .category-tags {
    font-size: 0.65rem;
  }
}

@media (max-width: 576px) {
  .category-container {
    max-height: 200px;
  }

  .category-card {
    min-width: calc(50% - 0.5rem);
  }
}
</style>
