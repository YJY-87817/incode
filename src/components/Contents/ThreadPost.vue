<template>
  <div class="thread-post mb-3">
    <div class="card shadow-sm">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
          <div class="flex-grow-1">
            <div class="d-flex align-items-center gap-2 mb-1">
              <h6 class="card-title mb-0">{{ post.author }}</h6>
              <span class="badge" :class="badgeClass">{{ typeLabel }}</span>
            </div>
            <small class="text-muted">{{ post.createdAt }}</small>
            <!-- 互動按鈕 -->
            <div class="mt-2">
              <InteractionButtons
                :key="`interactions-${post.id}`"
                :contentId="post.id"
                @interaction-success="handleInteractionSuccess"
                @interaction-error="handleInteractionError"
              />
            </div>
          </div>
          <button class="btn btn-sm btn-forum-secondary" @click="toggleReply">
            <i class="fas fa-reply me-1"></i>回覆
          </button>
        </div>
        <HtmlIframe
          class="card-text mt-2 rich-content"
          :html="safeContent"
          :min-height="60"
          text-color="#145369"
          :extra-css="'body { white-space: pre-wrap; }'"
        />

        <!-- 回覆輸入框 -->
        <div v-if="showReply" class="mt-3 border-top pt-3">
          <textarea
            v-model="replyContent"
            class="form-control"
            rows="3"
            placeholder="撰寫您的回覆..."
          ></textarea>
          <div
            class="d-flex flex-column flex-sm-row gap-2 align-items-start my-2"
          >
            <input
              type="file"
              accept="image/*"
              multiple
              class="form-control form-control-sm"
              @change="handleReplyImages"
              :disabled="isUploadingImages"
            />
            <small class="text-muted">
              上傳後會自動把圖片網址加入內容。{{
                isUploadingImages ? "上傳中..." : ""
              }}
            </small>
          </div>
          <div class="d-flex gap-2 mt-2">
            <button
              class="btn btn-forum-primary btn-sm"
              @click="submitReply"
              :disabled="isSubmittingReply || !replyContent.trim()"
            >
              <i class="fas fa-paper-plane me-1"></i>
              {{ isSubmittingReply ? "發送中..." : "發送回覆" }}
            </button>
            <button class="btn btn-forum-secondary btn-sm" @click="cancelReply">
              取消
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 巢狀回覆 -->
    <div
      v-if="!hideNestedReplies && post.replies && post.replies.length > 0"
      class="replies-container ms-4"
    >
      <ThreadPost
        v-for="reply in post.replies"
        :key="reply.id"
        :post="reply"
        :is-reply="true"
        :category-id="categoryId"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import HtmlIframe from "@/components/HtmlIframe.vue";
import apiClient from "@/api/request.js";
import { uploadImage } from "@/api/imageapi.js";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";

const forumStore = useForumStore();

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
  isReply: {
    type: Boolean,
    default: false,
  },
  categoryId: {
    type: Number,
    default: 1,
  },
  hideNestedReplies: {
    type: Boolean,
    default: false,
  },
});

const apiBaseUrl = (apiClient.defaults?.baseURL || "").replace(/\/api$/, "");

const toAbsoluteUrl = (url) => {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;
  if (url.startsWith("/")) return `${apiBaseUrl}${url}`;
  return `${apiBaseUrl}/${url}`;
};

const showReply = ref(false);
const replyContent = ref("");
const isSubmittingReply = ref(false);
const isUploadingImages = ref(false);

const appendUrls = (original, urls) => {
  const list = Array.isArray(urls) ? urls : [urls];
  const chunk = list.filter(Boolean).join("\n");
  if (!chunk) return original;
  return [original.trim(), chunk].filter(Boolean).join("\n");
};

const typeLabel = computed(() => {
  switch (props.post.type) {
    case CONTENT_TYPES.REPLY:
      return "回文";
    case CONTENT_TYPES.COMMENT:
      return "留言";
    case CONTENT_TYPES.COMMENT_REPLY:
      return "回覆";
    default:
      return "內容";
  }
});

const badgeClass = computed(() => {
  switch (props.post.type) {
    case CONTENT_TYPES.REPLY:
      return "bg-primary";
    case CONTENT_TYPES.COMMENT:
      return "bg-secondary";
    case CONTENT_TYPES.COMMENT_REPLY:
      return "bg-info";
    default:
      return "bg-light text-dark";
  }
});

const safeContent = computed(() => {
  const rawHtml = props.post.contentHtml ?? props.post.content ?? "";
  const { sanitizedHtml } = normalizeRichContent(rawHtml, toAbsoluteUrl);
  return sanitizedHtml;
});

const contentSummary = computed(() => {
  const base = props.post.contentText ?? props.post.content ?? "";
  return typeof base === "string" ? base : String(base ?? "");
});

function toggleReply() {
  showReply.value = !showReply.value;
  if (!showReply.value) {
    replyContent.value = "";
  }
}

async function handleReplyImages(event) {
  const files = event.target?.files;
  if (!files || !files.length) return;
  isUploadingImages.value = true;
  try {
    const uploaded = await uploadImage(files);
    replyContent.value = appendUrls(replyContent.value, uploaded);
  } catch (error) {
    console.error("回覆圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    isUploadingImages.value = false;
    if (event.target) event.target.value = "";
  }
}

async function submitReply() {
  if (!replyContent.value.trim()) {
    alert("請輸入回覆內容");
    return;
  }

  if (isSubmittingReply.value) return;
  isSubmittingReply.value = true;

  const dto = {
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId: props.post.categoryId, // 使用被回覆內容的 categoryId（後端會自動覆蓋）
    parentContentId: props.post.id, // 回覆到當前貼文/留言
    title: `Re: ${contentSummary.value.substring(0, 50)}...`, // 簡短標題
    body: replyContent.value.trim(),
    type: CONTENT_TYPES.COMMENT_REPLY, // 設定為留言回覆類型
  };

  try {
    await apiClient.post("/ContentApi", dto);
    alert("回覆成功！");
    replyContent.value = "";
    showReply.value = false;

    // 直接调用 forumStore 重新获取帖文详情
    if (forumStore.selectedThread) {
      await forumStore.fetchThreadDetail(forumStore.selectedThread);
    }
  } catch (error) {
    console.error("回覆失敗:", error);
    alert("回覆失敗，請稍後再試");
  } finally {
    isSubmittingReply.value = false;
  }
}

function cancelReply() {
  showReply.value = false;
  replyContent.value = "";
}

/**
 * 處理互動成功
 */
function handleInteractionSuccess(data) {
  console.log("互動成功:", data);
  // 可以在此顯示 toast 或其他反饋
}

/**
 * 處理互動錯誤
 */
function handleInteractionError(data) {
  console.error("互動失敗:", data);
  alert(`互動失敗: ${data.error}`);
}
</script>

<style scoped>
/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #145369;
  border-color: #145369;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

.btn-forum-primary:disabled {
  background-color: #d1d3d4;
  border-color: #d1d3d4;
  color: #fff;
  opacity: 0.6;
}

.btn-forum-secondary {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #145369;
  transition: all 0.3s ease;
}

.btn-forum-secondary:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #145369;
}

.card-title {
  font-weight: 600;
  color: #145369;
}

.thread-post .card {
  transition: all 0.2s ease;
  border-color: #d1d3d4;
}

.thread-post .card:hover {
  box-shadow: 0 4px 8px rgba(134, 176, 189, 0.3);
}

.rich-content {
  white-space: pre-wrap;
  word-break: break-word;
  color: #145369;
}

.rich-content img {
  max-width: 100%;
  height: auto;
  display: block;
  margin: 0.5rem 0;
}

/* 巢狀回覆的樣式 */
.thread-post .replies-container .thread-post .card {
  border-left: 3px solid #86b0bd;
  background-color: rgba(255, 240, 221, 0.3);
}

.thread-post .replies-container .thread-post .card:hover {
  background-color: rgba(255, 240, 221, 0.6);
}

/* 更深層巢狀的樣式 */
.thread-post .replies-container .replies-container .thread-post .card {
  border-left-color: #d1d3d4;
  background-color: rgba(255, 240, 221, 0.2);
}

.thread-post .replies-container .replies-container .thread-post .card:hover {
  background-color: rgba(255, 240, 221, 0.5);
}
</style>
