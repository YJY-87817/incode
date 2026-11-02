<script setup>
import { useDesktopExpand } from "@/views/PointShop/services/useDesktopExpand";
import { ORDER_STATUS } from "@/views/PointShop/services/pointShopUtils";

// v-model：目前選取的訂單狀態，空字串代表全部
const selectedStatus = defineModel({ type: String, default: "" });

const statuses = [
  { value: "", label: "全部訂單" },
  ...Object.values(ORDER_STATUS).map((status) => ({ value: status, label: status })),
];

// 桌面版預設展開篩選面板
const { collapseRef: filterCollapseRef, headerRef: filterHeaderRef } =
  useDesktopExpand();
</script>

<template>
  <div class="card shadow-sm border-0 mb-5 rounded sticky-top shop-filter-card" style="top: 80px">
    <div
      ref="filterHeaderRef"
      class="card-header text-white d-flex justify-content-between align-items-center collapsed"
      data-bs-toggle="collapse"
      data-bs-target="#filterCollapse"
      aria-expanded="false"
      aria-controls="filterCollapse"
      style="cursor: pointer; background-color: #86B0BD"
    >
      <h5 class="fw-bold mb-0">
        <i class="bi bi-funnel me-2"></i>訂單篩選
      </h5>
      <i class="bi bi-chevron-down"></i>
    </div>
    <div class="collapse" id="filterCollapse" ref="filterCollapseRef">
      <div class="card-body p-0">
        <div class="list-group list-group-flush">
          <button
            v-for="status in statuses"
            :key="status.value"
            @click="selectedStatus = status.value"
            :class="[
              'list-group-item list-group-item-action fw-bold',
              selectedStatus === status.value
                ? 'active'
                : ''
            ]"
            style="--bs-list-group-active-bg: #145369; --bs-list-group-active-border-color: #145369;"
          >
            {{ status.label }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.list-group-item-action:hover {
  background-color: #f0f0f0;
}
</style>