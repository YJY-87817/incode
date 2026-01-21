<script setup>
const props = defineProps({
  filters: {
    type: Object,
    required: true,
  },
  categories: {
    type: Array,
    required: true,
  },
});

const emit = defineEmits([
  "update:filters",
  "toggle-category",
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

const handleResetFilters = () => {
  emit("reset-filters");
};

import { onMounted, ref } from "vue";
// 如果您有引入 bootstrap JS
// import { Collapse } from 'bootstrap';

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
  <div class="card shadow-sm border-0 sticky-top mb-5" style="top: 80px">
    <div
      ref="filterHeaderRef"
      class="card-header text-white d-flex justify-content-between align-items-center collapsed"
      data-bs-toggle="collapse"
      data-bs-target="#filterCollapse"
      aria-expanded="false"
      aria-controls="filterCollapse"
      style="cursor: pointer; background-color: #f0f2f5"
    >
      <h6 class="mb-0"><i class="bi bi-funnel"></i> 篩選商品</h6>
      <i class="bi bi-chevron-down transition-icon"></i>
    </div>

    <div class="collapse" id="filterCollapse" ref="filterCollapseRef">
      <div class="card-body">
        <div class="mb-4 border-bottom pb-3">
          <label class="form-label fw-bold">
            <i class="bi bi-tag"></i> 價格範圍
          </label>
          <div class="row g-2 mb-3">
            <div class="col-6">
              <input
                :value="filters.minPrice"
                @input="updateMinPrice($event.target.value)"
                type="number"
                class="form-control form-control-sm border"
                placeholder="最低點數"
              />
            </div>
            <div class="col-6">
              <input
                :value="filters.maxPrice"
                @input="updateMaxPrice($event.target.value)"
                type="number"
                class="form-control form-control-sm border"
                placeholder="最高點數"
              />
            </div>
          </div>
          <div class="text-center small">
            {{ filters.minPrice }} ~ {{ filters.maxPrice }} 點
          </div>
        </div>

        <div class="mb-4 border-bottom pb-3">
          <label class="form-label fw-bold">
            <i class="bi bi-list"></i> 商品類別
          </label>
          <div class="form-check-list">
            <div
              v-for="category in categories"
              :key="category.id"
              class="form-check form-check-custom mb-2"
            >
              <input
                :id="`category-${category.id}`"
                type="checkbox"
                class="form-check-input"
                :checked="filters.categories.includes(category.name)"
                @change="handleToggleCategory(category.name)"
              />
              <label :for="`category-${category.id}`" class="form-check-label">
                {{ category.name }}
              </label>
            </div>
          </div>
        </div>

        <div class="mb-4">
          <label class="form-label fw-bold">
            <i class="bi bi-box-seam"></i> 庫存狀態
          </label>
          <div class="form-check">
            <input
              id="inStock"
              :checked="filters.inStock"
              @change="updateInStock($event.target.checked)"
              type="checkbox"
              class="form-check-input"
            />
            <label for="inStock" class="form-check-label">
              只顯示有庫存的商品
            </label>
          </div>
        </div>

        <hr />

        <button
          @click="handleResetFilters"
          class="btn btn-outline-secondary w-100"
        >
          <i class="bi bi-arrow-counterclockwise"></i> 重設篩選
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sticky-top {
  z-index: 10;
}

.form-check-custom .form-check-input {
  border-radius: 0.25rem;
}

.form-check-custom .form-check-label {
  margin-left: 0.5rem;
  cursor: pointer;
  font-size: 0.95rem;
}

.form-check-custom .form-check-input:checked {
  background-color: #0d6efd;
  border-color: #0d6efd;
}

/* 當 header 沒有 .collapsed class 時 (代表展開中)，箭頭旋轉 180度 */
.card-header:not(.collapsed) .bi-chevron-down {
  transform: rotate(180deg);
}

/* 加上過渡動畫效果 */
.bi-chevron-down {
  transition: transform 0.3s ease;
}

/* Bootstrap 預設顏色覆蓋 */
:deep(.btn.btn-outline-secondary) {
  color: #6c757d !important;
  border-color: #6c757d !important;
}

:deep(.btn.btn-outline-secondary:hover) {
  background-color: #6c757d !important;
  color: white !important;
}
</style>
