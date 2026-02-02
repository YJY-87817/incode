<script setup>
import { ref, onMounted } from "vue";
import { useTaskStore } from "@/stores/point/UseTaskStore";

// 引入所有子元件
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
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

const isClaiming = ref(false); // 用來控制「處理中」狀態

// 開啟詳情抽屜
const openDetail = (task) => {
  selectedTask.value = task;
  isDrawerOpen.value = true;
};

// 領取/接取任務
const handleClaim = async (payload) => {
  if (!payload?.type || (!payload?.mission && !payload?.commission)) {
    console.warn("無效的 claim payload", payload);
    return;
  }

  const task = payload.mission || payload.commission;
  if (!task?.id) {
    alert("無法識別任務 ID");
    return;
  }

  // 樂觀更新：先標記為已領取
  const originalClaimed = task.claimed;
  task.claimed = true;

  // 如果你想在抽屜內顯示 loading，可 emit 一個事件給抽屜
  // 或直接在父層控制
  isClaiming.value = true;

  try {
    await store.claimTask(payload.type, task.id);

    // 成功：顯示成功提示（建議改用 toast）
    // alert(`已成功${payload.type === "platform" ? "領取" : "接取"}：${task.title}`);

    // 更好的做法：使用 toast（假設你有安裝 vue-toastification）
    // toast.success(`已成功${payload.type === "platform" ? "領取" : "接取"}：${task.title}`);

    // 成功後才重新載入（確保最新資料）
    await store.fetchTasks();

    // 關閉抽屜
    isDrawerOpen.value = false;
  } catch (err) {
    // 失敗：回滾樂觀更新
    task.claimed = originalClaimed;

    // 顯示錯誤
    const errorMsg =
      err.response?.data?.message || err.message || "操作失敗，請稍後再試";
    alert(errorMsg); // 或 toast.error(errorMsg)

    // 可選擇不關閉抽屜，讓使用者看到錯誤後再試
    // isDrawerOpen.value = false;  ← 註解掉這行
  } finally {
    isClaiming.value = false;
  }
};

// 頁面載入時自動取資料
onMounted(() => {
  if (store.missions.length === 0 && store.entrustMissions.length === 0) {
    store.fetchTasks();
  }
});
</script>

<template>
<DefaultNavbar light />

  <div class="bg-gray-100 min-vh-100 pb-5">
    <div class="container mt-2">
      <div class="row">
        <div class="filter-container col-lg-11 mx-auto">
          <!-- Tab 導航 -->
          <TaskTabs :active-tab="store.activeTab" @change="store.setActiveTab">
            <!-- 搜尋、過濾、類別、清除篩選 -->
            <template #bottom-content>
              <TaskSearchFilterBar />
            </template>
          </TaskTabs>

          <!-- 統計卡片 -->
          <TaskStatsCards />

          <!-- 錯誤顯示 -->
          <div v-if="store.error" class="alert alert-danger text-center my-5">
            {{ store.error }}
            <button
              @click="store.fetchTasks"
              class="btn btn-outline-danger btn-sm mt-2"
            >
              重試
            </button>
          </div>

          <!-- 任務內容 -->
          <div v-else class="row">
            <!-- 平台任務卡片列表 -->
            <div v-if="store.activeTab === 'mission'">
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
      :tab="store.activeTab"
      :is-claiming="isClaiming"
      @claim="handleClaim"
    />

    <div
      v-if="isDrawerOpen"
      @click.self="isDrawerOpen = false"
      class="drawer-backdrop"
    ></div>
  </div>

  <FooterCentered />
</template>

<style scoped>
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
