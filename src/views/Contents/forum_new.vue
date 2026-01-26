<!-- 
  forum.vue - 論壇主頁面 (Pinia架構)
  
  職責：初始化和協調組件，所有狀態統一由 Pinia Store 管理
  不使用 props/emit，組件間透過 store 通訊
-->

<script setup>
import { onMounted, onUnmounted } from "vue";
import { useForumStore } from "@/stores/forumStore";

// 組件導入
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import ForumHeader from "@/components/Contents/Forum/ForumHeader.vue";
import CategoryFilter from "@/components/Contents/Forum/CategoryFilter.vue";
import NewTopicForm from "@/components/Contents/Forum/NewTopicForm.vue";
import TopicListView from "@/components/Contents/Forum/TopicListView.vue";

const forumStore = useForumStore();

/**
 * 更新視窗寬度
 */
const handleResize = () => {
  forumStore.updateWindowWidth(window.innerWidth);
};

/**
 * 初始化
 */
onMounted(() => {
  window.addEventListener("resize", handleResize);
  forumStore.fetchCategories();
  forumStore.fetchTopics();
});

/**
 * 清理
 */
onUnmounted(() => {
  window.removeEventListener("resize", handleResize);
});
</script>

<template>
  <DefaultNavbar light />

  <div class="container mt-5 mb-5">
    <!-- 論壇標題 -->
    <ForumHeader />

    <!-- 新主題表單 -->
    <NewTopicForm v-if="forumStore.showNewTopicBox" />

    <div class="row g-3 g-md-4">
      <!-- 主要內容區 -->
      <div class="col-lg-9 order-2 order-lg-1">
        <!-- 分類篩選 -->
        <CategoryFilter />

        <!-- 主題列表/詳情 -->
        <TopicListView />
      </div>

      <!-- 側邊欄 -->
      <div class="col-lg-3">
        <div class="card shadow-sm mb-4">
          <div class="card-header bg-white">
            <h6 class="mb-0">📊 論壇統計</h6>
          </div>
          <div class="card-body">
            <div class="mb-3">
              <div
                class="d-flex justify-content-between align-items-center mb-1"
              >
                <span class="small text-muted">總主題數</span>
                <strong class="text-primary">{{
                  forumStore.pagination.totalCount
                }}</strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <DefaultFooter />
</template>

<style scoped>
/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

.card-header {
  background-color: #fff0dd !important;
  border-bottom-color: #d1d3d4;
}

.card-header h6 {
  color: #432f28;
}

/* 文字顏色 */
.text-primary {
  color: #432f28 !important;
}
</style>
