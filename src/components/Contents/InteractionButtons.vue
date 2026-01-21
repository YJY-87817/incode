<template>
  <div class="interaction-buttons d-flex align-items-center gap-2 mt-2">
    <button
      v-for="cat in categories"
      :key="cat.id"
      class="btn btn-sm"
      :class="buttonClass(cat.name)"
      @click="handleClick(cat.id, cat.name)"
      :title="cat.name"
    >
      <i :class="iconClass(cat.name)"></i>
      <span class="ms-1">{{ cat.name }}</span>
    </button>
  </div>
</template>

<script setup>
import { ref } from "vue";
import apiClient from "@/api/request";

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

/**
 * Interaction categories.
 * In a real project these would be fetched from the backend
 * (e.g. /InteractionCategoryApi). For the purpose of the UI
 * we hard‑code the known categories.
 */
const categories = ref([
  { id: 1, name: "like" },
  { id: 3, name: "bookmark" },
  { id: 4, name: "block" },
  { id: 5, name: "hide" }, // 隱藏自己的內容
]);

/**
 * Returns a Bootstrap button class based on the category name.
 */
function buttonClass(name) {
  switch (name) {
    case "like":
      return "btn-outline-primary";

    case "bookmark":
      return "btn-outline-success";
    case "block":
      return "btn-outline-warning";
    case "hide":
      return "btn-outline-secondary";
    default:
      return "btn-outline-light";
  }
}

/**
 * Returns an icon class (Font Awesome) based on the category name.
 */
function iconClass(name) {
  switch (name) {
    case "like":
      return "fas fa-thumbs-up";

    case "bookmark":
      return "fas fa-bookmark";
    case "block":
      return "fas fa-ban";
    case "hide":
      return "fas fa-eye-slash";
    default:
      return "";
  }
}

/**
 * Sends the interaction to the backend.
 */
async function handleClick(categoryId, categoryName) {
  try {
    await apiClient.post("/ContentInteractionApi", {
      category_id: categoryId,
      content_id: props.contentId,
      // user_id is taken from the auth token on the server side,
      // so we don't need to send it from the client.
    });
    // Simple UI feedback – you can replace this with a toast or
    // a more sophisticated state update.
    alert(`${categoryName} 已送出`);
  } catch (error) {
    console.error("互動失敗:", error);
    alert("操作失敗，請稍後再試");
  }
}
</script>

<style scoped>
.interaction-buttons .btn {
  display: flex;
  align-items: center;
}
</style>
