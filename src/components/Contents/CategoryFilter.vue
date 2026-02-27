<!-- CategoryFilter.vue -->
<!-- 
  分類篩選組件 (Pinia版)
  職責: 顯示分類列表、提供篩選功能、展示分類統計
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useForumStore } from "@/stores/forumStore";

const forumStore = useForumStore();
const route = useRoute();
const router = useRouter();

const allTopicsCount = computed(() => {
  return forumStore.categoriesWithStats.reduce((total, category) => {
    const count = Number(category?.postCount ?? 0);
    return total + (Number.isFinite(count) ? count : 0);
  }, 0);
});

// 使用帶實時統計的分類列表，最前面固定加入「全部」
const categories = computed(() => [
  {
    id: null,
    name: "全部",
    description: "顯示所有分類",
    postCount: allTopicsCount.value,
  },
  ...forumStore.categoriesWithStats,
]);

const handleSelectCategory = (categoryId) => {
  const isDetailPage = Boolean(route.params?.id);
  forumStore.selectCategory(categoryId, { clearSearch: isDetailPage });
  if (isDetailPage) {
    router.push({ name: "forum" });
  }
};

const handleClearCategory = () => {
  forumStore.selectedCategory = null;
  forumStore.newTopic.categoryId = null;
  forumStore.pagination.currentPage = 1;
  forumStore.fetchTopics();
  const isDetailPage = Boolean(route.params?.id);
  if (isDetailPage) {
    router.push({ name: "forum" });
  }
};
</script>

<template>
  <div class="card shadow-sm mb-3">
    <!-- 卡片標題 -->
    <div class="card-header bg-white py-3 d-flex flex-column gap-2">
      <div class="d-flex flex-wrap align-items-center gap-2">
        <h5
          class="mb-0 fs-6 fs-md-5"
          :class="{ 'clearable-title': forumStore.selectedCategory !== null }"
          @click="forumStore.selectedCategory !== null && handleClearCategory()"
          role="button"
          :tabindex="forumStore.selectedCategory !== null ? 0 : -1"
          @keydown.enter="
            forumStore.selectedCategory !== null && handleClearCategory()
          "
          @keydown.space.prevent="
            forumStore.selectedCategory !== null && handleClearCategory()
          "
        >
          📁 討論分類
          <span
            v-if="forumStore.selectedCategory !== null"
            class="clear-indicator ms-2"
            title="點擊清除分類"
          >
            ✕
          </span>
        </h5>
      </div>
    </div>

    <!-- 分類內容 -->
    <div class="p-2 p-md-3">
      <!-- 載入中提示 -->
      <div
        v-if="forumStore.isLoading && categories.length === 0"
        class="text-center py-3"
      >
        <div
          class="spinner-border spinner-border-sm text-primary"
          role="status"
        >
          <span class="visually-hidden">載入中...</span>
        </div>
      </div>

      <!-- 分類列表容器與取消按鈕 -->
      <div v-else class="d-flex gap-2 align-items-flex-start category-wrapper">
        <!-- 分類列表 -->
        <div
          class="d-flex flex-wrap align-content-start p-0 category-container flex-grow-1"
        >
          <!-- 分類卡片 -->
          <div v-for="category in categories" :key="category.id" class="m-1">
            <div
              class="category-card p-2 border rounded cursor-pointer"
              :class="{
                'border-primary bg-light':
                  forumStore.selectedCategory === category.id,
                'border-light': forumStore.selectedCategory !== category.id,
              }"
              @click="handleSelectCategory(category.id)"
              :title="category.description || category.name"
              role="button"
              tabindex="0"
              @keydown.enter="handleSelectCategory(category.id)"
              @keydown.space.prevent="handleSelectCategory(category.id)"
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
                <span class="d-inline-block"
                  >💬 {{ category.postCount ?? 0 }}</span
                >
              </div>
            </div>
          </div>

          <!-- 空狀態提示 -->
          <div
            v-if="!forumStore.isLoading && categories.length === 0"
            class="w-100 text-center text-muted py-3 small"
          >
            <p>目前沒有分類</p>
          </div>
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
  background-color: #86b0bd !important;
  border-bottom-color: #d1d3d4;
}

.card-header h5 {
  color: #fff;
  font-size: 0.95rem;
}

.card-header .badge,
.card-header .category-input {
  font-size: 0.95rem;
}

.card-header .badge.bg-info {
  display: inline-flex;
  align-items: center;
  height: 38px;
  padding: 0 0.9rem;
  border-radius: 0.375rem;
}

.badge.bg-info {
  background-color: #145369 !important;
  color: #fff0dd;
}

.sort-dropdown {
  display: inline-block;
  min-width: 0;
  width: fit-content;
  position: relative;
}

.category-input-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
  width: fit-content;
  cursor: pointer;
  transition: all 0.2s ease;
  border-radius: 4px;
  user-select: none;
}

.category-input-wrapper:hover {
  background-color: rgba(255, 255, 255, 0.2);
  transform: translateX(2px);
}

.category-input-wrapper:active {
  background-color: rgba(255, 255, 255, 0.3);
  transform: translateX(0);
}

.category-input {
  padding-left: 0.5rem;
  padding-right: 1.2rem;
  height: auto;
  line-height: 1.25;
  padding-top: 0.25rem;
  padding-bottom: 0.25rem;
  border: none;
  border-radius: 4px;
  background-color: #e8f1f4;
  color: #145369;
  font-size: 0.95rem;
  font-weight: 500;
  width: var(--sort-control-width, 10em);
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
}

.category-input.category-input--expanded {
  padding-right: 0.9rem;
}

.category-input-dropdown {
  position: absolute;
  top: 50%;
  right: 0.5rem;
  transform: translateY(-50%);
  color: #9aa0a6;
  font-size: 0.8em;
  pointer-events: auto;
}

.category-input:focus {
  border: none;
  box-shadow: none;
}

.category-input-wrapper:focus-within {
  outline: 2px solid rgba(255, 255, 255, 0.6);
  outline-offset: 2px;
}

.category-dropdown {
  position: absolute;
  top: calc(100% + 0.5rem);
  left: 0;
  right: auto;
  width: var(--sort-control-width, 10em);
  min-width: var(--sort-control-width, 10em);
  z-index: 20;
  max-height: 220px;
  overflow-y: auto;
  background: #f2f7f9;
  box-shadow: 0 8px 16px rgba(20, 83, 105, 0.12);
  border: 1px solid #d1d3d4;
  border-radius: 4px;
  padding: 0.35rem;
}

.sort-dropdown-item {
  display: block;
  width: 100%;
  padding: 0.35rem 0.5rem;
  border: 1px solid transparent;
  border-radius: 4px;
  background: transparent;
  color: #145369;
  text-align: left;
  white-space: nowrap;
  font-size: 0.95rem;
  font-weight: 500;
  line-height: 1.25;
  transition: all 0.2s ease;
}

.sort-dropdown-item + .sort-dropdown-item {
  margin-top: 0.2rem;
}

.sort-dropdown-item.active,
.sort-dropdown-item:hover {
  background: #dceaf0;
  color: #145369;
  transform: translateX(2px);
}

.sort-dropdown-item:active {
  transform: translateX(0);
}

.sort-dropdown-item:focus-visible {
  outline: 2px solid rgba(20, 83, 105, 0.25);
  outline-offset: 1px;
}

/* 可點擊的標題樣式 */
.card-header h5.clearable-title {
  cursor: pointer;
  transition: all 0.2s ease;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  user-select: none;
}

.card-header h5.clearable-title:hover {
  background-color: rgba(255, 255, 255, 0.2);
  transform: translateX(2px);
}

.card-header h5.clearable-title:focus-visible {
  outline: 2px solid rgba(255, 255, 255, 0.6);
  outline-offset: 2px;
}

.card-header h5.clearable-title:active {
  background-color: rgba(255, 255, 255, 0.3);
  transform: translateX(0);
}

/* 清除指示器 */
.clear-indicator {
  font-size: 0.8em;
  opacity: 0.8;
  transition: all 0.2s ease;
}

.card-header h5.clearable-title:hover .clear-indicator {
  opacity: 1;
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
  border-color: #145369 !important;
  border-width: 2px;
  box-shadow: 0 0 0 3px rgba(134, 176, 189, 0.2);
}

/* 分類名稱樣式 */
.category-card h6 {
  color: #145369;
  font-size: 0.85rem;
  line-height: 1.2;
}

/* 分類標籤樣式 */
.category-tags {
  color: #666;
  line-height: 1.4;
}

.category-card.border-primary .category-tags {
  color: #145369;
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
  color: #145369 !important;
}

/* 鍵盤焦點樣式 (無障礙) */
.category-card:focus-visible {
  outline: 2px solid #145369;
  outline-offset: 2px;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .category-container {
    max-height: 250px;
  }

  .sort-dropdown {
    width: fit-content;
    max-width: 100%;
  }

  .category-input-wrapper,
  .category-input {
    width: fit-content;
    max-width: 100%;
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
  .sort-dropdown {
    width: 100%;
    max-width: 100%;
  }

  .category-dropdown {
    width: 100%;
    min-width: 100%;
    padding: 0.4rem;
  }

  .sort-dropdown-item {
    min-height: 44px;
    padding: 0.55rem 0.65rem;
    display: flex;
    align-items: center;
    line-height: 1.2;
  }

  .category-container {
    max-height: none;
    overflow-x: auto;
    overflow-y: hidden;
    flex-wrap: nowrap !important;
    align-items: stretch;
    padding-bottom: 0.25rem;
  }

  .category-container > .m-1 {
    flex: 0 0 auto;
    margin: 0 0.35rem 0 0 !important;
  }

  .category-card {
    min-width: 150px;
    min-height: 44px;
    padding: 0.45rem 0.7rem !important;
  }

  .category-card h6 {
    margin-bottom: 0.2rem !important;
    font-size: 0.78rem;
  }

  .category-tags {
    font-size: 0.62rem;
    line-height: 1.2;
  }
}
</style>
