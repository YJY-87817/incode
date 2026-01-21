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
</script>

<template>
  <div class="card shadow-sm border-0 sticky-top mb-5" style="top: 80px">
    <div class="card-header bg-success text-white">
      <h6 class="mb-0"><i class="bi bi-funnel"></i> 篩選商品</h6>
    </div>

    <div class="card-body">
      <!-- 價格篩選 -->
      <div class="mb-4">
        <label class="form-label fw-bold">
          <i class="bi bi-tag"></i> 價格範圍
        </label>
        <div class="row g-2 mb-3">
          <div class="col-6">
            <input
              :value="filters.minPrice"
              @input="updateMinPrice($event.target.value)"
              type="number"
              class="form-control form-control-sm"
              placeholder="最低點數"
            />
          </div>
          <div class="col-6">
            <input
              :value="filters.maxPrice"
              @input="updateMaxPrice($event.target.value)"
              type="number"
              class="form-control form-control-sm"
              placeholder="最高點數"
            />
          </div>
        </div>
        <div class="text-muted small">
          {{ filters.minPrice }} ~ {{ filters.maxPrice }} 點
        </div>
      </div>

      <hr />

      <!-- 類別篩選 -->
      <div class="mb-4">
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

      <hr />

      <!-- 庫存篩選 -->
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

      <!-- 重置按鈕 -->
      <button
        @click="handleResetFilters"
        class="btn btn-outline-secondary w-100"
      >
        <i class="bi bi-arrow-counterclockwise"></i> 重設篩選
      </button>
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
</style>
