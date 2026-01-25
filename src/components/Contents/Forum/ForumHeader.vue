<!-- ForumHeader.vue -->
<!-- 
  論壇標題與統計區塊組件 (Pinia版)
  職責: 顯示論壇標題、發表新主題按鈕
  使用 Pinia Store，不使用 props/emit
-->

<script setup>
import { useForumStore } from "@/stores/forumStore";
import ForumSearchBar from "@/components/Contents/ForumSearchBar.vue";

const forumStore = useForumStore();
</script>

<template>
  <div class="card shadow-sm mb-4">
    <div class="card-body">
      <div
        class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3 mb-3"
      >
        <!-- 論壇標題與說明 -->
        <div>
          <h2 class="mb-2 fs-3 fs-md-2">💬 技術論壇</h2>
          <p class="text-muted mb-0 small">分享知識、交流技術、共同成長</p>
        </div>

        <!-- 搜尋欄 -->
        <ForumSearchBar />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 按鈕樣式 (自訂色系) */
.btn-forum-primary {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #432f28;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(67, 47, 40, 0.3);
}

.btn-forum-primary:disabled {
  background-color: #d1d3d4;
  border-color: #d1d3d4;
  color: #fff;
  opacity: 0.6;
}

/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .fs-md-2 {
    font-size: 1.5rem;
  }
}
</style>
