<!-- src/components/mission-hall/TaskSearchFilterBar.vue -->
<script setup>
import { computed } from "vue"; // ← 這行一定要加！
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();

// 判斷是否有任何篩選條件被設定（用來控制「清除篩選」按鈕是否顯示）
const hasAnyFilter = computed(() => {
  return (
    store.searchQuery.trim() !== "" || // 有搜尋文字
    store.selectedCategory !== "全部" || // 類別不是全部
    store.selectedDifficulty !== "不限難度" || // 難度不是全部（僅 mission）
    store.selectedDuration !== "不限時間" || // 時長不是全部
    store.sortOption !== "default" // 排序不是預設
  );
});
</script>

<template>
  <div class="card shadow-blur blur border-0 mb-4">
    <div class="card-body p-3">
      <!-- 第一排：搜尋框 + 發布按鈕 + 清除篩選按鈕 -->
      <div class="row align-items-center mb-3">
        <!-- 搜尋輸入 -->
        <div class="col-md-7 col-lg-7 d-flex gap-2">
          <div
            class="input-group input-group-outline bg-white border-radius-md flex-grow-1"
          >
            <input
              v-model="store.searchQuery"
              type="text"
              class="form-control"
              placeholder="搜尋任務名稱或關鍵字..."
            />
          </div>

          <button
            @click="store.toggleAdvancedFilters"
            class="btn btn-link text-info text-xs p-0 mb-0 d-flex align-items-center text-nowrap"
            style="min-width: 70px"
          >
            <i
              :class="
                store.showAdvancedFilters
                  ? 'fas fa-chevron-up'
                  : 'fas fa-chevron-down'
              "
              class="me-1"
            ></i>
            {{ store.showAdvancedFilters ? "隱藏篩選" : "進階搜尋" }}
          </button>
        </div>

        <div
          class="col-md-5 col-lg-5 mt-3 mt-md-0 d-flex gap-2 justify-content-md-end justify-content-end align-items-center"
        >
          <!-- 清除篩選按鈕（當有任何篩選條件時顯示） -->
          <button
            v-if="hasAnyFilter"
            class="btn btn-outline-secondary mb-0 px-3 text-nowrap"
            @click="store.clearFilters"
            title="一鍵清除所有篩選條件"
          >
            清除篩選
          </button>

          <!-- 我要發布（僅委託 tab） -->
          <button
            v-if="store.activeTab === 'entrustMission'"
            class="btn bg-gradient-dark mb-0 px-4 text-nowrap"
            style="flex-shrink: 0"
          >
            <i class="fas fa-plus me-1"></i> 我要發布
          </button>
        </div>
      </div>
      <!-- 進階搜尋 -->
      <div v-show="store.showAdvancedFilters" class="mt-3 pt-3 border-top">
        <div class="row g-3">
          <div class="col-6 col-md-4">
            <label class="form-label text-xs mb-1">排序方式</label>
            <select
              v-model="store.sortOption"
              class="form-select form-select-sm border px-2"
            >
              <option value="default">預設排序</option>
              <option value="reward">按獎勵排序</option>
              <option value="popular">按熱門排序</option>
              <option value="urgent">按緊急程度</option>
            </select>
          </div>

          <div v-if="store.activeTab === 'mission'" class="col-6 col-md-4">
            <label class="form-label text-xs mb-1">任務難度</label>
            <select
              v-model="store.selectedDifficulty"
              class="form-select form-select-sm border px-2"
            >
              <option
                v-for="diff in store.difficultyOptions"
                :key="diff"
                :value="diff"
              >
                {{ diff }}
              </option>
            </select>
          </div>

          <div class="col-6 col-md-4">
            <label class="form-label text-xs mb-1">時間限制</label>
            <select
              v-model="store.selectedDuration"
              class="form-select form-select-sm border px-2"
            >
              <option
                v-for="dur in store.durationOptions"
                :key="dur"
                :value="dur"
              >
                {{ dur }}
              </option>
            </select>
          </div>
        </div>
      </div>
      <!--  類別按鈕列 -->
      <div
        class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3 w-100"
      >
        <div class="w-100" style="min-width: 0">
          <div
            class="d-flex gap-2 flex-nowrap overflow-x-auto pb-2 scrollbar-hidden"
            style="-webkit-overflow-scrolling: touch; display: flex !important"
          >
            <button
              v-for="cat in store.currentCategories"
              :key="cat"
              @click="store.selectedCategory = cat"
              :class="[
                'btn btn-xs rounded-pill text-nowrap shadow-none mb-0',
                store.selectedCategory === cat
                  ? 'btn-info'
                  : 'btn-outline-info bg-white',
              ]"
              style="flex-shrink: 0"
            >
              <i :class="store.getCategoryIcon(cat)" class="me-1"></i>
              {{ cat }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 確保橫向溢出時可以滑動，並隱藏原生捲軸 */
.scrollbar-hidden {
  display: flex !important;
  flex-wrap: nowrap !important;
  overflow-x: auto !important;
  -ms-overflow-style: none; /* IE/Edge */
  scrollbar-width: none; /* Firefox */
}

.scrollbar-hidden::-webkit-scrollbar {
  display: none; /* Chrome/Safari */
}

/* 確保文字不會換行撐開高度 */
.text-nowrap {
  white-space: nowrap !important;
}

/* 按鈕在小螢幕時自動換行 */
@media (max-width: 991px) {
  .text-end {
    text-align: left !important;
  }
}
</style>
