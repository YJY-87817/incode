<script setup>
import { useTaskStore } from "@/stores/point/UseTaskStore";
const store = useTaskStore();
</script>

<template>
  <div
    class="filter-controls-container p-3 bg-white border-radius-lg shadow-sm mb-3"
  >
    <div class="d-flex justify-content-center mb-3">
      <div
        class="input-group input-group-outline w-100 w-md-50 bg-light rounded"
      >
        <span class="input-group-text"
          ><i class="fas fa-search text-xs"></i
        ></span>
        <input
          v-model="store.searchQuery"
          type="text"
          class="form-control ps-2"
          placeholder="輸入關鍵字搜尋任務..."
        />
        <button
          @click="store.clearFilters"
          class="btn btn-link text-secondary mb-0 px-3 text-xs"
        >
          重置
        </button>
      </div>
    </div>

    <div
      class="d-flex flex-wrap justify-content-center align-items-center gap-2"
    >
      <div class="filter-select-wrapper">
        <select
          v-model="store.selectedCategory"
          class="form-select form-select-sm border-0 bg-gray-100"
        >
          <option
            v-for="cat in store.currentCategories"
            :key="cat"
            :value="cat"
          >
            {{ cat }}
          </option>
        </select>
      </div>

      <div v-if="store.activeTab === 'mission'" class="filter-select-wrapper">
        <select
          v-model="store.selectedDifficulty"
          class="form-select form-select-sm border-0 bg-gray-100"
        >
          <option
            v-for="opt in store.difficultyOptions"
            :key="opt"
            :value="opt"
          >
            {{ opt }}
          </option>
        </select>
      </div>

      <div class="filter-select-wrapper">
        <select
          v-model="store.selectedDuration"
          class="form-select form-select-sm border-0 bg-gray-100"
        >
          <option v-for="opt in store.durationOptions" :key="opt" :value="opt">
            {{ opt }}
          </option>
        </select>
      </div>

      <div class="filter-select-wrapper border-start ps-2 ms-1">
        <select
          v-model="store.sortOption"
          class="form-select form-select-sm border-0 bg-gray-100 font-weight-bold text-info"
        >
          <option value="default">預設排序</option>
          <option value="reward">獎勵：高到低</option>
          <option value="popular">熱門程度</option>
          <option value="urgent">緊急優先</option>
        </select>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 縮小選單寬度，防止過度分散導致的空白感 */
.filter-select-wrapper {
  min-width: 110px;
}

.form-select-sm {
  border-radius: 0.5rem !important;
  font-size: 0.75rem;
  padding: 0.5rem 2rem 0.5rem 0.75rem !important;
  cursor: pointer;
  transition: all 0.2s ease;
}

.form-select-sm:hover {
  background-color: #e9ecef !important;
}

/* 針對行動端微調，避免擠壓 */
@media (max-width: 576px) {
  .filter-select-wrapper {
    flex: 1 1 45%; /* 手機版每行顯示兩個 */
  }
}
</style>
