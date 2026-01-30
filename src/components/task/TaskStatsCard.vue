<script setup>
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();

const stats = [
  {
    label: "活躍任務",
    value: store.totalTasks,
    unit: "個",
    icon: "fas fa- rocket", // 這裡可以換成更動感的 icon
    color: "bg-gradient-info",
    shadow: "shadow-info",
  },
  {
    label: "影友參與",
    value: store.totalParticipants,
    unit: "人",
    icon: "fas fa-fire",
    color: "bg-gradient-danger",
    shadow: "shadow-danger",
  },
  {
    label: "總獎勵池",
    value: store.totalRewardPool,
    unit: "Pts",
    icon: "fas fa-gem",
    color: "bg-gradient-warning",
    shadow: "shadow-warning",
  },
];
</script>

<template>
  <div class="row mb-4">
    <div
      v-for="item in stats"
      :key="item.label"
      class="col-lg-4 col-md-6 mb-4 mb-lg-0"
    >
      <div
        class="card stats-card border-0 shadow-sm transition-all overflow-hidden"
      >
        <div class="card-body p-3">
          <div class="row align-items-center">
            <div class="col-8">
              <div class="numbers">
                <p
                  class="text-xs mb-0 text-uppercase font-weight-bold opacity-7 text-secondary"
                >
                  {{ item.label }}
                </p>
                <h4 class="font-weight-bolder mb-0 mt-1">
                  {{ item.value.toLocaleString() }}
                  <span
                    class="text-xs font-weight-normal text-secondary ms-1"
                    >{{ item.unit }}</span
                  >
                </h4>
              </div>
            </div>
            <div class="col-4 text-end">
              <div
                :class="[
                  'icon icon-shape text-center rounded-circle ms-auto',
                  item.color,
                  item.shadow,
                ]"
              >
                <i
                  :class="[item.icon, 'text-lg opacity-10']"
                  aria-hidden="true"
                ></i>
              </div>
            </div>
          </div>
        </div>
        <div class="stats-card-bg"></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.stats-card {
  position: relative;
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border-radius: 1rem;
}

.stats-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1) !important;
}

.stats-card-bg {
  position: absolute;
  top: -20px;
  right: -20px;
  width: 100px;
  height: 100px;
  background: currentColor;
  opacity: 0.03;
  border-radius: 50%;
  z-index: 0;
}

.icon-shape {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.75rem !important; /* 方圓形更有現代感 */
}

/* 讓 icon 數字更有呼吸感 */
.numbers h4 {
  letter-spacing: -0.5px;
}
</style>
