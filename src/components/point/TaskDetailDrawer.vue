<!-- src/components/mission-hall/TaskDetailDrawer.vue -->
<script setup>
import { computed } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const props = defineProps({
  open: {
    type: Boolean,
    required: true,
  },
  task: {
    type: Object,
    default: () => ({}),
  },
});

const emit = defineEmits(["update:open"]);

// 關閉抽屜
const close = () => {
  emit("update:open", false);
};

// 前往討論區
const goToForum = () => {
  if (!props.task?.contentId) return;
  router.push(`/forum/${props.task.contentId}`);
};

// 顯示賞金
const rewardText = computed(() => {
  if (!props.task) return "0 Pts";
  const points = props.task.price || props.task.rewardPoints || 0;
  return `${points} Pts`;
});

// 按鈕是否可用
const isButtonDisabled = computed(() => {
  return !props.task?.contentId;
});
</script>

<template>
  <div class="detail-drawer shadow-xl bg-white" :class="{ open: props.open }">
    <div v-if="props.task?.id" class="h-100 d-flex flex-column">
      <!-- Header -->
      <div
        class="p-4 border-bottom d-flex justify-content-between align-items-center custom-bg-light"
      >
        <h5 class="mb-0 font-weight-bolder custom-text-main">委託任務詳情</h5>
        <button @click="close" class="btn btn-link text-dark p-0 mb-0">
          <i class="fas fa-times text-lg"></i>
        </button>
      </div>

      <!-- 內容 -->
      <div class="p-4 flex-grow-1 overflow-auto">
        <div class="d-flex align-items-center mb-3 flex-wrap gap-2">
          <span class="badge custom-bg-main text-white">
            {{ props.task.category || "其他" }}
          </span>
          <span class="text-xs text-dark">
            <i class="fas fa-clock me-1"></i>
            {{ props.task.deadline || "無期限" }}
          </span>
          <span v-if="props.task.urgent" class="badge bg-gradient-danger">
            急
          </span>
        </div>

        <h4 class="font-weight-bolder mb-4 text-dark">
          {{ props.task.title || "未命名任務" }}
        </h4>

        <!-- 任務描述 -->
        <h6
          class="text-uppercase text-xs font-weight-bold mb-2 custom-text-main"
        >
          任務描述
        </h6>
        <div
          class="bg-gray-100 p-3 border-radius-lg text-sm text-dark mb-4"
          style="white-space: pre-wrap; word-break: break-word"
        >
          {{ props.task.desc || "無詳細說明" }}
        </div>

        <!-- 發布者 -->
        <h6
          class="text-uppercase text-xs font-weight-bold mb-3 custom-text-main"
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

      <!-- 底部 -->
      <div class="p-4 border-top bg-white">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <span class="text-sm font-weight-bold custom-text-main">
            預計獲得賞金
          </span>
          <h3 class="mb-0 font-weight-bolder custom-text-main">
            {{ rewardText }}
          </h3>
        </div>

        <button
          :disabled="isButtonDisabled"
          class="btn w-100 btn-lg mb-0 shadow-md custom-btn-action transition-all"
          @click="goToForum"
        >
          前往討論區
        </button>
      </div>
    </div>

    <!-- 無任務 -->
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
.custom-text-main {
  color: #145369 !important;
}
.custom-bg-main {
  background-color: #145369 !important;
}
.custom-bg-light {
  background-color: rgba(20, 83, 105, 0.05) !important;
}

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
