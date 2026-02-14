<script setup>
import { usePointStore } from "@/stores/point/UsePointStore.js";
const store = usePointStore();

const mainColor = "#145369";
</script>

<template>
  <div
    class="card-header bg-white border border-bottom-0 px-4 py-3"
    :style="{ borderColor: mainColor + ' !important' }"
  >
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h5 class="mb-0 fw-semibold" :style="{ color: mainColor }">
        <i class="ni ni-credit-card me-2"></i>點數收支明細
      </h5>
      <button
        class="btn btn-sm rounded-pill px-3 custom-btn"
        @click="store.fetchLogs"
        :disabled="store.isLoading"
      >
        <span v-if="!store.isLoading">
          <i class="ni ni-refresh-69 me-1"></i>刷新
        </span>
        <span v-else>
          <span class="spinner-border spinner-border-sm me-1"></span>更新中
        </span>
      </button>
    </div>

    <div class="d-flex gap-2">
      <div class="input-group flex-grow-1">
        <span
          class="input-group-text bg-white border-end-0 rounded-start-pill d-flex align-items-center"
        >
          <i class="bi bi-search" :style="{ color: mainColor }"></i>
        </span>
        <input
          type="text"
          class="form-control form-control-sm rounded-end-pill shadow-sm border-start-0"
          placeholder="搜尋項目或日期..."
          v-model="store.searchQuery"
        />
      </div>
      <select
        class="form-select form-select-sm rounded-pill px-3 shadow-sm"
        v-model="store.filterType"
        style="width: 160px; min-width: 140px"
      >
        <option value="全部類型">全部類型</option>
        <option value="平台任務">平台任務</option>
        <option value="委託任務">委託任務</option>
        <option value="預扣">預扣</option>
        <option value="商城">商城</option>
        <option value="打賞">打賞</option>
        <option value="儲值">儲值</option>
        <option value="系統調整">系統調整</option>
      </select>
    </div>
  </div>
</template>

<style scoped>
/* 自定義按鈕樣式，對應 #86B0BD */
.custom-btn {
  color: #145369;
  border-color: #145369;
  background-color: transparent;
  transition: all 0.3s ease;
}

.custom-btn:hover {
  background-color: #145369;
  color: white;
  border-color: #145369;
}

.custom-btn:disabled {
  opacity: 0.65;
  color: #145369;
  border-color: #145369;
}

/* 讓輸入框聚焦時也帶點主色調（選配） */
.form-control:focus,
.form-select:focus {
  border-color: #145369;
  box-shadow: 0 0 0 0.25rem rgba(134, 176, 189, 0.25);
}
</style>
