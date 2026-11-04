<script setup>
import { computed, ref, watch } from "vue";
import ProductCard from "@/components/pointshop/ProductCard.vue";

const props = defineProps({
  products: {
    type: Array,
    required: true,
  },
  filteredProducts: {
    type: Array,
    required: true,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["reset-filters"]);

// --- 分頁與排序相關邏輯 ---
const currentPage = ref(1); // 目前頁碼
const pageSize = ref(8); // 每頁顯示幾筆
const sortBy = ref(""); // 排序方式

// 排序後的商品
const sortedProducts = computed(() => {
  const products = [...props.filteredProducts];
  switch (sortBy.value) {
    case "pointAsc":
      return products.sort((a, b) => a.point - b.point);
    case "pointDesc":
      return products.sort((a, b) => b.point - a.point);
    case "nameAsc":
      return products.sort((a, b) => a.name.localeCompare(b.name));
    case "nameDesc":
      return products.sort((a, b) => b.name.localeCompare(a.name));
    default:
      return products;
  }
});

// 計算總頁數
const totalPages = computed(() => {
  return Math.ceil(sortedProducts.value.length / pageSize.value);
});

// 真正要顯示在畫面上的「該頁商品」
const paginatedProducts = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value;
  const end = start + pageSize.value;
  return sortedProducts.value.slice(start, end);
});

// 當篩選條件改變時，自動回到第一頁
watch(
  () => props.filteredProducts,
  () => {
    currentPage.value = 1;
  },
);

const handleResetFilters = () => {
  sortBy.value = "";
  emit("reset-filters");
};

const changePage = (page) => {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page;
    // 選擇性：換頁後捲動到頂部
    window.scrollTo({ top: 0, behavior: "smooth" });
  }
};
</script>

<template>
  <div>
    <div v-if="loading" class="col-12 text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">找尋中...</span>
      </div>
      <p class="mt-2">找尋中...</p>
    </div>

    <template v-else-if="filteredProducts.length > 0">
      <div class="row g-3"> 
        <div class="col-12">
          <div class="card border-0 shadow-sm">
            <div
              class="card-body d-flex justify-content-between align-items-center"
            >
              <div class="d-flex align-items-center">
                <label for="sortSelect" class="form-label me-2 mb-0">排序：</label>
                <select
                  id="sortSelect"
                  v-model="sortBy"
                  class="form-select d-inline-block w-auto text-center"
                >
                  <option value="">預設排序</option>
                  <option value="pointAsc">點數由低到高</option>
                  <option value="pointDesc">點數由高到低</option>
                  <option value="nameAsc">名稱 A-Z</option>
                  <option value="nameDesc">名稱 Z-A</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <div
          v-for="item in paginatedProducts"
          :key="item.productId"
          class="col-12 col-md-6 col-lg-6"
        >
          <ProductCard :product="item" />
        </div>
      </div>

      <div v-if="totalPages > 1" class="d-flex justify-content-center">
        <nav aria-label="Page navigation" class="mt-4">
          <ul class="pagination">
            <li class="page-item" :class="{ disabled: currentPage === 1 }">
              <a
                class="page-link btn-custom-primary"
                href="#"
                @click.prevent="changePage(currentPage - 1)"
                >&laquo;</a
              >
            </li>

            <li
              v-for="page in totalPages"
              :key="page"
              class="page-item"
              :class="{ active: currentPage === page }"
            >
              <a
                class="page-link btn-custom-primary"
                href="#"
                @click.prevent="changePage(page)"
                >{{ page }}</a
              >
            </li>

            <li
              class="page-item"
              :class="{ disabled: currentPage === totalPages }"
            >
              <a
                class="page-link btn-custom-primary"
                href="#"
                @click.prevent="changePage(currentPage + 1)"
                >&raquo;</a
              >
            </li>
          </ul>
        </nav>
      </div>
    </template>

    <div v-else class="col-12 text-center py-5">
      <h3>找不到符合條件的商品</h3>
      <button
        class="btn btn-custom-primary mt-3"
        @click="handleResetFilters"
      >
        清除篩選條件
      </button>
    </div>
  </div>
</template>

<style scoped>

/* 1. 基礎樣式：讓所有分頁按鈕符合你的自定義風格 */
:deep(.pagination .page-link) {
  color: #145369;
  background-color: transparent;
  border: 1px solid #145369;
  transition: all 0.2s ease-in-out;
  /* 增加一點平滑過渡感 */
}

/* 2. 選中狀態：當前頁碼填滿顏色 */
:deep(.pagination .page-item.active .page-link) {
  background-color: #145369;
  border-color: #145369;
  color: #ffffff;
  z-index: 3;
}

/* 3. 滑鼠懸停樣式 (Hover) */
:deep(.pagination .page-link:hover) {
  background-color: #145369;
  color: #ffffff;
  border-color: #145369;
}

/* 4. 禁用狀態 (例如在第一頁時的「上一頁」按鈕) */
:deep(.pagination .page-item.disabled .page-link) {
  color: #6c757d;
  pointer-events: none;
  background-color: transparent;
  border-color: #dee2e6;
}

</style>
