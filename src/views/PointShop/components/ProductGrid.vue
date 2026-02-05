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
const currentPage = ref(1);        // 目前頁碼
const pageSize = ref(6);           // 每頁顯示幾筆

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
watch(() => props.filteredProducts, () => {
  currentPage.value = 1;
});

const changePage = (page) => {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page;
    // 選擇性：換頁後捲動到頂部
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
};
</script>

<template>
  <div>
    <div v-if="loading" class="col-12 text-center py-5">...</div>

    <template v-else-if="filteredProducts.length > 0">
      <div class="row g-3">
        <div v-for="item in paginatedProducts" :key="item.productId" class="col-12 col-md-6 col-lg-6">
          <ProductCard :product="item" />
        </div>
      </div>

      <div v-if="totalPages > 1" class="d-flex justify-content-center">
        <nav aria-label="Page navigation" class="mt-4">
          <ul class="pagination">
            <li class="page-item" :class="{ disabled: currentPage === 1 }">
              <a class="page-link" href="#" @click.prevent="changePage(currentPage - 1)">&laquo;</a>
            </li>

            <li v-for="page in totalPages" :key="page" class="page-item" :class="{ active: currentPage === page }">
              <a class="page-link" href="#" @click.prevent="changePage(page)">{{ page }}</a>
            </li>

            <li class="page-item" :class="{ disabled: currentPage === totalPages }">
              <a class="page-link" href="#" @click.prevent="changePage(currentPage + 1)">&raquo;</a>
            </li>
          </ul>
        </nav>
      </div>
    </template>

    <div v-else class="col-12 text-center py-5">...</div>
  </div>
</template>

<style scoped>
.spinner-border {
  animation: spinner 0.75s linear infinite;
}

@keyframes spinner {
  to {
    transform: rotate(360deg);
  }
}
</style>
