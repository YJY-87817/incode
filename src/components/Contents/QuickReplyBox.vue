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
          {{ isSubmitting ? "送出中..." : "送出簡易留言" }}
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
const props = defineProps({
  modelValue: {
    type: String,
    required: true,
  },
  isSubmitting: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  "update:modelValue",
  "submit",
  "cancel",
  "textarea-input",
]);

function handleTextareaEvent(event) {
  emit("textarea-input", event);
}

function handleInput(event) {
  emit("update:modelValue", event.target.value);
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
