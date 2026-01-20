<template>
  <section class="thread-detail container py-5">
    <div class="card mb-4 shadow-sm">
      <div class="card-body">
        <h3 class="card-title">{{ thread.title }}</h3>
        <p class="text-muted mb-2">
          by {{ thread.author }} · {{ thread.createdAt }}
        </p>
        <p class="card-text">{{ thread.content }}</p>
      </div>
    </div>

    <ThreadPost v-for="post in paginatedPosts" :key="post.id" :post="post" />

    <nav v-if="totalPages > 1" class="mt-4">
      <ul class="pagination justify-content-center">
        <li
          class="page-item"
          :class="{ disabled: currentPage === 1 }"
          @click="prevPage"
        >
          <a class="page-link" href="javascript:void(0)">Previous</a>
        </li>
        <li
          class="page-item"
          v-for="page in totalPages"
          :key="page"
          :class="{ active: currentPage === page }"
          @click="goToPage(page)"
        >
          <a class="page-link" href="javascript:void(0)">{{ page }}</a>
        </li>
        <li
          class="page-item"
          :class="{ disabled: currentPage === totalPages }"
          @click="nextPage"
        >
          <a class="page-link" href="javascript:void(0)">Next</a>
        </li>
      </ul>
    </nav>

    <div class="mt-5">
      <h5>Reply</h5>
      <textarea
        v-model="newReply"
        class="form-control mb-2"
        rows="3"
        placeholder="Write your reply..."
      ></textarea>
      <button class="btn btn-primary" @click="submitReply">Post Reply</button>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import apiClient from "@/api/request.js";
import { useRoute } from "vue-router";
import ThreadPost from "@/components/forum/ThreadPost.vue";

// Mock thread data – replace with real API call later
const route = useRoute();
const threadId = Number(route.params.id);

const thread = ref({
  id: null,
  title: "",
  author: "",
  createdAt: "",
  content: "",
});

const allPosts = ref([]);

const fetchThread = async () => {
  try {
    const data = await apiClient.get("/ContentApi");
    const item = data.items.find((i) => i.contentId === threadId);
    if (item) {
      thread.value = {
        id: item.contentId,
        title: item.title,
        author: item.username,
        createdAt: item.createdAt,
        content: item.body,
      };
    }
    // Simple reply handling: treat other items as replies
    allPosts.value = data.items
      .filter((i) => i.contentId !== threadId)
      .map((i) => ({
        id: i.contentId,
        author: i.username,
        createdAt: i.createdAt,
        content: i.body,
      }));
  } catch (error) {
    console.error("Failed to fetch thread:", error);
  }
};

onMounted(() => {
  fetchThread();
});

const currentPage = ref(1);
const itemsPerPage = 5;
const totalPages = computed(() =>
  Math.ceil(allPosts.value.length / itemsPerPage)
);
const paginatedPosts = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage;
  return allPosts.value.slice(start, start + itemsPerPage);
});

function goToPage(page) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page;
  }
}
function prevPage() {
  if (currentPage.value > 1) currentPage.value--;
}
function nextPage() {
  if (currentPage.value < totalPages.value) currentPage.value++;
}

const newReply = ref("");
function submitReply() {
  if (!newReply.value.trim()) return;
  const newPost = {
    id: Date.now(),
    author: "CurrentUser",
    createdAt: new Date().toISOString().split("T")[0],
    content: newReply.value.trim(),
  };
  allPosts.value.unshift(newPost);
  newReply.value = "";
  currentPage.value = 1;
}
</script>

<style scoped>
.thread-detail .card-title {
  margin-bottom: 0.5rem;
}
.thread-detail textarea {
  resize: vertical;
}
</style>
