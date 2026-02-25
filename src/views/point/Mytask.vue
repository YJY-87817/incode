<script setup>
import { ref, computed, onMounted } from "vue";
import { useMissionStore } from "@/stores/point/UseMissionStore";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import FooterCentered from "@/examples/footers/FooterCentered.vue";
import Swal from "sweetalert2";

// Store
const missionStore = useMissionStore();

// 頁面狀態
const activeTab = ref("ongoing");
const isDetailOpen = ref(false);
const selectedMission = ref(null);

// 過濾任務：整合狀態與 canClaimReward 判斷
const filteredMissions = computed(() => {
  return missionStore.myMissions.filter((m) => {
    const isEntrust = !!m.entrustMissionId;

    if (activeTab.value === "ongoing") {
      // 平台任務：進行中
      // 委託任務：徵求解答中 (status === "open")
      if (isEntrust) {
        return m.status === "open";
      }
      return m.status === "ongoing";
    }

    if (activeTab.value === "pending") {
      // 個人委託看 status，系統任務需額外檢查 canClaimReward
      // 平台任務：待領取
      // 委託任務：因為自動發放，所以不進入待領取
      if (isEntrust) return false;
      return m.status === "completed" && m.canClaimReward === true;
    }

    if (activeTab.value === "completed") {
      // 已領取，或是處於冷卻期（completed 但不能領）
      // 平台任務：已領取或冷卻期
      // 委託任務：已結案 (status === "completed")
      if (isEntrust) {
        return m.status === "completed";
      }
      return (
        m.status === "rewarded" ||
        (m.status === "completed" && !m.canClaimReward)
      );
    }

    return false;
  });
});

// 開啟詳情抽屜
const viewDetail = (mission) => {
  selectedMission.value = mission;
  isDetailOpen.value = true;
};

// 計算進度條寬度
const calculateWidth = (mission) => {
  if (mission.entrustMissionId) {
    // 委託任務：徵求中 20%，已結案 100%
    return mission.hasBestAnswer ? 100 : 20;
  }
  // 平台任務維持原邏輯
  if (!mission.targetCount || mission.targetCount <= 0) return 0;
  return Math.min(
    100,
    Math.floor((mission.currentCount / mission.targetCount) * 100),
  );
};

// 領取獎勵 只處理平台任務，委託任務已自動發放
const claimReward = async (mission) => {
  // 如果是委託任務，直接提示已自動發放
  if (mission.entrustMissionId) {
    Swal.fire({
      icon: "info",
      title: "獎勵已自動發放",
      text: "您獲得的最佳解答獎勵已直接入帳，無需手動領取。",
      confirmButtonColor: "#145369",
    });
    return;
  }

  // 平台任務原有領取邏輯
  const missionName = mission.missionName || mission.title || "此任務";
  const progressId =
    mission.missionProgressId || mission.entrustMissionProgressId;

  if (!progressId) {
    Swal.fire({
      icon: "error",
      title: "無法領取",
      text: "系統無法取得此任務的進度資訊，請重新整理頁面",
      confirmButtonColor: "#145369",
    });
    return;
  }

  const result = await Swal.fire({
    title: "確定領取獎勵？",
    text: `確定領取「${missionName}」的獎勵？`,
    icon: "question",
    showCancelButton: true,
    confirmButtonColor: "#145369",
    cancelButtonColor: "#86B0BD",
    confirmButtonText: "確定領取",
    cancelButtonText: "取消",
    reverseButtons: true,
    customClass: {
      confirmButton: "text-white",
      cancelButton: "text-white",
    },
  });

  if (!result.isConfirmed) return;

  try {
    await missionStore.claimReward(progressId, false); // 只傳 false，表示平台任務

    await Swal.fire({
      icon: "success",
      title: "領取成功",
      text: "獎勵已發放至您的帳戶。",
      confirmButtonColor: "#145369",
      timer: 2000,
    });

    if (selectedMission.value) isDetailOpen.value = false;
  } catch (err) {
    console.error("領取過程出錯:", err.message);

    // 處理狀態同步問題
    if (err.message.includes("已領取過")) {
      await Swal.fire({
        icon: "info",
        title: "提示",
        text: "偵測到獎勵先前已領取，清單已為您自動更新。",
        confirmButtonColor: "#145369",
      });
      if (selectedMission.value) isDetailOpen.value = false;
    } else {
      Swal.fire({
        icon: "error",
        title: "領取失敗",
        text: err.message || "伺服器繁忙，請稍後再試",
        confirmButtonColor: "#145369",
      });
    }
  }
};

const formatDate = (date) =>
  date ? new Date(date).toLocaleString("zh-TW") : "---";

onMounted(() => {
  missionStore.fetchMyMissions();
});
</script>

<template>
  <DefaultNavbar light />

  <div class="bg-gray-100 min-vh-100 pb-5 custom-theme">
    <div class="container mt-5">
      <div class="row">
        <div class="col-lg-3">
          <div class="card border-0 shadow-lg mb-4 sticky-sidebar">
            <div class="card-body p-3">
              <h6 class="font-weight-bolder mb-3 ps-2 text-dark">任務管理</h6>
              <div class="nav flex-column">
                <button
                  @click="activeTab = 'ongoing'"
                  :class="[
                    'nav-link text-start mb-2 border-0 w-100 transition-all',
                    activeTab === 'ongoing'
                      ? 'active-tab text-white shadow'
                      : 'text-dark bg-transparent',
                  ]"
                >
                  <i class="fas fa-tasks me-2"></i> 執行中
                </button>
                <button
                  @click="activeTab = 'pending'"
                  :class="[
                    'nav-link text-start mb-2 border-0 w-100 transition-all',
                    activeTab === 'pending'
                      ? 'active-tab text-white shadow'
                      : 'text-dark bg-transparent',
                  ]"
                >
                  <i class="fas fa-gift me-2"></i> 待領取
                </button>
                <button
                  @click="activeTab = 'completed'"
                  :class="[
                    'nav-link text-start mb-2 border-0 w-100 transition-all',
                    activeTab === 'completed'
                      ? 'active-tab text-white shadow'
                      : 'text-dark bg-transparent',
                  ]"
                >
                  <i class="fas fa-history me-2"></i> 已完結
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="col-lg-9">
          <div v-if="missionStore.loading" class="text-center py-7">
            <div class="spinner-border theme-text-main"></div>
            <p class="mt-2 text-secondary">載入中...</p>
          </div>

          <div
            v-else-if="filteredMissions.length === 0"
            class="card border-0 shadow-sm text-center py-7"
          >
            <i
              class="fas fa-clipboard-list text-secondary opacity-3 fa-3x mb-3"
            ></i>
            <h6 class="text-secondary">目前沒有相關任務</h6>
          </div>

          <div v-else class="mission-list">
            <div
              v-for="mission in filteredMissions"
              :key="
                mission.missionProgressId || mission.entrustMissionProgressId
              "
              class="card border-0 shadow-sm mb-3 task-card"
              :class="{ 'entrust-border': mission.entrustMissionId }"
            >
              <div class="card-body p-3">
                <div class="row align-items-center">
                  <div class="col-md-8">
                    <div class="d-flex align-items-center mb-1">
                      <span class="badge badge-xxs me-2 badge-entrust">
                        {{
                          mission.entrustMissionId
                            ? "個人委託"
                            : mission.category || "系統任務"
                        }}
                      </span>
                      <h6 class="mb-0 text-sm font-weight-bolder">
                        {{ mission.missionName || mission.title }}
                        <small v-if="mission.isOwner" class="text-muted ms-2"
                          >(我發起)</small
                        >
                        <small
                          v-if="mission.isBestAnswerWinner"
                          class="text-success ms-2"
                          >(我獲得)</small
                        >
                      </h6>
                    </div>
                    <p class="text-xs text-black mb-3 text-truncate">
                      {{ mission.missionDetail || mission.detail }}
                    </p>

                    <div class="progress-wrapper">
                      <div class="progress mb-1" style="height: 6px">
                        <div
                          class="progress-bar theme-bg-main"
                          :style="{ width: calculateWidth(mission) + '%' }"
                        ></div>
                      </div>
                      <div class="d-flex justify-content-between">
                        <span class="text-xxs">
                          <template v-if="mission.entrustMissionId">
                            狀態：<span class="font-weight-bold">
                              {{
                                mission.hasBestAnswer ? "已結案" : "徵求解答中"
                              }}
                            </span>
                          </template>
                          <template v-else>
                            進度：{{ mission.currentCount || 0 }} /
                            {{ mission.targetCount || 0 }}
                          </template>
                        </span>
                        <span class="text-xxs"
                          >{{ calculateWidth(mission) }}%</span
                        >
                      </div>
                    </div>
                  </div>

                  <div class="col-md-4 text-end">
                    <div class="mb-2">
                      <span class="text-sm font-weight-bolder text-dark">
                        +{{ mission.rewardPoints || 0 }} Pts
                      </span>
                    </div>
                    <button
                      @click="viewDetail(mission)"
                      class="btn btn-xs btn-outline-theme mb-0 me-2"
                    >
                      歷程
                    </button>

                    <!-- 平台任務：顯示領取按鈕 -->
                    <button
                      v-if="
                        !mission.entrustMissionId &&
                        mission.status === 'completed' &&
                        mission.canClaimReward
                      "
                      @click="claimReward(mission)"
                      class="btn btn-xs btn-theme mb-0"
                    >
                      領取獎勵
                    </button>

                    <!-- 委託任務：如果是我獲得的，顯示已獲得 -->
                    <span
                      v-else-if="
                        mission.entrustMissionId && mission.isBestAnswerWinner
                      "
                      class="text-success text-xs font-weight-bold"
                    >
                      <i class="fas fa-trophy me-1"></i>已獲得獎勵
                    </span>

                    <!-- 已完成（平台任務） -->
                    <span
                      v-else-if="
                        !mission.entrustMissionId &&
                        mission.status === 'rewarded'
                      "
                      class="text-success text-xs font-weight-bold"
                    >
                      <i class="fas fa-check-circle me-1"></i>已完成
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 詳情抽屜：平台任務維持原樣，委託任務簡化 -->
    <div
      :class="['detail-drawer bg-white shadow-xl', isDetailOpen ? 'open' : '']"
    >
      <div v-if="selectedMission" class="p-4">
        <div class="d-flex justify-content-between align-items-center mb-5">
          <h5 class="font-weight-bolder mb-0">
            {{ selectedMission.entrustMissionId ? "委託詳情" : "任務日誌" }}
          </h5>
          <i
            @click="isDetailOpen = false"
            class="fas fa-times cursor-pointer text-secondary"
          ></i>
        </div>

        <div class="timeline timeline-one-side">
          <!-- 平台任務原時間軸 -->
          <template v-if="!selectedMission.entrustMissionId">
            <div class="timeline-block mb-3">
              <span class="timeline-step theme-bg-main text-white">
                <i class="fas fa-play text-xxs"></i>
              </span>
              <div class="timeline-content">
                <h6 class="text-dark text-sm font-weight-bold mb-0">
                  任務開始
                </h6>
                <p class="text-secondary text-xs mt-1">
                  {{ formatDate(selectedMission.acceptedAt) }}
                </p>
              </div>
            </div>

            <div v-if="selectedMission.submittedAt" class="timeline-block mb-3">
              <span class="timeline-step bg-dark text-white">
                <i class="fas fa-upload text-xxs"></i>
              </span>
              <div class="timeline-content">
                <h6 class="text-dark text-sm font-weight-bold mb-0">
                  提交作品
                </h6>
                <p class="text-secondary text-xs mt-1">
                  {{ formatDate(selectedMission.submittedAt) }}
                </p>
              </div>
            </div>

            <div
              v-if="selectedMission.completedAt || selectedMission.approvedAt"
              class="timeline-block mb-3"
            >
              <span class="timeline-step bg-success text-white">
                <i class="fas fa-check text-xxs"></i>
              </span>
              <div class="timeline-content">
                <h6 class="text-dark text-sm font-weight-bold mb-0">
                  任務達標
                </h6>
                <p class="text-secondary text-xs mt-1">
                  {{
                    formatDate(
                      selectedMission.completedAt || selectedMission.approvedAt,
                    )
                  }}
                </p>
              </div>
            </div>

            <div v-if="selectedMission.rewardedAt" class="timeline-block mb-3">
              <span class="timeline-step bg-warning text-white">
                <i class="fas fa-coins text-xxs"></i>
              </span>
              <div class="timeline-content">
                <h6 class="text-dark text-sm font-weight-bold mb-0">
                  獎勵撥款
                </h6>
                <p class="text-secondary text-xs mt-1">
                  {{ formatDate(selectedMission.rewardedAt) }}
                </p>
              </div>
            </div>
          </template>

          <!-- 委託任務簡化時間軸 -->
          <template v-else>
            <div class="timeline-block mb-3">
              <span class="timeline-step theme-bg-main text-white">
                <i class="fas fa-bullhorn text-xxs"></i>
              </span>
              <div class="timeline-content">
                <h6 class="text-dark text-sm font-weight-bold mb-0">
                  委託發布
                </h6>
                <p class="text-secondary text-xs mt-1">
                  {{ formatDate(selectedMission.createdAt) }}
                </p>
              </div>
            </div>

            <div v-if="selectedMission.selectedAt" class="timeline-block mb-3">
              <span class="timeline-step bg-success text-white">
                <i class="fas fa-trophy text-xxs"></i>
              </span>
              <div class="timeline-content">
                <h6 class="text-dark text-sm font-weight-bold mb-0">
                  最佳解答選定
                </h6>
                <p class="text-secondary text-xs mt-1">
                  {{ formatDate(selectedMission.selectedAt) }}
                </p>
                <small
                  v-if="selectedMission.isBestAnswerWinner"
                  class="text-success"
                >
                  （你獲得 {{ selectedMission.rewardPoints }} Pts）
                </small>
              </div>
            </div>
          </template>
        </div>

        <!-- 領取按鈕：只對平台任務顯示 -->
        <div class="mt-5 pt-4 border-top">
          <button
            v-if="
              !selectedMission.entrustMissionId &&
              selectedMission.status === 'completed' &&
              selectedMission.canClaimReward
            "
            @click="claimReward(selectedMission)"
            class="btn btn-theme w-100"
          >
            立即領取獎勵
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
  <FooterCentered />
</template>

<style scoped>
/* 自定義配色系統 */
.custom-theme {
  --main-color: #86b0bd; /* 主色調 */
  --btn-color: #145369; /* 按鈕與標籤色 */
}

/* 顏色類別輔助 */
.theme-bg-main {
  background-color: var(--main-color) !important;
}
.theme-text-main {
  color: var(--main-color) !important;
}

/* 委託任務標籤自定義 */
.badge-entrust {
  background-color: var(--btn-color) !important;
  color: white !important;
}

/* 按鈕主樣式 */
.btn-theme {
  background-color: var(--btn-color) !important;
  color: white !important;
  transition: all 0.2s ease;
}
.btn-theme:hover {
  background-color: #0d3d4d !important;
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
}

.btn-outline-theme {
  border: 1px solid var(--btn-color) !important;
  color: var(--btn-color) !important;
  background-color: transparent;
}
.btn-outline-theme:hover {
  background-color: var(--btn-color) !important;
  color: white !important;
}

/* 側邊欄 Active 樣式 */
.active-tab {
  background-color: var(--main-color) !important;
}

/* 任務卡片樣式 */
.task-card {
  transition: all 0.25s;
  cursor: pointer;
  border-radius: 12px;
}
.task-card:hover {
  transform: translateX(5px);
  box-shadow: 0 8px 15px -3px rgba(0, 0, 0, 0.1) !important;
}
.entrust-border {
  border-left: 5px solid var(--btn-color) !important;
}

/* 詳情抽屜樣式 */
.detail-drawer {
  position: fixed;
  top: 0;
  right: -400px;
  width: 400px;
  height: 100vh;
  z-index: 1100;
  transition: right 0.4s cubic-bezier(0.165, 0.84, 0.44, 1);
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

/* 側邊欄主體：加深陰影、拉開與背景區隔 */
.sticky-sidebar {
  position: sticky;
  top: 100px;
  z-index: 1000;
  border-radius: 15px !important;
  /* 使用多層陰影：第一層大範圍擴散，第二層強化底部紮實感 */
  box-shadow:
    0 15px 35px -5px rgba(0, 0, 0, 0.18),
    0 5px 15px -8px rgba(0, 0, 0, 0.12) !important;
  border: none !important;
  background-color: #ffffff;
}

/* 壓縮上方空格：強制覆蓋 Bootstrap 預設的 padding */
.sticky-sidebar .card-body {
  padding-top: 12px !important; /* 縮小頂部內距 */
}

/* 側邊欄按鈕基礎樣式 */
.nav-link {
  border-radius: 10px !important;
  padding: 12px 16px !important;
  margin-bottom: 6px !important;
  color: #444 !important;
  transition: all 0.25s ease;
}

/* 側邊欄 Active 狀態：改用您指定的 #86B0BD */
.active-tab {
  background-color: #86b0bd !important;
  color: #ffffff !important;
  font-weight: 600;
  box-shadow: 0 4px 10px rgba(134, 176, 189, 0.4) !important;
}

/* 滑過效果 */
.nav-link:hover:not(.active-tab) {
  background-color: #f0f4f7 !important;
  transform: translateX(5px);
  color: var(--btn-color) !important;
}
</style>
