<script setup>
import { ref, computed } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";

// 1. 頁面狀態控制
const activeTab = ref("ongoing"); // ongoing, pending, completed
const stats = ref({
  totalEarned: 2840,
  pendingReview: 450,
  rank: "資深放映師",
  avatar:
    "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=100&q=80",
});

// 2. 我的任務資料庫
const myTasks = ref([
  {
    id: 1,
    title: "《奧本海默》4K 字幕校對",
    type: "影友委託",
    reward: 800,
    status: "ongoing",
    partner: "字幕組老陳",
    timeLeft: "2 天 14 小時",
    desc: "需針對專業術語進行校對，目前已完成 30%。",
    logs: ["接取任務成功", "已提交樣本"],
  },
  {
    id: 2,
    title: "新片影評挑戰：沙丘 2",
    type: "官方挑戰",
    reward: 150,
    status: "pending",
    partner: "管理員",
    submitDate: "2026-01-12",
    desc: "影評審核中，字數檢測已通過。",
    logs: ["提交影評連結"],
  },
  {
    id: 3,
    title: "尋找 90 年代法國電影海報",
    type: "影友委託",
    reward: 1200,
    status: "completed",
    partner: "收藏家小張",
    finishDate: "2026-01-05",
    desc: "成功找到實體海報並完成寄送。",
    logs: ["任務圓滿結束"],
  },
]);

// 3. 過濾邏輯
const filteredTasks = computed(() => {
  return myTasks.value.filter((task) => task.status === activeTab.value);
});

// 4. 側邊欄詳情
const isDetailOpen = ref(false);
const selectedTask = ref(null);

const viewDetail = (task) => {
  selectedTask.value = task;
  isDetailOpen.value = true;
};
</script>

<template>
  <div class="fixed-top w-100 z-index-3">
    <DefaultNavbar transparent />
  </div>

  <div class="bg-gray-100 min-vh-100 pb-5">
    <header class="bg-gradient-dark pt-10 pb-9 shadow-lg">
      <div class="container mt-2">
        <div class="row align-items-center">
          <div class="col-lg-6">
            <div class="d-flex align-items-center">
              <div
                class="avatar avatar-xl bg-white border-radius-lg me-3 p-1 shadow"
              >
                <img :src="stats.avatar" class="border-radius-lg shadow-sm" />
              </div>
              <div>
                <h3 class="text-white font-weight-bolder mb-0">影迷管理中心</h3>
                <p class="text-white opacity-8 mb-0">
                  追蹤您的電影委託進度與積分報酬
                </p>
              </div>
            </div>
          </div>
          <div class="col-lg-6 text-lg-end mt-4 mt-lg-0">
            <div class="d-inline-block text-center me-4">
              <h3 class="text-white font-weight-bolder mb-0">
                {{ stats.totalEarned }}
              </h3>
              <p class="text-white text-xs opacity-7 mb-0">累計積分 Pts</p>
            </div>
            <div class="d-inline-block text-center">
              <h3 class="text-warning font-weight-bolder mb-0">
                {{ stats.pendingReview }}
              </h3>
              <p class="text-white text-xs opacity-7 mb-0">待入帳積分</p>
            </div>
          </div>
        </div>
      </div>
    </header>

    <div class="container mt-n6">
      <div class="row">
        <div class="col-lg-3">
          <div class="card border-0 shadow-lg mb-4 sticky-sidebar">
            <div class="card-body p-3">
              <h6 class="font-weight-bolder mb-3 ps-2 text-dark">我的進度</h6>
              <div class="nav flex-column">
                <button
                  @click="activeTab = 'ongoing'"
                  :class="[
                    'nav-link text-start mb-2 border-0 w-100 transition-all',
                    activeTab === 'ongoing'
                      ? 'bg-gradient-info text-white shadow'
                      : 'text-dark',
                  ]"
                >
                  <i class="fas fa-play-circle me-2"></i> 正在執行
                  <span
                    class="badge badge-sm bg-light text-dark ms-auto float-end"
                    >1</span
                  >
                </button>
                <button
                  @click="activeTab = 'pending'"
                  :class="[
                    'nav-link text-start mb-2 border-0 w-100 transition-all',
                    activeTab === 'pending'
                      ? 'bg-gradient-info text-white shadow'
                      : 'text-dark',
                  ]"
                >
                  <i class="fas fa-history me-2"></i> 等待審核
                </button>
                <button
                  @click="activeTab = 'completed'"
                  :class="[
                    'nav-link text-start mb-2 border-0 w-100 transition-all',
                    activeTab === 'completed'
                      ? 'bg-gradient-info text-white shadow'
                      : 'text-dark',
                  ]"
                >
                  <i class="fas fa-check-circle me-2"></i> 已完結紀錄
                </button>
              </div>
              <hr class="horizontal dark my-3" />
              <button class="btn btn-outline-info w-100 btn-sm mb-0">
                返回任務大廳
              </button>
            </div>
          </div>
        </div>

        <div class="col-lg-9">
          <div v-if="filteredTasks.length > 0">
            <div
              v-for="task in filteredTasks"
              :key="task.id"
              class="card border-0 shadow-sm mb-3 task-card overflow-hidden"
            >
              <div class="card-body p-3">
                <div class="row align-items-center">
                  <div class="col-md-8">
                    <div class="d-flex align-items-center mb-1">
                      <span
                        :class="[
                          'badge badge-xxs me-2',
                          task.type === '官方挑戰'
                            ? 'bg-gradient-primary'
                            : 'bg-gradient-dark',
                        ]"
                        >{{ task.type }}</span
                      >
                      <h6 class="mb-0 text-sm font-weight-bolder">
                        {{ task.title }}
                      </h6>
                    </div>
                    <p class="text-xs text-secondary mb-3">{{ task.desc }}</p>

                    <div
                      v-if="task.status === 'ongoing'"
                      class="d-flex align-items-center"
                    >
                      <i class="fas fa-user-circle text-xs me-1"></i>
                      <span class="text-xxs text-secondary"
                        >對接影友：{{ task.partner }}</span
                      >
                      <span class="mx-2 text-xxs text-secondary opacity-5"
                        >|</span
                      >
                      <i class="fas fa-clock text-xs me-1 text-danger"></i>
                      <span class="text-xxs text-danger font-weight-bold"
                        >剩餘 {{ task.timeLeft }}</span
                      >
                    </div>
                  </div>
                  <div class="col-md-4 text-end">
                    <div class="mb-3">
                      <span class="text-sm font-weight-bolder text-dark"
                        >{{ task.reward }} Pts</span
                      >
                    </div>
                    <button
                      @click="viewDetail(task)"
                      class="btn btn-xs btn-outline-info mb-0 me-2"
                    >
                      歷程
                    </button>
                    <button
                      v-if="task.status === 'ongoing'"
                      class="btn btn-xs bg-gradient-info mb-0"
                    >
                      提交證明
                    </button>
                    <button
                      v-if="task.status === 'pending'"
                      class="btn btn-xs btn-white mb-0"
                      disabled
                    >
                      審核中
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="card border-0 shadow-sm text-center py-7">
            <div class="card-body">
              <i class="fas fa-tasks text-secondary opacity-3 fa-3x mb-3"></i>
              <h6 class="text-secondary">
                目前沒有{{ activeTab === "ongoing" ? "執行中的" : "相關" }}任務
              </h6>
              <button class="btn btn-sm bg-gradient-dark mt-3">
                去接新任務
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div
      :class="['detail-drawer bg-white shadow-xl', isDetailOpen ? 'open' : '']"
    >
      <div v-if="selectedTask" class="p-4">
        <div class="d-flex justify-content-between align-items-center mb-5">
          <h5 class="font-weight-bolder mb-0">任務日誌</h5>
          <i
            @click="isDetailOpen = false"
            class="fas fa-times cursor-pointer text-secondary"
          ></i>
        </div>

        <div class="timeline timeline-one-side">
          <div
            v-for="(log, idx) in selectedTask.logs"
            :key="idx"
            class="timeline-block mb-3"
          >
            <span class="timeline-step bg-info text-white">
              <i class="fas fa-check text-xxs"></i>
            </span>
            <div class="timeline-content">
              <h6 class="text-dark text-sm font-weight-bold mb-0">{{ log }}</h6>
              <p class="text-secondary text-xs mt-1">系統自動紀錄</p>
            </div>
          </div>
        </div>

        <div class="mt-6 border-top pt-4">
          <h6 class="text-sm font-weight-bold">需要協助？</h6>
          <p class="text-xs text-secondary">
            若對任務進度有疑問，請直接聯繫夥伴或提交爭議申訴。
          </p>
          <button class="btn btn-link text-info px-0 font-weight-bold">
            聯繫 {{ selectedTask.partner }}
          </button>
        </div>
      </div>
    </div>
    <div
      v-if="isDetailOpen"
      @click="isDetailOpen = false"
      class="drawer-backdrop"
    ></div>
  </div>
</template>

<style scoped>
/* 佈局修正：確保導覽列不遮擋 */
.pt-10 {
  padding-top: 10rem !important;
}
.pb-9 {
  padding-bottom: 9rem !important;
}
.mt-n6 {
  margin-top: -6rem !important;
}
.z-index-3 {
  z-index: 1030 !important;
}

/* 側邊欄吸附：距離頂部 100px (避開 Navbar) */
.sticky-sidebar {
  position: sticky;
  top: 100px;
  z-index: 1000;
}

/* 卡片與動畫 */
.task-card {
  transition: all 0.25s;
  border-left: 0 solid #11cdef;
}
.task-card:hover {
  transform: translateX(5px);
  border-left: 5px solid #11cdef;
  box-shadow: 0 8px 15px -3px rgba(0, 0, 0, 0.1) !important;
}

/* 詳情抽屜 */
.detail-drawer {
  position: fixed;
  top: 0;
  right: -400px;
  width: 400px;
  height: 100vh;
  z-index: 1100;
  transition: right 0.4s ease;
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
  background: rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(2px);
  z-index: 1090;
}

/* 小元件樣式 */
.badge-xxs {
  font-size: 0.65rem;
  padding: 0.4em 0.6em;
}
.btn-xs {
  padding: 0.4rem 0.8rem;
  font-size: 0.7rem;
}
.transition-all {
  transition: all 0.3s ease;
}
.cursor-pointer {
  cursor: pointer;
}

@media (max-width: 991px) {
  .sticky-sidebar {
    position: relative;
    top: 0;
  }
  .detail-drawer {
    width: 100%;
    right: -100%;
  }
}
</style>
