<!-- src/components/mission-hall/TaskDetailDrawer.vue -->
<script setup>
import { computed } from "vue";
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();

const props = defineProps({
  open: {
    type: Boolean,
    required: true,
  },
  task: {
    type: Object,
    default: null,
  },
  tab: {
    type: String,
    required: true, // 'mission' 或 'commission'
  },
});

const emit = defineEmits(["update:open", "claim"]);

// 關閉抽屜
const close = () => {
  emit("update:open", false);
};

// 承接/領取任務
const handleClaim = () => {
  if (props.task?.title) {
    emit("claim", props.task.title);
  }
  close();
};

// 根據 tab 動態顯示獎金欄位
const rewardText = computed(() => {
  if (!props.task) return "0 Pts";
  if (props.tab === "mission") {
    return `+${props.task.reward || props.task.rewardPoints || 0} Pts`;
  }
  return `${props.task.price || props.task.rewardPoints || 0} Pts`;
});
</script>

<template>
  <div :class="['detail-drawer shadow-xl bg-white', open ? 'open' : '']">
    <div v-if="task" class="h-100 d-flex flex-column">
      <!-- 標頭 -->
      <div
        class="p-4 border-bottom d-flex justify-content-between align-items-center bg-gray-100"
      >
        <h5 class="mb-0 font-weight-bolder">任務詳情</h5>
        <button @click="close" class="btn btn-link text-dark p-0 mb-0">
          <i class="fas fa-times text-lg"></i>
        </button>
      </div>

      <!-- 內容主體 -->
      <div class="p-4 flex-grow-1 overflow-auto">
        <!-- 類別與期限 -->
        <div class="d-flex align-items-center mb-3 flex-wrap gap-2">
          <span class="badge bg-gradient-info">
            {{ task.category || "其他" }}
          </span>
          <span class="text-xs text-secondary">
            <i class="fas fa-clock me-1"></i>
            {{ task.deadline || task.duration || task.resetType || "無期限" }}
          </span>
          <span v-if="task.urgent" class="badge bg-gradient-danger"> 急 </span>
        </div>

        <!-- 標題 -->
        <h4 class="font-weight-bolder mb-3 text-dark">
          {{ task.title || task.missionName || "未命名任務" }}
        </h4>

        <!-- 平台任務專屬資訊 -->
        <div v-if="tab === 'mission'" class="mb-4">
          <div class="d-flex flex-wrap gap-4 mb-3">
            <!-- 難度 -->
            <div class="text-center">
              <div
                class="icon icon-shape bg-gradient-info text-white text-center mb-2"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-tasks"></i>
              </div>
              <span class="text-xs font-weight-bold d-block">難度</span>
              <div
                :class="[
                  'badge mt-1',
                  store.getDifficultyColor(task.difficulty),
                ]"
              >
                {{ task.difficulty || "中等" }}
              </div>
            </div>

            <!-- 持續時間 -->
            <div class="text-center">
              <div
                class="icon icon-shape bg-gradient-success text-white text-center mb-2"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-calendar-alt"></i>
              </div>
              <span class="text-xs font-weight-bold d-block">持續時間</span>
              <div class="badge bg-gradient-secondary mt-1">
                {{
                  task.duration ||
                  task.resetType ||
                  task.estimatedDuration ||
                  "每日"
                }}
              </div>
            </div>

            <!-- 參與人數 -->
            <div class="text-center">
              <div
                class="icon icon-shape bg-gradient-warning text-white text-center mb-2"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-users"></i>
              </div>
              <span class="text-xs font-weight-bold d-block">參與人數</span>
              <div class="badge bg-gradient-secondary mt-1">
                {{ task.participants || task.participantCount || 0 }} 人
              </div>
            </div>
          </div>
        </div>

        <!-- 任務描述 -->
        <h6
          class="text-uppercase text-xs font-weight-bold text-secondary mb-2 mt-4"
        >
          任務描述
        </h6>
        <div
          class="bg-gray-100 p-3 border-radius-lg text-sm text-dark line-height-base"
        >
          {{ task.detail || task.desc || task.missionDetail || "無詳細說明" }}
        </div>

        <!-- 發布者資訊 -->
        <div class="mt-4 pt-2">
          <h6
            class="text-uppercase text-xs font-weight-bold text-secondary mb-3"
          >
            發布者資訊
          </h6>
          <div class="d-flex align-items-center p-2 border border-radius-lg">
            <div class="avatar bg-gradient-info me-3">
              <i class="fas fa-user"></i>
            </div>
            <div>
              <h6 class="text-sm mb-0">
                {{ task.requester || task.requesterName || "匿名影友" }}
              </h6>
              <span v-if="task.rating" class="text-xxs text-warning">
                <i class="fas fa-star me-1"></i>資深影迷 · 好評率 100%
              </span>
              <span v-else class="text-xxs text-info">
                <i class="fas fa-check-circle me-1"></i>官方任務 / 委託
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- 底部：賞金 + 承接按鈕 -->
      <div class="p-4 border-top bg-white">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <span class="text-sm font-weight-bold text-secondary">
            預計獲得賞金
          </span>
          <h3 class="mb-0 font-weight-bolder text-info">
            {{ rewardText }}
            <small class="text-sm">Pts</small>
          </h3>
        </div>

        <button
          @click="handleClaim"
          class="btn bg-gradient-info w-100 btn-lg mb-0 shadow-lg"
        >
          立即承接此任務
        </button>
      </div>
    </div>

    <!-- 無任務時的佔位 -->
    <div v-else class="p-5 text-center text-muted">
      <i class="fas fa-info-circle fa-3x mb-3"></i>
      <p>請選擇一項任務查看詳情</p>
    </div>
  </div>
</template>

<style scoped>
.detail-drawer {
  position: fixed;
  top: 0;
  right: -450px;
  width: 450px;
  height: 100vh;
  z-index: 1100;
  transition: right 0.4s cubic-bezier(0.4, 0, 0.2, 1);
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
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(2px);
  z-index: 1090;
}

/* 響應式：手機全螢幕 */
@media (max-width: 576px) {
  .detail-drawer {
    width: 100%;
    right: -100%;
  }
}
</style>
