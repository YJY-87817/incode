<template>
  <DefaultNavbar
    :action="{
      route: 'javascript:;',
    }"
  />
  <div class="point-shop-container">
    <h1>點數商城</h1>
    <hr />

    <div v-if="loading" class="loading">載入中...</div>

    <div v-else class="product-grid">
      <ProductCard
        v-for="item in products"
        :key="item.productId"
        :product="item"
      />
    </div>

    <div v-if="!loading && products.length === 0" class="empty">
      目前沒有可兌換的商品。
    </div>
  </div>
  <DefaultFooter />
</template>

<script setup>
import { ref, onMounted } from "vue";
import productService from "@/views/PointShop/services/productService";
import ProductCard from "@/views/PointShop/componets/ProductCard.vue";

// Example components
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
    alert("無法取得商品資料，請稍後再試");
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchProducts();
});
</script>

<style scoped>
.point-shop-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}
.product-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 25px;
  margin-top: 30px;
}
.loading,
.empty {
  text-align: center;
  padding: 50px;
  font-size: 18px;
  color: #888;
}
</style>
