<script setup>
import { reactive, onMounted, computed, watch } from "vue";
import { useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";

import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";

const forumStore = useForumStore();

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
    })
    .catch((error) => {
      console.error("載入分類失敗:", error);
      alert("載入分類失敗");
    })
    .finally(() => {
      forumState.isLoading = false;
    });
};

// 取得主題列表（ContentApi）
const fetchTopics = (categoryId = null, page = 1) => {
  forumState.isLoading = true;

  const params = {
    page: page,
    pageSize: forumState.pagination.pageSize,
  };
  if (categoryId) params.categoryId = categoryId;

  apiClient
    .get("/ContentApi", { params })
    .then((data) => {
      console.log("主題資料:", data);

      const {
        items,
        totalCount,
        totalPages,
        page: currentPage,
        pageSize,
      } = data;

      forumState.pagination = {
        currentPage,
        pageSize,
        totalCount,
        totalPages,
      };

      // ✅ 重要：這裡改用 camelCase（contentId/title/body/username/categoryName/createdAt）
      forumState.topics = items.map((item) => ({
        id: item.contentId,
        title: item.title,
        content: item.body, // 這就是你要顯示的「實際內容」
        author: item.username,
        authorAvatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(
          item.username
        )}&background=random`,
        categoryName: item.categoryName,
        createdAt: item.createdAt,

        // 下面如果後端還沒做，就先用假資料撐住 UI
        replyCount: Math.floor(Math.random() * 100),
        viewCount: Math.floor(Math.random() * 500),
        lastReplyTime: formatTimeAgo(item.createdAt),
        lastReplyUser: item.username,
        isPinned: false,
        isHot: Math.random() > 0.7,
      }));
    })
    .catch((error) => {
      console.error("載入主題失敗:", error);

      if (error.response) {
        alert(error.response.data.message || "載入主題失敗");
      } else if (error.request) {
        alert("無法連接到伺服器，請檢查網路連線");
      } else {
        alert("發生未知錯誤");
      }

      forumState.topics = [];
    })
    .finally(() => {
      forumState.isLoading = false;
    });
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
  fetchCategories();
  fetchTopics();
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

// 初始化每個主題的留言框狀態
const initializeReplyInputs = () => {
  forumState.topics.forEach((topic) => {
    if (!replyInputs[topic.id]) {
      replyInputs[topic.id] = "";
    }
    if (!submittingReplies[topic.id]) {
      submittingReplies[topic.id] = false;
    }
  });
};

// 切換留言框顯示
const showReplyBox = reactive({});
const toggleReplyBox = (topicId) => {
  showReplyBox[topicId] = !showReplyBox[topicId];
};

// 提交留言
const submitReply = async (topicId) => {
  const content = replyInputs[topicId]?.trim();
  if (!content) {
    alert("請輸入留言內容");
    return;
  }

  if (submittingReplies[topicId]) return;
  submittingReplies[topicId] = true;

  const dto = {
    UserId: 1, // TODO: 改成實際使用者 ID
    CategoryId: 1, // TODO: 根據實際情況調整分類 ID
    ParentContentId: topicId, // 綁定到該篇貼文
    Title: "", // 留言不需要標題
    Body: content,
  };

  try {
    await apiClient.post("/ContentApi", dto);
    alert("留言成功！");
    replyInputs[topicId] = "";
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

// 監聽主題變更時初始化留言框
watch(
  () => forumState.topics,
  () => {
    initializeReplyInputs();
  },
  { immediate: true }
);
</script>

<template>
  <DefaultNavbar light />

  <div class="container mt-5 mb-5">
    <!-- 論壇標題區 -->
    <div class="card shadow-sm mb-4">
      <div class="card-body">
        <div class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
          <div>
            <h2 class="mb-2 fs-3 fs-md-2">💬 技術論壇</h2>
            <p class="text-muted mb-0 small">分享知識、交流技術、共同成長</p>
          </div>
          <router-link :to="{ name: 'new-topic' }" class="btn btn-primary w-100 w-md-auto">
            <i class="fas fa-plus"></i> <span class="d-none d-sm-inline">發表</span>新主題
          </router-link>
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

            <div v-else class="list-group list-group-flush">
              <div
                v-for="topic in forumState.topics"
                :key="topic.id"
                class="list-group-item list-group-item-action p-2 p-md-3"
              >
                <div class="d-flex flex-column flex-sm-row w-100 gap-2">
                  <!-- 作者頭像 -->
                  <div class="d-flex align-items-start">
                    <img
                      :src="topic.authorAvatar"
                      :alt="topic.author"
                      class="rounded-circle me-2 me-sm-3"
                      :width="$window?.innerWidth < 576 ? 36 : 48"
                      :height="$window?.innerWidth < 576 ? 36 : 48"
                    />
                  </div>

                  <!-- 主題內容 -->
                  <div class="flex-grow-1 min-w-0">
                    <div
                      class="d-flex flex-column gap-2 mb-2"
                    >
                      <div class="w-100">
                        <h6 class="mb-2 fs-6">
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
                          <router-link
                            :to="{
                              name: 'thread-detail',
                              params: { id: topic.id },
                            }"
                            class="topic-title-link text-break"
                          >
                            {{ topic.title }}
                          </router-link>
                        </h6>

                        <div class="small text-muted mb-2 d-flex flex-wrap gap-2">
                          <span class="text-nowrap">👤 {{ topic.author }}</span>
                          <span class="text-nowrap">📁 {{ topic.categoryName }}</span>
                          <span class="text-nowrap"
                            >🕒 {{ formatTimeAgo(topic.createdAt) }}</span
                          >
                        </div>

                        <!-- ✅ 這裡就是「實際內容」 -->
                        <p class="mb-1 text-muted topic-body-preview">
                          {{ topic.content }}
                        </p>
                        <InteractionButtons :contentId="topic.id" />
                      </div>
                    </div>

                    <div
                      class="d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center gap-2 mt-2"
                    >
                      <div class="small text-muted d-flex flex-wrap gap-2">
                        <span class="text-nowrap">💬 {{ topic.replyCount }} 回覆</span>
                        <span class="text-nowrap">👁 {{ topic.viewCount }} 瀏覽</span>
                      </div>
                      <div class="small text-muted text-truncate" style="max-width: 100%;">
                        最後回覆：{{ topic.lastReplyUser }} ·
                        {{ topic.lastReplyTime }}
                      </div>
                    </div>

                    <!-- 留言按鈕 -->
                    <div class="mt-2">
                      <button
                        class="btn btn-sm btn-outline-primary"
                        @click="toggleReplyBox(topic.id)"
                      >
                        <i class="fas fa-comment-dots me-1"></i>
                        {{ showReplyBox[topic.id] ? '收起' : '留言' }}
                      </button>
                    </div>

                    <!-- 留言輸入框 -->
                    <div v-if="showReplyBox[topic.id]" class="mt-3 reply-box">
                      <textarea
                        v-model="replyInputs[topic.id]"
                        class="form-control mb-2"
                        rows="3"
                        placeholder="撰寫您的留言..."
                      ></textarea>
                      <div class="d-flex gap-2">
                        <button
                          class="btn btn-sm btn-primary"
                          @click="submitReply(topic.id)"
                          :disabled="submittingReplies[topic.id] || !replyInputs[topic.id]?.trim()"
                        >
                          <i class="fas fa-paper-plane me-1"></i>
                          {{ submittingReplies[topic.id] ? '發送中...' : '發送' }}
                        </button>
                        <button
                          class="btn btn-sm btn-secondary"
                          @click="showReplyBox[topic.id] = false; replyInputs[topic.id] = ''"
                        >
                          取消
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 分頁 -->
          <div
            class="card-footer bg-white py-2 py-md-3"
            v-if="forumState.pagination.totalPages > 1"
          >
            <nav>
              <ul class="pagination pagination-sm mb-0 justify-content-center flex-wrap gap-1">
                <li
                  class="page-item"
                  :class="{ disabled: forumState.pagination.currentPage === 1 }"
                >
                  <a
                    class="page-link px-2 px-md-3"
                    href="#"
                    @click.prevent="
                      onPageChange(forumState.pagination.currentPage - 1)
                    "
                    ><span class="d-none d-sm-inline"><i class="fa-solid fa-caret-left"></i></span><span class="d-inline d-sm-none"><i class="fa-solid fa-caret-left"></i></span></a
                  >
                </li>

                <li v-if="displayPages[0] > 1" class="page-item d-none d-md-block">
                  <a class="page-link px-2 px-md-3" href="#" @click.prevent="onPageChange(1)"
                    >1</a
                  >
                </li>
                <li v-if="displayPages[0] > 2" class="page-item disabled d-none d-md-block">
                  <span class="page-link px-2">...</span>
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
                    class="page-link px-2 px-md-3"
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
                  <span class="page-link px-2">...</span>
                </li>
                <li
                  v-if="
                    displayPages[displayPages.length - 1] <
                    forumState.pagination.totalPages
                  "
                  class="page-item d-none d-md-block"
                >
                  <a
                    class="page-link px-2 px-md-3"
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
                    class="page-link px-2 px-md-3"
                    href="#"
                    @click.prevent="
                      onPageChange(forumState.pagination.currentPage + 1)
                    "
                    ><span class="d-none d-sm-inline"><i class="fa-solid fa-caret-right"></i></span><span class="d-inline d-sm-none"><i class="fa-solid fa-caret-right"></i></span></a
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

              <div class="d-flex justify-content-between align-items-center">
              </div>
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

.list-group-item {
  transition: background-color 0.2s ease;
}

.list-group-item:hover {
  background-color: #f8f9fa;
}

.cursor-pointer {
  cursor: pointer;
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

/* 貼文內容只顯示前 2 行（避免太長） */
.topic-body-preview {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  font-size: 0.9rem;
  white-space: pre-wrap; /* 讓換行比較自然 */
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
  
  .topic-body-preview {
    font-size: 0.85rem;
  }
}

@media (max-width: 576px) {
  .category-container {
    max-height: 200px;
  }
  
  .topic-title-link {
    font-size: 0.95rem;
  }
  
  .min-w-0 {
    min-width: 0;
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
</style>
