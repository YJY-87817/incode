<script setup>
import { useTaskStore } from "@/stores/point/UseTaskStore";
const store = useTaskStore();
</script>

<template>
  <div class="d-lg-none mb-3">
    <button
      class="btn w-100 d-flex align-items-center justify-content-between py-3 rounded-4 shadow-sm border-0"
      style="background: #86b0bd; color: #145369"
      type="button"
      data-bs-toggle="offcanvas"
      data-bs-target="#taskSidebar"
    >
      <span class="fw-bold"
        ><i class="fas fa-filter me-2"></i>切換任務與篩選</span
      >
      <i class="fas fa-sliders-h"></i>
    </button>
  </div>

  <div
    class="offcanvas-lg offcanvas-start border-0 shadow-lg rounded-4 overflow-hidden"
    tabindex="-1"
    id="taskSidebar"
    style="max-width: 300px; background: #fff"
  >
    <div
      class="p-4 border-bottom border-light"
      style="background-color: #86b0bd; color: #145369"
    >
      <h5 class="mb-1 fw-bold">任務大廳</h5>
    </div>

    <div class="card-body p-4 custom-scrollbar">
      <div class="section-label">選擇分類</div>
      <div class="nav flex-column mb-4">
        <button
          v-for="(tab, key) in { mission: '平台挑戰', entrust: '影友委託' }"
          :key="key"
          class="menu-item"
          :class="{ active: store.activeTab === key }"
          @click="store.activeTab = key"
        >
          <div class="d-flex align-items-center">
            <i
              :class="key === 'mission' ? 'fas fa-rocket' : 'fas fa-users'"
              class="me-3 nav-icon"
            ></i>
            {{ tab }}
          </div>
          <span v-if="store.activeTab === key" class="active-indicator"></span>
        </button>
      </div>

      <div class="section-label">快速搜尋</div>
      <div class="mb-4">
        <div class="search-container">
          <input
            v-model="store.searchQuery"
            type="text"
            class="minimal-search"
            placeholder="搜尋關鍵字..."
          />
          <i class="fas fa-search search-icon"></i>
        </div>
      </div>

      <div
        class="section-label d-flex justify-content-between align-items-center"
      >
        條件篩選
        <button @click="store.clearFilters" class="reset-link">重置</button>
      </div>

      <div class="filter-group mb-3">
        <label>任務類別</label>
        <select v-model="store.selectedCategory" class="modern-select">
          <option
            v-for="cat in store.currentCategories"
            :key="cat"
            :value="cat"
          >
            {{ cat }}
          </option>
        </select>
      </div>

      <div v-if="store.activeTab === 'mission'" class="filter-group mb-3">
        <label>挑戰難度</label>
        <select v-model="store.selectedDifficulty" class="modern-select">
          <option
            v-for="opt in store.difficultyOptions"
            :key="opt"
            :value="opt"
          >
            {{ opt }}
          </option>
        </select>
      </div>

      <div class="filter-group mb-4">
        <label>排序方式</label>
        <select v-model="store.sortOption" class="modern-select highlight">
          <option value="default">預設排序</option>
          <option value="reward">獎勵最高</option>
          <option value="popular">熱門程度</option>
          <option value="urgent" v-if="store.activeTab === 'entrust'">
            緊急優先
          </option>
        </select>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 統一區塊標籤 */
.section-label {
  font-size: 0.7rem;
  font-weight: 800;
  color: #145369;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  margin-bottom: 12px;
}

/* 頻道選單優化 */
.menu-item {
  position: relative;
  display: flex;
  align-items: center;
  padding: 12px 16px;
  margin-bottom: 6px;
  border: none;
  background: transparent;
  color: #145369;
  font-weight: 600;
  border-radius: 10px;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.menu-item:hover {
  background: #86b0bd;
  color: #145369;
}
.menu-item.active {
  background: #86b0bd;
  color: #145369;
}
.nav-icon {
  font-size: 1rem;
  width: 20px;
  text-align: center;
}
.active-indicator {
  width: 3px;
  height: 16px;
  background: #86b0bd;
  border-radius: 4px;
  position: absolute;
  right: 12px;
}

/* 搜尋框優化 */
.search-container {
  position: relative;
}
.minimal-search {
  width: 100%;
  padding: 10px 12px 10px 38px;
  border-radius: 10px;
  border: 1px solid #e9ecef;
  background: #f8f9fa;
  font-size: 0.85rem;
  transition: all 0.2s;
}
.minimal-search:focus {
  outline: none;
  background: #fff;
  border-color: #145369;
  box-shadow: 0 3px 10px rgba(17, 113, 239, 0.08);
}
.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: #ced4da;
  font-size: 0.8rem;
}

/* 篩選下拉框樣式 */
.filter-group label {
  font-size: 0.75rem;
  color: #145369;
  margin-bottom: 4px;
  display: block;
  font-weight: 700;
}
.modern-select {
  width: 100%;
  padding: 8px 10px;
  border-radius: 8px;
  border: 1px solid #e9ecef;
  background: #fff;
  color: #32325d;
  font-size: 0.85rem;
  cursor: pointer;
}
.modern-select.highlight {
  border-color: #1171ef44;
  color: black;
  font-weight: 700;
}

/* 重置按鈕 */
.reset-link {
  background: none;
  border: none;
  color: #145369;
  font-size: 0.7rem;
  font-weight: 700;
  text-decoration: underline;
  padding: 0;
}

/* 底部數據卡 */
.stats-card {
  background: linear-gradient(310deg, #11cdef, #1171ef);
  margin-top: 10px;
}

/* 側邊欄捲軸隱藏 */
.custom-scrollbar {
  max-height: calc(100vh - 120px);
  overflow-y: auto;
}
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: #eee;
  border-radius: 10px;
}
</style>
