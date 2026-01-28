<!-- 門面與廣告牌 (PointSummary.vue)
這是你頁面左邊那兩塊綠色和紅色的卡片。

它非常「單純」，它不負責計算，也不負責抓資料。

它只是從大腦 (Store) 讀取 totalGained 和 totalSpent 這兩個數字，然後套上漂亮的顏色顯示出來。 -->
<script setup>
import { useUserPointStore } from "@/stores/point/UserPoint.js";
const store = useUserPointStore();
</script>

<template>
  <div class="sticky-top" style="top: 20px; z-index: 10">
    <h6 class="text-uppercase text-muted small fw-bold mb-3">帳戶摘要</h6>

    <div
      class="card shadow-sm border-0 rounded-4 p-3 bg-success-subtle mb-3 text-center stat-card"
    >
      <div class="text-success small fw-bold">累計獲得</div>
      <div class="fw-bold text-success fs-3">+{{ store.totalGained }} P</div>
    </div>

    <div
      class="card shadow-sm border-0 rounded-4 p-3 bg-danger-subtle mb-3 text-center stat-card"
    >
      <div class="text-danger small fw-bold">累計消耗</div>
      <div class="fw-bold text-danger fs-3">-{{ store.totalSpent }} P</div>
    </div>
  </div>
</template>

<style scoped>
.stat-card {
  transition: transform 0.2s ease;
}
.stat-card:hover {
  transform: translateY(-3px);
}
</style>
