<script setup>
import { ref, computed, onMounted } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import FooterCentered from "@/examples/footers/FooterCentered.vue";

// 1. 狀態控制
const activeTab = ref("platform");
const searchQuery = ref("");
const selectedCategory = ref("全部");
const isDrawerOpen = ref(false);
const selectedTask = ref(null);
const isLoading = ref(false);
const showAdvancedFilters = ref(false);
const sortOption = ref("default");
const selectedDifficulty = ref("全部");
const selectedDuration = ref("全部");

// 2. 官方平台任務資料 - 增加更多任務類型和詳細信息
const platformMissions = ref([
  {
    id: 1,
    title: "影評先鋒",
    desc: "發表 300 字以上新片影評，帶動討論熱度。",
    reward: 150,
    status: "available",
    icon: "fas fa-pen-nib",
    color: "bg-gradient-info",
    category: "內容貢獻",
    difficulty: "中等",
    duration: "3-5 天",
    participants: 42,
    featured: true,
  },
  {
    id: 2,
    title: "每日觀影簽到",
    desc: "登入論壇領取每日活躍積分。",
    reward: 20,
    status: "completed",
    icon: "fas fa-check-circle",
    color: "bg-gradient-success",
    category: "日常",
    difficulty: "簡單",
    duration: "每日",
    participants: 128,
    featured: false,
  },
  {
    id: 3,
    title: "資料考據員",
    desc: "為 3 部冷門老片補充正確的導演與編劇資料。",
    reward: 200,
    status: "available",
    icon: "fas fa-film",
    color: "bg-gradient-warning",
    category: "資料庫",
    difficulty: "高級",
    duration: "7-14 天",
    participants: 15,
    featured: true,
  },
  {
    id: 4,
    title: "金牌推廣員",
    desc: "分享本周專題文章至外部社群並獲得 10 次點擊。",
    reward: 100,
    status: "available",
    icon: "fas fa-bullhorn",
    color: "bg-gradient-primary",
    category: "推廣",
    difficulty: "中等",
    duration: "7 天",
    participants: 33,
    featured: false,
  },
  {
    id: 5,
    title: "影迷問卷調查",
    desc: "完成本月影迷偏好調查問卷。",
    reward: 50,
    status: "available",
    icon: "fas fa-poll",
    color: "bg-gradient-secondary",
    category: "日常",
    difficulty: "簡單",
    duration: "3 天",
    participants: 89,
    featured: false,
  },
  {
    id: 6,
    title: "電影資源貢獻",
    desc: "上傳 5 部罕見電影的高清海報。",
    reward: 180,
    status: "available",
    icon: "fas fa-upload",
    color: "bg-gradient-dark",
    category: "資源",
    difficulty: "中等",
    duration: "10 天",
    participants: 22,
    featured: true,
  },
]);

// 3. 影友委託任務資料 - 增加更多詳細信息和類型
const commissions = ref([
  {
    id: 101,
    title: "徵求《敦克爾克》IMAX 2D 實體票根",
    requester: "收藏家老張",
    price: 500,
    category: "票務",
    deadline: "3天內",
    urgent: true,
    rating: 5.0,
    detail:
      "尋找 2017 年首映期間的 IMAX 實體票根（需無明顯摺痕），收藏用途，可走掛號郵寄或台北面交。",
    createdAt: "2026-01-24",
    views: 128,
    applicants: 5,
  },
  {
    id: 102,
    title: "求《2001太空漫遊》冷門花絮字幕翻譯",
    requester: "影迷小明",
    price: 800,
    category: "字幕",
    deadline: "7天內",
    urgent: false,
    rating: 4.8,
    detail:
      "有一段約 10 分鐘的導演訪談紀錄片，目前只有英文原聲，希望有大神能幫忙聽譯並製作簡單中文字幕檔。",
    createdAt: "2026-01-23",
    views: 89,
    applicants: 3,
  },
  {
    id: 103,
    title: "尋找一部 90 年代法國關於鋼琴的老片",
    requester: "記憶碎片",
    price: 200,
    category: "尋片",
    deadline: "無期限",
    urgent: false,
    rating: 4.2,
    detail:
      "小時候看過一部片，場景在巴黎，主角是一位失明的鋼琴家。線索很少，希望能有資深影友幫忙指路。",
    createdAt: "2026-01-20",
    views: 210,
    applicants: 12,
  },
  {
    id: 104,
    title: "求《沙丘2》原著小說關鍵劇情導讀",
    requester: "書蟲",
    price: 300,
    category: "解析",
    deadline: "5天內",
    urgent: false,
    rating: 4.9,
    detail:
      "看完電影後對世界觀很感興趣，希望能有讀過原著的朋友幫忙整理聖戰與保羅覺醒的邏輯關係。",
    createdAt: "2026-01-25",
    views: 65,
    applicants: 2,
  },
  {
    id: 105,
    title: "尋求《教父》三部曲 4K 修復版資源",
    requester: "高清愛好者",
    price: 1200,
    category: "資源",
    deadline: "14天內",
    urgent: true,
    rating: 4.7,
    detail: "尋找《教父》三部曲的 4K 修復版數位資源，需要包含導演評論音軌。",
    createdAt: "2026-01-26",
    views: 45,
    applicants: 1,
  },
  {
    id: 106,
    title: "組建《星際大戰》系列線下觀影小組",
    requester: "星戰迷",
    price: 300,
    category: "社交",
    deadline: "無期限",
    urgent: false,
    rating: 4.5,
    detail:
      "尋找 5-10 名《星際大戰》忠實影迷，計劃每月一次線下觀影和討論活動。",
    createdAt: "2026-01-18",
    views: 187,
    applicants: 8,
  },
]);

// 4. 過濾與搜尋邏輯 - 增強功能
const categories = {
  platform: ["全部", "內容貢獻", "資料庫", "日常", "推廣", "資源"],
  commission: ["全部", "票務", "字幕", "尋片", "解析", "資源", "社交"],
};

const difficultyOptions = ["全部", "簡單", "中等", "高級"];
const durationOptions = ["全部", "1-3 天", "3-7 天", "7-14 天", "14 天以上"];

const filteredList = computed(() => {
  isLoading.value = true;

  let source =
    activeTab.value === "platform" ? platformMissions.value : commissions.value;

  // 模擬 API 載入延遲
  setTimeout(() => {
    isLoading.value = false;
  }, 300);

  return source
    .filter((item) => {
      const matchesSearch = item.title
        .toLowerCase()
        .includes(searchQuery.value.toLowerCase());

      const matchesCategory =
        selectedCategory.value === "全部" ||
        item.category === selectedCategory.value;

      const matchesDifficulty =
        activeTab.value === "platform"
          ? selectedDifficulty.value === "全部" ||
            item.difficulty === selectedDifficulty.value
          : true;

      const matchesDuration =
        selectedDuration.value === "全部" ||
        item.duration === selectedDuration.value;

      return (
        matchesSearch && matchesCategory && matchesDifficulty && matchesDuration
      );
    })
    .sort((a, b) => {
      // 排序邏輯
      if (sortOption.value === "reward") {
        return (b.price || b.reward) - (a.price || a.reward);
      } else if (sortOption.value === "popular") {
        return (
          (b.views || b.participants || 0) - (a.views || a.participants || 0)
        );
      } else if (sortOption.value === "urgent") {
        return b.urgent === a.urgent ? 0 : b.urgent ? -1 : 1;
      } else {
        // 默認排序：特色任務優先，然後按 ID
        if (a.featured !== b.featured) {
          return b.featured ? 1 : -1;
        }
        return b.id - a.id;
      }
    });
});

// 5. 方法 - 增強功能
const openDetail = (task) => {
  selectedTask.value = task;
  isDrawerOpen.value = true;

  // 更新瀏覽量
  if (activeTab.value === "commission" && task.views) {
    task.views += 1;
  }
};

const handleClaim = (title) => {
  alert(`已成功接取：${title}\n請至「個人中心 > 我的任務」查看進度。`);
  isDrawerOpen.value = false;

  // 更新申請人數
  if (selectedTask.value && selectedTask.value.applicants !== undefined) {
    selectedTask.value.applicants += 1;
  }
};

const toggleAdvancedFilters = () => {
  showAdvancedFilters.value = !showAdvancedFilters.value;
};

const getCategoryIcon = (category) => {
  const icons = {
    內容貢獻: "fas fa-edit",
    資料庫: "fas fa-database",
    日常: "fas fa-calendar-check",
    推廣: "fas fa-share-alt",
    資源: "fas fa-cloud-upload-alt",
    票務: "fas fa-ticket-alt",
    字幕: "fas fa-closed-captioning",
    尋片: "fas fa-search",
    解析: "fas fa-book",
    社交: "fas fa-users",
  };
  return icons[category] || "fas fa-film";
};

const getDifficultyColor = (difficulty) => {
  const colors = {
    簡單: "bg-gradient-success",
    中等: "bg-gradient-info",
    高級: "bg-gradient-warning",
  };
  return colors[difficulty] || "bg-gradient-secondary";
};

// 模擬數據載入
onMounted(() => {
  isLoading.value = true;
  setTimeout(() => {
    isLoading.value = false;
  }, 800);
});
</script>

<template>
  <DefaultNavbar transparent />

  <div class="bg-gray-100 min-vh-100 pb-5">
    <header class="position-relative">
      <div
        class="page-header min-height-250 border-radius-xl mt-4 mx-4 shadow-lg"
        style="
          background-image: url(&quot;https://images.unsplash.com/photo-1478720568477-152d9b164e26?auto=format&fit=crop&w=1920&q=80&quot;);
        "
      >
        <span class="mask bg-gradient-dark opacity-7"></span>
        <div class="container text-center z-index-2">
          <h2 class="text-white font-weight-bolder">任務大廳</h2>
          <p class="text-white opacity-8">
            透過貢獻與互助，累積點數經驗值換取影城商品與提升等級!
          </p>
        </div>
      </div>
    </header>

    <div class="container mt-n5">
      <div class="row">
        <div class="col-lg-11 mx-auto">
          <!-- 任務統計信息卡片 -->
          <div class="row mb-4">
            <div class="col-md-12">
              <div class="card shadow-blur blur border-0 mb-4">
                <div class="card-body p-3">
                  <div class="row align-items-center">
                    <div class="col-lg-5">
                      <div class="nav-wrapper position-relative">
                        <ul
                          class="nav nav-pills nav-fill p-1 bg-gray-100 border-radius-lg"
                        >
                          <li class="nav-item">
                            <button
                              @click="
                                activeTab = 'platform';
                                selectedCategory = '全部';
                              "
                              :class="[
                                'nav-link mb-0 px-0 py-2 border-0 w-100 transition-all',
                                activeTab === 'platform'
                                  ? 'active bg-white shadow font-weight-bold text-info'
                                  : 'bg-transparent text-dark',
                              ]"
                            >
                              <i class="fas fa-certificate me-2 text-xs"></i>
                              官方挑戰
                            </button>
                          </li>
                          <li class="nav-item">
                            <button
                              @click="
                                activeTab = 'commission';
                                selectedCategory = '全部';
                              "
                              :class="[
                                'nav-link mb-0 px-0 py-2 border-0 w-100 transition-all',
                                activeTab === 'commission'
                                  ? 'active bg-white shadow font-weight-bold text-info'
                                  : 'bg-transparent text-dark',
                              ]"
                            >
                              <i class="fas fa-users me-2 text-xs"></i>影友委託
                            </button>
                          </li>
                        </ul>
                      </div>
                    </div>
                    <div class="col-lg-7 mt-3 mt-lg-0 d-flex gap-2">
                      <div
                        class="input-group input-group-outline bg-white border-radius-md"
                      >
                        <input
                          v-model="searchQuery"
                          type="text"
                          class="form-control"
                          placeholder="搜尋任務名稱或關鍵字..."
                        />
                      </div>
                      <button
                        v-if="activeTab === 'commission'"
                        class="btn bg-gradient-dark mb-0 px-4 text-nowrap"
                      >
                        <i class="fas fa-plus me-1"></i> 我要發布
                      </button>
                    </div>
                  </div>

                  <!-- 高級過濾器 -->
                  <div class="mt-3">
                    <button
                      @click="toggleAdvancedFilters"
                      class="btn btn-link text-info text-xs p-0 mb-0 d-flex align-items-center"
                    >
                      <i
                        :class="
                          showAdvancedFilters
                            ? 'fas fa-chevron-up'
                            : 'fas fa-chevron-down'
                        "
                        class="me-1"
                      ></i>
                      {{
                        showAdvancedFilters ? "隱藏高級過濾" : "顯示高級過濾"
                      }}
                    </button>

                    <div
                      v-if="showAdvancedFilters"
                      class="mt-3 pt-3 border-top"
                    >
                      <div class="row g-2">
                        <div class="col-md-4">
                          <select
                            v-model="sortOption"
                            class="form-control form-control-sm"
                          >
                            <option value="default">默認排序</option>
                            <option value="reward">按獎勵排序</option>
                            <option value="popular">按熱門排序</option>
                            <option
                              v-if="activeTab === 'commission'"
                              value="urgent"
                            >
                              按緊急程度
                            </option>
                          </select>
                        </div>
                        <div v-if="activeTab === 'platform'" class="col-md-4">
                          <select
                            v-model="selectedDifficulty"
                            class="form-control form-control-sm"
                          >
                            <option
                              v-for="diff in difficultyOptions"
                              :key="diff"
                              :value="diff"
                            >
                              {{ diff }}
                            </option>
                          </select>
                        </div>
                        <div class="col-md-4">
                          <select
                            v-model="selectedDuration"
                            class="form-control form-control-sm"
                          >
                            <option
                              v-for="dur in durationOptions"
                              :key="dur"
                              :value="dur"
                            >
                              {{ dur }}
                            </option>
                          </select>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 任務統計信息 -->
          <div class="row mb-4">
            <div class="col-md-12">
              <div class="card shadow-blur blur border-0">
                <div class="card-body p-3">
                  <div class="row">
                    <div class="col-md-4 mb-3 mb-md-0">
                      <div class="d-flex align-items-center">
                        <div
                          class="icon icon-shape bg-gradient-info text-white text-center me-3"
                        >
                          <i class="fas fa-tasks"></i>
                        </div>
                        <div>
                          <h6 class="text-sm mb-0">總任務數</h6>
                          <span class="text-lg font-weight-bolder text-dark">
                            {{
                              activeTab === "platform"
                                ? platformMissions.length
                                : commissions.length
                            }}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div class="col-md-4 mb-3 mb-md-0">
                      <div class="d-flex align-items-center">
                        <div
                          class="icon icon-shape bg-gradient-success text-white text-center me-3"
                        >
                          <i class="fas fa-users"></i>
                        </div>
                        <div>
                          <h6 class="text-sm mb-0">參與人數</h6>
                          <span class="text-lg font-weight-bolder text-dark">
                            {{
                              activeTab === "platform"
                                ? platformMissions.reduce(
                                    (sum, m) => sum + (m.participants || 0),
                                    0,
                                  )
                                : commissions.reduce(
                                    (sum, c) => sum + (c.applicants || 0),
                                    0,
                                  )
                            }}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div class="col-md-4">
                      <div class="d-flex align-items-center">
                        <div
                          class="icon icon-shape bg-gradient-warning text-white text-center me-3"
                        >
                          <i class="fas fa-coins"></i>
                        </div>
                        <div>
                          <h6 class="text-sm mb-0">總獎勵池</h6>
                          <span class="text-lg font-weight-bolder text-dark">
                            {{
                              activeTab === "platform"
                                ? platformMissions.reduce(
                                    (sum, m) => sum + (m.reward || 0),
                                    0,
                                  )
                                : commissions.reduce(
                                    (sum, c) => sum + (c.price || 0),
                                    0,
                                  )
                            }}
                            Pts
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 類別過濾 -->
          <div class="d-flex gap-2 mb-4 overflow-auto pb-2 scrollbar-hidden">
            <button
              v-for="cat in activeTab === 'platform'
                ? categories.platform
                : categories.commission"
              :key="cat"
              @click="selectedCategory = cat"
              :class="[
                'btn btn-xs rounded-pill text-nowrap shadow-none',
                selectedCategory === cat
                  ? 'btn-info'
                  : 'btn-outline-info bg-white',
              ]"
            >
              <i :class="getCategoryIcon(cat)" class="me-1"></i>
              {{ cat }}
            </button>
          </div>

          <!-- 加載狀態 -->
          <div v-if="isLoading" class="text-center my-4">
            <div class="spinner-border text-info" role="status">
              <span class="visually-hidden">載入中...</span>
            </div>
            <p class="text-sm text-secondary mt-2">正在載入任務數據...</p>
          </div>

          <div class="row">
            <template v-if="activeTab === 'platform'">
              <div
                v-for="m in filteredList"
                :key="m.id"
                class="col-md-6 col-lg-3 mb-4"
              >
                <div class="card border-0 shadow-sm h-100 mission-card">
                  <div class="card-body p-3 d-flex flex-column">
                    <div
                      class="d-flex justify-content-between align-items-center mb-2"
                    >
                      <div
                        :class="[
                          'icon icon-shape shadow-sm border-radius-md text-white text-center',
                          m.color,
                        ]"
                      >
                        <i :class="m.icon"></i>
                      </div>
                      <span
                        v-if="m.featured"
                        class="badge badge-xxs bg-gradient-warning"
                      >
                        <i class="fas fa-star me-1"></i>特色
                      </span>
                    </div>
                    <h6 class="font-weight-bolder text-dark mb-1">
                      {{ m.title }}
                    </h6>
                    <div class="d-flex gap-2 mb-2">
                      <span class="text-xxs font-weight-bold text-info">
                        <i
                          :class="getCategoryIcon(m.category)"
                          class="me-1"
                        ></i>
                        {{ m.category }}
                      </span>
                      <span
                        :class="[
                          'badge badge-xxs',
                          getDifficultyColor(m.difficulty),
                        ]"
                      >
                        {{ m.difficulty }}
                      </span>
                      <span class="text-xxs text-secondary">
                        <i class="fas fa-clock me-1"></i>{{ m.duration }}
                      </span>
                    </div>
                    <p class="text-xs text-secondary mb-3 flex-grow-1">
                      {{ m.desc }}
                    </p>
                    <div
                      class="d-flex justify-content-between align-items-center pt-3 border-top"
                    >
                      <div>
                        <span class="text-sm font-weight-bolder text-dark">
                          +{{ m.reward }} Pts
                        </span>
                        <span class="text-xs text-secondary ms-2">
                          <i class="fas fa-users me-1"></i
                          >{{ m.participants }} 人參與
                        </span>
                      </div>
                      <button
                        :disabled="m.status === 'completed'"
                        @click="handleClaim(m.title)"
                        :class="[
                          'btn btn-xs mb-0',
                          m.status === 'completed'
                            ? 'btn-outline-secondary'
                            : 'btn-info',
                        ]"
                      >
                        {{ m.status === "completed" ? "已達成" : "領取挑戰" }}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </template>

            <template v-else>
              <div class="col-12">
                <div class="card border-0 shadow-sm">
                  <div class="table-responsive">
                    <table class="table align-items-center mb-0">
                      <thead class="bg-gray-100">
                        <tr>
                          <th
                            class="text-uppercase text-secondary text-xxs font-weight-bolder opacity-7 ps-4"
                          >
                            委託標題與發布者
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            類別
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            賞金
                          </th>
                          <th
                            class="text-end pe-4 text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            動作
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr
                          v-for="c in filteredList"
                          :key="c.id"
                          class="commission-row"
                        >
                          <td class="ps-4">
                            <div
                              class="d-flex align-items-center"
                              @click="openDetail(c)"
                            >
                              <div
                                class="avatar avatar-sm bg-gradient-dark me-3 border-radius-md font-weight-bold"
                              >
                                {{ c.requester.substring(0, 1) }}
                              </div>
                              <div class="cursor-pointer">
                                <h6 class="mb-0 text-sm font-weight-bold">
                                  {{ c.title }}
                                </h6>
                                <span class="text-xxs text-secondary">
                                  <i class="fas fa-star text-warning me-1"></i>
                                  {{ c.rating }} · {{ c.requester }} ·
                                  {{ c.deadline }}
                                </span>
                              </div>
                              <span
                                v-if="c.urgent"
                                class="badge badge-xxs bg-gradient-danger ms-2"
                                >急</span
                              >
                            </div>
                          </td>
                          <td class="text-center">
                            <span class="text-xs font-weight-bold">{{
                              c.category
                            }}</span>
                          </td>
                          <td class="text-center">
                            <span class="text-sm font-weight-bolder text-info">
                              {{ c.price }} <small>Pts</small>
                            </span>
                          </td>
                          <td class="text-end pe-4">
                            <button
                              @click="openDetail(c)"
                              class="btn btn-link text-info text-xs mb-0 px-2"
                            >
                              詳情
                            </button>
                            <button
                              @click="handleClaim(c.title)"
                              class="btn btn-sm bg-gradient-info mb-0"
                            >
                              接取
                            </button>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- 任務詳情側邊欄 - 增強版 -->
    <div
      :class="['detail-drawer shadow-xl bg-white', isDrawerOpen ? 'open' : '']"
    >
      <div v-if="selectedTask" class="h-100 d-flex flex-column">
        <div
          class="p-4 border-bottom d-flex justify-content-between align-items-center bg-gray-100"
        >
          <h5 class="mb-0 font-weight-bolder">任務詳情</h5>
          <button
            @click="isDrawerOpen = false"
            class="btn btn-link text-dark p-0 mb-0"
          >
            <i class="fas fa-times text-lg"></i>
          </button>
        </div>

        <div class="p-4 flex-grow-1 overflow-auto">
          <div class="d-flex align-items-center mb-3">
            <span class="badge bg-gradient-info me-2">{{
              selectedTask.category
            }}</span>
            <span class="text-xs text-secondary">
              <i class="fas fa-clock me-1"></i>{{ selectedTask.deadline }}
            </span>
          </div>

          <h4 class="font-weight-bolder mb-3 text-dark">
            {{ selectedTask.title }}
          </h4>

          <!-- 增強的任務信息 -->
          <div v-if="activeTab === 'platform'" class="mb-4">
            <div class="d-flex gap-3 mb-3">
              <div class="text-center">
                <div
                  class="icon icon-shape bg-gradient-info text-white text-center mb-2"
                >
                  <i :class="selectedTask.icon"></i>
                </div>
                <span class="text-xs font-weight-bold">難度</span>
                <div
                  :class="[
                    'badge mt-1',
                    getDifficultyColor(selectedTask.difficulty),
                  ]"
                >
                  {{ selectedTask.difficulty }}
                </div>
              </div>
              <div class="text-center">
                <div
                  class="icon icon-shape bg-gradient-success text-white text-center mb-2"
                >
                  <i class="fas fa-calendar-alt"></i>
                </div>
                <span class="text-xs font-weight-bold">持續時間</span>
                <div class="badge bg-gradient-secondary mt-1">
                  {{ selectedTask.duration }}
                </div>
              </div>
              <div class="text-center">
                <div
                  class="icon icon-shape bg-gradient-warning text-white text-center mb-2"
                >
                  <i class="fas fa-users"></i>
                </div>
                <span class="text-xs font-weight-bold">參與人數</span>
                <div class="badge bg-gradient-secondary mt-1">
                  {{ selectedTask.participants }} 人
                </div>
              </div>
            </div>
          </div>

          <h6
            class="text-uppercase text-xs font-weight-bold text-secondary mb-2 mt-4"
          >
            委託描述
          </h6>
          <div
            class="bg-gray-100 p-3 border-radius-lg text-sm text-dark line-height-base"
          >
            {{ selectedTask.detail || selectedTask.desc }}
          </div>

          <div class="mt-4 pt-2">
            <h6
              class="text-uppercase text-xs font-weight-bold text-secondary mb-3"
            >
              發布者誠信紀錄
            </h6>
            <div class="d-flex align-items-center p-2 border border-radius-lg">
              <div class="avatar bg-gradient-info me-3">
                <i class="fas fa-user"></i>
              </div>
              <div>
                <h6 class="text-sm mb-0">
                  {{ selectedTask.requester || "官方系統" }}
                </h6>
                <span v-if="selectedTask.rating" class="text-xxs text-warning">
                  <i class="fas fa-star me-1"></i>資深影迷 · 好評率 100%
                </span>
                <span v-else class="text-xxs text-info">
                  <i class="fas fa-check-circle me-1"></i>認證官方任務
                </span>
              </div>
            </div>
          </div>
        </div>

        <div class="p-4 border-top bg-white">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <span class="text-sm font-weight-bold text-secondary"
              >預計獲得賞金</span
            >
            <h3 class="mb-0 font-weight-bolder text-info">
              {{ selectedTask.price || selectedTask.reward }}
              <small class="text-sm">Pts</small>
            </h3>
          </div>
          <button
            @click="handleClaim(selectedTask.title)"
            class="btn bg-gradient-info w-100 btn-lg mb-0 shadow-lg"
          >
            立即承接此任務
          </button>
        </div>
      </div>
    </div>
    <div
      v-if="isDrawerOpen"
      @click="isDrawerOpen = false"
      class="drawer-backdrop"
    ></div>
  </div>
  <FooterCentered />
</template>

<style scoped>
.transition-all {
  transition: all 0.25s ease-in-out;
}
.cursor-pointer {
  cursor: pointer;
}

/* 平台挑戰卡片效果 */
.mission-card {
  transition: all 0.3s;
  border-top: 4px solid transparent;
}
.mission-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 15px 30px -5px rgba(0, 0, 0, 0.1) !important;
  border-top-color: #11cdef;
}

/* 委託列表效果 */
.commission-row {
  transition: background-color 0.2s;
}
.commission-row:hover {
  background-color: rgba(17, 205, 239, 0.04);
}

/* 側邊詳情欄位動畫 */
.detail-drawer {
  position: fixed;
  top: 0;
  right: -450px;
  width: 450px;
  height: 100vh;
  z-index: 1100;
  transition: right 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}
.detail-drawer.open {
  right: 0;
}
.drawer-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(2px);
  z-index: 1090;
}

/* 滾動條美化 */
.scrollbar-hidden::-webkit-scrollbar {
  display: none;
}
.scrollbar-hidden {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

.icon-shape {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.badge-xxs {
  font-size: 0.6rem;
  padding: 0.35em 0.65em;
}

.line-height-base {
  line-height: 1.6;
}

@media (max-width: 576px) {
  .detail-drawer {
    width: 100%;
    right: -100%;
  }
}
</style>
