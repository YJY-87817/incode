<script setup>
import { ref, computed, watch } from "vue";
import { usePointStore } from "@/stores/point/UsePointStore.js";

const store = usePointStore();
const currentPage = ref(1);
const itemsPerPage = 10;

const mainColor = "#145369"; // 定義主色

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
    平台任務: "custom-badge-main",
    委託任務: "custom-badge-main",
    預扣: "custom-badge-main",
    商城: "custom-badge-main",
    打賞: "custom-badge-main",
    儲值: "custom-badge-main",
    系統調整: "custom-badge-main",
  };
  return map[category] || "bg-info-subtle text-info";
};
</script>

<template>
  <div
    class="card-body p-0 border border-top-0"
    :style="{ borderColor: mainColor + ' !important' }"
  >
    <div v-if="store.isLoading" class="text-center py-5">
      <div
        class="spinner-border"
        :style="{ width: '3rem', height: '3rem', color: mainColor }"
      ></div>
      <p class="mt-3 text-muted">正在載入點數紀錄...</p>
    </div>

    <div v-else-if="store.filteredLogs.length === 0" class="text-center py-5">
      <i class="ni ni-folder-17 text-muted fs-1"></i>
      <p class="mt-2 text-muted">找不到相符的點數紀錄</p>
    </div>

    <div v-else class="table-responsive">
      <table class="table table-hover align-middle mb-0 table-borderless">
        <thead class="custom-thead">
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
              <!-- 新增：顯示訂單編號（如果存在） -->
              <div v-if="log.subTitle" class="small text-muted mt-1">
                ({{ log.subTitle }})
              </div>
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
              {{ log.points > 0 ? "+" : "" }}{{ log.points.toLocaleString() }}
            </td>
            <td class="text-end pe-4 fw-medium" :style="{ color: '#145369' }">
              {{ log.balance.toLocaleString() }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <nav
      v-if="totalPages > 1"
      aria-label="Page navigation example"
      class="py-3 border-top d-flex justify-content-center"
    >
      <ul class="pagination pagination-sm mb-0">
        <li class="page-item" :class="{ disabled: currentPage === 1 }">
          <a
            class="page-link"
            href="#"
            aria-label="Previous"
            @click.prevent="currentPage--"
          >
            <span aria-hidden="true">&laquo;</span>
          </a>
        </li>

        <li
          v-for="page in totalPages"
          :key="page"
          class="page-item"
          :class="{ active: currentPage === page }"
        >
          <a class="page-link" href="#" @click.prevent="currentPage = page">
            {{ page }}
          </a>
        </li>

        <li class="page-item" :class="{ disabled: currentPage === totalPages }">
          <a
            class="page-link"
            href="#"
            aria-label="Next"
            @click.prevent="currentPage++"
          >
            <span aria-hidden="true">&raquo;</span>
          </a>
        </li>
      </ul>
    </nav>
  </div>
</template>

<style scoped>
/* --- 強化的表頭樣式 --- */
.custom-thead {
  background-color: #86b0bd !important;
}

.custom-thead th {
  /* 1. 解決字看不見的問題：改用深色或白色 */
  /* 如果背景是 #86b0bd，建議用深灰藍色（如照片中的層次感）或純白 */
  color: #2c3e50 !important;

  /* 2. 強化視覺效果 */
  font-size: 0.85rem; /* 對應 small */
  font-weight: 700; /* 加粗文字 */
  letter-spacing: 1px; /* 增加字距，這對中文非常有效 */
  padding-top: 12px;
  padding-bottom: 12px;
  border: none;
}
/* 自定義主色 Badge */
.custom-badge-main {
  background-color: #b2c9d1;
  color: #145369;
}
/* 修改一般狀態的數字顏色 */
.page-link {
  color: #86b0bd; /* 主色 */
}

/* 修改滑鼠移過去的顏色 */
.page-link:hover {
  color: #86b0bd;
  background-color: #e9ecef;
}

/* 修改當前頁面 (Active) 的顏色 */
.page-item.active .page-link {
  background-color: #86b0bd !important;
  border-color: #86b0bd !important;
  color: #145369 !important;
}
</style>
