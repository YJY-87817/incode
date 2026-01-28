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
    <h6 class="text-uppercase text-muted small fw-bold mb-3">帳戶點數總覽</h6>

    <div
      class="card shadow-sm border-0 rounded-4 p-3 bg-success-subtle mb-3 text-center stat-card"
      style="min-height: 110px"
    >
      <div class="d-flex align-items-center justify-content-center mb-2">
        <i class="ni ni-money-coins text-success fs-4 me-2"></i>
        <div class="text-success fw-bold small text-uppercase">累計獲得</div>
      </div>
      <div class="fw-bold text-success display-5 lh-1">
        +{{ store.totalGained.toLocaleString() }}
      </div>
    </div>

    <div
      class="card shadow-sm border-0 rounded-4 p-3 bg-danger-subtle mb-3 text-center stat-card"
      style="min-height: 110px"
    >
      <div class="d-flex align-items-center justify-content-center mb-2">
        <i class="ni ni-cart text-danger fs-4 me-2"></i>
        <div class="text-danger fw-bold small text-uppercase">累計消耗</div>
      </div>
      <div class="fw-bold text-danger display-5 lh-1">
        -{{ store.totalSpent.toLocaleString() }}
      </div>
    </div>

    <div
      class="card shadow-sm border-0 rounded-4 p-3 mb-3 text-center stat-card"
      style="background-color: #86b0bd; min-height: 110px"
    >
      <div class="d-flex align-items-center justify-content-center mb-2">
        <i class="bi bi-wallet2 fs-4 me-2 text-white"></i>
        <div class="text-white fw-bold small text-uppercase">目前可用點數</div>
      </div>
      <div class="fw-bold text-white display-5 lh-1">
        {{ store.currentBalance.toLocaleString() }}
      </div>
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
