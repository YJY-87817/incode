<script setup>
import { reactive, onMounted, computed, watch, ref, onUnmounted } from "vue";
import { useForumStore, CONTENT_TYPES } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import { uploadImage } from "@/api/imageapi.js";

import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";

// 取得不含 /api 的基底網址，方便把後端回傳的相對路徑轉為完整網址
const apiBaseUrl = (apiClient.defaults?.baseURL || "").replace(/\/api$/, "");

const toAbsoluteUrl = (url) => {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;
  if (url.startsWith("/")) return `${apiBaseUrl}${url}`;
  return `${apiBaseUrl}/${url}`;
};

const parseBodyContent = (body) => {
  const raw = body || "";
  // 改進的正則：匹配完整的 http/https 圖片 URL
  const imagePattern = /https?:\/\/[^\s()\[\]]+\.(?:png|jpe?g|gif|webp|bmp)/gi;
  const matches = raw.match(imagePattern) || [];
  const uniqueImages = Array.from(new Set(matches));

  const imageUrls = uniqueImages
    .map((url) => url.trim())
    .filter(Boolean)
    .map(toAbsoluteUrl);

  const textContent = raw
    .replace(/https?:\/\/[^\s()\[\]]+\.(?:png|jpe?g|gif|webp|bmp)/gi, "")
    .replace(/\n{2,}/g, "\n")
    .trim();
  const displayText = textContent || (imageUrls.length ? "(已附圖片)" : "");

  return {
    textContent: displayText,
    imageUrls,
  };
};

const forumStore = useForumStore();

// 視窗寬度響應式變數
const windowWidth = ref(window.innerWidth);

const updateWindowWidth = () => {
  windowWidth.value = window.innerWidth;
};

// 論壇狀態
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

// 取得分類列表
const fetchCategories = () => {
  forumState.isLoading = true;

  apiClient
    .get("/CategoryApi")
    .then((data) => {
      console.log("分類資料:", data);
      forumState.categories = data;
      // 預設選擇目前選取的分類或第一筆，便於立即發文
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

// 計算貼文的回應統計（從所有 content 中計算）
const calculateReplyStats = (postId, allContents) => {
  // 找出該貼文的所有回應
  // 包括：回文(type=2)、留言(type=3)、留言回覆(type=4)
  // 條件：parentContentId === postId
  const replies = allContents.filter(
    (c) =>
      c.parentContentId === postId &&
      (c.type === 2 || c.type === 3 || c.type === 4)
  );

  if (replies.length === 0) {
    return {
      replyCount: 0,
      lastReplyTime: null,
      lastReplyUser: null,
    };
  }

  // 按時間排序，取得最新的回應
  const sortedReplies = replies.sort(
    (a, b) => new Date(b.createdAt) - new Date(a.createdAt)
  );
  const lastReply = sortedReplies[0];

  return {
    replyCount: replies.length,
    lastReplyTime: lastReply.createdAt,
    lastReplyUser: lastReply.username,
  };
};

// 取得主題列表（ContentApi）- 只顯示 type=1/2（貼文、回文），留言類型 3/4 僅在詳情頁顯示
const fetchTopics = async (categoryId = null, page = 1) => {
  forumState.isLoading = true;

  try {
    // 1. 獲取所有內容（可依分類過濾），用於前端過濾與分頁
    const allContentParams = {
      pageSize: 9999,
    };
    if (categoryId) allContentParams.categoryId = categoryId;

    const allContentData = await apiClient.get("/ContentApi", {
      params: allContentParams,
    });
    const allContents = allContentData.items || [];

    // 2. 僅保留 type=1(貼文)、type=2(回文)
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

    // 4. 組合列表顯示資料並計算回應統計（replyCount/lastReply）
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

        // 從前端計算的回應統計
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

// 時間格式化（顯示「X 小時前」）
const formatTimeAgo = (date) => {
  const now = new Date();
  const d = new Date(date);
  const diff = now - d;

  const seconds = Math.floor(diff / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);

  if (days > 0) return `${days} 天前`;
  if (hours > 0) return `${hours} 小時前`;
  if (minutes > 0) return `${minutes} 分鐘前`;
  return "剛剛";
};

// 日期格式化（顯示 YYYY-MM-DD HH:MM 格式）
const formatDate = (date) => {
  if (!date) return "";
  const d = new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${year}-${month}-${day} ${hours}:${minutes}`;
};

// 選擇分類
const onCategorySelect = (categoryId) => {
  forumState.selectedCategory =
    categoryId === forumState.selectedCategory ? null : categoryId;

  forumState.pagination.currentPage = 1;
  fetchTopics(forumState.selectedCategory, 1);
};

// 換頁
const onPageChange = (page) => {
  if (page < 1 || page > forumState.pagination.totalPages) return;
  forumState.pagination.currentPage = page;
  fetchTopics(forumState.selectedCategory, page);
};

// 分頁顯示
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

// 初始化
onMounted(() => {
  window.addEventListener("resize", updateWindowWidth);
  fetchCategories();
  fetchTopics();
});

onUnmounted(() => {
  window.removeEventListener("resize", updateWindowWidth);
});

// 監聽主題刷新時間變更
watch(
  () => forumStore.lastRefreshTime,
  () => {
    fetchTopics(forumState.selectedCategory, forumState.pagination.currentPage);
  }
);

// 留言功能
const replyInputs = reactive({});
const submittingReplies = reactive({});
const uploadingReplyImages = reactive({});
const showNewTopicBox = ref(false);
const newTopic = reactive({
  title: "",
  body: "",
  categoryId: null,
});
const isSubmittingNewTopic = ref(false);
const isUploadingNewTopicImages = ref(false);

const appendUrls = (original, urls) => {
  const list = Array.isArray(urls) ? urls : [urls];
  const chunk = list.filter(Boolean).join("\n");
  if (!chunk) return original;
  return [original.trim(), chunk].filter(Boolean).join("\n");
};

const handleNewTopicImages = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;
  isUploadingNewTopicImages.value = true;
  try {
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

// 初始化每個主題的留言框狀態
const initializeReplyInputs = () => {
  forumState.topics.forEach((topic) => {
    if (!replyInputs[topic.id]) {
      replyInputs[topic.id] = "";
    }
    if (!submittingReplies[topic.id]) {
      submittingReplies[topic.id] = false;
    }
    if (!uploadingReplyImages[topic.id]) {
      uploadingReplyImages[topic.id] = false;
    }
  });
};

// 切換留言框顯示
const showReplyBox = reactive({});
const replyTitles = reactive({});
const toggleReplyBox = (topicId) => {
  showReplyBox[topicId] = !showReplyBox[topicId];
};

const toggleNewTopic = () => {
  showNewTopicBox.value = !showNewTopicBox.value;
};

// 初始化留言標題
const initializeReplyTitles = () => {
  forumState.topics.forEach((topic) => {
    if (!replyTitles[topic.id]) {
      replyTitles[topic.id] = "";
    }
  });
};

// 提交留言
const submitReply = async (topicId) => {
  const title = replyTitles[topicId]?.trim();
  const content = replyInputs[topicId]?.trim();

  if (!title) {
    alert("請輸入標題");
    return;
  }
  if (!content) {
    alert("請輸入留言內容");
    return;
  }

  if (submittingReplies[topicId]) return;
  submittingReplies[topicId] = true;

  const dto = {
    ParentContentId: topicId, // 綁定到該篇貼文
    CategoryId: 2, // TODO: 根據實際情況調整分類 ID
    Title: title, // 留言標題
    Body: content,
    Type: 3, // 設定為留言類型 (CONTENT_TYPES.COMMENT)
  };

  try {
    await apiClient.post("/ContentApi", dto);
    alert("留言成功！");
    replyInputs[topicId] = "";
    replyTitles[topicId] = "";
    showReplyBox[topicId] = false;
    // 重新載入主題列表
    await fetchTopics(
      forumState.selectedCategory,
      forumState.pagination.currentPage
    );
  } catch (error) {
    console.error("留言失敗:", error);
    alert("留言失敗，請稍後再試");
  } finally {
    submittingReplies[topicId] = false;
  }
};

const handleReplyImages = async (topicId, event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;
  uploadingReplyImages[topicId] = true;
  try {
    const uploaded = await uploadImage(files);
    replyInputs[topicId] = appendUrls(replyInputs[topicId], uploaded);
  } catch (error) {
    console.error("留言圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    uploadingReplyImages[topicId] = false;
    if (event.target) event.target.value = "";
  }
};

// 監聽主題變更時初始化留言框
watch(
  () => forumState.topics,
  () => {
    initializeReplyInputs();
    initializeReplyTitles();
  },
  { immediate: true }
);

// 發表新主題（同頁展開，不換頁）
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
    userId: 1, // TODO: 改成實際使用者 ID
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
    // 重新載入主題列表回到第一頁
    forumState.pagination.currentPage = 1;
    await fetchTopics(forumState.selectedCategory, 1);
  } catch (error) {
    console.error("發表失敗:", error);
    alert("發表失敗，請稍後再試");
  } finally {
    isSubmittingNewTopic.value = false;
  }
};

// 依目前分類更新新發文的預設分類
watch(
  () => forumState.selectedCategory,
  (val) => {
    if (val) newTopic.categoryId = val;
  }
);
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
            class="btn btn-primary w-100 w-md-auto"
            @click="toggleNewTopic"
            type="button"
          >
            <i class="fas fa-plus"></i>
            <span class="d-none d-sm-inline">發表</span>新主題
          </button>
        </div>

        <!-- 新主題編輯區（同頁展開，不換頁） -->
        <div v-if="showNewTopicBox" class="mt-3 p-3 border rounded bg-light">
          <div class="mb-2">
            <label class="form-label small text-muted">標題</label>
            <input
              v-model="newTopic.title"
              type="text"
              class="form-control"
              placeholder="輸入主題標題"
            />
          </div>
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
              class="btn btn-primary"
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
              class="btn btn-outline-secondary"
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
                    @click="
                      $router.push({
                        name: 'thread-detail',
                        params: { id: topic.id },
                      })
                    "
                  >
                    <!-- 主題欄 -->
                    <td class="p-3 py-md-3">
                      <div class="d-flex align-items-start gap-2">
                        <div class="flex-grow-1">
                          <div class="d-flex flex-wrap gap-1 mb-1">
                            <span v-if="topic.isPinned" class="badge bg-danger"
                              >置頂</span
                            >
                            <span
                              v-if="topic.isHot"
                              class="badge bg-warning text-dark"
                              >熱門</span
                            >
                          </div>
                          <h6 class="mb-2 fs-6 fw-bold">
                            <router-link
                              :to="{
                                name: 'thread-detail',
                                params: { id: topic.id },
                              }"
                              class="topic-title-link text-break"
                              @click.stop
                            >
                              {{ topic.title }}
                            </router-link>
                          </h6>
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
                          <div class="small text-muted d-flex flex-wrap gap-2">
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
                      <div class="badge bg-primary">{{ topic.replyCount }}</div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- 分頁 -->
          <div
            class="card-footer bg-white py-2 py-md-3"
            v-if="forumState.pagination.totalPages > 1"
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
.category-card.border-primary {
  background: #edf18d;
  box-shadow: 0 0 0 3px #edf18d;
}

/* 表格行樣式 */
.forum-row {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.forum-row:hover {
  background-color: #f8f9fa !important;
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
  color: #344767;
  text-decoration: none;
  font-weight: 500;
  transition: color 0.2s ease;
}

.topic-title-link:hover {
  color: #cb0c9f;
  text-decoration: underline;
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
  background-color: #f8f9fa;
  border-radius: 0.5rem;
  border-left: 3px solid #cb0c9f;
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

@media (max-width: 768px) {
  .category-container {
    max-height: 250px;
  }
}

@media (max-width: 576px) {
  .category-container {
    max-height: 200px;
  }
}
</style>
