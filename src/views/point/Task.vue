<!-- src/views/Task.vue -->
<script setup>
import { ref, onMounted } from "vue";
import { useTaskStore } from "@/stores/point/UseTaskStore";

// 引入所有子元件
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";
import FooterCentered from "@/examples/footers/FooterCentered.vue";

import TaskTabs from "@/components/task/TaskTab.vue";
import TaskSearchFilterBar from "@/components/task/TaskSearchFilterBar.vue";
import TaskStatsCards from "@/components/task/TaskStatsCard.vue";
import MissionList from "@/components/task/MissionList.vue";
import EntrustMissionTable from "@/components/task/EntrustMissionTable.vue";
import TaskDetailDrawer from "@/components/task/TaskDetailDrawer.vue";

// Pinia Store
const store = useTaskStore();

// 抽屜狀態
const isDrawerOpen = ref(false);
const selectedTask = ref(null);

// tab 切換
const activeTab = ref("mission");

const setActiveTab = (tab) => {
  activeTab.value = tab;
  store.setActiveTab(tab);
};

// 開啟詳情抽屜
const openDetail = (task) => {
  selectedTask.value = task;
  isDrawerOpen.value = true;
};

// 領取/接取任務
const handleClaim = (title) => {
  alert(`已成功接取：${title}\n請至「個人中心 > 我的任務」查看進度。`);
  isDrawerOpen.value = false;

  // 更新本地計數（未來可改成 API）
  if (selectedTask.value) {
    if (activeTab.value === "mission") {
      store.claimMission(selectedTask.value.id);
    } else {
      store.applyEntrust(selectedTask.value.id);
    }
  }
};

// 頁面載入時自動取資料
onMounted(() => {
  store.fetchTasks();
});
</script>

<template>
  <NavbarDefault style="background-color: #fff0dd" />

  <div class="bg-gray-100 min-vh-100 pb-5">
    <!-- 頁頭 -->
    <header class="position-relative">
      <div
        class="page-header min-height-250 border-radius-xl mt-4 mx-5 shadow-lg"
        style="
          background-image: url(&quot;https://images.unsplash.com/photo-1478720568477-152d9b164e26?auto=format&fit=crop&w=1920&q=80&quot;);
        "
      >
        <span class="mask bg-gradient-dark opacity-7"></span>
        <div class="container text-center z-index-2">
          <h2 class="text-white font-weight-bolder">任務大廳</h2>
          <p class="text-white opacity-8">
            透過貢獻與互助，累積點數經驗值換取影城商品與提升等級！
          </p>
        </div>
      </div>
    </header>

    <div class="container mt-2">
      <div class="row">
        <div class="col-lg-11 mx-auto">
          <!-- Tab 導航 -->
          <TaskTabs
            :active-tab="store.activeTab"
            @change="store.setActiveTab"
          />

          <!-- 搜尋、過濾、類別、清除篩選 -->
          <TaskSearchFilterBar />

          <!-- 統計卡片 -->
          <TaskStatsCards />

          <!-- 加載中 -->
          <div v-if="store.isLoading" class="text-center my-5">
            <div class="spinner-border text-info" role="status">
              <span class="visually-hidden">載入中...</span>
            </div>
            <p class="text-sm text-secondary mt-2">正在載入任務數據...</p>
          </div>

          <!-- 任務內容 -->
          <div v-else class="row">
            <!-- 平台任務卡片列表 -->
            <div v-if="activeTab === 'mission'">
              <MissionList @open-detail="openDetail" @claim="handleClaim" />
            </div>

            <!-- 委託任務表格 -->
            <EntrustMissionTable
              v-else
              @open-detail="openDetail"
              @claim="handleClaim"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- 任務詳情側邊欄 -->
    <TaskDetailDrawer
      v-model:open="isDrawerOpen"
      :task="selectedTask"
      :tab="activeTab"
      @claim="handleClaim"
    />

    <div
      v-if="isDrawerOpen"
      @click="isDrawerOpen = false"
      class="drawer-backdrop"
    ></div>
  </div>

  <FooterCentered />
</template>

<style scoped>
/* 全局樣式（可移到全局 CSS 或 scoped 保留） */
.transition-all {
  transition: all 0.25s ease-in-out;
}
.cursor-pointer {
  cursor: pointer;
}
.mission-card {
  transition: all 0.3s;
  border-top: 4px solid transparent;
}
.mission-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 15px 30px -5px rgba(0, 0, 0, 0.1) !important;
  border-top-color: #11cdef;
}
.commission-row {
  transition: background-color 0.2s;
}
.commission-row:hover {
  background-color: rgba(17, 205, 239, 0.04);
}
.detail-drawer {
  position: fixed;
  top: 0;
  right: -450px;
  width: 450px;
  height: 100vh;
  z-index: 1100;
  transition: right 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  overflow-y: auto;
}
.detail-drawer.open {
  right: 0;
}
.drawer-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(2px);
  z-index: 1090;
}
.scrollbar-hidden::-webkit-scrollbar {
  display: none;
}
.scrollbar-hidden {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
@media (max-width: 576px) {
  .detail-drawer {
    width: 100%;
    right: -100%;
  }
}
</style>
