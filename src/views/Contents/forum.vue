<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import { useForumPost } from "@/composables/useForumPost";
import { useLoginGuardAlert } from "@/composables/useLoginGuardAlert";

// 組件導入
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import CategoryFilter from "@/components/Contents/CategoryFilter.vue";
import NewTopicModal from "@/components/Contents/NewTopicModal.vue";
import NewTopicSidebar from "@/components/Contents/NewTopicSidebar.vue";
import NewTopicBanners from "@/components/Contents/NewTopicBanners.vue";
import TopicListView from "@/components/Contents/TopicListView.vue";

// 可以抓到前端userID pinia
const { ensureLoggedIn } = useLoginGuardAlert();

// ========== 狀態和路由配置 ==========
const forumStore = useForumStore();
const route = useRoute();
const { hasDraftContent } = useForumPost();
const isFabPinned = ref(false);
let navbarObserver = null;

const hasExpandedDraft = computed(() =>
  (forumStore.drafts || []).some((draft) => !draft.isMinimized),
);

const isCommentLikeDraft = (draft) => {
  const type = draft?.contentType;
  return type === CONTENT_TYPES.COMMENT || type === CONTENT_TYPES.COMMENT_REPLY;
};

const getDraftExtraValues = (draft) => {
  if (!draft?.showEntrustMission) return [];
  const data = draft?.entrustMission || {};
  return [
    data.title,
    data.detail,
    data.rewardPoints,
    data.category,
    data.deadline,
  ];
};

const hasVisibleDraftContent = (draft) => {
  const titleToCheck = isCommentLikeDraft(draft) ? "" : draft.title;
  return hasDraftContent(titleToCheck, draft.body, getDraftExtraValues(draft));
};

const shouldShowSidebar = computed(() => {
  const drafts = forumStore.drafts || [];
  // 檢查是否有展開的編輯器或有內容的最小化草稿
  const hasExpandedEditor = drafts.some((d) => !d.isMinimized);
  const hasMinimizedWithContent = drafts.some(
    (d) => d.isMinimized && hasVisibleDraftContent(d),
  );
  return hasExpandedEditor || hasMinimizedWithContent;
});

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

const openNewTopicModal = async () => {
  const canContinue = await ensureLoggedIn("請先登入後操作");
  if (!canContinue) return;

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
 * 此函數被 resize 事件監聽器調用，確保響應式布局信息同步到全局狀態
 */
const updateWindowWidth = () => {
  forumStore.updateWindowWidth(window.innerWidth);
};

const setupNavbarObserver = () => {
  const navbarEl = document.querySelector("nav.navbar");
  if (!navbarEl || !("IntersectionObserver" in window)) {
    isFabPinned.value = window.scrollY > 80;
    return;
  }

  navbarObserver = new IntersectionObserver(
    ([entry]) => {
      isFabPinned.value = !entry.isIntersecting;
    },
    {
      threshold: 0,
    },
  );

  navbarObserver.observe(navbarEl);
};

/**
 * 初始化與清理
 * 此處是信息流的起點：
 * 1. updateWindowWidth() → forumStore.updateWindowWidth() (視窗寬度同步)
 * 2. fetchCategories() → 調用 API 層 → 獲取分類列表 → 更新 state.categories
 * 3. fetchTopics() → 調用 API 層 → 獲取主題列表 → 更新 state.topics
 */
onMounted(() => {
  updateWindowWidth();
  window.addEventListener("resize", updateWindowWidth);
  setupNavbarObserver();
  forumStore.fetchCategories();
  forumStore.setSortOption("latest");
});

onUnmounted(() => {
  window.removeEventListener("resize", updateWindowWidth);
  if (navbarObserver) {
    navbarObserver.disconnect();
    navbarObserver = null;
  }
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

/**
 * 【主要信息流轉點】 路由參數改變 → 打開線程詳情
 * 流程：
 * 1. 用戶點擊主題 → TopicListView.goToThread() → router.push({ params: { id: threadId } })
 * 2. route.params.id 變化 → 此 watch 被觸發
 * 3. forumStore.openThread(threadId) → 調用 API 獲取線程詳情 → 並觸發 fetchThreadRepliesPage 等
 * 4. 線程詳情更新到 state.threadDetail → TopicListView 的對應子組件重新渲染
 */
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

  <div class="container mt-3 mb-5">
    <div class="row g-3 g-md-4">
      <!-- 主要內容區 -->
      <div
        class="order-1 order-lg-2 forum-main"
        :class="shouldShowSidebar ? 'col-lg-9' : 'col-lg-12'"
      >
        <!-- 分類欄 -->
        <CategoryFilter />
        <!-- 新主題改為使用彈出視窗 -->
        <NewTopicModal />
        <TopicListView />
      </div>

      <!-- 側邊欄 -->
      <div
        v-if="shouldShowSidebar"
        class="order-2 order-lg-1 col-lg-3 forum-sidebar"
        :class="{ 'forum-sidebar--no-editor': !hasExpandedDraft }"
      >
        <div class="forum-sidebar-top">
          <NewTopicSidebar />
        </div>
        <div class="forum-sidebar-bottom">
          <NewTopicBanners />
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />

  <button
    class="btn btn-forum-primary forum-new-topic-fab d-inline-flex align-items-center justify-content-center"
    :class="{ 'forum-new-topic-fab--pinned': isFabPinned }"
    type="button"
    @click="openNewTopicModal"
    aria-label="發佈新主題"
  >
    <i class="fas fa-plus"></i>
    <span class="ms-2 d-none d-sm-inline">發佈新主題</span>
    <span class="ms-2 d-none d-sm-inline">發佈新主題</span>
  </button>
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
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

@media (min-width: 992px) {
  .forum-sidebar-top {
    position: sticky;
    top: 1rem;
    width: 100%;
    z-index: 60;
  }

  .forum-sidebar-bottom {
    position: sticky;
    bottom: 1rem;
    margin-top: auto;
    z-index: 40;
  }

  .forum-sidebar--no-editor .forum-sidebar-bottom {
    z-index: 80;
  }
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

/* 載入動畫顏色 */
.spinner-border {
  color: #145369 !important;
}

/* 文字顏色 */
.text-primary {
  color: #145369 !important;
}

/* 浮動發佈新主題按鈕*/
.forum-new-topic-fab {
  position: fixed;
  /* 一般狀態位置（桌機）：調整右側距離 */
  right: 20px;
  /* 一般狀態位置（桌機）：調整上方距離 */
  top: 96px;
  z-index: 990;
  padding: 0.6rem 1rem;
  border-radius: 999px;
  box-shadow: 0 8px 18px rgba(20, 83, 105, 0.25);
  transition: top 0.2s ease;
}

.forum-new-topic-fab--pinned {
  /* 黏在右上角後（桌機）：調整上方距離 */
  top: 14px;
}

@media (max-width: 991.98px) {
  .forum-new-topic-fab {
    /* 一般狀態位置（手機）：原本靠右側中線 */
    top: 50%;
    right: -10px;
    bottom: auto;
    transform: translateY(-50%);
    border-radius: 30px 0 0 30px;
    width: auto;
    height: 45px;
    padding: 0 15px 0 18px;
    font-size: 0.9rem;
    box-shadow: -4px 0 15px rgba(20, 83, 105, 0.35);
  }

  .forum-new-topic-fab.forum-new-topic-fab--pinned {
    /* 黏在右上角後（手機）：調整上方距離 */
    top: 14px;
    /* 黏在右上角後（手機）：調整右側距離 */
    right: 10px;
    transform: none;
    border-radius: 999px;
    padding: 0.45rem 0.85rem;
    height: auto;
    box-shadow: 0 8px 18px rgba(20, 83, 105, 0.25);
  }

  .forum-new-topic-fab span {
    display: none !important;
  }

  .forum-new-topic-fab i {
    margin-right: 0 !important;
  }
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
