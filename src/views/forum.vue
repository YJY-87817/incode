<script setup>
import { ref, reactive, onMounted } from "vue";

// 塞在這裡
import axios from "axios";
const responseData = ref("");
const products = ref("");
// 塞在這裡

// Example components
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

// 論壇狀態
const forumState = reactive({
  selectedCategory: null,
  categories: [],
  topics: [],
  isLoading: false,
});

// 獲取分類列表
const fetchCategories = async () => {
  forumState.isLoading = true;
  try {
    // 模擬數據
    forumState.categories = [
      {
        id: 1,
        name: "一般討論",
        topicCount: 150,
        postCount: 1250,
        description: "自由討論各種話題",
      },
      {
        id: 2,
        name: "技術問答",
        topicCount: 89,
        postCount: 567,
        description: "技術相關問題討論",
      },
      {
        id: 3,
        name: "新手入門",
        topicCount: 234,
        postCount: 890,
        description: "新手提問與學習",
      },
      {
        id: 4,
        name: "專案分享",
        topicCount: 45,
        postCount: 234,
        description: "分享你的專案成果",
      },
    ];
  } catch (error) {
    console.error("載入分類失敗:", error);
  } finally {
    forumState.isLoading = false;
  }
};

// 獲取主題列表
const fetchTopics = async (categoryId = null) => {
  forumState.isLoading = true;
  try {
    // 模擬數據
    forumState.topics = [
      {
        id: 1,
        title: "Vue 3 Composition API 使用心得分享",
        author: "張三",
        authorAvatar:
          "https://ui-avatars.com/api/?name=Zhang+San&background=random",
        categoryId: 2,
        categoryName: "技術問答",
        replyCount: 23,
        viewCount: 456,
        lastReplyTime: "2 小時前",
        lastReplyUser: "李四",
        isPinned: true,
        isHot: true,
      },
      {
        id: 2,
        title: "如何優化前端效能？求大神指點",
        author: "李四",
        authorAvatar:
          "https://ui-avatars.com/api/?name=Li+Si&background=random",
        categoryId: 2,
        categoryName: "技術問答",
        replyCount: 15,
        viewCount: 289,
        lastReplyTime: "5 小時前",
        lastReplyUser: "王五",
        isPinned: false,
        isHot: true,
      },
      {
        id: 3,
        title: "ASP.NET Core 與 Vue 3 整合教學",
        author: "王五",
        authorAvatar:
          "https://ui-avatars.com/api/?name=Wang+Wu&background=random",
        categoryId: 2,
        categoryName: "技術問答",
        replyCount: 8,
        viewCount: 156,
        lastReplyTime: "1 天前",
        lastReplyUser: "趙六",
        isPinned: false,
        isHot: false,
      },
      {
        id: 4,
        title: "新手提問：Vue Router 路由設定問題",
        author: "趙六",
        authorAvatar:
          "https://ui-avatars.com/api/?name=Zhao+Liu&background=random",
        categoryId: 3,
        categoryName: "新手入門",
        replyCount: 32,
        viewCount: 678,
        lastReplyTime: "3 小時前",
        lastReplyUser: "張三",
        isPinned: false,
        isHot: true,
      },
      {
        id: 5,
        title: "分享我的第一個 Vue 專案！",
        author: "錢七",
        authorAvatar:
          "https://ui-avatars.com/api/?name=Qian+Qi&background=random",
        categoryId: 4,
        categoryName: "專案分享",
        replyCount: 12,
        viewCount: 234,
        lastReplyTime: "6 小時前",
        lastReplyUser: "李四",
        isPinned: false,
        isHot: false,
      },
    ];

    // 如果有選擇分類，過濾主題
    if (categoryId) {
      forumState.topics = forumState.topics.filter(
        (t) => t.categoryId === categoryId
      );
    }
  } catch (error) {
    console.error("載入主題失敗:", error);
  } finally {
    forumState.isLoading = false;
  }
};

// 選擇分類
const onCategorySelect = (categoryId) => {
  forumState.selectedCategory =
    categoryId === forumState.selectedCategory ? null : categoryId;
  fetchTopics(forumState.selectedCategory);
};

// 初始化
onMounted(() => {
  fetchCategories();
  fetchTopics();
});
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
            <!-- 塞在這裡 -->

            <!-- 塞在這裡 -->
          </div>
          <button class="btn btn-primary">
            <i class="fas fa-plus"></i> 發表新主題
          </button>
        </div>
      </div>
    </div>

    <div class="row">
      <!-- 主要內容區 -->
      <div class="col-lg-9">
        <!-- 分類列表 -->
        <div class="card shadow-sm mb-4">
          <div class="card-header bg-white">
            <h5 class="mb-0">📁 討論分類</h5>
          </div>
          <div class="card-body">
            <div v-if="forumState.isLoading" class="text-center py-4">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">載入中...</span>
              </div>
            </div>

            <div v-else class="row g-3">
              <div
                v-for="category in forumState.categories"
                :key="category.id"
                class="col-md-6"
              >
                <div
                  class="category-card p-3 border rounded cursor-pointer"
                  :class="{
                    'border-primary bg-light':
                      forumState.selectedCategory === category.id,
                  }"
                  @click="onCategorySelect(category.id)"
                >
                  <h6 class="mb-1">{{ category.name }}</h6>
                  <p class="text-muted small mb-2">
                    {{ category.description }}
                  </p>
                  <div class="d-flex gap-3 small text-muted">
                    <span>📝 {{ category.topicCount }} 主題</span>
                    <span>💬 {{ category.postCount }} 回覆</span>
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
            <span class="badge bg-secondary" v-if="forumState.selectedCategory">
              已篩選
            </span>
          </div>
          <div class="card-body p-0">
            <div v-if="forumState.isLoading" class="text-center py-5">
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
                        <div class="small text-muted">
                          <span class="me-3">👤 {{ topic.author }}</span>
                          <span class="me-3">📁 {{ topic.categoryName }}</span>
                        </div>
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
          <div class="card-footer bg-white">
            <nav>
              <ul class="pagination pagination-sm mb-0 justify-content-center">
                <li class="page-item disabled">
                  <a class="page-link" href="#">上一頁</a>
                </li>
                <li class="page-item active">
                  <a class="page-link" href="#">1</a>
                </li>
                <li class="page-item">
                  <a class="page-link" href="#">2</a>
                </li>
                <li class="page-item">
                  <a class="page-link" href="#">3</a>
                </li>
                <li class="page-item">
                  <a class="page-link" href="#">下一頁</a>
                </li>
              </ul>
            </nav>
          </div>
        </div>
      </div>

      <!-- 側邊欄 -->
      <div class="col-lg-3">
        <!-- 論壇統計 -->
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
                <strong class="text-primary">473</strong>
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

        <!-- 熱門主題 -->
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

        <!-- 最新回覆 -->
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
    </div>
  </div>

  <DefaultFooter />
</template>

<style scoped>
.category-card {
  cursor: pointer;
  transition: all 0.3s ease;
}

.category-card:hover {
  background-color: #f8f9fa;
  border-color: #0d6efd !important;
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
</style>
