<template>
  <transition name="fade">
    <div class="quick-reply-box">
      <textarea
        :value="modelValue"
        @input="handleInput"
        @focus="handleTextareaEvent"
        class="form-control"
        rows="1"
        placeholder="請輸入簡易留言（純文字）"
      ></textarea>

      <div class="d-flex flex-wrap gap-2 quick-reply-actions">
        <button
          class="btn btn-forum-primary btn-sm"
          type="button"
          @click="emit('submit')"
          :disabled="isSubmitting || !modelValue.trim()"
        >
          <i class="fas fa-paper-plane me-1"></i>
          {{ isSubmitting ? "送出中..." : "發佈" }}
        </button>
        <button
          class="btn btn-forum-secondary btn-sm"
          type="button"
          @click="emit('cancel')"
          :disabled="isSubmitting"
        >
          取消
        </button>
      </div>
    </div>
  </transition>
</template>

<script setup>
import { computed } from "vue";
import { useForumStore } from "@/stores/forumStore";

const props = defineProps({
  contentId: {
    type: Number,
    required: true,
  },
});

const emit = defineEmits(["submit", "cancel", "textarea-input"]);

const forumStore = useForumStore();

// 初始化快速回覆狀態
forumStore.initQuickReply(props.contentId);

// 從 Pinia store 獲取數據
const modelValue = computed({
  get: () => forumStore.getQuickReplyContent(props.contentId),
  set: (value) => forumStore.setQuickReplyContent(props.contentId, value),
});

const isSubmitting = computed(() =>
  forumStore.getQuickReplySubmitting(props.contentId),
);

function handleTextareaEvent(event) {
  emit("textarea-input", event);
}

function handleInput(event) {
  modelValue.value = event.target.value;
  handleTextareaEvent(event);
}
</script>

<style scoped>
.quick-reply-box textarea.form-control {
  resize: none;
  overflow: hidden;
  min-height: 38px;
  line-height: 1.4;
  transition: height 0.12s ease;
}
</style>
