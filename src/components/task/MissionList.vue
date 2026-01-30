<!-- src/components/mission-hall/MissionList.vue -->
<script setup>
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();

const emit = defineEmits(["open-detail", "claim"]);
</script>

<template>
  <div
    v-for="m in store.filteredList"
    :key="m.id"
    class="col-md-6 col-lg-4 mb-4"
  >
    <div class="card border-0 shadow-sm h-100 mission-card">
      <div class="card-body p-4 d-flex flex-column">
        <!-- 頭部：icon + 特色徽章 -->
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div
            class="icon icon-shape shadow-sm border-radius-md text-white text-center p-3"
            :class="m.color || 'bg-gradient-info'"
          >
            <i class="fas fa-film"></i>
          </div>
          <span v-if="m.featured" class="badge badge-xxs bg-gradient-warning">
            <i class="fas fa-star me-1"></i>特色
          </span>
        </div>

        <!-- 標題 -->
        <h6 class="font-weight-bolder text-dark mb-2">
          {{ m.title || m.missionName || "未命名任務" }}
        </h6>

        <!-- 標籤：類別 + 難度 + 時長 -->
        <div class="d-flex flex-wrap gap-2 mb-3">
          <span class="text-xxs font-weight-bold text-info">
            <i :class="store.getCategoryIcon(m.category)" class="me-1"></i>
            {{ m.category || "其他" }}
          </span>

          <span
            :class="['badge badge-xxs', store.getDifficultyColor(m.difficulty)]"
          >
            {{ m.difficulty || "中等" }}
          </span>

          <span class="text-xxs text-secondary">
            <i class="fas fa-clock me-1"></i>
            {{ m.duration || m.resetType || m.estimatedDuration || "每日" }}
          </span>
        </div>

        <!-- 描述 -->
        <p class="text-sm text-secondary mb-4 flex-grow-1">
          {{ m.desc || m.missionDetail || "無詳細說明" }}
        </p>

        <!-- 底部：獎勵 + 參與人數 + 領取按鈕 -->
        <div
          class="d-flex justify-content-between align-items-center pt-3 border-top mt-auto"
        >
          <div>
            <span class="text-lg font-weight-bolder text-dark">
              +{{ m.reward || m.rewardPoints || 0 }} Pts
            </span>
            <span class="text-xs text-secondary ms-2">
              <i class="fas fa-users me-1"></i>
              {{ m.participants || m.participantCount || 0 }} 人參與
            </span>
          </div>

          <button
            :disabled="m.status === 'completed'"
            @click="$emit('claim', m.title)"
            :class="[
              'btn btn-sm mb-0',
              m.status === 'completed' ? 'btn-outline-secondary' : 'btn-info',
            ]"
          >
            {{ m.status === "completed" ? "已達成" : "領取挑戰" }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mission-card {
  transition: all 0.3s ease;
  border-top: 4px solid transparent;
}

.mission-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 15px 30px -5px rgba(0, 0, 0, 0.1) !important;
  border-top-color: #11cdef;
}
</style>
