<script setup>
import { ref, onMounted, computed, watch } from "vue";
import {
  getProducts,
  getProductSearch,
} from "@/api/pointShopApi";
import { useCartStore } from "@/stores/pointshop/cartStore";
import FilterPanel from "@/components/pointshop/FilterPanel.vue";
import "@/assets/css/pointshop.css";

// 套版範例
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import DefaultFooter from "@/components/layout/FooterCentered.vue";

const cartStore = useCartStore();
const products = ref([]);
const loading = ref(true);
// 目前商品的最高點數，重設篩選時用來還原價格上限
const maxProductPrice = ref(10000);

const defaultFilters = () => ({
  minPrice: 0,
  maxPrice: maxProductPrice.value,
  categories: [],
  series: [],
  inStock: false,
  search: "",
});

// 篩選狀態
const filters = ref(defaultFilters());

// 可選的類別 / 系列 (由商品資料動態產生)
const toOptions = (values) =>
  [...new Set(values.filter(Boolean))].map((name, index) => ({
    id: index + 1,
    name,
  }));
const categories = computed(() => toOptions(products.value.map((p) => p.type)));
const series = computed(() => toOptions(products.value.map((p) => p.series)));

// 請求序號：連續搜尋時只採用最後一次請求的結果，避免較慢的舊回應覆蓋新結果
let requestSeq = 0;

// 抓取商品資料
const fetchProducts = async (keyword = "") => {
  const seq = ++requestSeq;
  const trimmed = keyword.trim();
  loading.value = true;

  try {
    const response = trimmed
      ? await getProductSearch(trimmed)
      : await getProducts();
    if (seq !== requestSeq) return;

    const data = response.data.data || [];
    products.value = data;

    // 取得完整商品清單時，同步購物車內的價格 / 庫存 (購物車存的是加入當下的舊資料)
    // (API 失敗時不同步，避免把購物車清空)
    if (!trimmed && response.data.ok) cartStore.syncWithProducts(data);

    // 價格上限設為目前商品最高點數
    maxProductPrice.value = data.length
      ? Math.max(...data.map((p) => p.point || 0))
      : 10000;
    filters.value.maxPrice = maxProductPrice.value;
  } catch (error) {
    if (seq === requestSeq) console.error("抓取商品失敗:", error);
  } finally {
    if (seq === requestSeq) loading.value = false;
  }
};

// 篩選後的商品
const filteredProducts = computed(() => {
  const f = filters.value;
  return products.value.filter(
    (product) =>
      product.point >= f.minPrice &&
      product.point <= f.maxPrice &&
      (f.categories.length === 0 || f.categories.includes(product.type)) &&
      (f.series.length === 0 || f.series.includes(product.series)) &&
      (!f.inStock || product.stock > 0),
  );
});

// 勾選 / 取消勾選
const toggleIn = (list, value) => {
  const index = list.indexOf(value);
  if (index > -1) list.splice(index, 1);
  else list.push(value);
};
const toggleCategory = (name) => toggleIn(filters.value.categories, name);
const toggleSeries = (name) => toggleIn(filters.value.series, name);

// 有搜尋關鍵字時，清空 search 會觸發下方 watch 重新載入全部商品；
// 沒有關鍵字時目前清單就是全部商品，不需要重抓
const resetFilters = () => {
  filters.value = defaultFilters();
};

const handleFilterUpdate = (newFilters) => {
  filters.value = newFilters;
};

// 停止輸入 300ms 後才搜尋 (避免每打一個字就打一次 API)
let searchTimer = null;
watch(
  () => filters.value.search,
  (newSearch) => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => fetchProducts(newSearch), 300);
  },
);

onMounted(() => {
  document.body.classList.add("bg-grey-lighten-5"); // 全局背景色
  fetchProducts();
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

