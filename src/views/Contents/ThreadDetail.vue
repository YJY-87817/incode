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
        <div
          v-if="thread.imageUrls && thread.imageUrls.length"
          class="d-flex flex-wrap gap-2 mt-2 topic-image-list"
        >
          <img
            v-for="(url, idx) in thread.imageUrls"
            :key="url + idx"
            :src="url"
            alt="貼文圖片"
            class="topic-image-thumb"
            loading="lazy"
          />
        </div>
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
      <!-- Display comments separately -->
      <div v-if="comments.length > 0" class="mb-4">
        <h6 class="fs-6 mb-2">📝 所有留言</h6>
        <ThreadPost
          v-for="comment in comments"
          :key="'comment-' + comment.id"
          :post="comment"
          :category-id="thread.categoryId"
          @reply-added="handleReplyAdded"
        />
      </div>

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
import contentInteractionApi from "@/api/contentInteractionApi.js";
import { useRoute } from "vue-router";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
import { CONTENT_TYPES } from "@/stores/forumStore.js";

const apiBaseUrl = (apiClient.defaults?.baseURL || "").replace(/\/api$/, "");

const toAbsoluteUrl = (url) => {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;
  if (url.startsWith("/")) return `${apiBaseUrl}${url}`;
  return `${apiBaseUrl}/${url}`;
};

const parseBodyContent = (body) => {
  const raw = body || "";
  const imagePattern =
    /(https?:\/\/[^\s]+|\/?img\/[^\s)]+?\.(?:png|jpe?g|gif|webp|bmp))/gi;
  const matches = raw.match(imagePattern) || [];
  const uniqueImages = Array.from(new Set(matches));

  const imageUrls = uniqueImages
    .map((url) => url.replace(/[)\]]+$/, ""))
    .map((url) => url.trim())
    .filter(Boolean)
    .map(toAbsoluteUrl);

  const textContent = raw
    .replace(imagePattern, "")
    .replace(/\n{2,}/g, "\n")
    .trim();
  const displayText = textContent || (imageUrls.length ? "(已附圖片)" : "");

  return {
    textContent: displayText,
    imageUrls,
  };
};
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
  imageUrls: [],
});

const allPosts = ref([]);
const comments = ref([]);

const fetchThread = async () => {
  try {
    const [detailRes, commentsRes] = await Promise.all([
      apiClient.get(`/ContentApi/${threadId}`),
      contentInteractionApi.getComments(threadId),
    ]);

    const detail = detailRes?.contentId ? detailRes : detailRes?.data;
    if (detail && detail.contentId) {
      const { textContent, imageUrls } = parseBodyContent(detail.body);
      thread.value = {
        id: detail.contentId,
        title: detail.title,
        author: detail.username,
        createdAt: detail.createdAt,
        content: textContent,
        imageUrls,
        categoryId: detail.categoryId,
        type: detail.type,
      };
    }

    // Process comments from the new API endpoint
    const commentsData = commentsRes?.comments || [];
    const commentItems = commentsData.map((comment) => {
      const { textContent, imageUrls } = parseBodyContent(comment.body);
      return {
        id: comment.contentId,
        author: comment.username,
        createdAt: comment.createdAt,
        content: textContent,
        imageUrls,
        type: CONTENT_TYPES.COMMENT,
        parentContentId: comment.parentContentId,
        replies: [],
      };
    });

    // For now, keep the existing logic for replies (replies to the main thread)
    // This fetches replies separately since the new comments API might not include them
    const repliesRes = await apiClient.get("/ContentApi", {
      params: { parentContentId: threadId },
    });
    const replyItems = Array.isArray(repliesRes?.items) ? repliesRes.items : [];

    const normalizedReplies = replyItems
      .filter((i) => i.type === CONTENT_TYPES.REPLY)
      .map((i) => {
        const { textContent, imageUrls } = parseBodyContent(i.body);
        return {
          id: i.contentId,
          author: i.username,
          createdAt: i.createdAt,
          content: textContent,
          imageUrls,
          type: i.type,
          parentContentId: i.parentContentId,
          replies: [],
        };
      });

    const sortByDate = (list) => {
      list.sort(
        (a, b) =>
          new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      );
    };

    sortByDate(commentItems);
    sortByDate(normalizedReplies);
    allPosts.value = normalizedReplies;
    comments.value = commentItems;
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

.topic-image-list {
  gap: 0.5rem;
}

.topic-image-thumb {
  width: 120px;
  height: 120px;
  object-fit: cover;
  border-radius: 8px;
  border: 1px solid #e9ecef;
  background: #fff;
}

@media (max-width: 576px) {
  .thread-detail .card-title {
    font-size: 1.25rem;
  }

  .topic-image-thumb {
    width: 90px;
    height: 90px;
  }
}
</style>
