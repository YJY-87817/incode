<script>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

export default {
  components: {
    DefaultNavbar,
    DefaultFooter,
  },
};
</script>

<template>
  <DefaultNavbar light />

  <section class="thread-detail container py-3 py-md-5 px-2 px-md-3">
    <div class="card mb-3 mb-md-4 shadow-sm">
      <div class="card-body p-2 p-md-3">
        <h3 class="card-title fs-4 fs-md-3 text-break">{{ thread.title }}</h3>
        <p class="text-muted mb-2 small d-flex flex-wrap gap-2">
          <span>by {{ thread.author }}</span>
          <span>·</span>
          <span>{{ thread.createdAt }}</span>
        </p>
        <p class="card-text" style="white-space: pre-wrap; word-break: break-word;">{{ thread.content }}</p>
      </div>
    </div>

    <ThreadPost v-for="post in paginatedPosts" :key="post.id" :post="post" />

    <nav v-if="totalPages > 1" class="mt-3 mt-md-4">
      <ul class="pagination pagination-sm justify-content-center flex-wrap gap-1">
        <li
          class="page-item"
          :class="{ disabled: currentPage === 1 }"
          @click="prevPage"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">
            <span class="d-none d-sm-inline"><i class="fa-solid fa-caret-left"></i></span>
            <span class="d-inline d-sm-none"><i class="fa-solid fa-caret-left"></i></span>
          </a>
        </li>
        <li
          class="page-item"
          v-for="page in totalPages"
          :key="page"
          :class="{ active: currentPage === page }"
          @click="goToPage(page)"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">{{ page }}</a>
        </li>
        <li
          class="page-item"
          :class="{ disabled: currentPage === totalPages }"
          @click="nextPage"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">
            <span class="d-none d-sm-inline"><i class="fa-solid fa-caret-right"></i></span>
            <span class="d-inline d-sm-none"><i class="fa-solid fa-caret-right"></i></span>
          </a>
        </li>
      </ul>
    </nav>

    <div class="mt-3 mt-md-5">
      <h5 class="fs-5 fs-md-4 mb-3">💬 回覆</h5>
      <textarea
        v-model="newReply"
        class="form-control mb-2"
        :rows="windowWidth < 768 ? 3 : 4"
        placeholder="撰寫您的回覆..."
      ></textarea>
      <button 
        class="btn btn-primary w-100 w-sm-auto" 
        @click="submitReply"
        :disabled="isSubmitting || !newReply.trim()"
      >
        <i class="fas fa-paper-plane me-2"></i>
        {{ isSubmitting ? '發送中...' : '發送回覆' }}
      </button>
    </div>
  </section>

  <DefaultFooter />
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from "vue";
import apiClient from "@/api/request.js";
import { useRoute } from "vue-router";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
// 視窗寬度響應式變數
const windowWidth = ref(window.innerWidth);

const updateWindowWidth = () => {
  windowWidth.value = window.innerWidth;
};
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
  window.addEventListener('resize', updateWindowWidth);
  fetchThread();
});

onUnmounted(() => {
  window.removeEventListener('resize', updateWindowWidth);
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
const isSubmitting = ref(false);

async function submitReply() {
  if (!newReply.value.trim()) {
    alert("請輸入回覆內容");
    return;
  }

  if (isSubmitting.value) return;
  isSubmitting.value = true;

  const dto = {
    UserId: 1, // TODO: 改成實際使用者 ID
    CategoryId: thread.value.id ? 1 : 1, // TODO: 根據實際情況調整分類 ID
    ParentContentId: threadId, // 綁定到當前貼文
    Title: `Re: ${thread.value.title}`, // 回覆標題格式：Re: 原標題
    Body: newReply.value.trim(),
    Type: 2, // 2: 回文（出現在貼文版面）
  };

  try {
    await apiClient.post("/ContentApi", dto);
    alert("回覆成功！");
    newReply.value = "";
    // 重新載入貼文和回覆
    await fetchThread();
    currentPage.value = 1;
  } catch (error) {
    console.error("回覆失敗:", error);
    alert("回覆失敗，請稍後再試");
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<style scoped>
.thread-detail .card-title {
  margin-bottom: 0.5rem;
  line-height: 1.3;
}
.thread-detail textarea {
  resize: vertical;
}

@media (max-width: 576px) {
  .thread-detail .card-title {
    font-size: 1.25rem;
  }
}
</style>
