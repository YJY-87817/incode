<!-- 
  forum.vue - 論壇主頁面
  
  職責：
  - 協調各個子組件 (Header, Filter, Form, List)
  - 管理全局論壇狀態 (分類、主題、分頁、詳情頁)
  - 處理API調用和資料更新
  - 監聽Pinia store變化並刷新列表
  
  子組件結構：
  - ForumHeader: 標題和統計
  - CategoryFilter: 分類篩選區
  - NewTopicForm: 新主題表單
  - TopicListView: 主題列表+詳情視圖
  - ThreadPost: 回覆內容組件
  
  功能模組：
  - forumUtils: 日期格式化、URL處理、內容解析等通用函數
-->

<script setup>
import { reactive, onMounted, computed, watch, ref, onUnmounted } from "vue";
import { useForumStore, CONTENT_TYPES } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import contentInteractionApi from "@/api/contentInteractionApi.js";

// 組件導入
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import ForumHeader from "@/components/Contents/Forum/ForumHeader.vue";
import CategoryFilter from "@/components/Contents/Forum/CategoryFilter.vue";
import NewTopicForm from "@/components/Contents/Forum/NewTopicForm.vue";
import TopicListView from "@/components/Contents/Forum/TopicListView.vue";
import ThreadPost from "@/components/Contents/ThreadPost.vue";

// 工具函數導入
import {
  parseBodyContent,
  formatDate,
  formatTimeAgo,
  calculateReplyStats,
  appendUrls,
} from "@/components/Contents/Forum/forumUtils";

const forumStore = useForumStore();

// ============================================================================
// 1. 視窗寬度響應式變數
// ============================================================================

// 視窗寬度響應式變數
const windowWidth = ref(window.innerWidth);

const updateWindowWidth = () => {
  windowWidth.value = window.innerWidth;
};

// ============================================================================
// 2. 論壇全局狀態管理
// ============================================================================

/**
 * 論壇頁面狀態
 * selectedCategory: 目前選擇的分類ID (null = 顯示全部)
 * categories: 分類列表
 * topics: 主題列表 (已經過濾和分頁)
 * isLoading: 是否載入中
 * pagination: 分頁資訊
 */
const forumState = reactive({
  selectedCategory: null,
  categories: [],
  topics: [],
  isLoading: false,
  pagination: {
    currentPage: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
});

// ============================================================================
// 3. 帖文詳情視圖狀態
// ============================================================================

/**
 * 帖文詳情和回覆資訊
 * selectedThread: 目前打開的主題ID
 * threadDetail: 主題詳細資訊
 * threadPosts: 該主題下的回文列表
 * threadComments: 該主題下的留言列表
 * threadCurrentPage: 回文分頁號碼
 */
const selectedThread = ref(null);
const threadDetail = reactive({
  id: null,
  title: "",
  author: "",
  createdAt: "",
  content: "",
  imageUrls: [],
  categoryId: null,
  type: null,
});
const threadPosts = ref([]);
const threadComments = ref([]);
const threadCurrentPage = ref(1);
const threadItemsPerPage = 5;

// ============================================================================
// 4. 分類管理
// ============================================================================

/**
 * 獲取分類列表
 * 調用API取得所有討論分類
 * 並設定新主題表單的預設分類
 */
const fetchCategories = () => {
  forumState.isLoading = true;

  apiClient
    .get("/CategoryApi")
    .then((data) => {
      console.log("分類資料:", data);
      forumState.categories = data;
      // 預設選擇目前選取的分類或第一筆
      if (!newTopic.categoryId) {
        const preferred =
          forumState.selectedCategory || (data.length > 0 ? data[0].id : null);
        newTopic.categoryId = preferred;
      }
    })
    .catch((error) => {
      console.error("載入分類失敗:", error);
      alert("載入分類失敗");
    })
    .finally(() => {
      forumState.isLoading = false;
    });
};

/**
 * 分類選擇事件
 * 切換分類篩選，重置到第一頁
 * 若選擇已選分類，則取消篩選
 */
const onCategorySelect = (categoryId) => {
  forumState.selectedCategory =
    categoryId === forumState.selectedCategory ? null : categoryId;

  forumState.pagination.currentPage = 1;
  fetchTopics(forumState.selectedCategory, 1);
};

// ============================================================================
// 5. 主題列表管理
// ============================================================================

/**
 * 獲取主題列表
 *
 * 流程：
 * 1. 取得所有內容資料 (categoryId可選篩選)
 * 2. 前端過濾為 type=1(貼文) 或 type=2(回文)
 * 3. 按時間排序
 * 4. 前端分頁
 * 5. 計算每個主題的回應統計
 *
 * @param {number} categoryId - 分類ID (可選)
 * @param {number} page - 頁碼
 */
const fetchTopics = async (categoryId = null, page = 1) => {
  forumState.isLoading = true;

  try {
    // 1. 獲取所有內容 (可依分類過濾)
    const allContentParams = {
      pageSize: 9999,
    };
    if (categoryId) allContentParams.categoryId = categoryId;

    const allContentData = await apiClient.get("/ContentApi", {
      params: allContentParams,
    });
    const allContents = allContentData.items || [];

    // 2. 僅保留 type=1(貼文) 和 type=2(回文)
    const listContents = allContents
      .filter(
        (c) => c.type === CONTENT_TYPES.POST || c.type === CONTENT_TYPES.REPLY
      )
      .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

    // 3. 前端分頁
    const pageSize = forumState.pagination.pageSize;
    const totalCount = listContents.length;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const currentPage = Math.min(Math.max(1, page), totalPages);
    const start = (currentPage - 1) * pageSize;
    const end = start + pageSize;
    const pageItems = listContents.slice(start, end);

    forumState.pagination = {
      currentPage,
      pageSize,
      totalCount,
      totalPages,
    };

    console.log("所有內容資料(已過濾 1/2):", pageItems);

    // 4. 組合列表顯示資料並計算回應統計
    forumState.topics = pageItems.map((item) => {
      const { textContent, imageUrls } = parseBodyContent(item.body);
      const replyStats = calculateReplyStats(item.contentId, allContents);

      return {
        id: item.contentId,
        title: item.title,
        content: textContent,
        imageUrls,
        author: item.username,
        authorAvatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(
          item.username
        )}&background=random`,
        categoryName: item.categoryName,
        createdAt: item.createdAt,
        type: item.type,

        // 前端計算的回應統計
        replyCount: replyStats.replyCount,
        lastReplyTime: replyStats.lastReplyTime,
        lastReplyUser: replyStats.lastReplyUser,

        // 其他欄位
        viewCount: item.viewCount ?? 0,
        isPinned: item.isPinned ?? false,
        isHot: replyStats.replyCount > 10,
      };
    });
  } catch (error) {
    console.error("載入主題失敗:", error);

    if (error.response) {
      alert(error.response.data.message || "載入主題失敗");
    } else if (error.request) {
      alert("無法連接到伺服器，請檢查網路連線");
    } else {
      alert("發生未知錯誤");
    }

    forumState.topics = [];
  } finally {
    forumState.isLoading = false;
  }
};

/**
 * 換頁事件
 */
const onPageChange = (page) => {
  if (page < 1 || page > forumState.pagination.totalPages) return;
  forumState.pagination.currentPage = page;
  fetchTopics(forumState.selectedCategory, page);
};

// ============================================================================
// 6. 帖文詳情頁管理
// ============================================================================

/**
 * 獲取帖文詳情
 * 包括主題內容、回文和留言
 *
 * @param {number} threadId - 主題ID
 */
const fetchThreadDetail = async (threadId) => {
  try {
    // 並行取得詳情和留言
    const [detailRes, commentsRes] = await Promise.all([
      apiClient.get(`/ContentApi/${threadId}`),
      contentInteractionApi.getComments(threadId),
    ]);

    // 處理主題詳情
    const detail = detailRes?.contentId ? detailRes : detailRes?.data;
    if (detail && detail.contentId) {
      const { textContent, imageUrls } = parseBodyContent(detail.body);
      threadDetail.id = detail.contentId;
      threadDetail.title = detail.title;
      threadDetail.author = detail.username;
      threadDetail.createdAt = detail.createdAt;
      threadDetail.content = textContent;
      threadDetail.imageUrls = imageUrls;
      threadDetail.categoryId = detail.categoryId;
      threadDetail.type = detail.type;
    }

    // 處理留言
    const commentsData = commentsRes?.comments || [];
    const commentItems = commentsData.map((comment) => {
      const { textContent, imageUrls } = parseBodyContent(comment.body);
      return {
        id: comment.contentId,
        author: comment.username,
        createdAt: comment.createdAt,
        content: textContent,
        imageUrls,
        type: CONTENT_TYPES.COMMENT,
        parentContentId: comment.parentContentId,
        replies: [],
      };
    });

    // 獲取回文
    const repliesRes = await apiClient.get("/ContentApi", {
      params: { parentContentId: threadId },
    });
    const replyItems = Array.isArray(repliesRes?.items) ? repliesRes.items : [];

    const normalizedReplies = replyItems
      .filter((i) => i.type === CONTENT_TYPES.REPLY)
      .map((i) => {
        const { textContent, imageUrls } = parseBodyContent(i.body);
        return {
          id: i.contentId,
          author: i.username,
          createdAt: i.createdAt,
          content: textContent,
          imageUrls,
          type: i.type,
          parentContentId: i.parentContentId,
          replies: [],
        };
      });

    // 排序
    const sortByDate = (list) => {
      list.sort(
        (a, b) =>
          new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      );
    };

    sortByDate(commentItems);
    sortByDate(normalizedReplies);
    threadPosts.value = normalizedReplies;
    threadComments.value = commentItems;
    threadCurrentPage.value = 1;
  } catch (error) {
    console.error("獲取帖文詳情失敗:", error);
  }
};

/**
 * 計算線程回文分頁資料
 */
const threadTotalPages = computed(() =>
  Math.ceil(threadPosts.value.length / threadItemsPerPage)
);

const paginatedThreadPosts = computed(() => {
  const start = (threadCurrentPage.value - 1) * threadItemsPerPage;
  return threadPosts.value.slice(start, start + threadItemsPerPage);
});

/**
 * 線程頁面導航
 */
const goToThreadPage = (page) => {
  if (page >= 1 && page <= threadTotalPages.value) {
    threadCurrentPage.value = page;
  }
};

const prevThreadPage = () => {
  if (threadCurrentPage.value > 1) threadCurrentPage.value--;
};

const nextThreadPage = () => {
  if (threadCurrentPage.value < threadTotalPages.value)
    threadCurrentPage.value++;
};

/**
 * 打開主題詳情頁面
 */
const openThread = (topicId) => {
  selectedThread.value = topicId;
  fetchThreadDetail(topicId);
};

/**
 * 關閉主題詳情頁面，返回列表
 */
const closeThread = () => {
  selectedThread.value = null;
  threadDetail.id = null;
  threadDetail.title = "";
  threadDetail.author = "";
  threadDetail.createdAt = "";
  threadDetail.content = "";
  threadDetail.imageUrls = [];
  threadPosts.value = [];
  threadComments.value = [];
};

/**
 * 回覆已提交事件
 * 刷新詳情頁資料
 */
const handleThreadReplyAdded = async () => {
  if (selectedThread.value) {
    await fetchThreadDetail(selectedThread.value);
  }
};

// ============================================================================
// 7. 新主題發表管理
// ============================================================================

// 新主題表單狀態
const showNewTopicBox = ref(false);
const newTopic = reactive({
  title: "",
  body: "",
  categoryId: null,
});
const isSubmittingNewTopic = ref(false);
const isUploadingNewTopicImages = ref(false);

/**
 * 切換新主題表單顯示/隱藏
 */
const toggleNewTopic = () => {
  showNewTopicBox.value = !showNewTopicBox.value;
};

/**
 * 提交新主題
 */
const submitNewTopic = async () => {
  if (!newTopic.title.trim()) {
    alert("請輸入標題");
    return;
  }
  if (!newTopic.body.trim()) {
    alert("請輸入內容");
    return;
  }
  if (!newTopic.categoryId) {
    alert("請選擇分類");
    return;
  }

  if (isSubmittingNewTopic.value) return;
  isSubmittingNewTopic.value = true;

  const dto = {
    userId: 1,
    categoryId: newTopic.categoryId,
    title: newTopic.title.trim(),
    body: newTopic.body.trim(),
    type: CONTENT_TYPES.POST,
  };

  try {
    console.log("提交DTO:", dto);
    await apiClient.post("/ContentApi", dto);
    alert("發表成功！");
    newTopic.title = "";
    newTopic.body = "";
    showNewTopicBox.value = false;
    forumState.pagination.currentPage = 1;
    await fetchTopics(forumState.selectedCategory, 1);
  } catch (error) {
    console.error("發表失敗:", error);
    alert("發表失敗，請稍後再試");
  } finally {
    isSubmittingNewTopic.value = false;
  }
};

/**
 * 新主題圖片上傳完成
 */
const handleNewTopicImages = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;
  isUploadingNewTopicImages.value = true;
  try {
    const { uploadImage } = await import("@/api/imageapi.js");
    const uploaded = await uploadImage(files);
    newTopic.body = appendUrls(newTopic.body, uploaded);
  } catch (error) {
    console.error("圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    isUploadingNewTopicImages.value = false;
    if (event.target) event.target.value = "";
  }
};

/**
 * 監聽分類選擇變更
 */
watch(
  () => forumState.selectedCategory,
  (val) => {
    if (val) newTopic.categoryId = val;
  }
);

// ============================================================================
// 8. 生命周期鉤子
// ============================================================================

/**
 * 初始化
 */
onMounted(() => {
  window.addEventListener("resize", updateWindowWidth);
  fetchCategories();
  fetchTopics();
});

/**
 * 清理
 */
onUnmounted(() => {
  window.removeEventListener("resize", updateWindowWidth);
});

/**
 * 監聽 Pinia store 的刷新信號
 */
watch(
  () => forumStore.lastRefreshTime,
  () => {
    fetchTopics(forumState.selectedCategory, forumState.pagination.currentPage);
  }
);

// ============================================================================
// 9. 分頁計算
// ============================================================================

/**
 * 分頁顯示計算
 */
const displayPages = computed(() => {
  const { currentPage, totalPages } = forumState.pagination;
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
  <DefaultNavbar light />

  <div class="container mt-5 mb-5">
    <!-- 論壇標題區 -->
    <div class="card shadow-sm mb-4">
      <div class="card-body">
        <div
          class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3"
        >
          <div>
            <h2 class="mb-2 fs-3 fs-md-2">💬 技術論壇</h2>
            <p class="text-muted mb-0 small">分享知識、交流技術、共同成長</p>
          </div>
          <button
            class="btn btn-forum-primary w-100 w-md-auto"
            @click="toggleNewTopic"
            type="button"
          >
            <i class="fas fa-plus"></i>
            <span class="d-none d-sm-inline">發表新主題</span>
          </button>
        </div>

        <!-- 新主題編輯區（同頁展開，不換頁） -->
        <div v-if="showNewTopicBox" class="mt-3 p-3 border rounded bg-light">
          <div class="mb-2">
            <label class="form-label small text-muted">分類</label>
            <select v-model="newTopic.categoryId" class="form-select">
              <option :value="null" disabled>選擇分類</option>
              <option
                v-for="category in forumState.categories"
                :key="category.id"
                :value="category.id"
              >
                {{ category.name }}
              </option>
            </select>
          </div>

          <div class="mb-2">
            <label class="form-label small text-muted">標題</label>
            <input
              v-model="newTopic.title"
              type="text"
              class="form-control"
              placeholder="輸入主題標題"
            />
          </div>
          <div class="mb-3">
            <label class="form-label small text-muted">內容</label>
            <textarea
              v-model="newTopic.body"
              class="form-control"
              :rows="windowWidth < 768 ? 4 : 5"
              placeholder="撰寫主題內容..."
            ></textarea>
            <div
              class="mt-2 d-flex flex-column flex-sm-row gap-2 align-items-start"
            >
              <input
                type="file"
                accept="image/*"
                multiple
                class="form-control form-control-sm"
                @change="handleNewTopicImages"
                :disabled="isUploadingNewTopicImages"
              />
              <small class="text-muted">
                上傳後會自動把圖片網址加入內容。{{
                  isUploadingNewTopicImages ? "上傳中..." : ""
                }}
              </small>
            </div>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button
              class="btn btn-forum-primary"
              @click="submitNewTopic"
              :disabled="
                isSubmittingNewTopic ||
                !newTopic.title.trim() ||
                !newTopic.body.trim() ||
                !newTopic.categoryId
              "
            >
              <i class="fas fa-paper-plane me-2"></i>
              {{ isSubmittingNewTopic ? "發送中..." : "發表" }}
            </button>
            <button
              class="btn btn-forum-secondary"
              type="button"
              @click="
                showNewTopicBox = false;
                newTopic.title = '';
                newTopic.body = '';
              "
            >
              取消
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="row g-3 g-md-4">
      <!-- 主要內容區 -->
      <div class="col-lg-9 order-2 order-lg-1">
        <!-- 分類列表（已縮小按鈕） -->
        <div class="card shadow-sm mb-4">
          <div
            class="card-header bg-white d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center py-3 gap-2"
          >
            <h5 class="mb-0 fs-6 fs-md-5">📁 討論分類</h5>
            <small class="text-muted">點擊分類篩選主題</small>
          </div>
          <div class="p-2 p-md-3">
            <div
              v-if="forumState.isLoading && forumState.categories.length === 0"
              class="text-center py-3"
            >
              <div
                class="spinner-border spinner-border-sm text-primary"
                role="status"
              >
                <span class="visually-hidden">載入中...</span>
              </div>
            </div>

            <div
              v-else
              class="d-flex flex-wrap align-content-start p-0 category-container"
            >
              <div
                v-for="category in forumState.categories"
                :key="category.id"
                class="m-1"
              >
                <!-- 已修正：改用 border-primary 來應用自訂選中顏色 -->
                <div
                  class="category-card p-0 border rounded cursor-pointer"
                  :class="{
                    'border-primary':
                      forumState.selectedCategory === category.id,
                    'border-light': forumState.selectedCategory !== category.id,
                  }"
                  @click="onCategorySelect(category.id)"
                  :title="category.description"
                >
                  <div class="d-flex justify-content-between align-items-start">
                    <h6 class="mb-0 fw-bold">{{ category.name }}</h6>
                  </div>
                  <div
                    class="category-tags mt-1"
                    :class="{
                      'text-white-50':
                        forumState.selectedCategory === category.id,
                    }"
                  >
                    <span>📝 {{ category.topicCount }}</span>
                    <span>💬 {{ category.postCount }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 主題列表 -->
        <div class="card shadow-sm">
          <!-- 標題列 -->
          <div
            class="card-header bg-white d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center gap-2 py-3"
          >
            <h5 class="mb-0 fs-6 fs-md-5">📋 討論主題</h5>
            <div class="d-flex flex-wrap gap-2">
              <span
                class="badge bg-secondary"
                v-if="forumState.selectedCategory"
              >
                {{
                  forumState.categories.find(
                    (c) => c.id === forumState.selectedCategory
                  )?.name || "當前分類"
                }}
              </span>
              <span class="badge bg-info"
                >共 {{ forumState.pagination.totalCount }} 筆</span
              >
            </div>
          </div>

          <div class="card-body p-0">
            <!-- 帖文詳情視圖 -->
            <div v-if="selectedThread">
              <!-- 返回按鈕 -->
              <div class="p-3 border-bottom">
                <button
                  class="btn btn-sm btn-forum-secondary"
                  @click="closeThread"
                >
                  <i class="fas fa-arrow-left me-2"></i>
                  返回列表
                </button>
              </div>

              <!-- 帖文主題 -->
              <div class="card-body border-bottom">
                <h3 class="card-title fs-4 fs-md-3 text-break">
                  {{ threadDetail.title }}
                </h3>
                <p class="text-muted mb-2 small d-flex flex-wrap gap-2">
                  <span>by {{ threadDetail.author }}</span>
                  <span>·</span>
                  <span>{{ formatDate(threadDetail.createdAt) }}</span>
                </p>
                <p
                  class="card-text"
                  style="white-space: pre-wrap; word-break: break-word"
                >
                  {{ threadDetail.content }}
                </p>
                <div
                  v-if="threadDetail.imageUrls && threadDetail.imageUrls.length"
                  class="d-flex flex-wrap gap-2 mt-2 topic-image-list"
                >
                  <img
                    v-for="(url, idx) in threadDetail.imageUrls"
                    :key="url + idx"
                    :src="url"
                    alt="貼文圖片"
                    class="topic-image-thumb thread-detail-image"
                    loading="lazy"
                  />
                </div>
              </div>

              <!-- 回覆 / 留言區域 -->
              <div class="p-3">
                <div class="mt-3 mt-md-5">
                  <h5 class="fs-5 fs-md-4 mb-3">💬 回覆 / 留言</h5>
                  <!-- 所有留言 -->
                  <div v-if="threadComments.length > 0" class="mb-4">
                    <h6 class="fs-6 mb-2">📝 所有留言</h6>
                    <ThreadPost
                      v-for="comment in threadComments"
                      :key="'comment-' + comment.id"
                      :post="comment"
                      :category-id="threadDetail.categoryId"
                      @reply-added="handleThreadReplyAdded"
                    />
                  </div>

                  <!-- 回文/留言分頁 -->
                  <div v-if="paginatedThreadPosts.length > 0" class="mb-4">
                    <h6 class="fs-6 mb-2">💭 回文</h6>
                    <ThreadPost
                      v-for="post in paginatedThreadPosts"
                      :key="post.id"
                      :post="post"
                      :category-id="threadDetail.categoryId"
                      @reply-added="handleThreadReplyAdded"
                    />
                  </div>

                  <!-- 分頁導航 -->
                  <nav v-if="threadTotalPages > 1" class="mt-3 mt-md-4">
                    <ul
                      class="pagination pagination-sm justify-content-center flex-wrap gap-1"
                    >
                      <li
                        class="page-item"
                        :class="{ disabled: threadCurrentPage === 1 }"
                        @click="prevThreadPage"
                      >
                        <a
                          class="page-link px-2 px-md-3"
                          href="javascript:void(0)"
                        >
                          <i class="fa-solid fa-caret-left"></i>
                        </a>
                      </li>
                      <li
                        class="page-item"
                        v-for="page in threadTotalPages"
                        :key="page"
                        :class="{ active: threadCurrentPage === page }"
                        @click="goToThreadPage(page)"
                      >
                        <a
                          class="page-link px-2 px-md-3"
                          href="javascript:void(0)"
                          >{{ page }}</a
                        >
                      </li>
                      <li
                        class="page-item"
                        :class="{
                          disabled: threadCurrentPage === threadTotalPages,
                        }"
                        @click="nextThreadPage"
                      >
                        <a
                          class="page-link px-2 px-md-3"
                          href="javascript:void(0)"
                        >
                          <i class="fa-solid fa-caret-right"></i>
                        </a>
                      </li>
                    </ul>
                  </nav>
                </div>
              </div>
            </div>

            <!-- 主題列表視圖 -->
            <div v-else>
              <div
                v-if="forumState.isLoading && forumState.topics.length === 0"
                class="text-center py-5"
              >
                <div class="spinner-border text-primary" role="status">
                  <span class="visually-hidden">載入中...</span>
                </div>
              </div>

              <div
                v-else-if="forumState.topics.length === 0"
                class="text-center py-5 text-muted"
              >
                <p>此分類目前沒有主題</p>
              </div>

              <!-- 表格式列表 -->
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
                      v-for="topic in forumState.topics"
                      :key="topic.id"
                      class="forum-row"
                      @click="openThread(topic.id)"
                    >
                      <!-- 主題欄 -->
                      <td class="p-3 py-md-3">
                        <div class="d-flex align-items-start gap-2">
                          <div class="flex-grow-1">
                            <div class="d-flex flex-wrap gap-1 mb-1">
                              <span
                                v-if="topic.isPinned"
                                class="badge bg-danger"
                                >置頂</span
                              >
                              <span
                                v-if="topic.isHot"
                                class="badge bg-warning text-dark"
                                >熱門</span
                              >
                            </div>
                            <h6 class="mb-2 fs-6 fw-bold">
                              <a
                                href="javascript:void(0)"
                                class="topic-title-link text-break"
                              >
                                {{ topic.title }}
                              </a>
                            </h6>
                            <div
                              v-if="topic.imageUrls && topic.imageUrls.length"
                              class="d-flex flex-wrap gap-2 mt-2 mb-2 topic-image-list"
                            >
                              <img
                                v-for="(url, idx) in topic.imageUrls.slice(
                                  0,
                                  3
                                )"
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
                            <div
                              class="small text-muted d-flex flex-wrap gap-2"
                            >
                              <span class="text-nowrap"
                                >👤 {{ topic.author }}</span
                              >
                              <span class="d-md-none text-nowrap"
                                >🕒 {{ formatTimeAgo(topic.createdAt) }}</span
                              >
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

          <!-- 分頁（只在列表視圖時顯示） -->
          <div
            class="card-footer bg-white py-2 py-md-3"
            v-if="!selectedThread && forumState.pagination.totalPages > 1"
          >
            <nav class="d-flex justify-content-center">
              <ul
                class="pagination pagination-sm mb-0 justify-content-center flex-wrap gap-1"
              >
                <li
                  class="page-item"
                  :class="{ disabled: forumState.pagination.currentPage === 1 }"
                >
                  <a
                    class="page-link"
                    href="#"
                    @click.prevent="
                      onPageChange(forumState.pagination.currentPage - 1)
                    "
                    >&lsaquo;</a
                  >
                </li>

                <li
                  v-if="displayPages[0] > 1"
                  class="page-item d-none d-md-block"
                >
                  <a class="page-link" href="#" @click.prevent="onPageChange(1)"
                    >1</a
                  >
                </li>
                <li
                  v-if="displayPages[0] > 2"
                  class="page-item disabled d-none d-md-block"
                >
                  <span class="page-link">...</span>
                </li>

                <li
                  v-for="pageNum in displayPages"
                  :key="pageNum"
                  class="page-item"
                  :class="{
                    active: pageNum === forumState.pagination.currentPage,
                  }"
                >
                  <a
                    class="page-link"
                    href="#"
                    @click.prevent="onPageChange(pageNum)"
                    >{{ pageNum }}</a
                  >
                </li>

                <li
                  v-if="
                    displayPages[displayPages.length - 1] <
                    forumState.pagination.totalPages - 1
                  "
                  class="page-item disabled d-none d-md-block"
                >
                  <span class="page-link">...</span>
                </li>
                <li
                  v-if="
                    displayPages[displayPages.length - 1] <
                    forumState.pagination.totalPages
                  "
                  class="page-item d-none d-md-block"
                >
                  <a
                    class="page-link"
                    href="#"
                    @click.prevent="
                      onPageChange(forumState.pagination.totalPages)
                    "
                    >{{ forumState.pagination.totalPages }}</a
                  >
                </li>

                <li
                  class="page-item"
                  :class="{
                    disabled:
                      forumState.pagination.currentPage ===
                      forumState.pagination.totalPages,
                  }"
                >
                  <a
                    class="page-link"
                    href="#"
                    @click.prevent="
                      onPageChange(forumState.pagination.currentPage + 1)
                    "
                    >&rsaquo;</a
                  >
                </li>
              </ul>
            </nav>
          </div>
        </div>
      </div>

      <!-- 側邊欄（保持你的原樣） -->
      <div class="col-lg-3">
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
                  forumState.pagination.totalCount
                }}</strong>
              </div>

              <div
                class="d-flex justify-content-between align-items-center"
              ></div>
            </div>
          </div>
        </div>
      </div>
      <!-- /側邊欄 -->
    </div>
  </div>

  <DefaultFooter />
</template>

<style scoped>
/* 自定義顏色方案 */
:root {
  --forum-primary: #432f28;
  --forum-light: #fff0dd;
  --forum-gray: #d1d3d4;
  --forum-accent: #86b0bd;
}

/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
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
  background-color: #432f28 !important;
  color: #fff0dd;
}

/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

.card-header {
  background-color: #fff0dd !important;
  border-bottom-color: #d1d3d4;
}

.card-header h5,
.card-header h6 {
  color: #432f28;
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
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
}

.pagination .page-link {
  color: #432f28;
  border-color: #d1d3d4;
}

.pagination .page-link:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}

/* 載入動畫顏色 */
.spinner-border {
  color: #432f28 !important;
}

/* 文字顏色 */
.text-primary {
  color: #432f28 !important;
}

/* 留言框樣式 */
.reply-box {
  background-color: rgba(255, 240, 221, 0.5);
  border-left-color: #432f28;
}

/* 邊框顏色 */
.border-forum-selected {
  border-color: #432f28 !important;
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
