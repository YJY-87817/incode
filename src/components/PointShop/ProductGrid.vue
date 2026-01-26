<script setup>
import { computed } from "vue";
import ProductCard from "@/components/PointShop/ProductCard.vue";

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
</script>

<template>
  <div>
    <div v-if="loading" class="col-12 text-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">載入中...</span>
      </div>
      <p class="mt-2 text-muted">商品載入中，請稍候...</p>
    </div>

    <template v-else-if="filteredProducts.length > 0">
      <div class="row g-3">
        <div
          v-for="item in filteredProducts"
          :key="item.productId"
          class="col-12 col-md-6 col-lg-6"
        >
          <ProductCard :product="item" />
        </div>
      </div>
    </template>

    <div v-else class="col-12 text-center py-5">
      <div class="py-5">
        <i class="bi bi-search fs-1 text-muted"></i>
        <p class="mt-3 mb-0 text-muted">沒有符合篩選條件的商品。</p>
        <button
          @click="$emit('reset-filters')"
          class="btn btn-sm btn-link mt-2"
        >
          清除篩選條件
        </button>
      </div>
    </div>
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
