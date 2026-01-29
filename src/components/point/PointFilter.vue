<!-- 過濾器與遙控器 (PointFilter.vue)
這是表格上方那一橫排「搜尋框」和「下拉選單」。

搜尋框：當你在這裡打字，它會直接告訴大腦：「嘿！使用者現在想找『簽到』。」

刷新按鈕：按下它，就會叫大腦裡的辦事員 (Action) 重新去抓一次最新的資料。 -->
<script setup>
import { useUserPointStore } from "@/stores/point/UserPoint.js";
const store = useUserPointStore();
</script>

<template>
  <div
    class="card-header bg-white border border-bottom-0 border-primary px-4 py-3"
  >
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h5 class="mb-0 fw-semibold">
        <i class="ni ni-credit-card me-2 text-primary"></i>點數收支明細
      </h5>
      <button
        class="btn btn-outline-primary btn-sm rounded-pill px-3"
        @click="store.fetchLogs"
        :disabled="store.isLoading"
      >
        <span v-if="!store.isLoading"
          ><i class="ni ni-refresh-69 me-1"></i>刷新</span
        >
        <span v-else
          ><span class="spinner-border spinner-border-sm me-1"></span
          >更新中</span
        >
      </button>
    </div>

    <div class="d-flex gap-2">
      <div class="input-group flex-grow-1">
        <span
          class="input-group-text bg-white border-end-0 rounded-start-pill d-flex align-items-center"
        >
          <i class="bi bi-search"></i>
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
        <option value="商城商品兌換">商城商品兌換</option>
        <option value="點數儲值">點數儲值</option>
        <option value="系統調整">系統調整</option>
      </select>
    </div>
  </div>
</template>
