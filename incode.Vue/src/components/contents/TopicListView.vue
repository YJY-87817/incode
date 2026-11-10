<!-- TopicListView.vue -->
<!--
  主題列表視圖的外層：卡片外框、量測卡片寬度、列表換頁
  詳情視圖在 ThreadDetailView.vue，列表視圖在 TopicList.vue；兩者都使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { computed, provide } from "vue";
import ThreadDetailView from "@/components/contents/ThreadDetailView.vue";
import TopicList from "@/components/contents/TopicList.vue";
import ForumPaginationCard from "@/components/contents/ForumPaginationCard.vue";
import { useForumStore } from "@/stores/forumStore.js";
import { useForumNavigation } from "@/composables/useForumNavigation.js";
import { useForumInteractionSync } from "@/composables/useForumInteractionSync.js";
import {
  TOPIC_LIST_LAYOUT_KEY,
  useTopicListLayout,
} from "@/composables/useTopicListLayout.js";

const forumStore = useForumStore();
const { goToPage } = useForumNavigation();

// 卡片寬度決定列表用手機卡片還是桌機表格；列表元件以 inject 取得
const topicListLayout = useTopicListLayout();
provide(TOPIC_LIST_LAYOUT_KEY, topicListLayout);
const { topicListCardRef, topicListResponsiveClasses } = topicListLayout;

useForumInteractionSync();

/**
 * 分頁相關計算
 */
const displayPages = computed(() => forumStore.displayPages);
</script>

<template>
  <div
    ref="topicListCardRef"
    class="card shadow-sm"
    :class="topicListResponsiveClasses"
  >
    <!-- 卡片內容 -->
    <div class="card-body p-0">
      <!-- 詳情視圖 -->
      <ThreadDetailView v-if="forumStore.selectedThread" />

      <!-- 列表視圖 -->
      <TopicList v-else />
    </div>

    <!-- 分頁導航 (列表視圖) -->
    <div
      v-if="!forumStore.selectedThread && forumStore.pagination.totalPages > 1"
      class="card-footer bg-white pt-0 pb-2 pb-md-3 px-0"
    >
      <ForumPaginationCard
        :current-page="forumStore.pagination.currentPage"
        :total-pages="forumStore.pagination.totalPages"
        :pages="displayPages"
        :show-edge-buttons="true"
        :show-jump="true"
        @page-change="goToPage"
      />
    </div>
  </div>
</template>

<style scoped>
/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

.card-footer {
  border-top-color: #d1d3d4;
}

.card-body {
  margin-bottom: 0;
  padding-bottom: 0;
}
</style>
