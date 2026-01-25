<!-- TopicListView.vue -->
<!-- 
  主題列表視圖組件 (Pinia版)
  職責: 管理主題列表顯示、詳情頁切換、分頁、回覆功能
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { computed } from "vue";
import { useForumStore } from "@/stores/forumStore";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
import { formatDate, formatTimeAgo } from "./forumUtils";

const forumStore = useForumStore();

/**
 * 返回列表視圖
 */
const handleBackToList = () => {
  forumStore.closeThread();
};

/**
 * 打開主題詳情
 */
const handleThreadOpen = (topicId) => {
  forumStore.openThread(topicId);
};

/**
 * 頁碼變更
 */
const handlePageChange = (page) => {
  forumStore.changePage(page);
};

/**
 * 帖文詳情頁碼變更
 */
const handleThreadPageChange = (page) => {
  forumStore.changeThreadPage(page);
};

/**
 * 取得分類名稱
 */
const getCategoryName = (categoryId) => {
  const category = forumStore.categories.find((c) => c.id === categoryId);
  return category?.name || "未分類";
};

/**
 * 帖文詳情分頁顯示
 */
const threadDisplayPages = computed(() => {
  const currentPage = forumStore.threadCurrentPage;
  const totalPages = forumStore.threadTotalPages;
  const pages = [];
  const maxDisplay = 5;

  if (totalPages <= 1) return pages;

  let start = Math.max(1, currentPage - Math.floor(maxDisplay / 2));
  let end = Math.min(totalPages, start + maxDisplay - 1);

  if (end - start < maxDisplay - 1) {
    start = Math.max(1, end - maxDisplay + 1);
  }

  for (let i = start; i <= end; i++) pages.push(i);
  return pages;
});
</script>

<template>
  <!-- 主題列表視圖 -->
  <div v-if="!forumStore.selectedThread" class="topic-list-view">
    <!-- Loading Skeleton -->
    <div v-if="forumStore.isLoading && forumStore.topics.length === 0">
      <div
        v-for="i in 5"
        :key="i"
        class="card mb-3 shadow-sm"
        style="
          height: 150px;
          background: linear-gradient(
            90deg,
            #f0f0f0 25%,
            #e0e0e0 50%,
            #f0f0f0 75%
          );
          animation: shimmer 1.5s infinite;
        "
      ></div>
    </div>

    <!-- 空狀態 -->
    <div
      v-else-if="!forumStore.isLoading && forumStore.topics.length === 0"
      class="text-center py-5"
    >
      <p class="text-muted">
        <i class="fas fa-inbox fa-3x mb-3 d-block"></i>
        目前沒有主題
      </p>
    </div>

    <!-- 主題列表 -->
    <div v-else>
      <div
        v-for="topic in forumStore.topics"
        :key="topic.id"
        class="card topic-card mb-3 shadow-sm"
        @click="handleThreadOpen(topic.id)"
      >
        <div class="card-body p-3">
          <div class="d-flex align-items-start">
            <!-- 作者頭像 -->
            <img
              :src="topic.authorAvatar"
              :alt="topic.author"
              class="rounded-circle me-3 author-avatar"
              style="width: 50px; height: 50px; object-fit: cover"
            />

            <!-- 主題資訊 -->
            <div class="flex-grow-1 overflow-hidden">
              <!-- 標題 & 標籤 -->
              <div class="d-flex align-items-center mb-2 flex-wrap gap-2">
                <h6 class="topic-title mb-0 text-truncate">
                  <span v-if="topic.isPinned" class="badge bg-warning me-2">
                    <i class="fas fa-thumbtack"></i> 置頂
                  </span>
                  <span v-if="topic.isHot" class="badge bg-danger me-2">
                    <i class="fas fa-fire"></i> 熱門
                  </span>
                  {{ topic.title }}
                </h6>
              </div>

              <!-- 作者 & 分類 -->
              <p class="small text-muted mb-2">
                <i class="fas fa-user"></i> {{ topic.author }}
                <span class="mx-2">•</span>
                <i class="fas fa-folder"></i> {{ topic.categoryName }}
                <span class="mx-2">•</span>
                <i class="fas fa-clock"></i>
                {{ formatTimeAgo(topic.createdAt) }}
              </p>

              <!-- 內容預覽 -->
              <p class="topic-content-preview text-muted mb-2">
                {{ topic.content }}
              </p>

              <!-- 統計資訊 -->
              <div
                class="d-flex align-items-center small text-muted flex-wrap gap-3"
              >
                <span>
                  <i class="fas fa-comment"></i> {{ topic.replyCount }} 則回覆
                </span>
                <span v-if="topic.viewCount">
                  <i class="fas fa-eye"></i> {{ topic.viewCount }} 次瀏覽
                </span>
                <span v-if="topic.lastReplyUser" class="ms-auto">
                  最後回覆：{{ topic.lastReplyUser }} •
                  {{ formatTimeAgo(topic.lastReplyTime) }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 分頁控件 -->
      <nav
        v-if="forumStore.pagination.totalPages > 1"
        aria-label="主題分頁"
        class="mt-4"
      >
        <ul class="pagination justify-content-center flex-wrap">
          <li
            class="page-item"
            :class="{ disabled: forumStore.pagination.currentPage === 1 }"
          >
            <button
              class="page-link"
              @click="handlePageChange(forumStore.pagination.currentPage - 1)"
              :disabled="forumStore.pagination.currentPage === 1"
            >
              上一頁
            </button>
          </li>

          <li
            v-for="page in forumStore.displayPages"
            :key="page"
            class="page-item"
            :class="{ active: page === forumStore.pagination.currentPage }"
          >
            <button class="page-link" @click="handlePageChange(page)">
              {{ page }}
            </button>
          </li>

          <li
            class="page-item"
            :class="{
              disabled:
                forumStore.pagination.currentPage ===
                forumStore.pagination.totalPages,
            }"
          >
            <button
              class="page-link"
              @click="handlePageChange(forumStore.pagination.currentPage + 1)"
              :disabled="
                forumStore.pagination.currentPage ===
                forumStore.pagination.totalPages
              "
            >
              下一頁
            </button>
          </li>
        </ul>
      </nav>
    </div>
  </div>

  <!-- 主題詳情視圖 -->
  <div v-else class="thread-detail-view">
    <!-- 返回按鈕 -->
    <button class="btn btn-forum-back mb-3" @click="handleBackToList">
      <i class="fas fa-arrow-left me-2"></i>返回列表
    </button>

    <!-- 主題詳情卡片 -->
    <div class="card shadow-sm mb-4">
      <div class="card-header bg-white py-3">
        <div class="d-flex align-items-center justify-content-between">
          <h5 class="mb-0">{{ forumStore.threadDetail.title }}</h5>
          <span class="badge bg-info">
            {{ getCategoryName(forumStore.threadDetail.categoryId) }}
          </span>
        </div>
      </div>

      <div class="card-body p-4">
        <!-- 作者資訊 -->
        <div class="d-flex align-items-center mb-3">
          <img
            :src="`https://ui-avatars.com/api/?name=${encodeURIComponent(
              forumStore.threadDetail.author
            )}&background=random`"
            :alt="forumStore.threadDetail.author"
            class="rounded-circle me-3"
            style="width: 50px; height: 50px"
          />
          <div>
            <div class="fw-bold">{{ forumStore.threadDetail.author }}</div>
            <small class="text-muted">
              {{ formatDate(forumStore.threadDetail.createdAt) }}
            </small>
          </div>
        </div>

        <!-- 內容 -->
        <div class="thread-content mb-3">
          <p class="mb-3">{{ forumStore.threadDetail.content }}</p>

          <!-- 圖片 -->
          <div
            v-if="
              forumStore.threadDetail.imageUrls &&
              forumStore.threadDetail.imageUrls.length > 0
            "
            class="thread-images"
          >
            <img
              v-for="(url, idx) in forumStore.threadDetail.imageUrls"
              :key="idx"
              :src="url"
              :alt="`圖片 ${idx + 1}`"
              class="img-fluid rounded mb-2"
              style="max-width: 100%; height: auto"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- 回文列表 -->
    <div class="replies-section mb-4">
      <h6 class="mb-3">
        <i class="fas fa-comments me-2"></i>
        回文 ({{ forumStore.threadPosts.length }})
      </h6>

      <div v-if="forumStore.threadPosts.length === 0" class="text-center py-4">
        <p class="text-muted">目前沒有回文，成為第一個回覆的人吧！</p>
      </div>

      <div v-else>
        <ThreadPost
          v-for="post in forumStore.paginatedThreadPosts"
          :key="post.id"
          :post="post"
          :comments="
            forumStore.threadComments.filter(
              (c) => c.parentContentId === post.id
            )
          "
        />

        <!-- 回文分頁 -->
        <nav
          v-if="forumStore.threadTotalPages > 1"
          aria-label="回文分頁"
          class="mt-3"
        >
          <ul class="pagination justify-content-center">
            <li
              class="page-item"
              :class="{ disabled: forumStore.threadCurrentPage === 1 }"
            >
              <button
                class="page-link"
                @click="
                  handleThreadPageChange(forumStore.threadCurrentPage - 1)
                "
                :disabled="forumStore.threadCurrentPage === 1"
              >
                上一頁
              </button>
            </li>

            <li
              v-for="page in threadDisplayPages"
              :key="page"
              class="page-item"
              :class="{ active: page === forumStore.threadCurrentPage }"
            >
              <button class="page-link" @click="handleThreadPageChange(page)">
                {{ page }}
              </button>
            </li>

            <li
              class="page-item"
              :class="{
                disabled:
                  forumStore.threadCurrentPage === forumStore.threadTotalPages,
              }"
            >
              <button
                class="page-link"
                @click="
                  handleThreadPageChange(forumStore.threadCurrentPage + 1)
                "
                :disabled="
                  forumStore.threadCurrentPage === forumStore.threadTotalPages
                "
              >
                下一頁
              </button>
            </li>
          </ul>
        </nav>
      </div>
    </div>

    <!-- 留言區 -->
    <div class="comments-section">
      <h6 class="mb-3">
        <i class="fas fa-comment-dots me-2"></i>
        留言 ({{ forumStore.threadComments.length }})
      </h6>

      <div
        v-if="forumStore.threadComments.length === 0"
        class="text-center py-4"
      >
        <p class="text-muted">目前沒有留言</p>
      </div>

      <div v-else>
        <div
          v-for="comment in forumStore.threadComments"
          :key="comment.id"
          class="card mb-2"
        >
          <div class="card-body p-3">
            <div class="d-flex align-items-start">
              <img
                :src="`https://ui-avatars.com/api/?name=${encodeURIComponent(
                  comment.author
                )}&background=random`"
                :alt="comment.author"
                class="rounded-circle me-3"
                style="width: 40px; height: 40px"
              />
              <div class="flex-grow-1">
                <div
                  class="d-flex justify-content-between align-items-center mb-2"
                >
                  <strong>{{ comment.author }}</strong>
                  <small class="text-muted">
                    {{ formatTimeAgo(comment.createdAt) }}
                  </small>
                </div>
                <p class="mb-2">{{ comment.content }}</p>

                <!-- 留言圖片 -->
                <div
                  v-if="comment.imageUrls && comment.imageUrls.length > 0"
                  class="comment-images"
                >
                  <img
                    v-for="(url, idx) in comment.imageUrls"
                    :key="idx"
                    :src="url"
                    :alt="`留言圖片 ${idx + 1}`"
                    class="img-fluid rounded mb-2"
                    style="max-width: 300px; height: auto"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 主題卡片 */
.topic-card {
  cursor: pointer;
  transition: all 0.3s ease;
  border-color: #d1d3d4;
}

.topic-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(67, 47, 40, 0.2) !important;
  border-color: #86b0bd;
}

/* 作者頭像 */
.author-avatar {
  border: 2px solid #86b0bd;
}

/* 主題標題 */
.topic-title {
  color: #432f28;
  font-weight: 600;
  font-size: 1.1rem;
}

/* 內容預覽 */
.topic-content-preview {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.5;
}

/* 分頁按鈕 */
.pagination .page-link {
  color: #432f28;
  border-color: #d1d3d4;
}

.pagination .page-link:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}

.pagination .page-item.active .page-link {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
}

.pagination .page-item.disabled .page-link {
  color: #d1d3d4;
  cursor: not-allowed;
}

/* 返回按鈕 */
.btn-forum-back {
  background-color: #fff0dd;
  border: 1px solid #86b0bd;
  color: #432f28;
  transition: all 0.3s ease;
}

.btn-forum-back:hover {
  background-color: #86b0bd;
  color: #fff;
}

/* 主題詳情 */
.thread-detail-view .card-header {
  background-color: #fff0dd !important;
  border-bottom-color: #d1d3d4;
}

.thread-detail-view .card-header h5 {
  color: #432f28;
}

/* 內容區域 */
.thread-content {
  font-size: 1rem;
  line-height: 1.8;
  color: #333;
}

/* 回文區塊 */
.replies-section h6,
.comments-section h6 {
  color: #432f28;
  font-weight: 600;
  border-left: 4px solid #86b0bd;
  padding-left: 12px;
}

/* Loading 動畫 */
@keyframes shimmer {
  0% {
    background-position: -1000px 0;
  }
  100% {
    background-position: 1000px 0;
  }
}

/* 響應式調整 */
@media (max-width: 768px) {
  .topic-title {
    font-size: 1rem;
  }

  .author-avatar {
    width: 40px !important;
    height: 40px !important;
  }

  .topic-content-preview {
    -webkit-line-clamp: 1;
  }
}
</style>
