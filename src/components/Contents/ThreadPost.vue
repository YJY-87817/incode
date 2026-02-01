<template>
  <div class="thread-post mb-1">
    <div class="card">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
          <div class="flex-grow-1">
            <div
              class="d-flex align-items-center justify-content-between gap-2 flex-wrap mb-1"
            >
              <div class="d-flex align-items-center gap-2 flex-wrap">
                <div class="post-meta">
                  <span class="badge" :class="badgeClass">{{ typeLabel }}</span>
                  <h6 class="card-title post-author mb-0">{{ post.author }}</h6>
                  <small
                    class="text-muted post-time"
                    :class="{ clickable: post.type === 2 || post.type === 3 }"
                    @click="goToPostDetail"
                  >
                    {{ post.createdAt }}
                  </small>
                </div>
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
          </div>
        </div>
        <div class="mt-2">
          <div
            ref="previewWrapper"
            class="preview-iframe-wrapper"
            :class="{
              'preview-collapsed': previewToggleEnabled && !isPreviewExpanded,
              'preview-expanded': previewToggleEnabled && isPreviewExpanded,
            }"
          >
            <!-- 這裡是preview(留言+回文) -->
            <div
              class="preview-body"
              :class="{
                'preview-row': previewToggleEnabled && !isPreviewExpanded,
                'preview-full': !previewToggleEnabled || isPreviewExpanded,
              }"
            >
              <div
                v-if="
                  previewToggleEnabled &&
                  !isPreviewExpanded &&
                  previewImages.length
                "
                class="preview-images-col"
              >
                <div class="preview-images-grid">
                  <img
                    v-for="(url, idx) in previewImages"
                    :key="url + idx"
                    :src="url"
                    alt="留言圖片"
                    class="preview-image-thumb"
                    loading="lazy"
                  />
                </div>
              </div>
              <div
                class="preview-text-col"
                :class="{
                  'preview-text-col--full':
                    !previewToggleEnabled || isPreviewExpanded,
                }"
              >
                <HtmlIframe
                  v-show="showCollapsedIframe"
                  class="card-text rich-content"
                  :html="safeContent"
                  :min-height="previewMinHeight"
                  text-color="#145369"
                  :extra-css="iframeExtraCssCollapsed"
                />
                <HtmlIframe
                  v-show="showExpandedIframe"
                  class="card-text rich-content"
                  :html="safeContent"
                  :min-height="previewMinHeight"
                  text-color="#145369"
                  :extra-css="iframeExtraCssExpanded"
                />
              </div>
              <div
                v-if="previewToggleEnabled"
                class="preview-click-layer"
                @click.stop="togglePreview"
              ></div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 巢狀回覆 -->
    <div
      v-if="!hideNestedReplies && post.replies && post.replies.length > 0"
      class="nested-replies-section ms-3 mt-2"
    >
      <div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
        <button
          class="btn btn-sm btn-forum-secondary thread-reply-btn"
          @click="toggleReply"
        >
          <i
            class="fas me-1"
            :class="showReply ? 'fa-chevron-up' : 'fa-reply'"
          ></i
          >{{ replyButtonLabel }}
        </button>
        <div v-if="showReply" class="reply-mode-tabs">
          <button
            class="nav-link"
            type="button"
            :class="{ active: replyTab === 'quick' }"
            @click="selectQuickReply"
          >
            <i class="fas fa-pen me-1"></i>
            簡易留言
          </button>
          <button
            class="nav-link"
            type="button"
            :class="{ active: replyTab === 'detailed' }"
            @click="selectDetailedReply"
          >
            <i class="fas fa-feather-alt me-1"></i>
            詳細回覆
          </button>
        </div>
        <button
          class="btn btn-sm btn-outline-secondary nested-toggle-btn"
          @click="toggleNestedReplies"
        >
          <i
            class="fas"
            :class="isNestedExpanded ? 'fa-chevron-up' : 'fa-chevron-down'"
          ></i>
          <span class="ms-1">
            {{ isNestedExpanded ? "收起" : "展開" }}{{ nestedLabel }} ({{
              post.replies.length
            }})
          </span>
        </button>
      </div>

      <!-- 回覆輸入框：置於展開內容上方 -->
      <div v-if="showReply" class="mb-3 pt-3">
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

      <div
        v-show="isNestedExpanded"
        ref="nestedRepliesWrapper"
        class="nested-replies-wrapper"
        :class="{
          'nested-collapsed': nestedToggleEnabled && !isNestedFullyExpanded,
          'nested-expanded': nestedToggleEnabled && isNestedFullyExpanded,
        }"
      >
        <div class="nested-replies-content">
          <ThreadPost
            v-for="reply in post.replies"
            :key="reply.id"
            :post="reply"
            :is-reply="true"
            :category-id="categoryId"
            :preview-only="true"
          />
          <div
            v-if="nestedToggleEnabled"
            class="nested-click-layer"
            @click.stop="toggleNestedFullExpand"
          ></div>
        </div>
      </div>
    </div>

    <!-- 無巢狀回覆時的單獨按鈕 -->
    <div
      v-if="hideNestedReplies || !post.replies || post.replies.length === 0"
      class="mt-2"
    >
      <button
        class="btn btn-sm btn-forum-secondary thread-reply-btn"
        @click="toggleReply"
      >
        <i
          class="fas me-1"
          :class="showReply ? 'fa-chevron-up' : 'fa-reply'"
        ></i
        >{{ replyButtonLabel }}
      </button>
      <div v-if="showReply" class="reply-mode-tabs ms-2">
        <button
          class="nav-link"
          type="button"
          :class="{ active: replyTab === 'quick' }"
          @click="selectQuickReply"
        >
          <i class="fas fa-pen me-1"></i>
          簡易留言
        </button>
        <button
          class="nav-link"
          type="button"
          :class="{ active: replyTab === 'detailed' }"
          @click="selectDetailedReply"
        >
          <i class="fas fa-feather-alt me-1"></i>
          詳細回覆
        </button>
      </div>

      <!-- 回覆輸入框 -->
      <div v-if="showReply" class="mt-3 pt-3">
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
</template>

<script setup>
import { ref, computed, onMounted, watch, nextTick } from "vue";
import { useRouter } from "vue-router";
import HtmlIframe from "@/components/HtmlIframe.vue";
import apiClient from "@/api/request.js";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { toAbsoluteUrl } from "@/utils/contentBody.js";
import Swal from "sweetalert2";

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
const replyTab = ref(null);
const previewWrapper = ref(null);
const previewToggleEnabled = ref(false);
const PREVIEW_COLLAPSED_MAX_HEIGHT = 180;
const PREVIEW_MIN_HEIGHT = 60;
const savedScrollPosition = ref(0);

// 巢狀回覆展開/收起狀態
const isNestedExpanded = ref(false);
const isNestedFullyExpanded = ref(false);
const nestedRepliesWrapper = ref(null);
const nestedToggleEnabled = ref(false);
const NESTED_COLLAPSED_MAX_HEIGHT = 200;

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

const replyButtonLabel = computed(() =>
  props.post.type === CONTENT_TYPES.REPLY ? "留言" : "回覆"
);

const nestedLabel = computed(() => {
  // 根據當前內容類型決定巢狀內容的標籤
  switch (props.post.type) {
    case CONTENT_TYPES.REPLY:
      return "留言"; // 回文下的是留言
    case CONTENT_TYPES.COMMENT:
      return "回覆"; // 留言下的是留言回覆
    default:
      return "回覆";
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

const hasInlineImages = computed(() => {
  const rawHtml = props.post.contentHtml ?? props.post.content ?? "";
  return /<img\s/i.test(rawHtml);
});

const contentSummary = computed(() => {
  const base = props.post.contentText ?? props.post.content ?? "";
  return typeof base === "string" ? base : String(base ?? "");
});

const previewImages = computed(() => {
  const images = props.post.imageUrls ?? [];
  return Array.isArray(images) ? images.slice(0, 3) : [];
});

const baseIframeCss =
  "body { white-space: pre-wrap; } img { max-width: 100%; max-height: 320px; object-fit: contain; } @media (max-width: 768px) { img { max-height: 240px; } }";
const iframeExtraCssCollapsed = computed(() => {
  if (!props.previewOnly || !previewToggleEnabled.value) return baseIframeCss;
  return `${baseIframeCss} img { display: none; }`;
});

const iframeExtraCssExpanded = computed(() => baseIframeCss);

const previewMinHeight = computed(() => {
  if (props.previewOnly && !previewToggleEnabled.value) return 0;
  return PREVIEW_MIN_HEIGHT;
});

const resolveReplyContentType = () => {
  switch (props.post.type) {
    case CONTENT_TYPES.REPLY:
      return {
        type: CONTENT_TYPES.COMMENT,
        mode: "comment",
      };
    case CONTENT_TYPES.COMMENT:
      return {
        type: CONTENT_TYPES.COMMENT_REPLY,
        mode: "commentReply",
      };
    default:
      return {
        type: CONTENT_TYPES.COMMENT_REPLY,
        mode: "commentReply",
      };
  }
};

function togglePreview() {
  if (!previewToggleEnabled.value) return;

  // 當前是收起狀態，即將展開 -> 保存當前的滾動位置
  if (!isPreviewExpanded.value) {
    savedScrollPosition.value = window.scrollY || window.pageYOffset;
  }

  isPreviewExpanded.value = !isPreviewExpanded.value;

  // 當前是展開狀態，即將收起 -> 恢復之前的滾動位置
  if (!isPreviewExpanded.value) {
    nextTick(() => {
      window.scrollTo({
        top: savedScrollPosition.value,
        behavior: "instant",
      });
    });
  }
}

const showCollapsedIframe = computed(
  () => previewToggleEnabled.value && !isPreviewExpanded.value
);

const showExpandedIframe = computed(
  () => !previewToggleEnabled.value || isPreviewExpanded.value
);

const measurePreviewHeight = () => {
  const wrapper = previewWrapper.value;
  if (!wrapper) return 0;
  const iframes = Array.from(wrapper.querySelectorAll("iframe"));
  if (!iframes.length) return 0;
  const visibleFrame = iframes.find((el) => el.getClientRects().length > 0);
  const target = visibleFrame || iframes[0];
  const rect = target.getBoundingClientRect?.();
  return rect?.height || target.offsetHeight || 0;
};

const updatePreviewToggleState = async () => {
  if (!props.previewOnly) {
    previewToggleEnabled.value = false;
    isPreviewExpanded.value = false;
    return;
  }

  await nextTick();
  requestAnimationFrame(() => {
    const height = measurePreviewHeight();
    const needsToggleByHeight = height > PREVIEW_COLLAPSED_MAX_HEIGHT + 1;
    const needsToggleByImages =
      previewImages.value.length > 0 || hasInlineImages.value;
    const shouldEnable = needsToggleByHeight || needsToggleByImages;
    previewToggleEnabled.value = shouldEnable;
    if (!shouldEnable) {
      isPreviewExpanded.value = false;
    }
  });
};

onMounted(() => {
  updatePreviewToggleState();
});

watch(
  [safeContent, previewImages, hasInlineImages, () => props.previewOnly],
  () => {
    updatePreviewToggleState();
  }
);

// ========== 巢狀回覆展開/收起邏輯 ==========
function toggleNestedReplies() {
  isNestedExpanded.value = !isNestedExpanded.value;
  if (isNestedExpanded.value) {
    nextTick(() => {
      updateNestedToggleState();
    });
  }
}

function toggleNestedFullExpand() {
  if (!nestedToggleEnabled.value) return;
  isNestedFullyExpanded.value = !isNestedFullyExpanded.value;
}

const measureNestedHeight = () => {
  const wrapper = nestedRepliesWrapper.value;
  if (!wrapper) return 0;
  return wrapper.scrollHeight || wrapper.offsetHeight || 0;
};

const updateNestedToggleState = async () => {
  if (
    !isNestedExpanded.value ||
    !props.post.replies ||
    props.post.replies.length === 0
  ) {
    nestedToggleEnabled.value = false;
    isNestedFullyExpanded.value = false;
    return;
  }

  await nextTick();
  requestAnimationFrame(() => {
    const height = measureNestedHeight();
    const needsToggle = height > NESTED_COLLAPSED_MAX_HEIGHT + 1;
    nestedToggleEnabled.value = needsToggle;
    if (!needsToggle) {
      isNestedFullyExpanded.value = false;
    }
  });
};

watch(
  () => isNestedExpanded.value,
  () => {
    if (isNestedExpanded.value) {
      updateNestedToggleState();
    }
  }
);

function toggleReply() {
  showReply.value = !showReply.value;
  if (!showReply.value) {
    replyContent.value = "";
    showQuickReply.value = false;
    replyTab.value = null;
    return;
  }
  replyTab.value = "quick";
  showQuickReply.value = true;
}

function selectQuickReply() {
  if (!showReply.value) {
    showReply.value = true;
  }
  replyTab.value = "quick";
  showQuickReply.value = true;
}

function selectDetailedReply() {
  if (!showReply.value) {
    showReply.value = true;
  }
  replyTab.value = "detailed";
  showQuickReply.value = false;
  openDetailedReplyModal();
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
  const replySetting = resolveReplyContentType();
  forumStore.newTopic.mode = replySetting.mode;
  forumStore.newTopic.contentType = replySetting.type;

  forumStore.toggleNewTopicModal(true);
};

async function submitReply() {
  if (!replyContent.value.trim()) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請輸入回覆內容",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
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
    type: resolveReplyContentType().type, // 根據被回覆內容決定類型
  };

  try {
    await apiClient.post("/ContentApi", dto);
    await Swal.fire({
      icon: "success",
      title: "已送出！",
      text: "簡易留言已成功送出",
      timer: 1500,
      showConfirmButton: false,
    });
    replyContent.value = "";
    showReply.value = false;

    // 直接调用 forumStore 重新获取帖文详情
    if (forumStore.selectedThread) {
      await forumStore.fetchThreadDetail(forumStore.selectedThread);
    }
  } catch (error) {
    console.error("回覆失敗:", error);
    await Swal.fire({
      icon: "error",
      title: "送出失敗",
      text: "回覆失敗，請稍後再試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
  } finally {
    isSubmittingReply.value = false;
  }
}

function cancelReply() {
  showReply.value = false;
  replyContent.value = "";
  showQuickReply.value = false;
  replyTab.value = null;
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
async function handleInteractionError(data) {
  console.error("互動失敗:", data);
  await Swal.fire({
    icon: "error",
    title: "互動失敗",
    text: data.error || "操作失敗，請稍後再試",
    confirmButtonText: "確定",
    confirmButtonColor: "#145369",
  });
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

.post-meta {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.post-time {
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  line-height: 1;
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
  height: 32px;
  min-height: 32px;
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.75rem;
  margin: 0;
  font-size: 0.875rem;
}

.thread-post .btn i {
  font-size: 0.8rem;
}

.quick-reply-toggle .btn {
  min-width: 120px;
}

.reply-mode-tabs {
  display: inline-flex;
  align-items: center;
  gap: 0;
  padding: 3px;
  background: #fff;
  border: 1px solid #d1d3d4;
  border-radius: 999px;
  height: 32px;
  font-size: 0.875rem;
  transition: all 0.2s ease;
  overflow: hidden;
  margin: 0;
}

.reply-mode-tabs .nav-link {
  border: none;
  background: transparent;
  color: #145369;
  padding: 0 0.75rem;
  border-radius: 999px;
  font-size: 0.875rem;
  font-weight: 600;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 26px;
  line-height: 26px;
  transition: all 0.2s ease;
  margin: 0;
}

.reply-mode-tabs .nav-link:hover {
  background: #f8f9fa;
}

.reply-mode-tabs .nav-link.active {
  background: #145369;
  color: #fff;
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
  border: none;
  box-shadow: none;
}

/* 留言之間的水平分隔線（與簡易留言上方同風格） */
.thread-post + .thread-post {
  position: relative;
  margin-top: 0.75rem;
  padding-top: 0.75rem;
}

.thread-post + .thread-post::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  border-top: 1px solid #d1d3d4;
}

.thread-post .card-body {
  padding: 0.75rem;
}

.thread-post .card:hover {
  box-shadow: none;
}

.interaction-container {
  position: relative;
  z-index: 20;
}

/* 隱藏留言/回文區塊內的所有滾動條（保留可滾動） */
.thread-post,
.thread-post :deep(*) {
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.thread-post :deep(*::-webkit-scrollbar) {
  display: none;
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
  transition:
    max-height 0.2s ease,
    padding 0.2s ease,
    background-color 0.2s ease,
    border-color 0.2s ease;
}

.preview-body {
  width: 100%;
  position: relative;
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.preview-row {
  display: flex;
  gap: 0.75rem;
  align-items: stretch;
  transition: gap 0.2s ease;
}

.preview-text-col {
  position: relative;
  flex: 1 1 auto;
  min-width: 0;
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.preview-text-col--full {
  width: 100%;
}

.preview-images-col {
  flex: 0 0 140px;
  display: flex;
  align-items: flex-start;
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.preview-images-grid {
  display: grid;
  grid-template-columns: repeat(1, minmax(0, 1fr));
  gap: 0.4rem;
  width: 100%;
}

.preview-image-thumb {
  width: 100%;
  aspect-ratio: 1 / 1;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid #d1d3d4;
  background: #fff0dd;
}

.preview-iframe-wrapper.preview-collapsed {
  max-height: 180px;
  overflow-y: auto;
  overscroll-behavior: contain;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
  -ms-overflow-style: none;
  background-color: rgba(255, 240, 221, 0.35);
  border: 1px dashed #d1d3d4;
  border-radius: 6px;
  z-index: 2;
}

.preview-iframe-wrapper.preview-collapsed::-webkit-scrollbar {
  display: none;
}

.preview-iframe-wrapper.preview-collapsed iframe {
  pointer-events: none;
}

.preview-iframe-wrapper.preview-collapsed .preview-body {
  opacity: 0.96;
  transform: translateY(1px);
}

.preview-iframe-wrapper.preview-expanded {
  border-top: 1px dashed #d1d3d4;
  padding-top: 0.75rem;
  z-index: 2;
}

.preview-iframe-wrapper.preview-expanded .preview-body {
  opacity: 1;
  transform: translateY(0);
}

.preview-click-layer {
  position: absolute;
  inset: 0;
  cursor: pointer;
  background: transparent;
  z-index: 3;
}

@media (max-width: 768px) {
  .preview-row {
    flex-direction: column;
  }

  .preview-images-col {
    flex: 0 0 auto;
  }

  .preview-images-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

/* 巢狀回覆的樣式 */
.thread-post .replies-container .thread-post .card {
  border: none;
  background-color: rgba(255, 240, 221, 0.3);
}

.thread-post .replies-container .thread-post .card:hover {
  background-color: rgba(255, 240, 221, 0.6);
}

/* 更深層巢狀的樣式 */
.thread-post .replies-container .replies-container .thread-post .card {
  border: none;
  background-color: rgba(255, 240, 221, 0.2);
}

.thread-post .replies-container .replies-container .thread-post .card:hover {
  background-color: rgba(255, 240, 221, 0.5);
}

/* ========== 巢狀回覆展開/收起樣式 ========== */
.nested-replies-section {
  margin-top: 0.5rem;
}

.nested-toggle-btn {
  font-size: 0.875rem;
  padding: 0.25rem 0.75rem;
  border-color: #d1d3d4;
  color: #145369;
  background-color: #fff;
  transition: all 0.2s ease;
  height: 32px;
  min-height: 32px;
  display: inline-flex;
  align-items: center;
  margin: 0;
}

.nested-toggle-btn:hover {
  background-color: #f8f9fa;
  border-color: #145369;
  color: #145369;
}

.nested-toggle-btn i {
  transition: transform 0.2s ease;
}

.nested-replies-wrapper {
  position: relative;
  transition:
    max-height 0.3s ease,
    padding 0.2s ease,
    background-color 0.2s ease,
    border-color 0.2s ease;
}

.nested-replies-content {
  position: relative;
  width: 100%;
}

.nested-replies-wrapper.nested-collapsed {
  max-height: 200px;
  overflow-y: auto;
  overscroll-behavior: contain;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
  -ms-overflow-style: none;
  background-color: rgba(255, 240, 221, 0.25);
  border: 1px dashed #d1d3d4;
  border-radius: 6px;
  padding: 0.5rem;
}

.nested-replies-wrapper.nested-collapsed::-webkit-scrollbar {
  display: none;
}

.nested-replies-wrapper.nested-collapsed .nested-replies-content {
  opacity: 0.95;
}

.nested-replies-wrapper.nested-expanded {
  border-top: 1px dashed #d1d3d4;
  padding-top: 0.5rem;
  margin-top: 0.5rem;
}

.nested-replies-wrapper.nested-expanded .nested-replies-content {
  opacity: 1;
}

.nested-click-layer {
  position: absolute;
  inset: 0;
  cursor: pointer;
  background: transparent;
  z-index: 1;
}

/* 調整巢狀回覆內的卡片樣式 */
.nested-replies-wrapper .thread-post .card {
  border: none;
  background-color: rgba(255, 240, 221, 0.4);
  margin-bottom: 0.5rem;
}

.nested-replies-wrapper .thread-post .card:hover {
  background-color: rgba(255, 240, 221, 0.7);
}

.nested-replies-wrapper.nested-collapsed .thread-post .card {
  pointer-events: none;
}

.nested-replies-wrapper.nested-expanded .thread-post .card {
  pointer-events: auto;
}
</style>
