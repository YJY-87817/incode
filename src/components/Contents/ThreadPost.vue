<template>
  <div class="thread-post mb-3">
    <div class="card shadow-sm">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
          <div class="flex-grow-1">
            <div
              class="d-flex align-items-center justify-content-between gap-2 flex-wrap mb-1"
            >
              <div class="d-flex align-items-center gap-2">
                <h6 class="card-title post-author mb-0">{{ post.author }}</h6>
                <span class="badge" :class="badgeClass">{{ typeLabel }}</span>
              </div>
              <div class="interaction-container" @click.stop>
                <InteractionButtons
                  :key="`interactions-${post.id}`"
                  :contentId="post.id"
                  @interaction-success="handleInteractionSuccess"
                  @interaction-error="handleInteractionError"
                />
              </div>
            </div>
            <small
              class="text-muted post-time"
              :class="{ clickable: post.type === 2 || post.type === 3 }"
              @click="goToPostDetail"
            >
              {{ post.createdAt }}
            </small>
          </div>
          <button
            class="btn btn-sm btn-forum-secondary thread-reply-btn"
            @click="toggleReply"
          >
            <i class="fas fa-reply me-1"></i>回覆
          </button>
        </div>
        <div class="mt-2">
          <div
            class="preview-iframe-wrapper"
            :class="{
              'preview-collapsed': previewOnly && !isPreviewExpanded,
              'preview-expanded': previewOnly && isPreviewExpanded,
            }"
          >
            <HtmlIframe
              class="card-text rich-content"
              :html="safeContent"
              :min-height="60"
              text-color="#145369"
              :extra-css="'body { white-space: pre-wrap; } img { max-width: 100%; max-height: 320px; object-fit: contain; } @media (max-width: 768px) { img { max-height: 240px; } }'"
            />
            <div
              v-if="previewOnly"
              class="preview-click-layer"
              @click.stop="togglePreview"
            ></div>
          </div>
        </div>

        <!-- 回覆輸入框 -->
        <div v-if="showReply" class="mt-3 border-top pt-3">
          <div
            class="d-flex flex-wrap gap-2 align-items-center mb-3 quick-reply-toggle"
          >
            <button
              class="btn btn-forum-primary btn-sm"
              type="button"
              @click="toggleQuickReply"
            >
              <i
                class="fas"
                :class="showQuickReply ? 'fa-times' : 'fa-pen'"
              ></i>
              <span class="ms-1">
                {{ showQuickReply ? "收起簡易留言" : "簡易留言" }}
              </span>
            </button>
            <button
              class="btn btn-forum-secondary btn-sm"
              type="button"
              @click="openDetailedReplyModal"
            >
              <i class="fas fa-feather-alt me-1"></i>
              詳細回覆
            </button>
            <small class="text-muted" v-if="!showQuickReply">
              簡易留言僅文字；詳細回覆會開啟編輯視窗
            </small>
          </div>

          <transition name="fade">
            <div v-show="showQuickReply" class="quick-reply-box">
              <textarea
                v-model="replyContent"
                class="form-control"
                rows="4"
                placeholder="請輸入簡易留言（純文字）"
              ></textarea>

              <div class="d-flex flex-wrap gap-2 quick-reply-actions">
                <button
                  class="btn btn-forum-primary btn-sm"
                  type="button"
                  @click="submitReply"
                  :disabled="isSubmittingReply || !replyContent.trim()"
                >
                  <i class="fas fa-paper-plane me-1"></i>
                  {{ isSubmittingReply ? "送出中..." : "送出簡易留言" }}
                </button>
                <button
                  class="btn btn-forum-secondary btn-sm"
                  type="button"
                  @click="cancelReply"
                  :disabled="isSubmittingReply"
                >
                  取消
                </button>
              </div>
            </div>
          </transition>
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
import { useRouter } from "vue-router";
import HtmlIframe from "@/components/HtmlIframe.vue";
import apiClient from "@/api/request.js";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { toAbsoluteUrl } from "@/utils/contentBody.js";

const forumStore = useForumStore();
const router = useRouter();

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
  previewOnly: {
    type: Boolean,
    default: false,
  },
});

const showReply = ref(false);
const replyContent = ref("");
const isSubmittingReply = ref(false);
const isPreviewExpanded = ref(false);
const showQuickReply = ref(false);

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

function togglePreview() {
  if (!props.previewOnly) return;
  isPreviewExpanded.value = !isPreviewExpanded.value;
}

function toggleReply() {
  showReply.value = !showReply.value;
  if (!showReply.value) {
    replyContent.value = "";
    showQuickReply.value = false;
    return;
  }
  showQuickReply.value = false;
}

function toggleQuickReply() {
  if (!showReply.value) {
    showReply.value = true;
  }
  showQuickReply.value = !showQuickReply.value;
}

const getCategoryIdValue = (cat) =>
  cat?.id ??
  cat?.content_category_id ??
  cat?.categoryId ??
  cat?.category_id ??
  null;

const getParentCategoryIdValue = (cat) =>
  cat?.parentCategoryId ??
  cat?.parent_category_id ??
  cat?.parentCategoryID ??
  cat?.parent_id ??
  null;

const getCategoryName = (cat) =>
  cat?.name ?? cat?.categoryName ?? cat?.category_name ?? "";

const openDetailedReplyModal = () => {
  const categoryId = props.categoryId ?? props.post.categoryId ?? null;
  const categories = forumStore.categories || [];

  const matchedCategory = categories.find(
    (cat) => String(getCategoryIdValue(cat)) === String(categoryId)
  );

  const parentCategoryId = matchedCategory
    ? getParentCategoryIdValue(matchedCategory)
    : null;

  const parentCategory = parentCategoryId
    ? categories.find(
        (cat) => String(getCategoryIdValue(cat)) === String(parentCategoryId)
      )
    : null;

  const categorySearchName = parentCategory
    ? getCategoryName(parentCategory)
    : matchedCategory
      ? getCategoryName(matchedCategory)
      : "";

  forumStore.newTopic.title = "";
  forumStore.newTopic.body = "";
  forumStore.newTopic.categoryId = categoryId;
  forumStore.newTopic.parentContentId = props.post.id;
  forumStore.newTopic.categorySearch = categorySearchName;
  forumStore.newTopic.mode = "commentReply";
  forumStore.newTopic.contentType = CONTENT_TYPES.COMMENT_REPLY;

  forumStore.toggleNewTopicModal(true);
};

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
  showQuickReply.value = false;
}

/**
 * 點擊發文時間跳轉到該回文/留言的詳細頁面
 */
function goToPostDetail() {
  // 只有回文(type=2)或留言(type=3)可以跳轉
  if (
    props.post.type === CONTENT_TYPES.REPLY ||
    props.post.type === CONTENT_TYPES.COMMENT
  ) {
    router.push({ name: "forum-thread", params: { id: props.post.id } });
  }
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

.post-author {
  font-weight: 700;
  color: #0f3c4d;
  letter-spacing: 0.2px;
  font-size: 1rem;
}

.post-time {
  transition: all 0.2s ease;
}

.post-time.clickable {
  cursor: pointer;
  text-decoration: underline;
  text-decoration-style: dotted;
  text-underline-offset: 2px;
}

.post-time.clickable:hover {
  color: #145369 !important;
  text-decoration-style: solid;
  font-weight: 500;
}

.thread-reply-btn {
  height: 28px;
  min-height: 28px;
  display: inline-flex;
  align-items: center;
}

.quick-reply-toggle .btn {
  min-width: 120px;
}

.quick-reply-box,
.detailed-reply-box {
  background: rgba(255, 240, 221, 0.5);
  border: 1px solid #d1d3d4;
  border-radius: 8px;
  padding: 1rem;
}

.quick-reply-actions .btn {
  min-width: 100px;
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

.preview-iframe-wrapper {
  position: relative;
}

.preview-iframe-wrapper.preview-collapsed {
  max-height: 180px;
  overflow: hidden;
  background-color: rgba(255, 240, 221, 0.35);
  border: 1px dashed #d1d3d4;
  border-radius: 6px;
}

.preview-iframe-wrapper.preview-expanded {
  border-top: 1px dashed #d1d3d4;
  padding-top: 0.75rem;
}

.preview-click-layer {
  position: absolute;
  inset: 0;
  cursor: pointer;
  background: transparent;
  z-index: 2;
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
