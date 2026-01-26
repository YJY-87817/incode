<!-- 
  論壇搜尋列組件 (Pinia版)
  職責: 提供搜尋功能，搜尋內容的 title 和 body
  使用 Pinia Store，不使用 props/emit
-->

<template>
  <div class="search-bar mb-4">
    <div class="input-group">
      <input
        type="text"
        class="form-control"
        placeholder="搜尋主題標題或內容…"
        v-model="searchInput"
        @input="onInput"
        @keyup.enter="onSearch"
      />
      <button
        v-if="searchInput"
        class="btn btn-outline-secondary"
        type="button"
        @click="clearSearch"
        title="清除搜尋"
      >
        <i class="fas fa-times"></i>
      </button>
      <button class="btn btn-forum-primary" type="button" @click="onSearch">
        <i class="fas fa-search"></i>
      </button>
    </div>
    <div v-if="forumStore.searchKeyword" class="search-info mt-2">
      <small class="text-muted">
        搜尋結果: "{{ forumStore.searchKeyword }}" (共
        {{ forumStore.pagination.totalCount }} 筆)
      </small>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from "vue";
import { useForumStore } from "@/stores/forumStore";

const forumStore = useForumStore();
const searchInput = ref(forumStore.searchKeyword || "");

let debounceTimer = null;

function onInput() {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    onSearch();
  }, 500);
}

function onSearch() {
  const keyword = searchInput.value.trim();
  forumStore.setSearchKeyword(keyword);
}

function clearSearch() {
  searchInput.value = "";
  forumStore.clearSearch();
}

// 同步 store 的 searchKeyword 到輸入框
watch(
  () => forumStore.searchKeyword,
  (newVal) => {
    if (!newVal && searchInput.value) {
      searchInput.value = "";
    }
  }
);
</script>

<style scoped>
.search-bar {
  width: 100%;
}

.input-group {
  border-radius: 8px;
  overflow: hidden;
}

.input-group > input.form-control,
.input-group > .btn {
  height: 44px;
}

.input-group > .btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0 12px;
}

.input-group i {
  line-height: 1;
}

input.form-control {
  border: 1px solid #d1d3d4;
  border-right: none;
}

input.form-control:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}

.btn-outline-secondary {
  border: 1px solid #d1d3d4;
  border-left: none;
  border-right: none;
  background-color: #fff;
  color: #6c757d;
}

.btn-outline-secondary:hover {
  background-color: #f8f9fa;
  color: #495057;
}

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
}

.search-info {
  padding-left: 0.5rem;
}

@media (max-width: 768px) {
  .search-bar {
    max-width: 100%;
  }
}
</style>
