<script setup>
import { computed, ref, watch } from "vue";
import ProductCard from "@/views/PointShop/components/ProductCard.vue";

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

// --- 分頁相關邏輯 ---
const currentPage = ref(1); // 目前頁碼
const pageSize = ref(8); // 每頁顯示幾筆

// 計算總頁數
const totalPages = computed(() => {
  return Math.ceil(props.filteredProducts.length / pageSize.value);
});

// 真正要顯示在畫面上的「該頁商品」
const paginatedProducts = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value;
  const end = start + pageSize.value;
  return props.filteredProducts.slice(start, end);
});

// 當篩選條件改變時，自動回到第一頁
watch(
  () => props.filteredProducts,
  () => {
    currentPage.value = 1;
  },
);

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
    <div v-if="loading" class="col-12 text-center py-5">找尋中...</div>
    <div v-if="loading" class="col-12 text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">找尋中...</span>
      </div>
      <p class="mt-2">找尋中...</p>
    </div>

    <template v-else-if="filteredProducts.length > 0">
      <div class="row g-3">
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

    <div v-else class="col-12 text-center py-5">找不到符合條件的商品</div>
  </div>
</template>

<style scoped>
.btn-custom-primary {
  color: #145369;
  background-color: transparent;
  border: 1px solid #145369 !important;
}

.btn-custom-primary:hover {
  color: #fff;
  background-color: #145369;
  border-color: #145369;
}

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

/* 原有的動畫 */
@keyframes spinner {
  to {
    transform: rotate(360deg);
  }
}
</style>
