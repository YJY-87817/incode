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
        <p
          class="card-text"
          style="white-space: pre-wrap; word-break: break-word"
        >
          {{ thread.content }}
        </p>
      </div>
    </div>

    <ThreadPost
      v-for="post in paginatedPosts"
      :key="post.id"
      :post="post"
      :category-id="thread.categoryId"
      @reply-added="handleReplyAdded"
    />

    <nav v-if="totalPages > 1" class="mt-3 mt-md-4">
      <ul
        class="pagination pagination-sm justify-content-center flex-wrap gap-1"
      >
        <li
          class="page-item"
          :class="{ disabled: currentPage === 1 }"
          @click="prevPage"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">
            <span class="d-none d-sm-inline"
              ><i class="fa-solid fa-caret-left"></i
            ></span>
            <span class="d-inline d-sm-none"
              ><i class="fa-solid fa-caret-left"></i
            ></span>
          </a>
        </li>
        <li
          class="page-item"
          v-for="page in totalPages"
          :key="page"
          :class="{ active: currentPage === page }"
          @click="goToPage(page)"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">{{
            page
          }}</a>
        </li>
        <li
          class="page-item"
          :class="{ disabled: currentPage === totalPages }"
          @click="nextPage"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">
            <span class="d-none d-sm-inline"
              ><i class="fa-solid fa-caret-right"></i
            ></span>
            <span class="d-inline d-sm-none"
              ><i class="fa-solid fa-caret-right"></i
            ></span>
          </a>
        </li>
      </ul>
    </nav>

    <div class="mt-3 mt-md-5">
      <h5 class="fs-5 fs-md-4 mb-3">💬 回覆 / 留言</h5>

      <div class="btn-group mb-2" role="group" aria-label="reply type">
        <input
          type="radio"
          class="btn-check"
          name="compose-mode"
          id="compose-reply"
          value="reply"
          v-model="activeComposer"
        />
        <label class="btn btn-outline-primary" for="compose-reply">
          回文
        </label>

        <input
          type="radio"
          class="btn-check"
          name="compose-mode"
          id="compose-comment"
          value="comment"
          v-model="activeComposer"
        />
        <label class="btn btn-outline-primary" for="compose-comment">
          留言
        </label>
      </div>

      <textarea
        v-model="drafts[activeComposer]"
        class="form-control mb-2"
        :rows="windowWidth < 768 ? 3 : 4"
        :placeholder="composerPlaceholder"
      ></textarea>
      <button
        class="btn btn-primary w-100 w-sm-auto"
        @click="submitContent"
        :disabled="isSubmitting || !composerHasContent"
      >
        <i class="fas fa-paper-plane me-2"></i>
        {{ isSubmitting ? "發送中..." : composerButtonText }}
      </button>
      <p class="text-muted small mt-2 mb-0">
        回文會出現在貼文下方，留言則可作為單獨討論，兩者都可再被回覆。
      </p>
    </div>
  </section>

  <DefaultFooter />
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import apiClient from "@/api/request.js";
import { useRoute } from "vue-router";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
import { CONTENT_TYPES } from "@/stores/forumStore.js";
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
    const items = Array.isArray(data.items) ? data.items : [];

    const item = items.find((i) => i.contentId === threadId);
    if (item) {
      thread.value = {
        id: item.contentId,
        title: item.title,
        author: item.username,
        createdAt: item.createdAt,
        content: item.body,
        categoryId: item.categoryId,
        type: item.type,
      };
    }

    const contentItems = items.filter((i) =>
      [
        CONTENT_TYPES.REPLY,
        CONTENT_TYPES.COMMENT,
        CONTENT_TYPES.COMMENT_REPLY,
      ].includes(i.type)
    );

    const normalized = contentItems.map((i) => ({
      id: i.contentId,
      author: i.username,
      createdAt: i.createdAt,
      content: i.body,
      type: i.type,
      parentContentId: i.parentContentId,
      replies: [],
    }));

    const nodeMap = new Map();
    normalized.forEach((node) => {
      nodeMap.set(node.id, node);
    });

    const roots = [];
    nodeMap.forEach((node) => {
      if (node.parentContentId === threadId) {
        roots.push(node);
      } else if (nodeMap.has(node.parentContentId)) {
        const parent = nodeMap.get(node.parentContentId);
        parent.replies.push(node);
      }
    });

    const sortReplies = (list) => {
      list.sort(
        (a, b) =>
          new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      );
      list.forEach((child) => sortReplies(child.replies));
    };

    sortReplies(roots);
    allPosts.value = roots;
  } catch (error) {
    console.error("Failed to fetch thread:", error);
  }
};

onMounted(() => {
  window.addEventListener("resize", updateWindowWidth);
  fetchThread();
});

onUnmounted(() => {
  window.removeEventListener("resize", updateWindowWidth);
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

async function handleReplyAdded() {
  await fetchThread();
  currentPage.value = 1;
}

const activeComposer = ref("reply");
const drafts = reactive({
  reply: "",
  comment: "",
});
const isSubmitting = ref(false);

const composerConfig = {
  reply: {
    type: CONTENT_TYPES.REPLY,
    button: "發送回文",
    placeholder: "撰寫您的回文...",
  },
  comment: {
    type: CONTENT_TYPES.COMMENT,
    button: "發送留言",
    placeholder: "撰寫您的留言...",
  },
};

const composerPlaceholder = computed(
  () => composerConfig[activeComposer.value]?.placeholder || "撰寫內容..."
);
const composerButtonText = computed(
  () => composerConfig[activeComposer.value]?.button || "發送"
);
const composerHasContent = computed(
  () => drafts[activeComposer.value].trim().length > 0
);

async function submitContent() {
  const config = composerConfig[activeComposer.value];
  if (!config) return;

  const body = drafts[activeComposer.value].trim();
  if (!body) {
    alert("請輸入內容");
    return;
  }

  if (isSubmitting.value) return;
  isSubmitting.value = true;

  const dto = {
    UserId: 1, // TODO: 改成實際使用者 ID
    CategoryId: thread.value.categoryId ? thread.value.categoryId : 1,
    ParentContentId: threadId,
    Title: `Re: ${thread.value.title}`,
    Body: body,
    Type: config.type,
  };

  try {
    await apiClient.post("/ContentApi", dto);
    drafts[activeComposer.value] = "";
    await fetchThread();
    currentPage.value = 1;
  } catch (error) {
    console.error("發送失敗:", error);
    alert("發送失敗，請稍後再試");
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
