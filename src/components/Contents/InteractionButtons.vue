<script setup>
import { ref, computed, onMounted, watch } from "vue";
import Swal from "sweetalert2";
import contentInteractionApi from "@/api/contentInteractionApi";
import contentApi from "@/api/contentApi";
import { useAuthStore } from "@/stores/user/auth";
import { useForumStore } from "@/stores/forumStore";

/**
 * Props
 *  - contentId: the ID of the content (post) to interact with
 *  - authorUserId: the user ID of the content author
 */
const props = defineProps({
  contentId: {
    type: Number,
    required: true,
  },
  authorUserId: {
    type: Number,
    default: null,
  },
});

const authStore = useAuthStore();
const forumStore = useForumStore();

// 初始化當前內容的互動狀態
watch(
  () => props.contentId,
  (newId) => {
    if (newId) {
      forumStore.setCurrentInteractionContent(newId, props.authorUserId);
    }
  },
  { immediate: true },
);

// 檢查是否為當前使用者的內容
const isOwnContent = computed(() => {
  return (
    authStore.user?.id &&
    props.authorUserId &&
    authStore.user.id === props.authorUserId
  );
});

onMounted(() => {});

const emit = defineEmits([
  "interaction-success",
  "interaction-error",
  "edit-content",
  "delete-content",
]);

/**
 * Interaction categories with UI details.
 */
const categories = ref([
  { id: 1, name: "like", label: "讚", icon: "fas fa-thumbs-up" },
  { id: 3, name: "bookmark", label: "收藏", icon: "fas fa-bookmark" },
]);

// 從 Pinia store 獲取互動狀態
const interactionStates = computed(() => {
  return (
    forumStore.interactionStates[props.contentId] || {
      1: false,
      2: false,
      3: false,
      4: false,
    }
  );
});

const isLoading = computed(() => {
  return (
    forumStore.interactionLoadingStates[props.contentId] || {
      1: false,
      2: false,
      3: false,
      4: false,
    }
  );
});

/**
 * Sends the interaction to the backend.
 */
async function handleInteraction(categoryId, categoryName) {
  if (isLoading.value[categoryId]) return;

  const result = await forumStore.handleInteraction(
    props.contentId,
    categoryId,
  );

  if (result.success) {
    emit("interaction-success", {
      contentId: props.contentId,
      categoryId,
      categoryName,
      isRemoved: result.isRemoved,
      message: result.message,
    });
  } else {
    emit("interaction-error", {
      contentId: props.contentId,
      categoryId,
      categoryName,
      error: result.error || "操作失敗，請稍後再試",
    });
  }
}

/**
 * 取得按鈕樣式類別
 */
function getButtonClass(categoryId) {
  // "隱藏"按鈕(categoryId=2)不顯示激活狀態，因為被隱藏的內容不會在列表中顯示
  if (categoryId === 2) {
    return "btn-interaction";
  }
  const isActive = interactionStates.value[categoryId];
  return isActive ? "btn-interaction-active" : "btn-interaction";
}

/**
 * 更多選單的顯示狀態
 */
const showMoreMenu = computed(() => {
  return forumStore.isMoreMenuOpen(props.contentId);
});

/**
 * 切換更多選單
 */
function toggleMoreMenu() {
  forumStore.toggleMoreMenu(props.contentId);
}

/**
 * 處理編輯操作
 */
function handleEdit() {
  forumStore.closeMoreMenu();
  emit("edit-content", props.contentId);
}

/**
 * 處理刪除操作
 */
async function handleDelete() {
  forumStore.closeMoreMenu();

  const result = await Swal.fire({
    title: "確定要刪除嗎？",
    text: "刪除後無法復原",
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#d33",
    cancelButtonColor: "#3085d6",
    confirmButtonText: "刪除",
    cancelButtonText: "取消",
  });

  if (!result.isConfirmed) {
    return;
  }

  try {
    await contentApi.deleteContent(props.contentId);
    emit("delete-content", props.contentId);
    emit("interaction-success", {
      categoryId: null,
      categoryName: "delete",
      message: "內容已刪除",
    });
  } catch (error) {
    console.error("刪除失敗:", error);
    emit("interaction-error", {
      categoryId: null,
      categoryName: "delete",
      error: error.message || "刪除失敗，請稍後再試",
    });
  }
}

/**
 * 點擊外部關閉選單
 */
function handleClickOutside(event) {
  // 如果點擊在當前 contentId 的菜單外部，則關閉這個菜單
  if (forumStore.isMoreMenuOpen(props.contentId)) {
    const moreMenu = document.querySelector(".more-menu-container");
    if (moreMenu && !moreMenu.contains(event.target)) {
      forumStore.closeMoreMenu();
    }
  }
}

onMounted(() => {
  document.addEventListener("click", handleClickOutside);
});
</script>

<template>
  <div
    class="interaction-buttons d-inline-flex align-items-center gap-2"
    :class="{ 'more-menu-open': showMoreMenu }"
  >
    <button
      v-for="cat in categories"
      :key="cat.id"
      class="btn btn-sm btn-interaction-button"
      :class="getButtonClass(cat.id)"
      @click="handleInteraction(cat.id, cat.name)"
      :disabled="isLoading[cat.id]"
      :title="cat.label"
      type="button"
    >
      <i :class="[cat.icon, { 'fa-spin': isLoading[cat.id] }]"></i>
      <span class="ms-1">{{ cat.label }}</span>
    </button>

    <!-- 更多選單 -->
    <div class="more-menu-container position-relative">
      <button
        class="btn btn-sm btn-interaction-button btn-interaction"
        @click.stop="toggleMoreMenu"
        type="button"
        title="更多"
      >
        <i class="fas fa-ellipsis-h"></i>
        <span class="ms-1">更多</span>
      </button>

      <!-- 下拉選單 -->
      <div v-if="showMoreMenu" class="more-menu-dropdown" @click.stop>
        <button
          class="more-menu-item"
          @click="handleInteraction(2, 'hide')"
          :disabled="isLoading[2]"
          type="button"
        >
          <i
            :class="['fas fa-eye-slash me-2', { 'fa-spin': isLoading[2] }]"
          ></i>
          隱藏
        </button>
        <button
          class="more-menu-item"
          @click="handleInteraction(4, 'block')"
          :disabled="isLoading[4]"
          type="button"
        >
          <i :class="['fas fa-ban me-2', { 'fa-spin': isLoading[4] }]"></i>
          檢舉
        </button>
        <button
          v-if="isOwnContent"
          class="more-menu-item"
          @click="handleEdit"
          type="button"
        >
          <i class="fas fa-edit me-2"></i>
          編輯
        </button>
        <button
          v-if="isOwnContent"
          class="more-menu-item more-menu-item-danger"
          @click="handleDelete"
          type="button"
        >
          <i class="fas fa-trash-alt me-2"></i>
          刪除
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 互動按鈕容器 */
.interaction-buttons {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem;
  position: relative;
  z-index: 20;
}

.interaction-buttons.more-menu-open {
  z-index: 3000;
}

/* 基礎互動按鈕樣式 */
.btn-interaction-button {
  padding: 0.2rem 0.45rem;
  font-size: 0.75rem;
  line-height: 1;
  height: 28px;
  min-height: 28px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.btn-interaction-button i {
  font-size: 0.8rem;
}

/* 未激活狀態 */
.btn-interaction {
  background-color: transparent;
  border: 1px solid #86b0bd;
  color: #145369;
}

.btn-interaction:hover:not(:disabled) {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

/* 激活狀態 */
.btn-interaction-active {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

.btn-interaction-active:hover:not(:disabled) {
  background-color: #6b96a8;
  border-color: #6b96a8;
}

/* 禁用狀態 */
.btn-interaction-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* 動畫 */
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.fa-spin {
  animation: spin 1s linear infinite;
}

/* 更多選單容器 */
.more-menu-container {
  position: relative;
  z-index: 2100;
}

/* 下拉選單 */
.more-menu-dropdown {
  position: absolute;
  top: calc(100% - 0.7rem);
  right: 0;
  margin-top: 0;
  background-color: #fff;
  border: 1px solid #dee2e6;
  border-radius: 0.375rem;
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
  z-index: 2200;
  min-width: 120px;
  overflow: hidden;
  opacity: 1;
  pointer-events: auto;
}

/* 選單項目 */
.more-menu-item {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 0.35rem 1rem;
  border: none;
  background: white;
  color: #145369;
  font-size: 0.875rem;
  text-align: left;
  cursor: pointer;
  transition: all 0.2s ease;
}

.more-menu-item:hover {
  background-color: #f8f9fa;
  color: #86b0bd;
}

.more-menu-item-danger {
  color: #dc3545;
}

.more-menu-item-danger:hover {
  background-color: #ffe6e6;
  color: #c82333;
}

.more-menu-item i {
  width: 1rem;
  text-align: center;
}
</style>
