<script setup>
import { onMounted, ref } from "vue";

const props = defineProps({
  filters: {
    type: Object,
    required: true,
  },
  categories: {
    type: Array,
    required: true,
  },
  series: {
    type: Array,
    required: true,
  },
});

const emit = defineEmits([
  "update:filters",
  "toggle-category",
  "toggle-series",
  "reset-filters",
]);

const updateMinPrice = (value) => {
  emit("update:filters", { ...props.filters, minPrice: Number(value) });
};

const updateMaxPrice = (value) => {
  emit("update:filters", { ...props.filters, maxPrice: Number(value) });
};

const updateInStock = (value) => {
  emit("update:filters", { ...props.filters, inStock: value });
};

const handleToggleCategory = (categoryName) => {
  emit("toggle-category", categoryName);
};

const handleToggleSeries = (seriesName) => {
  emit("toggle-series", seriesName);
};

const handleResetFilters = () => {
  emit("reset-filters");
};


const filterCollapseRef = ref(null);
const filterHeaderRef = ref(null);

onMounted(() => {
  // 設定斷點，768px 對應 Bootstrap 的 md
  const mdBreakpoint = 768;

  // 檢查目前視窗寬度是否大於等於 md
  if (window.innerWidth >= mdBreakpoint) {
    // 情境 A：單純操作 Class (最簡單，不需引入 Bootstrap JS 物件)
    if (filterCollapseRef.value && filterHeaderRef.value) {
      // 1. 內容區塊加上 'show' 讓它顯示
      filterCollapseRef.value.classList.add("show");

      // 2. Header 去除 'collapsed' (這是 Bootstrap 控制箭頭旋轉的 class)
      filterHeaderRef.value.classList.remove("collapsed");

      // 3. 更新無障礙屬性
      filterHeaderRef.value.setAttribute("aria-expanded", "true");
    }
  }
});
</script>

<template>
  <div class="card shadow-sm border-0 mb-5 rounded" style="top: 80px ">

    <div ref="filterHeaderRef"
      class="card-header text-white d-flex justify-content-between align-items-center collapsed"
      data-bs-toggle="collapse" data-bs-target="#filterCollapse" aria-expanded="false" aria-controls="filterCollapse"
      style="cursor: pointer; background-color: #86B0BD">
      <h6 class="mb-0"><i class="bi bi-funnel"></i> 篩選商品</h6>
      <i class="bi bi-chevron-down transition-icon"></i>
    </div>

    <div class="collapse" id="filterCollapse" ref="filterCollapseRef">
      <div class="card-body p-3">
        <div class="mb-2 border-bottom pb-2">
          <label class="form-label fw-bold small mb-1">
            <i class="bi bi-tag"></i> 價格範圍
          </label>
          <div class="row g-1 mb-2">
            <div class="col-6">
              <input :value="filters.minPrice" @input="updateMinPrice($event.target.value)" type="number"
                class="form-control form-control-sm border px-2" placeholder="最低" />
            </div>
            <div class="col-6">
              <input :value="filters.maxPrice" @input="updateMaxPrice($event.target.value)" type="number"
                class="form-control form-control-sm border px-2" placeholder="最高" />
            </div>
          </div>
          <div class="text-center small text-muted" style="font-size: 0.8rem;">
            {{ filters.minPrice }} ~ {{ filters.maxPrice }} 點
          </div>
        </div>

        <div class="mb-2 border-bottom pb-2">
          <label class="form-label fw-bold small mb-1">
            <i class="bi bi-list"></i> 商品類別
          </label>
          <div class="form-check-list">
            <div v-for="category in categories" :key="category.id" class="form-check mb-1">
              <input :id="`category-${category.id}`" type="checkbox" class="form-check-input mt-1"
                :checked="filters.categories.includes(category.name)" @change="handleToggleCategory(category.name)" />
              <label :for="`category-${category.id}`" class="form-check-label small" style="padding-top: 2px;">
                {{ category.name }}
              </label>
            </div>
          </div>
        </div>

        <div class="mb-2 border-bottom pb-2">
          <label class="form-label fw-bold small mb-1">
            <i class="bi bi-collection"></i> 商品系列
          </label>
          <div class="form-check-list">
            <div v-for="seriesItem in series" :key="seriesItem.id" class="form-check mb-1">
              <input :id="`series-${seriesItem.id}`" type="checkbox" class="form-check-input mt-1"
                :checked="filters.series.includes(seriesItem.name)" @change="handleToggleSeries(seriesItem.name)" />
              <label :for="`series-${seriesItem.id}`" class="form-check-label small" style="padding-top: 2px;">
                {{ seriesItem.name }}
              </label>
            </div>
          </div>
        </div>

        <div class="mb-2">
          <label class="form-label fw-bold small mb-1">
            <i class="bi bi-box-seam"></i> 庫存狀態
          </label>
          <div class="form-check">
            <input id="inStock" :checked="filters.inStock" @change="updateInStock($event.target.checked)"
              type="checkbox" class="form-check-input mt-1" />
            <label for="inStock" class="form-check-label small" style="padding-top: 2px;">
              只顯示有庫存的商品
            </label>
          </div>
        </div>

        <hr class="my-2" />

        <button @click="handleResetFilters" class="btn btn-custom-primary w-100 btn-sm">
          <i class="bi bi-arrow-counterclockwise"></i> 重設篩選
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 當 header 沒有 .collapsed class 時 (代表展開中)，箭頭旋轉 180度 */
.card-header:not(.collapsed) .bi-chevron-down {
  transform: rotate(180deg);
}

/* 加上過渡動畫效果 */
.bi-chevron-down {
  transition: transform 0.3s ease;
}

/* 修正收合時底部圓角消失的問題 */
.card-header.collapsed {
  border-bottom-left-radius: 0.375rem;
  border-bottom-right-radius: 0.375rem;
}

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
</style>
