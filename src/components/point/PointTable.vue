<!-- 明細帳本 (PointTable.vue)
這是最複雜的部分，也就是下方的列表。

它從大腦拿「過濾後的資料」來顯示。

它包含了一個**「切片功能（分頁）」**：如果大腦給它 100 筆資料，它會設定一次只秀 10 筆，讓你按「下一頁」才看後面的。 -->
<script setup>
import { ref, computed, watch } from "vue";
import { useUserPointStore } from "@/stores/point/UserPoint.js";

const store = useUserPointStore();
const currentPage = ref(1); //紀錄現在在第幾頁
const itemsPerPage = 10; //設定一頁秀 10 筆

// 當過濾條件改變時，回到第一頁
watch([() => store.searchQuery, () => store.filterType], () => {
  currentPage.value = 1;
});

const paginatedLogs = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage;
  return store.filteredLogs.slice(start, start + itemsPerPage);
});

const totalPages = computed(() =>
  Math.ceil(store.filteredLogs.length / itemsPerPage),
);

const getBadgeClass = (category) => {
  const map = {
    平台任務: "bg-success-subtle text-success",
    委託任務: "bg-success-subtle text-success",
    商城商品兌換: "bg-primary-subtle text-primary",
    點數儲值: "bg-info-subtle text-info",
    系統調整: "bg-secondary-subtle text-secondary",
  };
  return map[category] || "bg-info-subtle text-info";
};
</script>

<template>
  <div class="card-body p-0 border border-top-0 border-primary">
    <div v-if="store.isLoading" class="text-center py-5">
      <div
        class="spinner-border text-primary"
        style="width: 3rem; height: 3rem"
      ></div>
      <p class="mt-3 text-muted">正在載入點數紀錄...</p>
    </div>

    <div v-else-if="store.filteredLogs.length === 0" class="text-center py-5">
      <i class="ni ni-folder-17 text-muted fs-1"></i>
      <p class="mt-2 text-muted">找不到相符的點數紀錄</p>
    </div>

    <div v-else class="table-responsive">
      <table class="table table-hover align-middle mb-0 table-borderless">
        <thead class="bg-info text-dark small">
          <tr>
            <th class="ps-4">日期 / 項目</th>
            <th>類別</th>
            <th class="text-center">異動</th>
            <th class="text-end pe-4">餘額</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="log in paginatedLogs" :key="log.id">
            <td class="ps-4">
              <div class="small text-muted">{{ log.date }}</div>
              <div class="fw-bold text-dark">{{ log.item }}</div>
            </td>
            <td>
              <span
                class="badge rounded-pill"
                :class="getBadgeClass(log.category)"
                >{{ log.category }}</span
              >
            </td>
            <td
              class="text-center fw-bold"
              :class="log.points > 0 ? 'text-success' : 'text-danger'"
            >
              {{ log.points > 0 ? "+" : "" }}{{ log.points.toLocaleString()
              }}<i class="bi bi-coin fs-6 ms-1 text-warning"></i>
            </td>
            <td class="text-end pe-4 text-secondary fw-medium">
              {{ log.balance.toLocaleString()
              }}<i class="bi bi-coin fs-6 ms-1 text-warning"></i>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div
      v-if="totalPages > 1"
      class="py-3 border-top d-flex justify-content-center"
    >
      <ul class="pagination pagination-sm mb-0">
        <li class="page-item" :class="{ disabled: currentPage === 1 }">
          <button class="page-link" @click="currentPage--">前一頁</button>
        </li>
        <li class="page-item active">
          <span class="page-link">{{ currentPage }} / {{ totalPages }}</span>
        </li>
        <li class="page-item" :class="{ disabled: currentPage === totalPages }">
          <button class="page-link" @click="currentPage++">後一頁</button>
        </li>
      </ul>
    </div>
  </div>
</template>
