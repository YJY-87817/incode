<template>
  <section class="forum-list container py-5">
    <ForumSearchBar @search="onSearch" />
    <div class="row mt-4">
      <div
        class="col-md-6 col-lg-4 mb-4"
        v-for="topic in paginatedTopics"
        :key="topic.id"
      >
        <div class="card h-100 shadow-sm">
          <div class="card-body d-flex flex-column">
            <h5 class="card-title">{{ topic.title }}</h5>
            <p class="card-text text-truncate flex-grow-1">
              {{ topic.excerpt }}
            </p>
            <div class="mt-3 d-flex justify-content-between align-items-center">
              <small class="text-muted">by {{ topic.author }}</small>
              <router-link
                :to="{ name: 'thread-detail', params: { id: topic.id } }"
                class="btn btn-sm btn-primary"
                >View</router-link
              >
            </div>
          </div>
        </div>
      </div>
    </div>

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
  </section>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import ForumSearchBar from "@/components/forum/ForumSearchBar.vue";
import apiClient from "@/api/request.js";

const topics = ref([]);

const fetchTopics = async () => {
  try {
    const data = await apiClient.get("/ContentApi");
    topics.value = data.items.map((item) => ({
      id: item.contentId,
      title: item.title,
      excerpt: item.body,
      author: item.username,
    }));
  } catch (error) {
    console.error("Failed to fetch topics:", error);
  }
};

onMounted(() => {
  fetchTopics();
});

const searchQuery = ref("");

const filteredTopics = computed(() => {
  if (!searchQuery.value) return topics.value;
  const q = searchQuery.value.toLowerCase();
  return topics.value.filter(
    (t) =>
      t.title.toLowerCase().includes(q) ||
      t.excerpt.toLowerCase().includes(q) ||
      t.author.toLowerCase().includes(q)
  );
});

const itemsPerPage = 9;
const currentPage = ref(1);
const totalPages = computed(() =>
  Math.ceil(filteredTopics.value.length / itemsPerPage)
);

const paginatedTopics = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage;
  return filteredTopics.value.slice(start, start + itemsPerPage);
});

function onSearch(query) {
  searchQuery.value = query;
  currentPage.value = 1;
}

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
</script>

<style scoped>
.forum-list .card {
  transition: transform 0.2s;
}
.forum-list .card:hover {
  transform: translateY(-3px);
}
</style>
