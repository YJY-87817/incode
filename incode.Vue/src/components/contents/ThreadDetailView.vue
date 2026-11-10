<!-- ThreadDetailView.vue -->
<!--
  主題詳情視圖：主文、留言 / 回文列表與分頁、簡易留言框、詳細留言 / 回文草稿、最佳解答
  由 TopicListView.vue 在 forumStore.selectedThread 有值時顯示
-->

<script setup>
import { computed } from "vue";
import ThreadPost from "@/components/contents/ThreadPost.vue";
import QuickReplyBox from "@/components/contents/QuickReplyBox.vue";
import InteractionButtons from "@/components/contents/InteractionButtons.vue";
import ForumPaginationCard from "@/components/contents/ForumPaginationCard.vue";
import HtmlIframe from "@/components/common/HtmlIframe.vue";
import UserPreview from "@/components/users/UserPreview.vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import { useForumNavigation } from "@/composables/useForumNavigation.js";
import { useTopicInteractionHandlers } from "@/composables/useTopicInteractionHandlers.js";
import { useBestAnswer } from "@/composables/useBestAnswer.js";
import { useThreadReply } from "@/composables/useThreadReply.js";
import { useThreadPagination } from "@/composables/useThreadPagination.js";
import { formatDate } from "@/utils/contentBody.js";
import { getAvatarUrl } from "@/utils/avatar.js";

const forumStore = useForumStore();
const { goToList, goToThread } = useForumNavigation();
const {
  handleTopicInteractionSuccess,
  handleTopicInteractionError,
  handleTopicEditContent,
  handleTopicDeleteContent,
} = useTopicInteractionHandlers();
const {
  activeEntrustMissionId,
  activeMissionOwnerUserId,
  activeRewardPoints,
  activeBestAnswerContentId,
  canEnableBestAnswer,
  handleBestAnswerSelected,
} = useBestAnswer();
const {
  showQuickReply,
  replyTab,
  selectQuickReply,
  selectDetailedComment,
  closeQuickReply,
  submitQuickReply,
  autoResizeTextarea,
  openDetailedReply,
  isThreadReplyingTarget,
  handleThreadReplyingTargetClick,
} = useThreadReply();
const {
  threadTotalPages,
  paginatedThreadPosts,
  threadCommentsTotalPages,
  paginatedThreadComments,
  threadDisplayPages,
  threadCommentsDisplayPages,
  goToThreadPage,
  goToCommentPage,
} = useThreadPagination();

const threadHasInlineImages = computed(() => {
  const html = forumStore.threadDetail?.contentHtml || "";
  return /<img\b/i.test(html);
});
</script>

<template>
  <div>
    <!-- 返回按鈕 -->
    <div class="p-3 border-bottom">
      <button class="btn btn-sm btn-forum-secondary mb-0" @click="goToList">
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
      <!-- 主題徽章 + 互動按鈕 -->
      <div class="d-flex flex-wrap gap-1 align-items-center">
        <span
          v-if="forumStore.threadDetail?.categoryName"
          class="badge bg-secondary-light text-dark interaction-aligned-badge"
        >
          {{ forumStore.threadDetail.categoryName }}
        </span>
        <button
          v-if="
            forumStore.threadDetail?.type === CONTENT_TYPES.REPLY &&
            forumStore.threadDetail?.parentContentId
          "
          class="badge interaction-aligned-badge parent-link-btn"
          type="button"
          @click.stop="goToThread(forumStore.threadDetail.parentContentId)"
        >
          <i class="fa-solid fa-people-pulling"></i>
          主文
        </button>
        <span
          v-if="forumStore.threadDetail?.hasEntrustMission"
          class="badge bg-secondary interaction-aligned-badge"
        >
          委託任務
        </span>
        <span
          v-if="forumStore.threadDetail?.isHot"
          class="badge bg-warning text-dark interaction-aligned-badge"
        >
          熱門
        </span>
        <div class="interaction-container ms-auto" @click.stop>
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

      <div
        class="d-flex align-items-center justify-content-between gap-2 flex-wrap mt-1"
      >
        <h3
          class="card-title thread-detail-title fs-4 fs-md-3 text-break mb-0 mt-0"
        >
          {{ forumStore.threadDetail.title }}
        </h3>
      </div>
      <p
        class="text-muted mb-2 small d-flex flex-wrap gap-2 align-items-center"
      >
        <span>by</span>
        <UserPreview
          :user-id="forumStore.threadDetail.userId"
          :username="forumStore.threadDetail.author"
          :avatar-url="
            getAvatarUrl(
              forumStore.threadDetail.author,
              forumStore.threadDetail.authorAvatar,
            )
          "
          :size="26"
        />
        <span>·</span>
        <span>{{ formatDate(forumStore.threadDetail.createdAt) }}</span>
      </p>

      <!-- 主題互動按鈕已移到分類徽章同列 -->

      <HtmlIframe
        class="card-text rich-content"
        :html="
          forumStore.threadDetail.contentHtml ||
          forumStore.threadDetail.content
        "
        :min-height="120"
        text-color="#432f28"
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
            :enable-best-answer="canEnableBestAnswer"
            :entrust-mission-id="activeEntrustMissionId"
            :mission-owner-user-id="activeMissionOwnerUserId"
            :best-answer-content-id="activeBestAnswerContentId"
            :reward-points="activeRewardPoints"
            @best-answer-selected="handleBestAnswerSelected"
          />
          <ForumPaginationCard
            v-if="threadCommentsTotalPages > 1"
            class="mt-3"
            :current-page="forumStore.threadCommentsCurrentPage"
            :total-pages="threadCommentsTotalPages"
            :pages="threadCommentsDisplayPages"
            :show-edge-buttons="true"
            :show-jump="true"
            @page-change="goToCommentPage"
          />
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
            :enable-best-answer="canEnableBestAnswer"
            :entrust-mission-id="activeEntrustMissionId"
            :mission-owner-user-id="activeMissionOwnerUserId"
            :best-answer-content-id="activeBestAnswerContentId"
            :reward-points="activeRewardPoints"
            @best-answer-selected="handleBestAnswerSelected"
          />
        </div>

        <!-- 分頁導航 -->
        <ForumPaginationCard
          v-if="threadTotalPages > 1"
          class="mt-3 mt-md-4"
          :current-page="forumStore.threadCurrentPage"
          :total-pages="threadTotalPages"
          :pages="threadDisplayPages"
          :show-edge-buttons="true"
          :show-jump="true"
          @page-change="goToThreadPage"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.card-body {
  margin-bottom: 0;
  padding-bottom: 0;
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

.topic-sort-item.active {
  background: #fff0dd;
}

.topic-list--container-max-768 .interaction-aligned-badge {
  min-height: 40px;
  height: 40px;
  padding: 0.35rem 0.65rem;
  font-size: 0.82rem;
}

/* 徽章樣式 */
.badge.bg-secondary {
  background-color: #86b0bd !important;
  color: #fff;
}

.badge.bg-secondary-light {
  background-color: #d0e8f2 !important;
  color: #145369;
}

.interaction-aligned-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 28px;
  min-height: 28px;
  padding: 0.2rem 0.45rem;
  margin-bottom: 0;
  font-size: 0.75rem;
  line-height: 1;
  white-space: nowrap;
  transition: all 0.3s ease;
}

.parent-link-btn {
  border: 1px solid #86b0bd;
  color: #145369;
  background-color: #d0e8f2;
  cursor: pointer;
}

.parent-link-btn:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

.badge.bg-primary {
  background-color: #86b0bd !important;
  color: #fff;
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

.thread-detail-image {
  max-width: 100%;
  max-height: 500px;
  object-fit: scale-down;
}

@media (max-width: 768px) {
.interaction-aligned-badge {
    min-height: 40px;
    height: 40px;
    padding: 0.35rem 0.65rem;
    font-size: 0.82rem;
  }

.thread-detail-image {
    max-height: 400px;
  }

.reply-mode-actions {
    width: 100%;
    align-items: stretch;
    flex-direction: column;
    gap: 0.6rem;
  }

.reply-mode-tabs {
    width: 100%;
    justify-content: space-between;
    height: auto;
    min-height: 42px;
  }

.reply-mode-tabs .nav-link,
  .reply-standalone-btn {
    flex: 1 1 auto;
    min-height: 34px;
  }

.reply-standalone-btn {
    width: 100%;
  }

.quick-reply-actions .btn {
    min-width: 0;
    min-height: 40px;
    flex: 1 1 calc(50% - 0.25rem);
  }
}

@media (max-width: 576px) {
.thread-detail-image {
    max-height: 300px;
  }
}

.comment-list :deep(.thread-post),
.reply-list :deep(.thread-post) {
  margin-bottom: 0.25rem !important;
}

.comment-list :deep(.card-body),
.reply-list :deep(.card-body) {
  padding: 0.5rem !important;
}
</style>
