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
        <HtmlIframe
          class="card-text rich-content-frame"
          :html="thread.contentHtml"
          :min-height="120"
          text-color="#432f28"
          :extra-css="'img { max-width: 100%; max-height: 360px; object-fit: contain; } @media (max-width: 768px) { img { max-height: 260px; } }'"
          :allow-scripts="true"
        />
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
        <label class="btn btn-forum-outline" for="compose-reply"> 回文 </label>

        <input
          type="radio"
          class="btn-check"
          name="compose-mode"
          id="compose-comment"
          value="comment"
          v-model="activeComposer"
        />
        <label class="btn btn-forum-outline" for="compose-comment">
          留言
        </label>
      </div>

      <textarea
        v-model="drafts[activeComposer]"
        class="form-control mb-2"
        :rows="windowWidth < 768 ? 3 : 4"
        :placeholder="composerPlaceholder"
      ></textarea>
      <div class="d-flex flex-column flex-sm-row gap-2 align-items-start mb-2">
        <input
          type="file"
          accept="image/*"
          multiple
          class="form-control form-control-sm"
          @change="handleComposerImages"
          :disabled="isUploadingImages"
        />
        <small class="text-muted">
          上傳後會自動把圖片網址加入內容。{{
            isUploadingImages ? "上傳中..." : ""
          }}
        </small>
      </div>
      <button
        class="btn btn-forum-primary w-100 w-sm-auto"
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
import HtmlIframe from "@/components/HtmlIframe.vue";
import apiClient from "@/api/request.js";
import contentInteractionApi from "@/api/contentInteractionApi.js";
import { useRoute } from "vue-router";
import ThreadPost from "@/components/Contents/ThreadPost.vue";
import { CONTENT_TYPES } from "@/stores/forumStore.js";
import { uploadImage } from "@/api/imageapi.js";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { fetchBodyHtml, toAbsoluteUrl } from "@/utils/contentBody.js";
import { appendUrls } from "@/components/Contents/Forum/forumUtils";

const normalizeContent = (body) => normalizeRichContent(body, toAbsoluteUrl);
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
  contentHtml: "",
  contentText: "",
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
      const detailBodyHtml = await fetchBodyHtml(detail.body);
      const { sanitizedHtml, textContent, imageUrls } = normalizeContent(
        detailBodyHtml
      );
      thread.value = {
        id: detail.contentId,
        title: detail.title,
        author: detail.username,
        createdAt: detail.createdAt,
        contentHtml: sanitizedHtml,
        contentText: textContent,
        imageUrls,
        categoryId: detail.categoryId,
        type: detail.type,
      };
    }

    // Process comments from the new API endpoint
    const commentsData = commentsRes?.comments || [];
    const commentItems = await Promise.all(
      commentsData.map(async (comment) => {
        const commentHtml = await fetchBodyHtml(comment.body);
        const { sanitizedHtml, textContent, imageUrls } = normalizeContent(
          commentHtml
        );
        return {
          id: comment.contentId,
          author: comment.username,
          createdAt: comment.createdAt,
          contentHtml: sanitizedHtml,
          contentText: textContent,
          imageUrls,
          type: CONTENT_TYPES.COMMENT,
          parentContentId: comment.parentContentId,
          replies: [],
        };
      })
    );

    // For now, keep the existing logic for replies (replies to the main thread)
    // This fetches replies separately since the new comments API might not include them
    const repliesRes = await apiClient.get("/ContentApi", {
      params: { parentContentId: threadId },
    });
    const replyItems = Array.isArray(repliesRes?.items) ? repliesRes.items : [];

    const normalizedReplies = await Promise.all(
      replyItems
        .filter((i) => i.type === CONTENT_TYPES.REPLY)
        .map(async (i) => {
          const replyHtml = await fetchBodyHtml(i.body);
          const { sanitizedHtml, textContent, imageUrls } = normalizeContent(
            replyHtml
          );
          return {
            id: i.contentId,
            author: i.username,
            createdAt: i.createdAt,
            contentHtml: sanitizedHtml,
            contentText: textContent,
            imageUrls,
            type: i.type,
            parentContentId: i.parentContentId,
            replies: [],
          };
        })
    );

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
const isUploadingImages = ref(false);

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

const handleComposerImages = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;
  isUploadingImages.value = true;
  try {
    const uploaded = await uploadImage(files);
    drafts[activeComposer.value] = appendUrls(
      drafts[activeComposer.value],
      uploaded
    );
  } catch (error) {
    console.error("圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    isUploadingImages.value = false;
    if (event.target) event.target.value = "";
  }
};

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
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId: thread.value.categoryId ? thread.value.categoryId : 1,
    parentContentId: threadId,
    title: `Re: ${thread.value.title}`,
    body,
    type: config.type,
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
/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #432f28;
}

.btn-forum-primary:disabled {
  background-color: #d1d3d4;
  border-color: #d1d3d4;
  color: #fff;
  opacity: 0.6;
}

.btn-forum-outline {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #432f28;
  transition: all 0.3s ease;
}

.btn-forum-outline:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}

.btn-check:checked + .btn-forum-outline {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
}

.thread-detail .card {
  border-color: #d1d3d4;
}

.thread-detail .card-title {
  margin-bottom: 0.5rem;
  line-height: 1.3;
  color: #432f28;
}

.rich-content-frame {
  width: 100%;
  border: 0;
  display: block;
  min-height: 120px;
  background: transparent;
}

.thread-detail textarea {
  resize: vertical;
  border-color: #d1d3d4;
}

.thread-detail textarea:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}

.topic-image-list {
  gap: 0.5rem;
}

.topic-image-thumb {
  width: 120px;
  height: 120px;
  object-fit: cover;
  border-radius: 8px;
  border: 1px solid #d1d3d4;
  background: #fff0dd;
}

/* 分頁樣式 */
.pagination .page-item.active .page-link {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
}

.pagination .page-link {
  color: #432f28;
  border-color: #d1d3d4;
}

.pagination .page-link:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
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
