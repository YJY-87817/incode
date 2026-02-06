<script setup>
import { ref, reactive, computed, onMounted } from "vue";
import { useMissionStore } from "@/stores/point/UseMissionStore";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import FooterCentered from "@/examples/footers/FooterCentered.vue";

// Store
const missionStore = useMissionStore();

// 頁面狀態
const activeTab = ref("ongoing"); // ongoing / pending / completed
const isDetailOpen = ref(false);
const selectedMission = ref(null);

// 統計區（示範用）
const stats = ref({
  totalEarned: 2840,
  pendingReview: 450,
  rank: "資深放映師",
  avatar:
    "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=100&q=80",
});

// 過濾任務（根據 activeTab）
const filteredMissions = computed(() => {
  return missionStore.myMissions.filter((mission) => {
    if (activeTab.value === "ongoing") return mission.status === "ongoing";
    if (activeTab.value === "pending")
      return mission.status === "pending" || mission.status === "completed";
    if (activeTab.value === "completed")
      return mission.status === "rewarded" || mission.status === "completed";
    return true;
  });
});

// 開啟詳情抽屜
const viewDetail = (mission) => {
  selectedMission.value = mission;
  isDetailOpen.value = true;
};

/// 改用函數處理，接收 mission 作為參數
const calculateWidth = (current, target) => {
  if (!target) return 0;
  const percentage = Math.floor((current / target) * 100);
  return Math.min(100, percentage);
};

// 領取獎勵
const claimReward = async (mission) => {
  if (!confirm(`確定領取「${mission.missionName}」的獎勵？`)) return;

  try {
    const updatedMission = await missionStore.claimReward(mission.missionId);

    if (
      updatedMission &&
      selectedMission.value?.missionId === mission.missionId
    ) {
      selectedMission.value = { ...updatedMission };
    }

    alert("獎勵領取成功！");
  } catch (err) {
    alert(err.message || "領取失敗，請稍後再試");
  }
};

// 初始載入任務
onMounted(() => {
  missionStore.fetchMyMissions();
});
</script>

<template>
  <DefaultNavbar light />

  <div class="bg-gray-100 min-vh-100 pb-5">
    <!-- 主要內容 -->
    <div class="container mt-5">
      <div class="row">
        <!-- 側邊欄 -->
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
                  >
                    {{
                      missionStore.myMissions.filter(
                        (m) => m.status === "ongoing",
                      ).length
                    }}
                  </span>
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
                  <i class="fas fa-history me-2"></i> 待審核 / 已達標
                  <span
                    class="badge badge-sm bg-light text-dark ms-auto float-end"
                  >
                    {{
                      missionStore.myMissions.filter(
                        (m) =>
                          m.status === "completed" || m.status === "pending",
                      ).length
                    }}
                  </span>
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
                  <span
                    class="badge badge-sm bg-light text-dark ms-auto float-end"
                  >
                    {{
                      missionStore.myMissions.filter(
                        (m) => m.status === "rewarded",
                      ).length
                    }}
                  </span>
                </button>
              </div>

              <hr class="horizontal dark my-3" />

              <router-link
                to="/task"
                class="btn btn-outline-info w-100 btn-sm mb-0"
              >
                返回任務大廳
              </router-link>
            </div>
          </div>
        </div>

        <!-- 任務列表 -->
        <div class="col-lg-9">
          <div v-if="missionStore.loading" class="loading text-center py-7">
            <div class="spinner"></div>
            <p>載入中...</p>
          </div>

          <div v-else-if="missionStore.error" class="error text-center py-7">
            <p class="text-danger">{{ missionStore.error }}</p>
            <button
              @click="missionStore.fetchMyMissions"
              class="btn btn-primary mt-3"
            >
              重新載入
            </button>
          </div>

          <div
            v-else-if="filteredMissions.length === 0"
            class="card border-0 shadow-sm text-center py-7"
          >
            <div class="card-body">
              <i class="fas fa-tasks text-secondary opacity-3 fa-3x mb-3"></i>
              <h6 class="text-secondary">
                目前沒有{{
                  activeTab === "ongoing"
                    ? "執行中的"
                    : activeTab === "pending"
                      ? "待審核 / 已達標"
                      : "已完結"
                }}任務
              </h6>
              <router-link to="/task" class="btn btn-sm bg-gradient-dark mt-3">
                去接新任務
              </router-link>
            </div>
          </div>

          <div v-else class="mission-list">
            <div
              v-for="mission in filteredMissions"
              :key="mission.missionId"
              class="card border-0 shadow-sm mb-3 task-card overflow-hidden"
              :class="{
                completed: mission.status === 'completed',
                rewarded: mission.status === 'rewarded',
              }"
            >
              <div class="card-body p-3">
                <div class="row align-items-center">
                  <div class="col-md-8">
                    <div class="d-flex align-items-center mb-1">
                      <span
                        class="badge badge-xxs me-2"
                        :class="
                          mission.category === '週常'
                            ? 'bg-gradient-primary'
                            : 'bg-gradient-dark'
                        "
                      >
                        {{ mission.category || "任務" }}
                      </span>
                      <h6 class="mb-0 text-sm font-weight-bolder">
                        {{ mission.missionName }}
                      </h6>
                    </div>
                    <p class="text-xs text-secondary mb-3">
                      {{ mission.missionDetail }}
                    </p>

                    <div class="progress">
                      <div class="progress-bar">
                        <div
                          class="progress-fill"
                          :style="{
                            width:
                              calculateWidth(
                                mission.currentCount,
                                mission.targetCount,
                              ) + '%',
                          }"
                        ></div>
                      </div>
                      <span class="progress-text">
                        {{ mission.currentCount }} / {{ mission.targetCount }}
                      </span>
                    </div>
                  </div>

                  <div class="col-md-4 text-end">
                    <div class="mb-3">
                      <span class="text-sm font-weight-bolder text-dark">
                        +{{ mission.rewardPoints }} Pts
                      </span>
                      <small class="text-xs text-secondary ms-1">
                        +{{ mission.rewardExp }} Exp
                      </small>
                    </div>

                    <button
                      @click="viewDetail(mission)"
                      class="btn btn-xs btn-outline-info mb-0 me-2"
                    >
                      歷程
                    </button>

                    <button
                      v-if="mission.canClaimReward"
                      @click="claimReward(mission)"
                      class="btn btn-xs bg-gradient-info mb-0"
                      :disabled="missionStore.loading"
                    >
                      領取獎勵
                    </button>

                    <button
                      v-else-if="mission.status === 'completed'"
                      class="btn btn-xs btn-white mb-0"
                      disabled
                    >
                      已達標
                    </button>

                    <button
                      v-else-if="mission.status === 'rewarded'"
                      class="btn btn-xs btn-outline-success mb-0"
                      disabled
                    >
                      已領取
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 右側抽屜（任務日誌） -->
    <div
      :class="['detail-drawer bg-white shadow-xl', isDetailOpen ? 'open' : '']"
    >
      <div v-if="selectedMission" class="p-4">
        <div class="d-flex justify-content-between align-items-center mb-5">
          <h5 class="font-weight-bolder mb-0">
            任務日誌 - {{ selectedMission.missionName }}
          </h5>
          <i
            @click="isDetailOpen = false"
            class="fas fa-times cursor-pointer text-secondary"
          ></i>
        </div>

        <!-- 日誌（示範用） -->
        <div class="timeline timeline-one-side">
          <div class="timeline-block mb-3">
            <span class="timeline-step bg-info text-white">
              <i class="fas fa-check text-xxs"></i>
            </span>
            <div class="timeline-content">
              <h6 class="text-dark text-sm font-weight-bold mb-0">
                任務已領取
              </h6>
              <p class="text-secondary text-xs mt-1">
                {{ selectedMission.acceptedAt }}
              </p>
            </div>
          </div>

          <div class="timeline-block mb-3">
            <span class="timeline-step bg-success text-white">
              <i class="fas fa-check text-xxs"></i>
            </span>
            <div class="timeline-content">
              <h6 class="text-dark text-sm font-weight-bold mb-0">進度達標</h6>
              <p class="text-secondary text-xs mt-1">
                {{ selectedMission.completedAt }}
              </p>
            </div>
          </div>

          <div v-if="selectedMission.rewardedAt" class="timeline-block mb-3">
            <span class="timeline-step bg-warning text-white">
              <i class="fas fa-gift text-xxs"></i>
            </span>
            <div class="timeline-content">
              <h6 class="text-dark text-sm font-weight-bold mb-0">
                獎勵領取成功
              </h6>
              <p class="text-secondary text-xs mt-1">
                {{ selectedMission.rewardedAt }}
              </p>
            </div>
          </div>
        </div>

        <!-- 協助區 -->
        <div class="mt-6 border-top pt-4">
          <h6 class="text-sm font-weight-bold">需要協助？</h6>
          <p class="text-xs text-secondary">
            若對任務進度有疑問，請聯繫管理員或提交爭議。
          </p>
          <button class="btn btn-link text-info px-0 font-weight-bold">
            聯繫管理員
          </button>
        </div>
      </div>
    </div>

    <!-- 抽屜背景遮罩 -->
    <div
      v-if="isDetailOpen"
      @click="isDetailOpen = false"
      class="drawer-backdrop"
    ></div>
  </div>

  <FooterCentered />
</template>

<style scoped>
/* 佈局修正 */
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

/* 側邊欄吸附 */
.sticky-sidebar {
  position: sticky;
  top: 100px;
  z-index: 1000;
}

/* 卡片動畫 */
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
  overflow-y: auto;
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
</style>
