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
      <div
        class="p-4 border-bottom d-flex justify-content-between align-items-center custom-bg-light"
      >
        <h5 class="mb-0 font-weight-bolder custom-text-main">任務詳情</h5>
        <button @click="close" class="btn btn-link text-dark p-0 mb-0">
          <i class="fas fa-times text-lg"></i>
        </button>
      </div>

      <div class="p-4 flex-grow-1 overflow-auto">
        <div class="d-flex align-items-center mb-3 flex-wrap gap-2">
          <span class="badge custom-bg-main text-white">
            {{ props.task.category || "其他" }}
          </span>
          <span class="text-xs text-dark">
            <i class="fas fa-clock me-1"></i>
            {{ props.task.deadline || props.task.duration || "無期限" }}
          </span>
          <span v-if="props.task.urgent" class="badge bg-gradient-danger">
            急
          </span>
        </div>

        <h4 class="font-weight-bolder mb-4 text-dark">
          {{ props.task.title || props.task.missionName || "未命名任務" }}
        </h4>

        <div v-if="props.tab === 'mission'" class="mb-5">
          <div class="row g-3 text-center">
            <div class="col-4">
              <div
                class="icon icon-shape custom-bg-main text-white mb-2 mx-auto shadow-sm"
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
                class="icon icon-shape custom-bg-main text-white mb-2 mx-auto shadow-sm"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-calendar-alt"></i>
              </div>
              <div class="text-xs font-weight-bold">持續時間</div>
              <div class="badge bg-gray-200 text-dark mt-1 px-3">
                {{ props.task.duration || "每日" }}
              </div>
            </div>
            <div class="col-4">
              <div
                class="icon icon-shape custom-bg-main text-white mb-2 mx-auto shadow-sm"
                style="width: 48px; height: 48px; line-height: 48px"
              >
                <i class="fas fa-users"></i>
              </div>
              <div class="text-xs font-weight-bold">參與人數</div>
              <div class="badge bg-gray-200 text-dark mt-1 px-3">
                {{ props.task.participants || 0 }} 人
              </div>
            </div>
          </div>
        </div>

        <h6
          class="text-uppercase text-xs font-weight-bold mb-2"
          style="color: #145369"
        >
          任務描述
        </h6>
        <div
          class="bg-gray-100 p-3 border-radius-lg text-sm text-dark mb-4"
          style="white-space: pre-wrap; word-break: break-word"
        >
          {{ props.task.desc || "無詳細說明" }}
        </div>

        <h6
          class="text-uppercase text-xs font-weight-bold mb-3"
          style="color: #145369"
        >
          發布者資訊
        </h6>
        <div
          class="d-flex align-items-center p-3 border border-radius-lg border-light shadow-none"
        >
          <img
            v-if="props.task.imageUrl"
            :src="props.task.imageUrl"
            class="me-3 border-radius-md"
            style="width: 40px; height: 40px; object-fit: cover"
          />
          <div
            v-else
            class="avatar avatar-sm custom-bg-main me-3 border-radius-md font-weight-bold d-flex align-items-center justify-content-center text-white"
            style="width: 40px; height: 40px"
          >
            {{ (props.task.username || "匿").substring(0, 1) }}
          </div>
          <div>
            <h6 class="text-sm mb-0 font-weight-bold">
              {{ props.task.username || "匿名影友" }}
            </h6>
            <span class="text-xxs text-dark d-block mt-1">
              <i class="fas fa-calendar-alt me-1"></i>
              發布於
              {{
                props.task.createdAt
                  ? new Date(props.task.createdAt).toLocaleString()
                  : "未知時間"
              }}
            </span>
          </div>
        </div>
      </div>

      <div class="p-4 border-top bg-white">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <span class="text-sm font-weight-bold" style="color: #145369"
            >預計獲得賞金</span
          >
          <h3 class="mb-0 font-weight-bolder custom-text-main">
            {{ rewardText }}
          </h3>
        </div>

        <button
          :disabled="!isClaimable || props.isClaiming"
          :class="[
            'btn w-100 btn-lg mb-0 shadow-md transition-all',
            isClaimable && !props.isClaiming
              ? 'custom-btn-action'
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

    <div
      v-else
      class="p-5 text-center text-muted h-100 d-flex flex-column justify-content-center"
    >
      <i class="fas fa-info-circle fa-3x mb-3 custom-text-main opacity-5"></i>
      <p class="mb-0">請從任務列表選擇一項任務查看詳情</p>
    </div>
  </div>
</template>

<style scoped>
/* 引用品牌色定義 */
.custom-text-main {
  color: #145369 !important;
}
.custom-bg-main {
  background-color: #145369 !important; /* 強調用深色 */
}
.custom-bg-light {
  background-color: rgba(20, 83, 105, 0.05) !important; /* 標頭淡淡的底色 */
}

/* 底部主要按鈕樣式 */
.custom-btn-action {
  background-color: #145369 !important;
  color: white !important;
  border: none;
}
.custom-btn-action:hover {
  background-color: #0d3b4b !important;
  transform: translateY(-2px);
}

.detail-drawer {
  position: fixed;
  top: 0;
  right: -450px;
  width: 450px;
  height: 100vh;
  z-index: 1100;
  transition: right 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  overflow-y: auto;
  box-shadow: -10px 0 30px rgba(0, 0, 0, 0.1);
}

.detail-drawer.open {
  right: 0;
}

.transition-all {
  transition: all 0.3s ease;
}

@media (max-width: 576px) {
  .detail-drawer {
    width: 100%;
    right: -100%;
  }
}
</style>
