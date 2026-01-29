<!-- TopicListView.vue -->
<!-- 
  主題列表視圖組件 (含列表/詳情雙視圖)
  職責: 管理主題列表顯示、詳情頁切換、分頁、回覆功能
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import apiClient from "@/api/request.js";
import { uploadImage } from "@/api/imageapi.js";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";
import HtmlIframe from "@/components/HtmlIframe.vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import { appendUrls, formatDate, formatTimeAgo } from "./forumUtils";

const forumStore = useForumStore();
const router = useRouter();

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
const quickReplyContent = ref("");
const quickReplyMode = ref("comment"); // comment | reply
const isSubmittingQuickReply = ref(false);
const isUploadingQuickReplyImages = ref(false);

const quickReplyLabel = computed(() =>
  quickReplyMode.value === "comment" ? "留言" : "回文"
);

/**
 * 處理主題互動成功
 */
const handleTopicInteractionSuccess = (data) => {
  console.log("主題互動成功:", data);
  // 可以在此顯示 toast 或其他反饋
};

/**
 * 處理主題互動錯誤
 */
const handleTopicInteractionError = (data) => {
  console.error("主題互動失敗:", data);
  alert(`互動失敗: ${data.error}`);
};

/**
 * 分頁相關計算
 */
const displayPages = computed(() => forumStore.displayPages);

/**
 * 執行線程分頁
 */
const threadTotalPages = computed(() => forumStore.threadTotalPages);

const paginatedThreadPosts = computed(() => forumStore.paginatedThreadPosts);

/**
 * 頁面導航
 */
const goToPage = (page) => {
  forumStore.changePage(page);
};

const goToThreadPage = (page) => {
  forumStore.changeThreadPage(page);
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

/**
 * 留言/回文輸入框相關
 */
const resetQuickReply = () => {
  quickReplyContent.value = "";
  quickReplyMode.value = "comment";
};

const toggleQuickReply = () => {
  showQuickReply.value = !showQuickReply.value;
  if (!showQuickReply.value) {
    resetQuickReply();
  }
};

const handleQuickReplyImages = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;
  isUploadingQuickReplyImages.value = true;
  try {
    const uploaded = await uploadImage(files);
    quickReplyContent.value = appendUrls(quickReplyContent.value, uploaded);
  } catch (error) {
    console.error("留言/回文圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    isUploadingQuickReplyImages.value = false;
    if (event.target) event.target.value = "";
  }
};

const submitQuickReply = async () => {
  if (!forumStore.threadDetail?.id) {
    alert("未找到對應的主題，請重新嘗試");
    return;
  }

  if (!quickReplyContent.value.trim()) {
    alert("請輸入內容");
    return;
  }

  if (isSubmittingQuickReply.value) return;
  isSubmittingQuickReply.value = true;

  const isComment = quickReplyMode.value === "comment";
  const dto = {
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId: forumStore.threadDetail.categoryId,
    parentContentId: forumStore.threadDetail.id,
    title: `${isComment ? "留言" : "Re"}: ${forumStore.threadDetail.title}`,
    body: quickReplyContent.value.trim(),
    type: isComment ? CONTENT_TYPES.COMMENT : CONTENT_TYPES.REPLY,
  };

  try {
    await apiClient.post("/ContentApi", dto);
    alert("已送出！");
    
    // 直接调用 store 重新获取详情
    if (forumStore.selectedThread) {
      await forumStore.fetchThreadDetail(forumStore.selectedThread);
    }
    
    resetQuickReply();
    showQuickReply.value = false;
  } catch (error) {
    console.error("送出留言/回文失敗:", error);
    alert("送出失敗，請稍後再試");
  } finally {
    isSubmittingQuickReply.value = false;
  }
};
</script>

<template>
  <div class="card shadow-sm">
    <!-- 卡片標題 (列表視圖) -->
    <div
      v-if="!forumStore.selectedThread"
      class="card-header bg-white d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center gap-2 py-3"
    >
      <h5 class="mb-0 fs-6 fs-md-5">📋 討論主題</h5>
      <div class="d-flex flex-wrap gap-2">
        <span
          class="badge bg-secondary"
          v-if="forumStore.selectedCategoryName"
        >
          {{ forumStore.selectedCategoryName }}
        </span>
        <span class="badge bg-info"> 共 {{ forumStore.pagination.totalCount }} 筆 </span>
      </div>
    </div>

    <!-- 卡片內容 -->
    <div class="card-body p-0">
      <!-- 詳情視圖 -->
      <div v-if="forumStore.selectedThread">
        <!-- 返回按鈕 -->
        <div class="p-3 border-bottom">
          <button
            class="btn btn-sm btn-forum-secondary"
            @click="goToList"
          >
            <i class="fas fa-arrow-left me-2"></i>
            返回列表
          </button>
        </div>

        <!-- 帖文內容 -->
        <div class="card-body border-bottom">
          <h3 class="card-title fs-4 fs-md-3 text-break">
            {{ forumStore.threadDetail.title }}
          </h3>
          <p class="text-muted mb-2 small d-flex flex-wrap gap-2">
            <span>by {{ forumStore.threadDetail.author }}</span>
            <span>·</span>
            <span>{{ formatDate(forumStore.threadDetail.createdAt) }}</span>
          </p>

          <!-- 主題互動按鈕 -->
          <div class="mb-3">
            <InteractionButtons
              :key="`thread-detail-interactions-${forumStore.threadDetail.id}`"
              :contentId="forumStore.threadDetail.id"
              @interaction-success="handleTopicInteractionSuccess"
              @interaction-error="handleTopicInteractionError"
            />
          </div>

          <HtmlIframe
            class="card-text rich-content"
            :html="forumStore.threadDetail.contentHtml || forumStore.threadDetail.content"
            :min-height="120"
            text-color="#432f28"
            :extra-css="'body { white-space: pre-wrap; } img { width: 30%; max-width: 30%; height: auto; object-fit: contain; }'"
          />
          <!-- 改圖片大小在上面這裡 -->

          <!-- 帖文圖片 -->
          <div
            v-if="!threadHasInlineImages && forumStore.threadDetail.imageUrls && forumStore.threadDetail.imageUrls.length"
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
        <div class="p-3">
          <div class="mt-3 mt-md-5">
            <h5 class="fs-5 fs-md-4 mb-3">💬 回覆 / 留言</h5>

            <div
              class="d-flex flex-wrap gap-2 align-items-center mb-3 quick-reply-toggle"
            >
              <button
                class="btn btn-forum-primary btn-sm"
                type="button"
                @click="toggleQuickReply"
              >
                <i class="fas" :class="showQuickReply ? 'fa-times' : 'fa-pen'"></i>
                <span class="ms-1">
                  {{ showQuickReply ? "收起留言/回文" : "留言 / 回文" }}
                </span>
              </button>
              <small class="text-muted" v-if="!showQuickReply">
                開啟後可直接留言或回文，不需切換頁面
              </small>
            </div>

            <transition name="fade">
              <div v-show="showQuickReply" class="quick-reply-box">
                <div class="d-flex flex-wrap gap-3 align-items-center mb-2">
                  <label class="mb-0 small text-muted">送出類型</label>
                  <div class="btn-group btn-group-sm" role="group">
                    <input
                      type="radio"
                      class="btn-check"
                      name="quickReplyMode"
                      id="quickReplyComment"
                      value="comment"
                      v-model="quickReplyMode"
                    />
                    <label class="btn btn-outline-dark" for="quickReplyComment">
                      留言
                    </label>

                    <input
                      type="radio"
                      class="btn-check"
                      name="quickReplyMode"
                      id="quickReplyReply"
                      value="reply"
                      v-model="quickReplyMode"
                    />
                    <label class="btn btn-outline-dark" for="quickReplyReply">
                      回文
                    </label>
                  </div>
                  <small class="text-muted">
                    目前為：{{ quickReplyLabel }}
                  </small>
                </div>

                <textarea
                  v-model="quickReplyContent"
                  class="form-control"
                  rows="4"
                  placeholder="直接輸入{{ quickReplyLabel }}內容，可貼上圖片網址或使用下方上傳"
                ></textarea>

                <div class="d-flex flex-column flex-sm-row gap-2 align-items-start my-2">
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    class="form-control form-control-sm"
                    @change="handleQuickReplyImages"
                    :disabled="isUploadingQuickReplyImages"
                  />
                  <small class="text-muted">
                    {{ isUploadingQuickReplyImages ? "上傳中..." : "上傳後自動附加圖片網址" }}
                  </small>
                </div>

                <div class="d-flex flex-wrap gap-2 quick-reply-actions">
                  <button
                    class="btn btn-forum-primary btn-sm"
                    type="button"
                    @click="submitQuickReply"
                    :disabled="isSubmittingQuickReply || !quickReplyContent.trim()"
                  >
                    <i class="fas fa-paper-plane me-1"></i>
                    {{ isSubmittingQuickReply ? "送出中..." : "送出" }}
                  </button>
                  <button
                    class="btn btn-forum-secondary btn-sm"
                    type="button"
                    @click="toggleQuickReply"
                    :disabled="isSubmittingQuickReply"
                  >
                    取消
                  </button>
                </div>
              </div>
            </transition>

            <!-- 所有留言 -->
            <div v-if="forumStore.threadComments.length > 0" class="mb-4">
              <h6 class="fs-6 mb-2">📝 所有留言</h6>
              <ThreadPost
                v-for="comment in forumStore.threadComments"
                :key="'comment-' + comment.id"
                :post="comment"
                :category-id="forumStore.threadDetail.categoryId"
                :hide-nested-replies="true"
              />
            </div>

            <!-- 回文分頁 -->
            <div v-if="paginatedThreadPosts.length > 0" class="mb-4">
              <h6 class="fs-6 mb-2">💭 回文</h6>
              <ThreadPost
                v-for="post in paginatedThreadPosts"
                :key="post.id"
                :post="post"
                :category-id="forumStore.threadDetail.categoryId"
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
                  :class="{ disabled: forumStore.threadCurrentPage === threadTotalPages }"
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
        <div v-if="forumStore.isLoading && forumStore.topics.length === 0" class="text-center py-5">
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
                  <div class="d-flex align-items-start gap-2">
                    <div class="flex-grow-1">
                      <!-- 主題徽章 -->
                      <div class="d-flex flex-wrap gap-1 mb-1">
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

                      <!-- 主題標題 -->
                      <h6 class="mb-2 fs-6 fw-bold">
                        <a
                          href="javascript:void(0)"
                          class="topic-title-link text-break"
                        >
                          {{ topic.title }}
                        </a>
                      </h6>

                      <!-- 主題圖片縮圖 -->
                      <div
                        v-if="topic.imageUrls && topic.imageUrls.length"
                        class="d-flex flex-wrap gap-2 mt-2 mb-2 topic-image-list"
                      >
                        <img
                          v-for="(url, idx) in topic.imageUrls.slice(0, 3)"
                          :key="url + idx"
                          :src="url"
                          alt="貼文圖片"
                          class="topic-image-thumb"
                          loading="lazy"
                          style="
                            max-width: 80px;
                            max-height: 80px;
                            object-fit: cover;
                            border-radius: 4px;
                          "
                        />
                        <div
                          v-if="topic.imageUrls.length > 3"
                          class="d-flex align-items-center justify-content-center text-muted small"
                          style="
                            width: 80px;
                            height: 80px;
                            background-color: #f0f0f0;
                            border-radius: 4px;
                          "
                        >
                          +{{ topic.imageUrls.length - 3 }} 張
                        </div>
                      </div>

                      <!-- 主題作者資訊 -->
                      <div class="small text-muted d-flex flex-wrap gap-2">
                        <span class="text-nowrap"> 👤 {{ topic.author }} </span>
                        <span class="d-md-none text-nowrap">
                          🕒 {{ formatTimeAgo(topic.createdAt) }}
                        </span>
                      </div>

                      <!-- 互動按鈕 -->
                      <div class="mt-2 interaction-container" @click.stop>
                        <InteractionButtons
                          :key="`topic-interactions-${topic.id}`"
                          :contentId="topic.id"
                          @interaction-success="handleTopicInteractionSuccess"
                          @interaction-error="handleTopicInteractionError"
                        />
                      </div>
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
                  <div class="text-muted">{{ topic.author }}</div>
                </td>

                <!-- 最後回應時間欄 -->
                <td
                  class="py-3 py-md-3 d-none d-md-table-cell text-muted small"
                >
                  <div v-if="topic.lastReplyTime" class="text-nowrap">
                    {{ formatDate(topic.lastReplyTime) }}
                  </div>
                  <div v-else class="text-muted">尚無回應</div>
                  <div v-if="topic.lastReplyUser" class="text-muted">
                    {{ topic.lastReplyUser }}
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
              displayPages[displayPages.length - 1] < forumStore.pagination.totalPages - 1
            "
            class="page-item disabled d-none d-md-block"
          >
            <span class="page-link">...</span>
          </li>

          <!-- 末頁 -->
          <li
            v-if="displayPages[displayPages.length - 1] < forumStore.pagination.totalPages"
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
              disabled: forumStore.pagination.currentPage === forumStore.pagination.totalPages,
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
  background-color: #86B0BD !important;
  border-bottom-color: #d1d3d4;
}

.card-header h5 {
  color: #fff;
}

.card-footer {
  border-top-color: #d1d3d4;
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

.table thead {
  font-weight: 600;
  font-size: 0.9rem;
}

/* 徽章樣式 */
.badge.bg-secondary {
  background-color: #86b0bd !important;
  color: #fff;
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
  color: #145369;
  text-decoration: none;
  font-weight: 500;
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
  display: block;
  position: relative;
  z-index: 10;
}

.quick-reply-toggle .btn {
  min-width: 120px;
}

.quick-reply-box {
  background: rgba(255, 240, 221, 0.5);
  border: 1px solid #d1d3d4;
  border-radius: 8px;
  padding: 1rem;
}

.quick-reply-actions .btn {
  min-width: 100px;
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
</style>
