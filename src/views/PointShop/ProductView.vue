<script setup>
import { ref, onMounted, computed, watch } from "vue";
import {
  getProducts,
  getProductSearch,
} from "@/views/PointShop/services/PointShopApi";
import FilterPanel from "@/views/PointShop/components/FilterPanel.vue";

// 套版範例
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";

const products = ref([]);
const loading = ref(true);
const keyword = ref("");

// 篩選狀態
const filters = ref({
  minPrice: 0,
  maxPrice: 10000,
  categories: [],
  series: [],
  inStock: false,
  search: "",
});

// 可用的類別列表（從產品資料動態生成）
const categories = ref([]);
const series = ref([]);

// 抓取商品資料
const fetchProducts = (keyword = "") => {
  loading.value = true;

  const searchRequest =
    keyword && keyword.trim()
      ? getProductSearch(keyword.trim())
      : getProducts();

  searchRequest
    .then((response) => {
      products.value = response.data.data;
      // 從產品資料動態提取類別
      const uniqueCategories = new Set();
      const uniqueSeries = new Set();
      response.data.data.forEach((product) => {
        if (product.type) {
          uniqueCategories.add(product.type);
        }
        if (product.series) {
          uniqueSeries.add(product.series);
        }
      });

      // 轉換為陣列格式
      categories.value = Array.from(uniqueCategories).map((name, index) => ({
        id: index + 1,
        name: name,
      }));

      series.value = Array.from(uniqueSeries).map((name, index) => ({
        id: index + 1,
        name: name,
      }));

      // 設定最大價格
      const priceValues = response.data.data.map((p) => p.point || 0);
      const maxPrice =
        priceValues.length > 0 ? Math.max(...priceValues) : 10000;
      filters.value.maxPrice = maxPrice;
    })
    .catch((error) => {
      console.error("抓取商品失敗:", error);
    })
    .finally(() => {
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
    // 系列篩選（使用 series）
    const seriesCheck =
      filters.value.series.length === 0 ||
      filters.value.series.includes(product.series);
    // 庫存篩選
    const stockCheck =
      !filters.value.inStock || (product.stock && product.stock > 0);
    return priceCheck && categoryCheck && seriesCheck && stockCheck;
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

const toggleSeries = (seriesName) => {
  const index = filters.value.series.indexOf(seriesName);
  if (index > -1) {
    filters.value.series.splice(index, 1);
  } else {
    filters.value.series.push(seriesName);
  }
};

const resetFilters = () => {
  filters.value = {
    minPrice: 0,
    maxPrice: 10000,
    categories: [],
    series: [],
    inStock: false,
    search: "",
  };
  fetchProducts(""); // 重設篩選時，清空搜尋並重新載入所有商品
};

const handleFilterUpdate = (newFilters) => {
  filters.value = newFilters;
};

// 監聽搜尋字段變化，自動觸發搜尋
watch(
  () => filters.value.search,
  (newSearch) => {
    fetchProducts(newSearch);
  },
  { immediate: false },
);

onMounted(() => {
  const body = document.getElementsByTagName("body")[0];
  body.classList.add("bg-grey-lighten-5"); // 全局背景色

  fetchProducts(""); // 初始載入所有商品
});
</script>

<template>
  <DefaultNavbar light />
  <div class="min-vh-100 pb-5">
    <div class="container pt-5 mb-5">
      <div class="row">
        <div class="col-lg-3">
          <FilterPanel
            :filters="filters"
            :categories="categories"
            :series="series"
            @update:filters="handleFilterUpdate"
            @toggle-category="toggleCategory"
            @toggle-series="toggleSeries"
            @reset-filters="resetFilters"
          />
        </div>
        <div class="col-lg-9">
          <router-view v-slot="{ Component }">
            <transition name="fade" mode="out-in">
              <component
                :is="Component"
                :products="products"
                :filtered-products="filteredProducts"
                :loading="loading"
                @reset-filters="resetFilters"
              />
            </transition>
          </router-view>
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped></style>
