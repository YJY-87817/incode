<!-- forum.vue -->
<!-- 
  論壇主頁 (Pinia版)
  職責: 整合所有論壇子組件，使用 Pinia 統一管理狀態
  不使用 props/emit，所有狀態和方法由 forumStore 提供
-->

<script setup>
import { onMounted, onUnmounted, watch } from "vue";
import { useForumStore } from "@/stores/forumStore";
import ForumHeader from "@/components/Contents/Forum/ForumHeader.vue";
import CategoryFilter from "@/components/Contents/Forum/CategoryFilter.vue";
import NewTopicForm from "@/components/Contents/Forum/NewTopicForm_new.vue";
import TopicListView from "@/components/Contents/Forum/TopicListView_new.vue";

const forumStore = useForumStore();

/**
 * 初始化論壇
 */
onMounted(async () => {
  // 更新視窗寬度
  forumStore.updateWindowWidth(window.innerWidth);
  window.addEventListener("resize", handleResize);

  // 載入分類和主題
  await forumStore.fetchCategories();
  await forumStore.fetchTopics();
});

/**
 * 清理事件監聽
 */
onUnmounted(() => {
  window.removeEventListener("resize", handleResize);
});

/**
 * 視窗大小變更處理
 */
const handleResize = () => {
  forumStore.updateWindowWidth(window.innerWidth);
};

/**
 * 監聽刷新觸發
 */
watch(
  () => forumStore.lastRefreshTime,
  async (newVal) => {
    if (newVal) {
      await forumStore.fetchTopics();
    }
  }
);
</script>

<template>
  <div class="forum-page container-fluid px-3 px-md-4 py-4">
    <!-- 論壇標題 -->
    <ForumHeader />

    <!-- 新主題表單 -->
    <Transition name="slide-fade">
      <NewTopicForm v-if="forumStore.showNewTopicBox" />
    </Transition>

    <!-- 分類篩選 -->
    <CategoryFilter />

    <!-- 主題列表/詳情視圖 -->
    <TopicListView />
  </div>
</template>

<style scoped>
/* 論壇頁面容器 */
.forum-page {
  max-width: 1200px;
  margin: 0 auto;
  background-color: #f8f9fa;
  min-height: 100vh;
}

/* 表單展開/收起動畫 */
.slide-fade-enter-active {
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.slide-fade-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 1, 1);
}

.slide-fade-enter-from {
  transform: translateY(-20px);
  opacity: 0;
}

.slide-fade-leave-to {
  transform: translateY(-10px);
  opacity: 0;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .forum-page {
    padding: 1rem !important;
  }
}
</style>
