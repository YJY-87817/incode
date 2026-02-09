<!-- TopicListView.vue -->
<!-- 
  主題列表視圖組件 (含列表/詳情雙視圖)
  職責: 管理主題列表顯示、詳情頁切換、分頁、回覆功能
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { ref, computed, watch } from "vue";
import { useRouter } from "vue-router";
import apiClient from "@/api/request.js";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
import QuickReplyBox from "@/components/Contents/QuickReplyBox.vue";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";
import HtmlIframe from "@/components/HtmlIframe.vue";
import UserHoverName from "@/components/UserHoverName.vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import { formatDate, formatTimeAgo } from "@/utils/contentBody.js";
import Swal from "sweetalert2";

const forumStore = useForumStore();
const router = useRouter();

const getAvatarUrl = (name, explicitUrl = "") => {
  if (explicitUrl) return explicitUrl;
  const safeName = String(name || "User").trim() || "User";
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(safeName)}&background=random`;
};

const basePreviewCss = `
  body {
    margin: 0;
    padding: 0;
    white-space: normal;
    line-height: 1.3;
  }
  * {
    margin: 0;
    padding: 0;
  }
  p, div, br {
    margin: 0;
    padding: 0;
    line-height: 1.3;
  }
  br {
    display: none;
  }
  img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 2px 0;
  }
  a {
    color: inherit;
    text-decoration: none;
  }
`;

const getTopicPreviewCss = (topic) => {
  const hasImages = Array.isArray(topic?.imageUrls) && topic.imageUrls.length;
  if (hasImages) return `${basePreviewCss} img { display: none; }`;
  return basePreviewCss;
};

const goToThread = (threadId) => {
  if (!threadId) return;
  router.push({ name: "forum-thread", params: { id: threadId } });
};

const goToList = () => {
  router.push({ name: "forum" });
};

const threadHasInlineImages = computed(() => {
  const html = forumStore.threadDetail?.contentHtml || "";
  return /<img\b/i.test(html);
});

// ========== 留言/回文輸入狀態 ==========
const showQuickReply = ref(false);
const replyTab = ref(null);

/**
 * 處理主題互動成功
 */
const handleTopicInteractionSuccess = async (data) => {
  // 隱藏功能的特殊處理
  if (data.categoryId === 2 && data.categoryName === "hide") {
    // 立即從列表中移除隱藏的主題
    forumStore.topics = forumStore.topics.filter(
      (topic) => topic.id !== data.contentId,
    );

    // 隱藏或解除隱藏後，重新載入主題列表以反映後端狀態
    // fetchTopics 會更新分類統計
    await forumStore.fetchTopics();

    if (forumStore.selectedThread) {
      // 在詳情視圖中，隱藏後返回列表
      goToList();
    }
  }
};

/**
 * 處理主題互動錯誤
 */
const handleTopicInteractionError = async (data) => {
  console.error("主題互動失敗:", data);
  await Swal.fire({
    icon: "error",
    title: "互動失敗",
    text: data.error || "操作失敗，請稍後再試",
    confirmButtonText: "確定",
    confirmButtonColor: "#145369",
  });
};

/**
 * 處理主題編輯操作
 */
const handleTopicEditContent = (contentId) => {
  // 尋找對應的主題或回文進行編輯
  const targetContent =
    forumStore.threadDetail?.id === contentId
      ? forumStore.threadDetail
      : forumStore.topics.find((t) => t.id === contentId);

  if (targetContent) {
    forumStore.openEditMode(contentId, targetContent);
  }
};

/**
 * 處理主題刪除操作
 */
const handleTopicDeleteContent = async (contentId) => {
  // 刪除成功後重新載入數據
  // fetchTopics 會更新分類統計
  await forumStore.fetchTopics();

  if (forumStore.threadDetail?.id === contentId) {
    // 如果刪除的是當前詳情中的主題，返回列表
    goToList();
  } else if (forumStore.threadDetail?.id) {
    // 否則重新載入詳情
    forumStore.fetchThreadDetail(forumStore.threadDetail.id);
  }
};

/**
 * 分頁相關計算
 */
const displayPages = computed(() => forumStore.displayPages);

const sortOption = computed({
  get: () => forumStore.sortOption,
  set: (value) => forumStore.setSortOption(value),
});

/**
 * 執行線程分頁
 */
const threadTotalPages = computed(() => forumStore.threadTotalPages);

const paginatedThreadPosts = computed(() => forumStore.paginatedThreadPosts);

const threadCommentsTotalPages = computed(
  () => forumStore.threadCommentsTotalPages,
);

const paginatedThreadComments = computed(
  () => forumStore.paginatedThreadComments,
);

/**
 * 頁面導航
 */
const goToPage = (page) => {
  forumStore.changePage(page);
};

const goToThreadPage = (page) => {
  forumStore.changeThreadPage(page);
};

const goToCommentPage = (page) => {
  forumStore.changeThreadCommentsPage(page);
};

const prevThreadPage = () => {
  if (forumStore.threadCurrentPage > 1) {
    forumStore.changeThreadPage(forumStore.threadCurrentPage - 1);
  }
};

const nextThreadPage = () => {
  if (forumStore.threadCurrentPage < threadTotalPages.value) {
    forumStore.changeThreadPage(forumStore.threadCurrentPage + 1);
  }
};

const prevCommentPage = () => {
  if (forumStore.threadCommentsCurrentPage > 1) {
    forumStore.changeThreadCommentsPage(
      forumStore.threadCommentsCurrentPage - 1,
    );
  }
};

const nextCommentPage = () => {
  if (forumStore.threadCommentsCurrentPage < threadCommentsTotalPages.value) {
    forumStore.changeThreadCommentsPage(
      forumStore.threadCommentsCurrentPage + 1,
    );
  }
};

/**
 * 留言/回文輸入框相關
 */
const selectQuickReply = () => {
  // Toggle功能：如果已經是簡易留言模式，則切換展開/縮合狀態
  if (replyTab.value === "quick" && showQuickReply.value) {
    showQuickReply.value = false;
    replyTab.value = null;
  } else {
    replyTab.value = "quick";
    showQuickReply.value = true;
  }
};

const selectDetailedComment = () => {
  replyTab.value = "detailed";
  if (showQuickReply.value && forumStore.threadDetail?.id) {
    forumStore.clearQuickReply(forumStore.threadDetail.id);
  }
  showQuickReply.value = false;
  openDetailedComment();
};

const closeQuickReply = () => {
  showQuickReply.value = false;
  replyTab.value = null;
  if (forumStore.threadDetail?.id) {
    forumStore.cancelQuickReply(forumStore.threadDetail.id);
  }
};

const submitQuickReply = async () => {
  if (!forumStore.threadDetail?.id) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "未找到對應的主題，請重新嘗試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return;
  }

  const quickReplyContent = forumStore.getQuickReplyContent(
    forumStore.threadDetail.id,
  );

  if (!quickReplyContent.trim()) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請輸入內容",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return;
  }

  const isSubmitting = forumStore.getQuickReplySubmitting(
    forumStore.threadDetail.id,
  );
  if (isSubmitting) return;

  forumStore.setQuickReplySubmitting(forumStore.threadDetail.id, true);

  const dto = {
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId: forumStore.threadDetail.categoryId,
    parentContentId: forumStore.threadDetail.id,
    title: buildReplyTitle(forumStore.threadDetail.title),
    body: quickReplyContent.trim(),
    type: CONTENT_TYPES.COMMENT,
  };

  try {
    await apiClient.post("/ContentApi", dto);
    await Swal.fire({
      icon: "success",
      title: "已送出！",
      text: "簡易留言已成功送出",
      timer: 1500,
      showConfirmButton: false,
    });

    // 直接调用 store 重新获取详情
    if (forumStore.selectedThread) {
      await forumStore.fetchThreadDetail(forumStore.selectedThread);
    }

    forumStore.clearQuickReply(forumStore.threadDetail.id);
    showQuickReply.value = false;
    replyTab.value = null;
  } catch (error) {
    console.error("送出留言/回文失敗:", error);
    await Swal.fire({
      icon: "error",
      title: "送出失敗",
      text: "送出失敗，請稍後再試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
  } finally {
    forumStore.setQuickReplySubmitting(forumStore.threadDetail.id, false);
  }
};

function autoResizeTextarea(event) {
  const textarea = event?.target;
  if (!textarea) return;
  textarea.style.height = "auto";
  textarea.style.height = `${textarea.scrollHeight}px`;
}

const getCategoryIdValue = (cat) =>
  cat?.id ??
  cat?.content_category_id ??
  cat?.categoryId ??
  cat?.category_id ??
  null;

const getParentCategoryIdValue = (cat) =>
  cat?.parentCategoryId ??
  cat?.parent_category_id ??
  cat?.parentCategoryID ??
  cat?.parent_id ??
  null;

const getCategoryName = (cat) =>
  cat?.name ?? cat?.categoryName ?? cat?.category_name ?? "";

const buildReplyTitle = (parentTitle) => {
  const base = String(parentTitle ?? "").trim();
  return base ? `Re: ${base}` : "Re:";
};

const openDetailedComment = () => {
  if (!forumStore.threadDetail?.id) {
    console.error("[openDetailedComment] No threadDetail.id found");
    return;
  }

  // 檢查是否已經有針對這個主文的詳細留言草稿
  const existingDraft = forumStore.drafts.find(
    (draft) =>
      draft.replyingToContentId === forumStore.threadDetail.id &&
      draft.mode === "comment",
  );

  if (existingDraft) {
    // 如果已存在草稿，先最小化其他所有的draft
    forumStore.drafts.forEach((draft) => {
      if (draft.id !== existingDraft.id && !draft.isMinimized) {
        forumStore.toggleDraftMinimize(draft.id, true);
      }
    });
    // 再展開這個草稿
    forumStore.toggleDraftMinimize(existingDraft.id, false);
  } else {
    // 如果不存在草稿，先最小化所有已展開的draft
    forumStore.drafts.forEach((draft) => {
      if (!draft.isMinimized) {
        forumStore.toggleDraftMinimize(draft.id, true);
      }
    });

    // 如果不存在草稿，創建新的草稿
    const threadCategoryId = forumStore.threadDetail.categoryId ?? null;

    const categories = forumStore.categories || [];

    const matchedCategory = categories.find(
      (cat) => String(getCategoryIdValue(cat)) === String(threadCategoryId),
    );

    const parentCategoryId = matchedCategory
      ? getParentCategoryIdValue(matchedCategory)
      : null;

    const parentCategory = parentCategoryId
      ? categories.find(
          (cat) => String(getCategoryIdValue(cat)) === String(parentCategoryId),
        )
      : null;

    const categorySearchName = parentCategory
      ? getCategoryName(parentCategory)
      : matchedCategory
        ? getCategoryName(matchedCategory)
        : "";

    forumStore.selectedCategory = threadCategoryId;

    // 創建新的草稿
    forumStore.createDraft({
      title: buildReplyTitle(forumStore.threadDetail.title),
      body: "",
      categoryId: threadCategoryId,
      parentContentId: forumStore.threadDetail.id,
      categorySearch: categorySearchName,
      mode: "comment",
      contentType: CONTENT_TYPES.COMMENT,
      replyingToContentId: forumStore.threadDetail.id,
      replyingToContentData: {
        id: forumStore.threadDetail.id,
        title: forumStore.threadDetail.title,
        author: forumStore.threadDetail.author,
        createdAt: forumStore.threadDetail.createdAt,
        contentText: forumStore.threadDetail.contentText,
      },
      replyingToThreadId: forumStore.threadDetail.id,
    });
  }
};

const openDetailedReply = () => {
  if (!forumStore.threadDetail?.id) {
    console.error("[openDetailedReply] No threadDetail.id found");
    return;
  }

  // 檢查是否已經有針對這個主文的詳細回文草稿
  const existingDraft = forumStore.drafts.find(
    (draft) =>
      draft.replyingToContentId === forumStore.threadDetail.id &&
      draft.mode === "reply",
  );

  if (existingDraft) {
    // 如果已存在草稿，先最小化其他所有的draft
    forumStore.drafts.forEach((draft) => {
      if (draft.id !== existingDraft.id && !draft.isMinimized) {
        forumStore.toggleDraftMinimize(draft.id, true);
      }
    });
    // 再展開這個草稿
    forumStore.toggleDraftMinimize(existingDraft.id, false);
  } else {
    // 如果不存在草稿，先最小化所有已展開的draft
    forumStore.drafts.forEach((draft) => {
      if (!draft.isMinimized) {
        forumStore.toggleDraftMinimize(draft.id, true);
      }
    });

    // 如果不存在草稿，創建新的草稿
    const threadCategoryId = forumStore.threadDetail.categoryId ?? null;
    const categories = forumStore.categories || [];

    const matchedCategory = categories.find(
      (cat) => String(getCategoryIdValue(cat)) === String(threadCategoryId),
    );

    const parentCategoryId = matchedCategory
      ? getParentCategoryIdValue(matchedCategory)
      : null;

    const parentCategory = parentCategoryId
      ? categories.find(
          (cat) => String(getCategoryIdValue(cat)) === String(parentCategoryId),
        )
      : null;

    const categorySearchName = parentCategory
      ? getCategoryName(parentCategory)
      : matchedCategory
        ? getCategoryName(matchedCategory)
        : "";

    forumStore.selectedCategory = threadCategoryId;

    // 創建新的草稿
    forumStore.createDraft({
      title: "",
      body: "",
      categoryId: threadCategoryId,
      parentContentId: forumStore.threadDetail.id,
      categorySearch: categorySearchName,
      mode: "reply",
      contentType: CONTENT_TYPES.REPLY,
      replyingToContentId: forumStore.threadDetail.id,
      replyingToContentData: {
        id: forumStore.threadDetail.id,
        title: forumStore.threadDetail.title,
        author: forumStore.threadDetail.author,
        createdAt: forumStore.threadDetail.createdAt,
        contentText: forumStore.threadDetail.contentText,
      },
      replyingToThreadId: forumStore.threadDetail.id,
    });
  }
};

// 主文是否為正在回覆的目標（檢查所有草稿）
const isThreadReplyingTarget = computed(() => {
  const targetId = forumStore.threadDetail?.id;
  if (!targetId) return false;
  return forumStore.drafts.some(
    (draft) => draft.replyingToContentId === targetId,
  );
});

/**
 * 過濾掉已隱藏的主題
 */
// 點擊高亮的"正在回覆此內容"切換對應草稿的展開/最小化狀態
function handleThreadReplyingTargetClick() {
  if (!isThreadReplyingTarget.value) return;

  const targetId = forumStore.threadDetail?.id;
  if (!targetId) return;

  const targetDraft = forumStore.drafts.find(
    (draft) => draft.replyingToContentId === targetId,
  );

  if (targetDraft) {
    forumStore.toggleDraftMinimize(targetDraft.id, !targetDraft.isMinimized);
  }
}
</script>

<template>
  <div class="card shadow-sm">
    <!-- 卡片標題 (列表視圖) -->
    <div
      v-if="!forumStore.selectedThread"
      class="card-header bg-white d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center gap-2 py-3"
    >
      <h5 class="mb-0 fs-6 fs-md-5">📋 討論主題</h5>
      <div class="d-flex flex-wrap gap-2 align-items-center">
        <span class="badge bg-secondary" v-if="forumStore.selectedCategoryName">
          {{ forumStore.selectedCategoryName }}
        </span>
        <span class="badge bg-info">
          共 {{ forumStore.pagination.totalCount }} 筆
        </span>
        <div class="ms-sm-2">
          <select
            v-model="sortOption"
            class="form-select form-select-sm"
            aria-label="主題排序"
          >
            <option value="createdAt_desc">發佈時間（新→舊）</option>
          </select>
        </div>
      </div>
    </div>

    <!-- 卡片內容 -->
    <div class="card-body p-0">
      <!-- 詳情視圖 -->
      <div v-if="forumStore.selectedThread">
        <!-- 返回按鈕 -->
        <div class="p-3 border-bottom">
          <button class="btn btn-sm btn-forum-secondary" @click="goToList">
            <i class="fas fa-arrow-left me-2"></i>
            返回列表
          </button>
        </div>

        <!-- 帖文內容 -->
        <div
          class="card-body thread-detail-card"
          :class="{
            'replying-target': isThreadReplyingTarget,
            'cursor-pointer': isThreadReplyingTarget,
          }"
          :id="
            forumStore.threadDetail?.id != null
              ? `content-${forumStore.threadDetail.id}`
              : null
          "
          @click="handleThreadReplyingTargetClick"
        >
          <!-- 主題徽章 -->
          <div class="d-flex flex-wrap gap-1">
            <span
              v-if="forumStore.threadDetail?.categoryName"
              class="badge bg-secondary-light text-dark"
            >
              {{ forumStore.threadDetail.categoryName }}
            </span>
            <button
              v-if="
                forumStore.threadDetail?.type === CONTENT_TYPES.REPLY &&
                forumStore.threadDetail?.parentContentId
              "
              class="btn btn-sm btn-outline-primary parent-link-btn"
              type="button"
              @click.stop="goToThread(forumStore.threadDetail.parentContentId)"
            >
              <i class="fas fa-link me-1"></i>
              主文
            </button>
            <span
              v-if="forumStore.threadDetail?.isPinned"
              class="badge bg-danger"
            >
              置頂
            </span>
            <span
              v-if="forumStore.threadDetail?.isHot"
              class="badge bg-warning text-dark"
            >
              熱門
            </span>
          </div>

          <div
            class="d-flex align-items-center justify-content-between gap-2 flex-wrap"
          >
            <h3
              class="card-title thread-detail-title fs-4 fs-md-3 text-break mb-0 mt-0"
            >
              {{ forumStore.threadDetail.title }}
            </h3>

            <div class="interaction-container" @click.stop>
              <InteractionButtons
                v-if="forumStore.threadDetail?.id != null"
                :key="`thread-detail-interactions-${forumStore.threadDetail.id}`"
                :contentId="Number(forumStore.threadDetail.id)"
                :authorUserId="forumStore.threadDetail.userId"
                @interaction-success="handleTopicInteractionSuccess"
                @interaction-error="handleTopicInteractionError"
                @edit-content="handleTopicEditContent"
                @delete-content="handleTopicDeleteContent"
              />
            </div>
          </div>
          <p
            class="text-muted mb-2 small d-flex flex-wrap gap-2 align-items-center"
          >
            <span>by</span>
            <UserHoverName
              :user-id="forumStore.threadDetail.userId"
              :username="forumStore.threadDetail.author"
              :avatar-url="getAvatarUrl(forumStore.threadDetail.author)"
              :size="26"
            />
            <span>·</span>
            <span>{{ formatDate(forumStore.threadDetail.createdAt) }}</span>
          </p>

          <!-- 主題互動按鈕已移到標題右側 -->

          <HtmlIframe
            class="card-text rich-content"
            :html="
              forumStore.threadDetail.contentHtml ||
              forumStore.threadDetail.content
            "
            :min-height="120"
            text-color="#432f28"
            :allow-scripts="true"
            :extra-css="`body { white-space: pre-wrap; } img:not([style*='width']) { width: 30%; } img { max-width: 100%; height: auto; object-fit: contain; }`"
          />
          <!-- 改圖片大小在上面這裡 -->

          <!-- 帖文圖片 -->
          <div
            v-if="
              !threadHasInlineImages &&
              forumStore.threadDetail.imageUrls &&
              forumStore.threadDetail.imageUrls.length
            "
            class="d-flex flex-wrap gap-2 mt-2 topic-image-list"
          >
            <img
              v-for="(url, idx) in forumStore.threadDetail.imageUrls"
              :key="url + idx"
              :src="url"
              alt="貼文圖片"
              class="topic-image-thumb thread-detail-image"
              loading="lazy"
            />
          </div>
        </div>

        <!-- 回覆區域 -->
        <div class="pl-3 pb-3 pr-3">
          <div class="">
            <div
              class="d-flex flex-wrap gap-2 align-items-center quick-reply-toggle border-bottom pb-3"
            >
              <div class="reply-mode-actions">
                <div class="reply-mode-tabs">
                  <button
                    class="nav-link"
                    type="button"
                    :class="{ active: replyTab === 'quick' }"
                    @click="selectQuickReply"
                  >
                    留言
                  </button>
                  <button
                    class="nav-link"
                    type="button"
                    :class="{ active: replyTab === 'detailed' }"
                    @click="selectDetailedComment"
                  >
                    <i class="fas fa-pen me-1"></i>
                    詳細留言
                  </button>
                </div>
                <button
                  class="nav-link reply-standalone-btn"
                  type="button"
                  @click="openDetailedReply"
                >
                  <i class="fas fa-solid fa-retweet me-1"></i>
                  回文
                </button>
              </div>
            </div>

            <QuickReplyBox
              v-if="showQuickReply"
              :content-id="forumStore.threadDetail.id"
              @submit="submitQuickReply"
              @cancel="closeQuickReply"
              @textarea-input="autoResizeTextarea"
            />

            <!-- 所有留言 (type=3) -->
            <div
              v-if="forumStore.threadComments.length > 0"
              class="mb-4 comment-list"
            >
              <ThreadPost
                v-for="comment in paginatedThreadComments"
                :key="'comment-' + comment.id"
                :post="comment"
                :category-id="forumStore.threadDetail.categoryId"
                :hide-nested-replies="false"
                :preview-only="true"
              />
              <nav v-if="threadCommentsTotalPages > 1" class="mt-3">
                <ul
                  class="pagination pagination-sm justify-content-center flex-wrap gap-1"
                >
                  <li
                    class="page-item"
                    :class="{
                      disabled: forumStore.threadCommentsCurrentPage === 1,
                    }"
                    @click="prevCommentPage"
                  >
                    <a class="page-link px-2 px-md-3" href="javascript:void(0)">
                      <i class="fa-solid fa-caret-left"></i>
                    </a>
                  </li>
                  <li
                    class="page-item"
                    v-for="page in threadCommentsTotalPages"
                    :key="`comment-page-${page}`"
                    :class="{
                      active: forumStore.threadCommentsCurrentPage === page,
                    }"
                    @click="goToCommentPage(page)"
                  >
                    <a class="page-link px-2 px-md-3" href="javascript:void(0)">
                      {{ page }}
                    </a>
                  </li>
                  <li
                    class="page-item"
                    :class="{
                      disabled:
                        forumStore.threadCommentsCurrentPage ===
                        threadCommentsTotalPages,
                    }"
                    @click="nextCommentPage"
                  >
                    <a class="page-link px-2 px-md-3" href="javascript:void(0)">
                      <i class="fa-solid fa-caret-right"></i>
                    </a>
                  </li>
                </ul>
              </nav>
            </div>

            <!-- 回文分頁 (type=2) -->
            <div v-if="paginatedThreadPosts.length > 0" class="mb-4 reply-list">
              <ThreadPost
                v-for="post in paginatedThreadPosts"
                :key="'reply-' + post.id"
                :post="post"
                :category-id="forumStore.threadDetail.categoryId"
                :hide-nested-replies="false"
                :preview-only="true"
                :show-parent-link="false"
              />
            </div>

            <!-- 分頁導航 -->
            <nav v-if="threadTotalPages > 1" class="mt-3 mt-md-4">
              <ul
                class="pagination pagination-sm justify-content-center flex-wrap gap-1"
              >
                <li
                  class="page-item"
                  :class="{ disabled: forumStore.threadCurrentPage === 1 }"
                  @click="prevThreadPage"
                >
                  <a class="page-link px-2 px-md-3" href="javascript:void(0)">
                    <i class="fa-solid fa-caret-left"></i>
                  </a>
                </li>
                <li
                  class="page-item"
                  v-for="page in threadTotalPages"
                  :key="page"
                  :class="{ active: forumStore.threadCurrentPage === page }"
                  @click="goToThreadPage(page)"
                >
                  <a class="page-link px-2 px-md-3" href="javascript:void(0)">
                    {{ page }}
                  </a>
                </li>
                <li
                  class="page-item"
                  :class="{
                    disabled: forumStore.threadCurrentPage === threadTotalPages,
                  }"
                  @click="nextThreadPage"
                >
                  <a class="page-link px-2 px-md-3" href="javascript:void(0)">
                    <i class="fa-solid fa-caret-right"></i>
                  </a>
                </li>
              </ul>
            </nav>
          </div>
        </div>
      </div>

      <!-- 列表視圖 -->
      <div v-else>
        <!-- 載入中 -->
        <div
          v-if="forumStore.isLoading && forumStore.topics.length === 0"
          class="text-center py-5"
        >
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">載入中...</span>
          </div>
        </div>

        <!-- 空狀態 -->
        <div
          v-else-if="forumStore.topics.length === 0"
          class="text-center py-5 text-muted"
        >
          <p>此分類目前沒有主題</p>
        </div>

        <!-- 主題表格 -->
        <div v-else class="table-responsive">
          <table class="table table-hover mb-0">
            <thead class="table-light">
              <tr>
                <th class="py-2 py-md-3" style="width: 50%">主題</th>
                <th
                  class="py-2 py-md-3 d-none d-md-table-cell"
                  style="width: 15%"
                >
                  發佈時間
                </th>
                <th
                  class="py-2 py-md-3 d-none d-md-table-cell"
                  style="width: 15%"
                >
                  最後回應時間
                </th>
                <th class="py-2 py-md-3 text-center" style="width: 10%">
                  回應
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="topic in forumStore.topics"
                :key="topic.id"
                class="forum-row"
                @click="goToThread(topic.id)"
              >
                <!-- 主題欄 -->
                <td class="p-3 py-md-3">
                  <div class="d-flex align-items-start gap-3">
                    <div class="flex-grow-1 min-width-0">
                      <!-- 主題徽章 -->
                      <div class="d-flex flex-wrap gap-1">
                        <span
                          v-if="topic.categoryName"
                          class="badge bg-secondary-light text-dark"
                        >
                          {{ topic.categoryName }}
                        </span>
                        <button
                          v-if="
                            topic.type === CONTENT_TYPES.REPLY &&
                            topic.parentContentId
                          "
                          class="btn btn-sm btn-outline-primary parent-link-btn"
                          type="button"
                          @click.stop="goToThread(topic.parentContentId)"
                        >
                          <i class="fas fa-link me-1"></i>
                          主文
                        </button>
                        <span v-if="topic.isPinned" class="badge bg-danger">
                          置頂
                        </span>
                        <span
                          v-if="topic.isHot"
                          class="badge bg-warning text-dark"
                        >
                          熱門
                        </span>
                      </div>

                      <!-- 主題標題 + 互動按鈕 (同一行) -->
                      <div
                        class="d-flex align-items-center justify-content-between gap-2 flex-wrap"
                      >
                        <h6 class="mb-0 fs-6 fw-bold">
                          <a
                            href="javascript:void(0)"
                            class="topic-title-link text-break"
                          >
                            {{ topic.title }}
                          </a>
                        </h6>

                        <div class="interaction-container" @click.stop>
                          <InteractionButtons
                            :key="`topic-interactions-${topic.id}`"
                            :contentId="topic.id"
                            :authorUserId="topic.userId"
                            @interaction-success="handleTopicInteractionSuccess"
                            @interaction-error="handleTopicInteractionError"
                            @edit-content="handleTopicEditContent"
                            @delete-content="handleTopicDeleteContent"
                          />
                        </div>
                      </div>

                      <div
                        class="topic-preview-body topic-preview-collapsed mb-2"
                      >
                        <div
                          v-if="topic.imageUrls && topic.imageUrls.length"
                          class="topic-preview-images-col"
                        >
                          <div class="topic-preview-images-grid">
                            <img
                              v-for="(url, idx) in topic.imageUrls.slice(0, 1)"
                              :key="url + idx"
                              :src="url"
                              alt="貼文圖片"
                              class="topic-preview-image-thumb"
                              loading="lazy"
                            />
                          </div>
                        </div>
                        <div class="topic-preview-text-col">
                          <HtmlIframe
                            class="topic-preview-iframe"
                            :html="topic.contentHtml || topic.content"
                            :min-height="0"
                            text-color="#145369"
                            :allow-scripts="true"
                            :extra-css="getTopicPreviewCss(topic)"
                          />
                        </div>
                        <div
                          class="topic-preview-click-layer"
                          @click="goToThread(topic.id)"
                        ></div>
                      </div>

                      <!-- 互動按鈕已移到標題右側 -->
                    </div>
                  </div>
                </td>

                <!-- 發佈時間欄 -->
                <td
                  class="py-3 py-md-3 d-none d-md-table-cell text-muted small"
                >
                  <div class="text-nowrap">
                    {{ formatDate(topic.createdAt) }}
                  </div>
                  <UserHoverName
                    class="text-muted"
                    :user-id="topic.userId"
                    :username="topic.author"
                    :avatar-url="getAvatarUrl(topic.author, topic.authorAvatar)"
                    :size="24"
                  />
                </td>

                <!-- 最後回應時間欄 -->
                <td
                  class="py-3 py-md-3 d-none d-md-table-cell text-muted small"
                >
                  <div v-if="topic.lastReplyTime" class="text-nowrap">
                    {{ formatDate(topic.lastReplyTime) }}
                  </div>
                  <div v-else class="text-muted">尚無回應</div>
                  <div
                    v-if="topic.lastReplyUser"
                    class="text-muted d-inline-flex align-items-center gap-1"
                  >
                    <UserHoverName
                      :user-id="topic.lastReplyUserId"
                      :username="topic.lastReplyUser"
                      :avatar-url="getAvatarUrl(topic.lastReplyUser)"
                      :size="20"
                    />
                  </div>
                </td>

                <!-- 回應數欄 -->
                <td class="py-3 py-md-3 text-center">
                  <div class="badge bg-primary">
                    {{ topic.replyCount }}
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- 分頁導航 (列表視圖) -->
    <div
      v-if="!forumStore.selectedThread && forumStore.pagination.totalPages > 1"
      class="card-footer bg-white py-2 py-md-3"
    >
      <nav class="d-flex justify-content-center">
        <ul
          class="pagination pagination-sm mb-0 justify-content-center flex-wrap gap-1"
        >
          <!-- 上一頁 -->
          <li
            class="page-item"
            :class="{ disabled: forumStore.pagination.currentPage === 1 }"
          >
            <a
              class="page-link"
              href="#"
              @click.prevent="goToPage(forumStore.pagination.currentPage - 1)"
            >
              &lsaquo;
            </a>
          </li>

          <!-- 首頁 -->
          <li v-if="displayPages[0] > 1" class="page-item d-none d-md-block">
            <a class="page-link" href="#" @click.prevent="goToPage(1)"> 1 </a>
          </li>

          <!-- 省略號 -->
          <li
            v-if="displayPages[0] > 2"
            class="page-item disabled d-none d-md-block"
          >
            <span class="page-link">...</span>
          </li>

          <!-- 分頁數字 -->
          <li
            v-for="pageNum in displayPages"
            :key="pageNum"
            class="page-item"
            :class="{ active: pageNum === forumStore.pagination.currentPage }"
          >
            <a class="page-link" href="#" @click.prevent="goToPage(pageNum)">
              {{ pageNum }}
            </a>
          </li>

          <!-- 省略號 -->
          <li
            v-if="
              displayPages[displayPages.length - 1] <
              forumStore.pagination.totalPages - 1
            "
            class="page-item disabled d-none d-md-block"
          >
            <span class="page-link">...</span>
          </li>

          <!-- 末頁 -->
          <li
            v-if="
              displayPages[displayPages.length - 1] <
              forumStore.pagination.totalPages
            "
            class="page-item d-none d-md-block"
          >
            <a
              class="page-link"
              href="#"
              @click.prevent="goToPage(forumStore.pagination.totalPages)"
            >
              {{ forumStore.pagination.totalPages }}
            </a>
          </li>

          <!-- 下一頁 -->
          <li
            class="page-item"
            :class="{
              disabled:
                forumStore.pagination.currentPage ===
                forumStore.pagination.totalPages,
            }"
          >
            <a
              class="page-link"
              href="#"
              @click.prevent="goToPage(forumStore.pagination.currentPage + 1)"
            >
              &rsaquo;
            </a>
          </li>
        </ul>
      </nav>
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
}

.card-footer {
  border-top-color: #d1d3d4;
}

.card-body {
  margin-bottom: 0;
  padding-bottom: 0;
}

.topic-preview-body {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  position: relative;
}

.topic-preview-collapsed {
  max-height: 65px;
  overflow: hidden;
  position: relative;
}

.topic-preview-images-col {
  flex: 0 0 auto;
}

.topic-preview-images-grid {
  display: grid;
  grid-template-columns: 56px;
  gap: 8px;
}

.topic-preview-image-thumb {
  width: 56px;
  height: 56px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid #d1d3d4;
  background: #fff0dd;
}

.topic-preview-text-col {
  flex: 1 1 auto;
  min-width: 0;
}

.topic-preview-iframe {
  width: 100%;
  display: flex;
  flex-direction: column;
}

.topic-preview-click-layer {
  position: absolute;
  inset: 0;
  z-index: 2;
}

/* 按鈕樣式 */
.btn-forum-secondary {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #145369;
  transition: all 0.3s ease;
}

.btn-forum-secondary:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #145369;
}

/* 表格樣式 */
.table-light {
  background-color: #fff0dd !important;
  color: #145369;
}

.table-hover tbody tr:hover {
  background-color: rgba(255, 240, 221, 0.5) !important;
}

.forum-row {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.table {
  margin-bottom: 0;
}

.table-responsive {
  overflow-y: visible;
}

.table thead {
  font-weight: 600;
  font-size: 0.9rem;
}

/* 徽章樣式 */
.badge.bg-secondary {
  background-color: #86b0bd !important;
  color: #fff;
}

.badge.bg-secondary-light {
  background-color: #d0e8f2 !important;
  color: #145369;
  display: inline-flex;
  align-items: center;
  height: 22px;
  min-height: 22px;
  padding: 0 0.5rem;
  line-height: 1;
}

.parent-link-btn {
  height: 22px;
  min-height: 22px;
  padding: 0 0.5rem;
  font-size: 0.75rem;
  line-height: 1;
  border-color: #86b0bd;
  color: #145369;
  background: transparent;
}

.parent-link-btn:hover {
  background-color: #fff0dd;
  border-color: #145369;
  color: #145369;
}

.badge.bg-info {
  background-color: #145369 !important;
  color: #fff0dd;
}

.badge.bg-primary {
  background-color: #86b0bd !important;
  color: #fff;
}

/* 主題標題 */
.topic-title-link {
  color: #0f3c4d;
  text-decoration: none;
  font-weight: 700;
  letter-spacing: 0.2px;
  font-size: 1.05rem;
  transition: color 0.2s ease;
}

.topic-title-link:hover {
  color: #86b0bd;
}

/* 圖片樣式 */
.topic-image-thumb {
  max-width: 100%;
  height: auto;
  border-radius: 8px;
  border: 1px solid #d1d3d4;
  background: #fff0dd;
}

/* 互動按鈕容器樣式 */
.interaction-container {
  display: inline-flex;
  align-items: center;
  position: relative;
}

.thread-detail-title {
  font-weight: 700;
  color: #0f3c4d;
  letter-spacing: 0.2px;
  font-size: 1.35rem;
}

/* 主文正在回覆的目標內容高亮效果 */
.thread-detail-card.replying-target {
  border: 2px solid #145369;
  box-shadow: 0 0 20px rgba(20, 83, 105, 0.3);
  background: linear-gradient(
    135deg,
    rgba(20, 83, 105, 0.08) 0%,
    rgba(20, 83, 105, 0.03) 100%
  );
  position: relative;
  animation: pulse-border 2s ease-in-out infinite;
  cursor: pointer;
  transition: all 0.3s ease;
  border-radius: 0.5rem;
}

.thread-detail-card.replying-target:hover {
  box-shadow: 0 0 30px rgba(20, 83, 105, 0.5);
  background: linear-gradient(
    135deg,
    rgba(20, 83, 105, 0.12) 0%,
    rgba(20, 83, 105, 0.06) 100%
  );
}

.thread-detail-card.replying-target::before {
  content: "✍️ 正在回覆此內容";
  position: absolute;
  top: -12px;
  left: 12px;
  background: linear-gradient(135deg, #145369 0%, #0f3c4d 100%);
  color: #fff0dd;
  padding: 2px 12px;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(20, 83, 105, 0.4);
  z-index: 10;
}

@keyframes pulse-border {
  0%,
  100% {
    box-shadow: 0 0 20px rgba(20, 83, 105, 0.3);
  }
  50% {
    box-shadow: 0 0 30px rgba(20, 83, 105, 0.5);
  }
}

.quick-reply-toggle {
  padding-bottom: 0.5rem !important;
}

.quick-reply-toggle .btn {
  min-width: 120px;
}

.reply-mode-actions {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
}

.reply-mode-tabs {
  display: inline-flex;
  align-items: center;
  gap: 0;
  padding: 6px;
  background: #fff;
  border: 2px solid #d1d3d4;
  border-radius: 999px;
  height: 42px;
  font-size: 1rem;
  transition: all 0.2s ease;
  overflow: hidden;
  margin: 0;
}

.reply-mode-tabs .nav-link,
.reply-standalone-btn {
  border: none;
  background: transparent;
  color: #145369;
  padding: 0 1rem;
  border-radius: 999px;
  font-size: 1rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 32px;
  line-height: 32px;
  transition: all 0.2s ease;
  margin: 0;
}

.reply-standalone-btn {
  border: 2px solid #d1d3d4;
  background: #fff;
  height: 42px;
  padding: 0 1.1rem;
}

.reply-mode-tabs .nav-link i,
.reply-standalone-btn i {
  font-size: 1.05rem;
  margin-right: 8px;
}

.reply-mode-tabs .nav-link:hover,
.reply-standalone-btn:hover {
  background: #f8f9fa;
}

.reply-mode-tabs .nav-link.active {
  background: #145369;
  color: #fff;
}

.quick-reply-box {
  background: rgba(255, 240, 221, 0.5);
  border: 1px solid #d1d3d4;
  border-radius: 8px;
  padding-top: 1rem; /* 上 */
  padding-right: 1rem; /* 右 */
  padding-left: 1rem; /* 左 */
  padding-bottom: 0; /* 下 */
}

.quick-reply-actions .btn {
  min-width: 100px;
}

.thread-author-link,
.topic-author-link {
  color: inherit;
}

.thread-author-link:hover,
.topic-author-link:hover {
  text-decoration: underline;
}

.thread-detail-image {
  max-width: 100%;
  max-height: 500px;
  object-fit: scale-down;
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

/* 載入動畫 */
.spinner-border {
  color: #145369 !important;
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

  .thread-detail-image {
    max-height: 400px;
  }

  .quick-reply-actions .btn {
    min-width: 90px;
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

  .thread-detail-image {
    max-height: 300px;
  }
}

/* 回文子留言樣式 */
.reply-comments {
  border-left: 3px solid #86b0bd;
  padding-left: 0.5rem;
  background-color: rgba(255, 240, 221, 0.2);
  border-radius: 4px;
}

.comment-list :deep(.thread-post),
.reply-list :deep(.thread-post),
.reply-comments :deep(.thread-post) {
  margin-bottom: 0.25rem !important;
}

.comment-list :deep(.card-body),
.reply-list :deep(.card-body),
.reply-comments :deep(.card-body) {
  padding: 0.5rem !important;
}
</style>
