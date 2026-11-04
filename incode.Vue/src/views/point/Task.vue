<script setup>
import { ref, onMounted, computed } from "vue"; // 引入 computed
import { useTaskStore } from "@/stores/point/taskStore";
import { useRouter } from "vue-router";

// 引入元件
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import FooterCentered from "@/components/layout/FooterCentered.vue";
import TaskSidebar from "@/components/point/TaskSidebar.vue";
import MissionList from "@/components/point/MissionList.vue";
import EntrustMissionTable from "@/components/point/EntrustMissionTable.vue";
import TaskDetailDrawer from "@/components/point/TaskDetailDrawer.vue";
import Swal from "sweetalert2";

const router = useRouter();
const store = useTaskStore();

// 狀態控制
const isDrawerOpen = ref(false);
const selectedTask = ref(null);
const isClaiming = ref(false);

// 安全讀取長度的計算屬性 (防止 undefined.length 報錯)
const missionCount = computed(() => store.missions?.length || 0);
const entrustCount = computed(() => store.entrustMissions?.length || 0);

const openDetail = (task) => {
  selectedTask.value = task;
  isDrawerOpen.value = true;
};

const handleClaim = async (payload) => {
  if (!payload?.type) return;
  const task = payload.mission || payload.commission;
  if (!task?.id) return;

  isClaiming.value = true;
  const originalClaimed = task.claimed;
  task.claimed = true;

  try {
    await store.claimTask(payload.type, task.id);
    if (payload.type === "entrust" && task.contentId) {
      router.push(`/forum/${task.contentId}`);
    }
  } catch (err) {
    task.claimed = originalClaimed;
    Swal.fire({
      icon: "error",
      title: "接取失敗",
      text: err.message || "接取失敗，請稍後再試",
    });
  } finally {
    isClaiming.value = false;
  }
};

onMounted(() => {
  // 使用可選鏈防禦，確保 store.missions 存在
  if (
    (store.missions?.length || 0) === 0 &&
    (store.entrustMissions?.length || 0) === 0
  ) {
    store.fetchTasks();
  }
});
</script>

<template>
  <DefaultNavbar light />

  <div class="bg-gray-100 min-vh-100 pb-5">
    <div class="container-fluid mt-4 px-lg-5">
      <div class="row">
        <aside
          class="col-lg-3 mb-4 sticky-lg-top"
          style="top: 20px; height: fit-content"
        >
          <TaskSidebar />
        </aside>

        <main class="col-lg-9">
          <div
            v-if="store.error"
            class="alert alert-danger shadow-sm rounded-4 text-center p-5"
          >
            <i class="fas fa-exclamation-triangle fs-2 mb-3 d-block"></i>
            <h5 class="text-white">{{ store.error }}</h5>
            <button
              @click="store.fetchTasks"
              class="btn btn-white btn-sm mt-3 px-4 rounded-pill"
            >
              重試一次
            </button>
          </div>

          <div v-else class="content-wrapper">
            <div
              class="d-flex justify-content-between align-items-center mb-4 px-2"
            >
              <h4 class="fw-bold text-dark mb-0">
                <i
                  :class="
                    store.activeTab === 'mission'
                      ? 'fas fa-rocket'
                      : 'fas fa-users'
                  "
                  class="me-2 text-primary"
                ></i>
                {{ store.activeTab === "mission" ? "平台挑戰" : "影友委託" }}
                <span
                  class="badge bg-white text-primary ms-2 fw-normal fs-6 shadow-sm border"
                >
                  {{
                    store.activeTab === "mission"
                      ? store.filteredMissions?.length || 0
                      : store.filteredEntrusts?.length || 0
                  }}
                  筆結果
                </span>
              </h4>
            </div>

            <transition name="fade-slide" mode="out-in">
              <div :key="store.activeTab">
                <div v-if="store.activeTab === 'mission'">
                  <MissionList @open-detail="openDetail" @claim="handleClaim" />
                </div>
                <div v-else>
                  <EntrustMissionTable
                    @open-detail="openDetail"
                    @claim="handleClaim"
                  />
                </div>
              </div>
            </transition>
          </div>
        </main>
      </div>
    </div>

    <TaskDetailDrawer
      v-model:open="isDrawerOpen"
      :task="selectedTask"
      :tab="store.activeTab"
      :is-claiming="isClaiming"
      @claim="handleClaim"
    />
    <div
      v-if="isDrawerOpen"
      @click.self="isDrawerOpen = false"
      class="drawer-backdrop"
    ></div>
  </div>

  <FooterCentered />
</template>

<style scoped>
/* 側邊欄與內容的切換動畫 */
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.3s ease;
}
.fade-slide-enter-from {
  opacity: 0;
  transform: translateX(20px);
}
.fade-slide-leave-to {
  opacity: 0;
  transform: translateX(-20px);
}

.content-wrapper {
  min-height: 70vh;
}

/* 抽屜背景 */
.drawer-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(4px);
  z-index: 1090;
}

/* 針對電腦版側欄的滾動優化 */
@media (min-width: 992px) {
  .sticky-lg-top {
    position: sticky;
    top: 80px; /* 避開可能的 Navbar 高度 */
    z-index: 100;
  }
}
</style>
