<script setup>
import { ref, onMounted, computed } from "vue";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import productService from "@/views/PointShop/services/productService";
import FilterPanel from "@/views/PointShop/componets/FilterPanel.vue";
import ProductGrid from "@/views/PointShop/componets/ProductGrid.vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import Cart from "@/views/PointShop/componets/Cart.vue";

const products = ref([]);
const loading = ref(true);
const CartNow = useCartStore();

// 篩選狀態
const filters = ref({
  minPrice: 0,
  maxPrice: 10000,
  categories: [],
  inStock: false,
});

// 可用的類別列表（從產品資料動態生成）
const categories = ref([]);

const fetchProducts = async () => {
  try {
    const response = await productService.getProducts();
    products.value = response.data;

    // 從產品資料動態提取類別
    const uniqueCategories = new Set();
    response.data.forEach((product) => {
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
    const maxPrice = Math.max(...response.data.map((p) => p.point || 0));
    filters.value.maxPrice = maxPrice;
  } catch (error) {
    console.error("抓取商品失敗:", error);
  } finally {
    loading.value = false;
  }
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

function btnclick() {
  CartNow.cartItems.forEach((element) => {
    console.log(element);
  });
}

onMounted(() => {
  fetchProducts();
});
</script>

<template>
  <DefaultNavbar darkText />

  <div class="min-vh-100 pb-5">
    <div class="container mt-5 mb-5">
      <div class="col-12 mb-4">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>

      <div class="row">
        <div class="col-lg-3">
          <FilterPanel
            :filters="filters"
            :categories="categories"
            @update:filters="handleFilterUpdate"
            @toggle-category="toggleCategory"
            @reset-filters="resetFilters"
          />
        </div>

        <div class="col-lg-9">
          <ProductGrid
            :products="products"
            :filtered-products="filteredProducts"
            :loading="loading"
            @reset-filters="resetFilters"
          />
        </div>
      </div>
    </div>
  </div>
  <button
    class="btn position-fixed start-0 top-50 translate-middle-y me-3"
    data-bs-toggle="offcanvas"
    data-bs-target="#cartOffcanvas"
    @click="btnclick"
  >
    <i class="bi bi-cart-dash-fill h1"></i>
  </button>

  <Cart />
  <DefaultFooter />
</template>

<style scoped>
/* Bootstrap 預設顏色覆蓋 */
:deep(.bg-light) {
  background-color: #f8f9fa !important;
}

:deep(.btn-primary) {
  color: #0b5ed7 !important;
}
</style>
