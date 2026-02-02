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
    default: () => ({}),
  },
  tab: {
    type: String,
    required: true, // 'mission' 或 'entrust'
  },
  isClaiming: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:open", "claim"]);

// 關閉抽屜
const close = () => {
  emit("update:open", false);
};

// 領取/接取任務 - 統一傳送完整 payload 給父層處理
const handleClaim = () => {
  if (!props.task?.id) return;

  const payload = {
    type: props.tab === "mission" ? "platform" : "entrust",
    [props.tab === "mission" ? "mission" : "commission"]: props.task,
  };

  emit("claim", payload);
  // 不直接 close，讓父層決定（成功後再關閉，避免失敗還關）
};

// 動態獎勵文字
const rewardText = computed(() => {
  if (!props.task) return "0 Pts";
  const points =
    props.tab === "mission"
      ? props.task.reward || props.task.rewardPoints || 0
      : props.task.price || props.task.rewardPoints || 0;
  return props.tab === "mission" ? `+${points} Pts` : `${points} Pts`;
});

// 按鈕文字與狀態
const buttonText = computed(() => {
  if (props.task?.completed) return "已完成";
  if (props.task?.claimed) return props.tab === "mission" ? "已領取" : "已接取";
  return props.tab === "mission" ? "立即領取" : "立即接取";
});

const isClaimable = computed(() => {
  return !props.task?.claimed && !props.task?.completed;
});
</script>

<template>
  <div class="detail-drawer shadow-xl bg-white" :class="{ open: props.open }">
    <div v-if="props.task?.id" class="h-100 d-flex flex-column">
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
            {{ props.task.category || "其他" }}
          </span>
          <span class="text-xs text-secondary">
            <i class="fas fa-clock me-1"></i>
            {{
              props.task.deadline ||
              props.task.duration ||
              props.task.resetType ||
              "無期限"
            }}
          </span>
          <span v-if="props.task.urgent" class="badge bg-gradient-danger">
            急
          </span>
        </div>

        <!-- 標題 -->
        <h4 class="font-weight-bolder mb-4 text-dark">
          {{ props.task.title || props.task.missionName || "未命名任務" }}
        </h4>

        <!-- 平台任務專屬資訊 -->
        <div v-if="props.tab === 'mission'" class="mb-5">
          <div class="row g-3 text-center">
            <div class="col-4">
              <div
                class="icon icon-shape bg-gradient-info text-white mb-2 mx-auto"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-tasks"></i>
              </div>
              <div class="text-xs font-weight-bold">難度</div>
              <div
                :class="[
                  'badge mt-1 px-3',
                  store.getDifficultyColor(props.task.difficulty),
                ]"
              >
                {{ props.task.difficulty || "中等" }}
              </div>
            </div>
            <div class="col-4">
              <div
                class="icon icon-shape bg-gradient-success text-white mb-2 mx-auto"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-calendar-alt"></i>
              </div>
              <div class="text-xs font-weight-bold">持續時間</div>
              <div class="badge bg-gradient-secondary mt-1 px-3">
                {{ props.task.duration || props.task.resetType || "每日" }}
              </div>
            </div>
            <div class="col-4">
              <div
                class="icon icon-shape bg-gradient-warning text-white mb-2 mx-auto"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-users"></i>
              </div>
              <div class="text-xs font-weight-bold">參與人數</div>
              <div class="badge bg-gradient-secondary mt-1 px-3">
                {{ props.task.participants || 0 }} 人
              </div>
            </div>
          </div>
        </div>

        <!-- 任務描述 -->
        <h6 class="text-uppercase text-xs font-weight-bold text-secondary mb-2">
          任務描述
        </h6>
        <div
          class="bg-gray-100 p-3 border-radius-lg text-sm text-dark mb-4"
          style="white-space: pre-wrap; word-break: break-word"
        >
          {{
            props.task.desc ||
            props.task.detail ||
            props.task.missionDetail ||
            "無詳細說明"
          }}
        </div>

        <!-- 發布者資訊 -->
        <h6 class="text-uppercase text-xs font-weight-bold text-secondary mb-3">
          發布者資訊
        </h6>
        <div class="d-flex align-items-center p-3 border border-radius-lg">
          <div
            class="avatar avatar-sm bg-gradient-dark me-3 border-radius-md font-weight-bold"
          >
            {{ (props.task.requester || "匿").substring(0, 1) }}
          </div>
          <div>
            <h6 class="text-sm mb-0 font-weight-bold">
              {{
                props.task.requester || props.task.requesterName || "匿名影友"
              }}
            </h6>
            <span class="text-xxs text-secondary d-block mt-1">
              <i class="fas fa-calendar-alt me-1"></i>
              發布於 {{ props.task.createdAt || "未知時間" }}
            </span>
          </div>
        </div>
      </div>

      <!-- 底部：賞金 + 按鈕 -->
      <div class="p-4 border-top bg-white">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <span class="text-sm font-weight-bold text-secondary">
            預計獲得賞金
          </span>
          <h3 class="mb-0 font-weight-bolder text-info">
            {{ rewardText }}
            <small class="text-sm">Pts</small>
          </h3>
        </div>

        <button
          :disabled="!isClaimable || props.isClaiming"
          :class="[
            'btn w-100 btn-lg mb-0 shadow-lg',
            isClaimable && !props.isClaiming
              ? 'bg-gradient-info'
              : 'btn-outline-secondary',
          ]"
          @click="handleClaim"
        >
          <span v-if="props.isClaiming">
            <i class="fas fa-spinner fa-spin me-2"></i>處理中...
          </span>
          <span v-else>{{ buttonText }}</span>
        </button>
      </div>
    </div>

    <!-- 無任務佔位 -->
    <div
      v-else
      class="p-5 text-center text-muted h-100 d-flex flex-column justify-content-center"
    >
      <i class="fas fa-info-circle fa-3x mb-3 text-info"></i>
      <p class="mb-0">請從任務列表選擇一項任務查看詳情</p>
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
  box-shadow: -10px 0 30px rgba(0, 0, 0, 0.15);
}

.detail-drawer.open {
  right: 0;
}

@media (max-width: 576px) {
  .detail-drawer {
    width: 100%;
    right: -100%;
  }
}
</style>
