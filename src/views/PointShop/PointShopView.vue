<script setup>
import { ref, onMounted, computed } from "vue";
import productService from "@/views/PointShop/services/productService";
import ProductCard from "@/views/PointShop/componets/ProductCard.vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

const products = ref([]);
const loading = ref(true);

const fetchProducts = async () => {
  try {
    const response = await productService.getProducts();
    products.value = response.data;
  } catch (error) {
    console.error("抓取商品失敗:", error);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchProducts();
});
</script>

<template>
  <DefaultNavbar :action="{ route: 'javascript:;' }" />

  <div class="container mt-5 mb-5">
    <div class="row mb-4">
      <div class="col-12">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>
    </div>

    <div class="row">
      <div v-if="loading" class="col-12 text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">載入中...</span>
        </div>
        <p class="mt-2 text-muted">商品載入中，請稍候...</p>
      </div>

      <template v-else-if="products.length > 0">
        <div
          v-for="item in products"
          :key="item.productId"
          class="col-12 col-md-6 col-lg-3 mb-4"
        >
          <ProductCard :product="item" />
        </div>
      </template>

      <div v-else class="col-12 text-center py-5">
        <div class="alert alert-secondary py-5">
          <i class="bi bi-cart-x fs-1"></i>
          <p class="mt-3 mb-0">目前沒有可兌換的商品。</p>
        </div>
      </div>
    </div>
  </div>

  <DefaultFooter />
</template>
