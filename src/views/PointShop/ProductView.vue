<script setup>
import { ref, onMounted, computed } from "vue";
import { getProducts } from "@/views/PointShop/services/PointShopApi";
import FilterPanel from "@/views/PointShop/components/FilterPanel.vue";
import ProductGrid from "@/views/PointShop/components/ProductGrid.vue";
import ProductDetail from "@/views/PointShop/components/ProductDetail.vue";
// 套版範例
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";

const products = ref([]);
const loading = ref(true);

// 篩選狀態
const filters = ref({
  minPrice: 0,
  maxPrice: 10000,
  categories: [],
  inStock: false,
});

// 可用的類別列表（從產品資料動態生成）
const categories = ref([]);

const fetchProducts = () => {
  // 開始抓取前，通常會手動開啟 loading (原本 async 函式通常也會在開頭設定)
  loading.value = true;

  getProducts()
    .then((response) => {
      // 成功處理邏輯
      products.value = response.data.data;

      // 從產品資料動態提取類別
      const uniqueCategories = new Set();
      response.data.data.forEach((product) => {
        if (product.type) {
          uniqueCategories.add(product.type);
        }
      });

      // 轉換為陣列格式
      categories.value = Array.from(uniqueCategories).map((name, index) => ({
        id: index + 1,
        name: name,
      }));

      // 設定最大價格
      const maxPrice = Math.max(...response.data.data.map((p) => p.point || 0));
      filters.value.maxPrice = maxPrice;
    })
    .catch((error) => {
      // 錯誤處理邏輯
      console.error("抓取商品失敗:", error);
    })
    .finally(() => {
      // 不論成功或失敗都會執行的邏輯
      loading.value = false;
    });
};

// 篩選後的商品
const filteredProducts = computed(() => {
  return products.value.filter((product) => {
    // 價格篩選
    const priceCheck =
      product.point >= filters.value.minPrice &&
      product.point <= filters.value.maxPrice;

    // 類別篩選（使用 Type）
    const categoryCheck =
      filters.value.categories.length === 0 ||
      filters.value.categories.includes(product.type);

    // 庫存篩選
    const stockCheck =
      !filters.value.inStock || (product.stock && product.stock > 0);

    return priceCheck && categoryCheck && stockCheck;
  });
});

const toggleCategory = (categoryName) => {
  const index = filters.value.categories.indexOf(categoryName);
  if (index > -1) {
    filters.value.categories.splice(index, 1);
  } else {
    filters.value.categories.push(categoryName);
  }
};

const resetFilters = () => {
  filters.value = {
    minPrice: 0,
    maxPrice: 10000,
    categories: [],
    inStock: false,
  };
};

const handleFilterUpdate = (newFilters) => {
  filters.value = newFilters;
};

onMounted(() => {
  fetchProducts();
});
</script>

<template>
  <DefaultNavbar light />

  <div class="min-vh-100 pb-5 " style="">
    <div class="container pt-5 mb-5">
      <div class="col-12 mb-4">
        <div class="card border-0 shadow-sm" style="background-color: #86B0BD;">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>

      <div class="row">
        <div class="col-lg-3">
          <FilterPanel :filters="filters" :categories="categories" @update:filters="handleFilterUpdate"
            @toggle-category="toggleCategory" @reset-filters="resetFilters" />
        </div>

        <div class="col-lg-9">
          <!-- <ProductGrid :products="products" :filtered-products="filteredProducts" :loading="loading"
            @reset-filters="resetFilters" />
          <br>
          <br> -->
          <router-view v-slot="{ Component }">
            <transition name="fade" mode="out-in">
              <component :is="Component" :products="products" :filtered-products="filteredProducts" :loading="loading"
                @reset-filters="resetFilters" />
            </transition>
          </router-view>
        </div>


      </div>
    </div>
  </div>

  <DefaultFooter />
</template>

<style scoped></style>
