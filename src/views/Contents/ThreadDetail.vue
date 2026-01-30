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
  <DefaultNavbar />
  <section
    class="py-5"
    style="background: linear-gradient(to bottom, #f5f7fa 0%, #ffffff 100%)"
  >
    <!-- Thread Header Section -->
    <div class="container">
      <div v-if="thread.id" class="row mb-5">
        <div class="col-12">
          <!-- Back Button -->
          <router-link
            to="/forum"
            class="btn btn-sm btn-outline-secondary mb-3"
          >
            <i class="fas fa-arrow-left me-2"></i>返回討論區
          </router-link>

          <!-- Thread Title -->
          <h1 class="display-4 fw-bold mb-2">{{ thread.title }}</h1>
          <p class="text-muted fs-5">
            <span class="fw-semibold text-dark">{{ thread.author }}</span>
            於 {{ new Date(thread.createdAt).toLocaleDateString("zh-TW") }} 發布
          </p>

          <!-- Category Badge -->
          <div v-if="thread.categoryId" class="mb-3">
            <span class="badge bg-info">{{
              getCategoryName(thread.categoryId)
            }}</span>
          </div>

          <!-- Thread Content -->
          <div class="mb-4">
            <div
              v-if="thread.imageUrls && thread.imageUrls.length > 0"
              class="mb-4"
            >
              <img
                v-for="(url, index) in thread.imageUrls"
                :key="index"
                :src="url"
                :alt="'Thread image ' + (index + 1)"
                class="img-fluid mb-3 rounded"
                style="max-width: 100%; max-height: 400px; object-fit: cover"
              />
            </div>
            <HtmlIframe :body="thread.contentHtml" />
          </div>

          <!-- Thread Stats -->
          <div class="row text-center mb-5">
            <div class="col-md-4 mb-3 mb-md-0">
              <p class="mb-0">
                <span class="display-6 fw-bold text-primary">{{
                  allPosts.length
                }}</span>
              </p>
              <p class="text-muted">回文</p>
            </div>
            <div class="col-md-4 mb-3 mb-md-0">
              <p class="mb-0">
                <span class="display-6 fw-bold text-success">{{
                  comments.length
                }}</span>
              </p>
              <p class="text-muted">留言</p>
            </div>
            <div class="col-md-4">
              <p class="mb-0">
                <span class="display-6 fw-bold text-warning">{{
                  totalReplies
                }}</span>
              </p>
              <p class="text-muted">所有回覆</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination for Posts -->
      <nav v-if="totalPages > 1" class="mb-4">
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

        <div v-if="paginatedPosts.length > 0" class="mb-4">
          <h6 class="fs-6 mb-2">🗨️ 回文</h6>
          <div v-for="reply in paginatedPosts" :key="`reply-${reply.id}`">
            <ThreadPost
              :post="reply"
              :category-id="thread.categoryId"
              :hide-nested-replies="true"
              @reply-added="handleReplyAdded"
            />
            <div class="ms-4 mb-3">
              <div class="d-flex align-items-center gap-2 mb-2">
                <button
                  class="btn btn-sm btn-forum-secondary"
                  @click="toggleReplyComments(reply.id)"
                  :disabled="!reply.replies || reply.replies.length === 0"
                >
                  {{
                    isReplyExpanded(reply.id)
                      ? "收合留言"
                      : `查看留言(${reply.replies?.length || 0})`
                  }}
                </button>
                <small
                  v-if="!reply.replies || reply.replies.length === 0"
                  class="text-muted"
                >
                  尚無留言
                </small>
              </div>
              <div
                v-if="!isReplyExpanded(reply.id) && reply.replies?.length"
                class="text-muted small"
              >
                <div
                  v-for="preview in reply.replies.slice(0, 2)"
                  :key="`reply-preview-${preview.id}`"
                  class="mb-1"
                >
                  • {{ preview.contentText || preview.content || "" }}
                </div>
              </div>
              <div v-if="isReplyExpanded(reply.id)" class="ms-3">
                <ThreadPost
                  v-for="comment in reply.replies"
                  :key="`reply-comment-${comment.id}`"
                  :post="comment"
                  :category-id="thread.categoryId"
                  @reply-added="handleReplyAdded"
                />
              </div>
            </div>
          </div>
        </div>

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

        <div class="btn-group mb-2" role="group" aria-label="reply type">
          <input
            type="radio"
            class="btn-check"
            name="compose-mode"
            id="compose-reply"
            value="reply"
            v-model="activeComposer"
          />
          <label class="btn btn-forum-outline" for="compose-reply">
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
        <div
          class="d-flex flex-column flex-sm-row gap-2 align-items-start mb-2"
        >
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
    </div>
  </section>

  <DefaultFooter />
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import HtmlIframe from "@/components/HtmlIframe.vue";
import apiClient from "@/api/request.js";
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
const expandedReplyIds = ref({});

const isReplyExpanded = (replyId) => !!expandedReplyIds.value[replyId];
const toggleReplyComments = (replyId) => {
  expandedReplyIds.value = {
    ...expandedReplyIds.value,
    [replyId]: !expandedReplyIds.value[replyId],
  };
};

const fetchThread = async () => {
  try {
    const detailRes = await apiClient.get(`/ContentApi/${threadId}`);

    const detail = detailRes?.contentId ? detailRes : detailRes?.data;
    if (detail && detail.contentId) {
      const detailBodyHtml = await fetchBodyHtml(detail.body);
      const { sanitizedHtml, textContent, imageUrls } =
        normalizeContent(detailBodyHtml);
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

    // 先撈出 parent_content_id = 該篇ID 的所有內容
    const childrenRes = await apiClient.get("/ContentApi", {
      params: { parentContentId: threadId },
    });
    const childItems = Array.isArray(childrenRes?.items)
      ? childrenRes.items
      : Array.isArray(childrenRes)
        ? childrenRes
        : [];

    console.log("🔍 [ThreadDetail] threadId:", threadId);
    console.log("🔍 [ThreadDetail] childrenRes:", childrenRes);
    console.log("🔍 [ThreadDetail] childItems:", childItems);
    console.log(
      "🔍 [ThreadDetail] childItems.map(type):",
      childItems.map((i) => ({
        id: i.contentId,
        type: i.type,
        parent: i.parentContentId,
      }))
    );

    const normalizeItem = async (item, fallbackType) => {
      const itemBodyHtml = await fetchBodyHtml(item.body);
      const { sanitizedHtml, textContent, imageUrls } =
        normalizeContent(itemBodyHtml);
      return {
        id: item.contentId,
        author: item.username,
        createdAt: item.createdAt,
        contentHtml: sanitizedHtml,
        contentText: textContent,
        imageUrls,
        type: item.type ?? fallbackType,
        parentContentId: item.parentContentId,
        replies: [],
      };
    };

    const commentSources = childItems.filter(
      (item) => item.type === CONTENT_TYPES.COMMENT
    );
    const replySources = childItems.filter(
      (item) => item.type === CONTENT_TYPES.REPLY
    );

    console.log(
      "🔍 [ThreadDetail] CONTENT_TYPES.COMMENT:",
      CONTENT_TYPES.COMMENT
    );
    console.log("🔍 [ThreadDetail] CONTENT_TYPES.REPLY:", CONTENT_TYPES.REPLY);
    console.log("🔍 [ThreadDetail] commentSources:", commentSources);
    console.log("🔍 [ThreadDetail] replySources:", replySources);

    const commentItems = await Promise.all(
      commentSources.map((comment) =>
        normalizeItem(comment, CONTENT_TYPES.COMMENT)
      )
    );

    const normalizedReplies = await Promise.all(
      replySources.map(async (reply) => {
        const normalizedReply = await normalizeItem(reply, CONTENT_TYPES.REPLY);
        const replyChildrenRes = await apiClient.get("/ContentApi", {
          params: { parentContentId: reply.contentId },
        });
        const replyChildItems = Array.isArray(replyChildrenRes?.items)
          ? replyChildrenRes.items
          : Array.isArray(replyChildrenRes)
            ? replyChildrenRes
            : [];
        const replyCommentsSource = replyChildItems.filter(
          (item) => item.type === CONTENT_TYPES.COMMENT
        );
        const replyComments = await Promise.all(
          replyCommentsSource.map((comment) =>
            normalizeItem(comment, CONTENT_TYPES.COMMENT)
          )
        );
        replyComments.sort(
          (a, b) =>
            new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
        );
        normalizedReply.replies = replyComments;
        return normalizedReply;
      })
    );

    const sortByDate = (list) => {
      list.sort(
        (a, b) =>
          new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      );
    };

    console.log("✅ [ThreadDetail] Final allPosts:", allPosts.value);
    console.log("✅ [ThreadDetail] Final comments:", comments.value);
  } catch (error) {
    console.error("❌ [ThreadDetail] Failed to normalize replies:", error);
    allPosts.value = normalizedReplies;
    comments.value = commentItems;
    expandedReplyIds.value = {};
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
