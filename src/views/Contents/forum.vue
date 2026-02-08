<script setup>
import { nextTick, onMounted, onUnmounted, watch } from "vue";
import { useRoute } from "vue-router";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";

// 組件導入
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import CategoryFilter from "@/components/Contents/CategoryFilter.vue";
import NewTopicModal from "@/components/Contents/NewTopicModal.vue";
import NewTopicSidebar from "@/components/Contents/NewTopicSidebar.vue";
import TopicListView from "@/components/Contents/TopicListView.vue";

// 可以抓到前端userID pinia
import { useAuthStore } from "@/stores/user/auth.js";
const authStore = useAuthStore();
// 可以抓到前端userID pinia

const forumStore = useForumStore();
const route = useRoute();

const tryScrollToPendingTarget = async () => {
  const targetId = forumStore.pendingScrollTargetId;
  if (!targetId) return;

  await nextTick();
  const targetEl = document.getElementById(`content-${targetId}`);
  if (targetEl) {
    const offsetTop =
      targetEl.getBoundingClientRect().top + window.scrollY - 60;
    window.scrollTo({ top: offsetTop, behavior: "smooth" });
    forumStore.pendingScrollTargetId = null;
  }
};

const resolveThreadId = (rawId) => {
  if (rawId === undefined || rawId === null || rawId === "") return null;
  const numericId = Number(rawId);
  return Number.isNaN(numericId) ? rawId : numericId;
};

const openNewTopicModal = () => {
  forumStore.newTopic.title = "";
  forumStore.newTopic.body = "";
  forumStore.newTopic.categoryId = forumStore.selectedCategory ?? null;
  forumStore.newTopic.parentContentId = null;
  forumStore.newTopic.categorySearch = "";
  forumStore.newTopic.mode = "post";
  forumStore.newTopic.contentType = CONTENT_TYPES.POST;
  forumStore.toggleNewTopicSidebar(false);
  forumStore.toggleNewTopicModal(true);
};

/**
 * 同步視窗寬度到 store (供子組件使用)
 */
const updateWindowWidth = () => {
  forumStore.updateWindowWidth(window.innerWidth);
};

/**
 * 初始化與清理
 */
onMounted(() => {
  updateWindowWidth();
  window.addEventListener("resize", updateWindowWidth);
  forumStore.fetchCategories();
  forumStore.fetchTopics();
});

onUnmounted(() => {
  window.removeEventListener("resize", updateWindowWidth);
});

/**
 * 監聽 refresh 訊號
 */
watch(
  () => forumStore.lastRefreshTime,
  () => {
    forumStore.fetchTopics();
  },
);

watch(
  () => route.params.id,
  (threadId) => {
    const resolvedId = resolveThreadId(threadId);
    if (resolvedId) {
      forumStore.openThread(resolvedId);
    } else {
      forumStore.closeThread();
    }
  },
  { immediate: true },
);

watch(
  () => forumStore.pendingScrollTargetId,
  () => {
    if (forumStore.pendingScrollTargetId) {
      tryScrollToPendingTarget();
    }
  },
);

watch(
  () => [
    forumStore.threadDetail.id,
    forumStore.threadPosts.length,
    forumStore.threadComments.length,
  ],
  () => {
    if (forumStore.pendingScrollTargetId) {
      tryScrollToPendingTarget();
    }
  },
);
</script>

<template>
  <DefaultNavbar light />

  <div class="container mt-5 mb-5">
    <div class="row g-3 g-md-4">
      <!-- 主要內容區 -->
      <div class="col-lg-9 order-2 order-lg-1">
        <!-- 分類欄 -->
        <CategoryFilter />
        <!-- 新主題改為使用彈出視窗 -->
        <NewTopicModal />
        <TopicListView />
      </div>

      <!-- 側邊欄 -->
      <div class="col-lg-3 forum-sidebar">
        <div class="card shadow-sm mb-4">
          <div class="card-header bg-white">
            <h6 class="mb-0">📊 論壇統計</h6>
          </div>
          <div class="card-body">
            <div class="mb-3">
              <div
                class="d-flex justify-content-between align-items-center mb-1"
              >
                <span class="small text-muted">總主題數</span>
                <strong class="text-primary">{{
                  forumStore.pagination.totalCount
                }}</strong>
              </div>
            </div>

            <button
              class="btn btn-forum-primary w-100"
              type="button"
              @click="openNewTopicModal"
            >
              <i class="fas fa-plus"></i>
              <span class="ms-2">發佈新主題</span>
            </button>
          </div>
        </div>
        <NewTopicSidebar />
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped>
/* 自定義顏色方案 */
:root {
  --forum-primary: #145369;
  --forum-light: #fff0dd;
  --forum-gray: #d1d3d4;
  --forum-accent: #86b0bd;
}

/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #145369;
  border-color: #145369;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(20, 83, 105, 0.3);
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

.btn-forum-secondary:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}

/* 徽章樣式 */
.badge.bg-secondary {
  background-color: #86b0bd !important;
  color: #432f28;
}

.badge.bg-info {
  background-color: #145369 !important;
  color: #fff0dd;
}

/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

.card-header {
  background-color: #86b0bd !important;
  border-bottom-color: #d1d3d4;
}

.card-header h5,
.card-header h6 {
  color: #fff;
}

/* 側邊欄排版，確保 banner 貼底 */
.forum-sidebar {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

/* 表格樣式 */
.table-light {
  background-color: #fff0dd !important;
  color: #432f28;
}

.table-hover tbody tr:hover {
  background-color: rgba(255, 240, 221, 0.5) !important;
}

/* 鏈接顏色 */
.topic-title-link {
  color: #432f28;
}

.topic-title-link:hover {
  color: #86b0bd;
}

/* 分頁樣式 */
.pagination .page-item.active .page-link {
  background-color: #145369;
  border-color: #145369;
  color: #fff0dd;
}

.pagination .page-link {
  color: #145369;
  border-color: #d1d3d4;
}

.pagination .page-link:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #145369;
}

/* 載入動畫顏色 */
.spinner-border {
  color: #145369 !important;
}

/* 文字顏色 */
.text-primary {
  color: #145369 !important;
}

/* 留言框樣式 */
.reply-box {
  background-color: rgba(255, 240, 221, 0.5);
  border-left-color: #145369;
}

/* 邊框顏色 */
.border-forum-selected {
  border-color: #145369 !important;
  border-width: 2px !important;
}

/* 分類卡片樣式 */
.category-card {
  transition: all 0.2s ease;
  border-width: 1px;
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem !important;
  min-height: 60px;
}

.category-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

/* 選中狀態 */
.category-card.border-forum-selected {
  background: rgba(255, 240, 221, 0.8);
  box-shadow: 0 0 0 3px rgba(134, 176, 189, 0.3);
}

/* 表格行樣式 */
.forum-row {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.forum-row:hover {
  background-color: rgba(255, 240, 221, 0.3) !important;
}

.table {
  margin-bottom: 0;
}

.table thead {
  font-weight: 600;
  font-size: 0.9rem;
}

/* 主題標題連結樣式 */
.topic-title-link {
  text-decoration: none;
  font-weight: 500;
  transition: color 0.2s ease;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .table {
    font-size: 0.9rem;
  }

  .table td {
    padding: 0.75rem 0.5rem;
  }

  .topic-title-link {
    font-size: 0.95rem;
  }
}

@media (max-width: 576px) {
  .table {
    font-size: 0.85rem;
  }

  .table td {
    padding: 0.6rem 0.5rem;
  }

  .table thead {
    font-size: 0.8rem;
  }
}

/* 留言框樣式 */
.reply-box {
  padding: 1rem;
  border-radius: 0.5rem;
}

.reply-box textarea {
  resize: vertical;
  min-height: 80px;
}

.reply-box .btn {
  font-size: 0.875rem;
}

/* 分類容器響應式高度 */
.category-container {
  max-height: 400px;
  overflow-y: auto;
}

/* 帖文詳情中的圖片樣式 */
.topic-image-thumb {
  max-width: 100%;
  height: auto;
  border-radius: 8px;
  border: 1px solid #d1d3d4;
  background: #fff0dd;
}

.thread-detail-image {
  max-width: 100%;
  max-height: 500px;
  object-fit: scale-down;
}

@media (max-width: 768px) {
  .category-container {
    max-height: 250px;
  }

  .thread-detail-image {
    max-height: 400px;
  }
}

@media (max-width: 576px) {
  .category-container {
    max-height: 200px;
  }

  .thread-detail-image {
    max-height: 300px;
  }
}
</style>
