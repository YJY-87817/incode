<script setup>
import { reactive, onMounted, computed, watch } from "vue";
import axios from "axios";
import { useForumStore } from "@/stores/forumStore";

import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import NewTopicModal from "@/components/forum/NewTopicModal.vue";

const forumStore = useForumStore();

// 後端 API 基底網址
const apiBaseUrl = "https://localhost:7190/api"; // 改成你的後端

// 建立 axios 實例（.then().catch() 你熟悉的格式）
const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

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
    .then((response) => {
      console.log("分類資料:", response.data);
      forumState.categories = response.data;
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
    .then((response) => {
      console.log("主題資料:", response.data);

      const {
        items,
        totalCount,
        totalPages,
        page: currentPage,
        pageSize,
      } = response.data;

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

// 開啟新主題 Modal
const openNewTopic = () => {
  forumStore.toggleNewTopicModal(true);
};

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
</script>

<template>
  <DefaultNavbar
    :action="{
      route: 'javascript:;',
    }"
  />

  <div class="container mt-5 mb-5">
    <!-- 論壇標題區 -->
    <div class="card shadow-sm mb-4">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-center">
          <div>
            <h2 class="mb-2">💬 技術論壇</h2>
            <p class="text-muted mb-0">分享知識、交流技術、共同成長</p>
          </div>
          <button class="btn btn-primary" @click="openNewTopic">
            <i class="fas fa-plus"></i> 發表新主題
          </button>
        </div>
      </div>
    </div>

    <!-- 新主題發表 Modal 組件 -->
    <NewTopicModal />

    <div class="row">
      <!-- 主要內容區 -->
      <div class="col-lg-9">
        <!-- 分類列表（已縮小按鈕） -->
        <div class="card shadow-sm mb-4">
          <div
            class="card-header bg-white d-flex justify-content-between align-items-center py-3"
          >
            <h5 class="mb-0">📁 討論分類</h5>
            <small class="text-muted">點擊分類篩選主題</small>
          </div>
          <div class="p-3">
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
              class="d-flex flex-wrap align-content-start p-0"
              style="max-height: 400px"
            >
              <div
                v-for="category in forumState.categories"
                :key="category.id"
                class="m-1"
              >
                <!-- 這裡有問題 -->
                <div
                  class="category-card p-0 border rounded cursor-pointer"
                  :class="{
                    'bg-info text-white':
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
            class="card-header bg-white d-flex justify-content-between align-items-center"
          >
            <h5 class="mb-0">📋 討論主題</h5>
            <div>
              <span
                class="badge bg-secondary me-2"
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
                class="list-group-item list-group-item-action"
              >
                <div class="d-flex w-100">
                  <!-- 作者頭像 -->
                  <div class="me-3">
                    <img
                      :src="topic.authorAvatar"
                      :alt="topic.author"
                      class="rounded-circle"
                      width="48"
                      height="48"
                    />
                  </div>

                  <!-- 主題內容 -->
                  <div class="flex-grow-1">
                    <div
                      class="d-flex justify-content-between align-items-start mb-2"
                    >
                      <div>
                        <h6 class="mb-1">
                          <span
                            v-if="topic.isPinned"
                            class="badge bg-danger me-2"
                            >置頂</span
                          >
                          <span
                            v-if="topic.isHot"
                            class="badge bg-warning text-dark me-2"
                            >熱門</span
                          >
                          {{ topic.title }}
                        </h6>

                        <div class="small text-muted mb-1">
                          <span class="me-3">👤 {{ topic.author }}</span>
                          <span class="me-3">📁 {{ topic.categoryName }}</span>
                          <span class="me-3"
                            >🕒 {{ formatTimeAgo(topic.createdAt) }}</span
                          >
                        </div>

                        <!-- ✅ 這裡就是「實際內容」 -->
                        <p class="mb-1 text-muted topic-body-preview">
                          {{ topic.content }}
                        </p>
                      </div>
                    </div>

                    <div
                      class="d-flex justify-content-between align-items-center"
                    >
                      <div class="small text-muted">
                        <span class="me-3">💬 {{ topic.replyCount }} 回覆</span>
                        <span class="me-3">👁 {{ topic.viewCount }} 瀏覽</span>
                      </div>
                      <div class="small text-muted">
                        最後回覆：{{ topic.lastReplyUser }} ·
                        {{ topic.lastReplyTime }}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 分頁 -->
          <div
            class="card-footer bg-white"
            v-if="forumState.pagination.totalPages > 1"
          >
            <nav>
              <ul class="pagination pagination-sm mb-0 justify-content-center">
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
                    >上一頁</a
                  >
                </li>

                <li v-if="displayPages[0] > 1" class="page-item">
                  <a class="page-link" href="#" @click.prevent="onPageChange(1)"
                    >1</a
                  >
                </li>
                <li v-if="displayPages[0] > 2" class="page-item disabled">
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
                  class="page-item disabled"
                >
                  <span class="page-link">...</span>
                </li>
                <li
                  v-if="
                    displayPages[displayPages.length - 1] <
                    forumState.pagination.totalPages
                  "
                  class="page-item"
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
                    >下一頁</a
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
                class="d-flex justify-content-between align-items-center mb-1"
              >
                <span class="small text-muted">總回覆數</span>
                <strong class="text-success">2,707</strong>
              </div>
              <div
                class="d-flex justify-content-between align-items-center mb-1"
              >
                <span class="small text-muted">註冊會員</span>
                <strong class="text-info">1,234</strong>
              </div>
              <div class="d-flex justify-content-between align-items-center">
                <span class="small text-muted">在線用戶</span>
                <strong class="text-danger">45 👥</strong>
              </div>
            </div>
          </div>
        </div>

        <div class="card shadow-sm mb-4">
          <div class="card-header bg-white">
            <h6 class="mb-0">🔥 熱門主題</h6>
          </div>
          <div class="card-body p-0">
            <div class="list-group list-group-flush">
              <a href="#" class="list-group-item list-group-item-action small">
                <div class="d-flex justify-content-between">
                  <span class="text-truncate">Vue 3 最佳實踐</span>
                  <span class="badge bg-primary rounded-pill">156</span>
                </div>
              </a>
              <a href="#" class="list-group-item list-group-item-action small">
                <div class="d-flex justify-content-between">
                  <span class="text-truncate">前端效能優化技巧</span>
                  <span class="badge bg-primary rounded-pill">132</span>
                </div>
              </a>
              <a href="#" class="list-group-item list-group-item-action small">
                <div class="d-flex justify-content-between">
                  <span class="text-truncate">RESTful API 設計</span>
                  <span class="badge bg-primary rounded-pill">98</span>
                </div>
              </a>
            </div>
          </div>
        </div>

        <div class="card shadow-sm">
          <div class="card-header bg-white">
            <h6 class="mb-0">💬 最新回覆</h6>
          </div>
          <div class="card-body p-0">
            <div class="list-group list-group-flush">
              <div class="list-group-item small">
                <div class="d-flex align-items-center mb-1">
                  <img
                    src="https://ui-avatars.com/api/?name=User&background=random"
                    class="rounded-circle me-2"
                    width="24"
                    height="24"
                  />
                  <strong>李四</strong>
                </div>
                <p class="mb-1 text-muted text-truncate">
                  回覆了「Vue 3 使用心得」
                </p>
                <small class="text-muted">5 分鐘前</small>
              </div>
              <div class="list-group-item small">
                <div class="d-flex align-items-center mb-1">
                  <img
                    src="https://ui-avatars.com/api/?name=User2&background=random"
                    class="rounded-circle me-2"
                    width="24"
                    height="24"
                  />
                  <strong>王五</strong>
                </div>
                <p class="mb-1 text-muted text-truncate">
                  回覆了「效能優化問題」
                </p>
                <small class="text-muted">15 分鐘前</small>
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
  box-shadow: 0 0 0 3px rgba(145, 189, 255, 0.25);
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

/* 貼文內容只顯示前 2 行（避免太長） */
.topic-body-preview {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  font-size: 0.9rem;
  white-space: pre-wrap; /* 讓換行比較自然 */
}

/* 響應式調整 */
@media (max-width: 768px) {
  .row-cols-2 > .col {
    flex: 0 0 50%;
    max-width: 50%;
  }
}

@media (max-width: 576px) {
  .row-cols-2 > .col {
    flex: 0 0 100%;
    max-width: 100%;
  }
}
</style>
