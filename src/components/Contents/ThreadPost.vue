<template>
  <div class="card mb-3 shadow-sm">
    <div class="card-body">
      <div class="d-flex justify-content-between align-items-start">
        <div class="flex-grow-1">
          <div class="d-flex align-items-center gap-2 mb-1">
            <h6 class="card-title mb-0">{{ post.author }}</h6>
            <span
              class="badge"
              :class="post.type === 2 ? 'bg-primary' : 'bg-secondary'"
            >
              {{ post.type === 2 ? "回文" : "留言" }}
            </span>
          </div>
          <small class="text-muted">{{ post.createdAt }}</small>
        </div>
        <button class="btn btn-sm btn-outline-secondary" @click="toggleQuote">
          Quote
        </button>
      </div>
      <p class="card-text mt-2">{{ post.content }}</p>
      <div v-if="showQuote" class="mt-2">
        <textarea
          v-model="quoteContent"
          class="form-control"
          rows="2"
          placeholder="Add your comment..."
        ></textarea>
        <button class="btn btn-primary btn-sm mt-1" @click="submitQuote">
          Submit Quote
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
});

const showQuote = ref(false);
const quoteContent = ref("");

function toggleQuote() {
  showQuote.value = !showQuote.value;
}

function submitQuote() {
  // Placeholder: In a real app, you'd send this to the backend.
  console.log("Quote submitted:", {
    postId: props.post.id,
    content: quoteContent.value,
  });
  quoteContent.value = "";
  showQuote.value = false;
}
</script>

<style scoped>
.card-title {
  font-weight: 600;
}
</style>
