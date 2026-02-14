<!-- 可用點數 -->
<script setup>
import { usePointStore } from "@/stores/point/UsePointStore.js";
const store = usePointStore();
</script>

<template>
  <div class="d-lg-none mb-3">
    <button
      class="btn w-100 d-flex align-items-center justify-content-between py-3 rounded-4 shadow-sm text-white"
      style="background-color: #86b0bd"
      type="button"
      data-bs-toggle="offcanvas"
      data-bs-target="#sidebarOffcanvas"
    >
      <span><i class="bi bi-person-badge me-2"></i>帳戶點數總覽</span>
      <i class="bi bi-list fs-4"></i>
    </button>
  </div>

  <div
    class="offcanvas-lg offcanvas-start border-0 shadow-sm rounded-end-4 overflow-hidden"
    tabindex="-1"
    id="sidebarOffcanvas"
    style="max-width: 300px"
  >
    <div
      class="card-header border-0 py-3 px-4 d-flex justify-content-between align-items-center text-white"
      style="background-color: #86b0bd"
    >
      <h6 class="fw-bold mb-0 text-uppercase tracking-wider">
        <i class="bi bi-person-circle me-2"></i>帳戶資訊
      </h6>
      <button
        type="button"
        class="btn-close btn-close-white d-lg-none"
        data-bs-dismiss="offcanvas"
        data-bs-target="#sidebarOffcanvas"
      ></button>
    </div>

    <div class="card-body p-4 bg-white h-100">
      <div class="text-center mb-4">
        <span class="text-muted small fw-bold text-uppercase d-block mb-2"
          >目前可用點數</span
        >
        <div class="display-6 fw-bold" style="color: #4a6a73">
          {{ store.currentBalance.toLocaleString() }}
        </div>
      </div>

      <hr class="my-4 opacity-25" />

      <div class="stats-list">
        <div class="d-flex align-items-center mb-4">
          <div class="icon-circle bg-success-subtle me-3">
            <i class="bi bi-arrow-up-right text-success fw-bold"></i>
          </div>
          <div>
            <div class="text-muted smaller-font fw-bold text-uppercase">
              累計獲得
            </div>
            <div class="fw-bold text-dark">
              +{{ store.totalGained.toLocaleString() }}
            </div>
          </div>
        </div>

        <div class="d-flex align-items-center mb-2">
          <div class="icon-circle bg-danger-subtle me-3">
            <i class="bi bi-arrow-down-left text-danger fw-bold"></i>
          </div>
          <div>
            <div class="text-muted smaller-font fw-bold text-uppercase">
              累計消耗
            </div>
            <div class="fw-bold text-dark">
              -{{ store.totalSpent.toLocaleString() }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 圓形圖標 */
.icon-circle {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
}

.smaller-font {
  font-size: 0.7rem;
}
.tracking-wider {
  letter-spacing: 1px;
}

/* 響應式調整：在電腦版（lg）時，offcanvas 會變成一般的容器 */
@media (min-width: 992px) {
  .offcanvas-lg {
    position: sticky;
    top: 20px;
    display: flex;
    flex-direction: column;
    height: auto !important;
    visibility: visible !important;
    transform: none !important;
    width: 100% !important;
    border-radius: 1rem !important; /* 恢復圓角 */
  }
}
</style>
