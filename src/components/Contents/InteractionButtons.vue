<script setup>
import { ref, onMounted } from "vue";
import contentInteractionApi from "@/api/contentInteractionApi";

/**
 * Props
 *  - contentId: the ID of the content (post) to interact with
 */
const props = defineProps({
  contentId: {
    type: Number,
    required: true,
  },
});

onMounted(() => {
  console.log("InteractionButtons mounted for contentId:", props.contentId);
});

const emit = defineEmits(["interaction-success", "interaction-error"]);

/**
 * Interaction categories with UI details.
 */
const categories = ref([
  { id: 1, name: "like", label: "讚", icon: "fas fa-thumbs-up" },
  { id: 3, name: "bookmark", label: "收藏", icon: "fas fa-bookmark" },
  { id: 4, name: "block", label: "檢舉", icon: "fas fa-ban" },
  { id: 5, name: "hide", label: "隱藏", icon: "fas fa-eye-slash" },
]);

// 追蹤互動狀態
const interactionStates = ref({
  1: false,
  3: false,
  4: false,
  5: false,
});

const isLoading = ref({
  1: false,
  3: false,
  4: false,
  5: false,
});

/**
 * Sends the interaction to the backend.
 */
async function handleInteraction(categoryId, categoryName) {
  if (isLoading.value[categoryId]) return;

  isLoading.value[categoryId] = true;

  try {
    const response = await contentInteractionApi.postInteraction({
      interactionCategoryId: categoryId,
      contentId: props.contentId,
    });

    if (response.isRemoved) {
      // 移除互動
      interactionStates.value[categoryId] = false;
      emit("interaction-success", {
        categoryId,
        categoryName,
        isRemoved: true,
        message: "互動已移除",
      });
    } else {
      // 新增或更新互動
      interactionStates.value[categoryId] = true;
      emit("interaction-success", {
        categoryId,
        categoryName,
        isRemoved: false,
        message: `已${categoryName}`,
      });
    }
  } catch (error) {
    console.error("互動失敗:", error);
    emit("interaction-error", {
      categoryId,
      categoryName,
      error: error.message || "操作失敗，請稍後再試",
    });
  } finally {
    isLoading.value[categoryId] = false;
  }
}

/**
 * 取得按鈕樣式類別
 */
function getButtonClass(categoryId) {
  const isActive = interactionStates.value[categoryId];
  return isActive ? "btn-interaction-active" : "btn-interaction";
}
</script>

<template>
  <div class="interaction-buttons d-inline-flex align-items-center gap-2">
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
  </div>
</template>

<style scoped>
/* 互動按鈕容器 */
.interaction-buttons {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
}

/* 基礎互動按鈕樣式 */
.btn-interaction-button {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
  line-height: 1.25;
  height: fit-content;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  white-space: nowrap;
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
</style>
