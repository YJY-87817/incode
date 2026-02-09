<template>
  <div class="thread-post mb-1" :id="`content-${post.id}`">
    <div
      class="card"
      :class="{
        'replying-target': isReplyingTarget,
        'cursor-pointer': isReplyingTarget,
      }"
      @click="handleReplyingTargetClick"
    >
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
          <div class="flex-grow-1">
            <div
              class="d-flex align-items-center justify-content-between gap-2 flex-wrap mb-1"
            >
              <div class="d-flex align-items-center gap-2 flex-wrap">
                <div class="post-meta">
                  <span
                    v-if="post.categoryName"
                    class="badge bg-secondary-light text-dark"
                  >
                    {{ post.categoryName }}
                  </span>
                  <button
                    v-if="shouldShowParentLink"
                    class="btn btn-sm btn-outline-primary parent-link-btn"
                    type="button"
                    @click.stop="goToParentThread"
                  >
                    <i class="fas fa-link me-1"></i>
                    主文
                  </button>
                  <span class="badge" :class="badgeClass">{{ typeLabel }}</span>
                  <h6 class="card-title post-author mb-0" @click.stop>
                    <UserHoverName
                      :user-id="post.userId"
                      :username="post.author"
                      :avatar-url="getAvatarUrl(post.author)"
                      :size="24"
                    />
                  </h6>
                  <small
                    class="text-muted post-time clickable"
                    @click.stop="goToPostDetail"
                  >
                    {{ post.createdAt }}
                  </small>
                </div>
              </div>
              <div class="interaction-container" @click.stop>
                <InteractionButtons
                  :key="`interactions-${post.id}`"
                  :contentId="post.id"
                  :authorUserId="post.userId"
                  @interaction-success="handleInteractionSuccess"
                  @interaction-error="handleInteractionError"
                  @edit-content="handleEditContent"
                  @delete-content="handleDeleteContent"
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
                  :allow-scripts="true"
                  :extra-css="iframeExtraCssCollapsed"
                />
                <HtmlIframe
                  v-show="showExpandedIframe"
                  class="card-text rich-content"
                  :html="safeContent"
                  :min-height="previewMinHeight"
                  text-color="#145369"
                  :allow-scripts="true"
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
    <div v-if="canHaveNestedReplies" class="nested-replies-section ms-3 mt-2">
      <div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
        <button
          v-if="canReplyHere"
          class="btn btn-sm btn-forum-secondary thread-reply-btn"
          @click="toggleReply"
        >
          <i
            class="fas me-1"
            :class="showReply ? 'fa-chevron-up' : 'fa-reply'"
          ></i
          >{{ replyButtonLabel }}
        </button>
        <div v-if="showReply && canReplyHere" class="reply-mode-tabs">
          <button
            class="nav-link"
            type="button"
            :class="{ active: replyTab === 'quick' }"
            @click="selectQuickReply"
          >
            {{ quickReplyLabel }}
          </button>
          <button
            class="nav-link"
            type="button"
            :class="{ active: replyTab === 'detailed' }"
            @click="selectDetailedReply"
          >
            <i class="fas fa-pen me-1"></i>
            {{ detailedReplyLabel }}
          </button>
        </div>
        <button
          v-if="shouldShowNestedToggleButton"
          class="btn btn-sm btn-outline-secondary nested-toggle-btn"
          @click="toggleNestedReplies"
        >
          <i
            class="fas"
            :class="isNestedExpanded ? 'fa-chevron-up' : 'fa-chevron-down'"
          ></i>
          <span class="ms-1">
            {{ isNestedExpanded ? "收起" : "展開" }}{{ nestedLabel }} ({{
              nestedCountLabel
            }})
          </span>
        </button>
        <span v-if="nestedRepliesLoading" class="text-muted small">
          載入中...
        </span>
      </div>

      <!-- 回覆輸入框：置於展開內容上方 -->
      <QuickReplyBox
        v-if="showReply && canReplyHere"
        v-show="showQuickReply"
        :content-id="props.post.id"
        @submit="submitReply"
        @cancel="cancelReply"
        @textarea-input="autoResizeTextarea"
        class="mb-3 pt-3"
      />

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
            v-for="reply in nestedReplies"
            :key="reply.id"
            :post="reply"
            :is-reply="true"
            :category-id="categoryId"
            :preview-only="true"
          />
          <div
            v-if="nestedRepliesLoaded && nestedRepliesCount === 0"
            class="text-muted small py-2"
          >
            尚無{{ nestedLabel }}
          </div>
          <div
            v-if="nestedToggleEnabled"
            class="nested-click-layer"
            @click.stop="toggleNestedFullExpand"
          ></div>
        </div>
      </div>
    </div>

    <!-- 無巢狀回覆時的單獨按鈕 -->
    <div v-if="!canHaveNestedReplies" class="mt-2">
      <button
        v-if="canReplyHere"
        class="btn btn-sm btn-forum-secondary thread-reply-btn"
        @click="toggleReply"
      >
        <i
          class="fas me-1"
          :class="showReply ? 'fa-chevron-up' : 'fa-reply'"
        ></i
        >{{ replyButtonLabel }}
      </button>
      <div v-if="showReply && canReplyHere" class="reply-mode-tabs ms-2">
        <button
          class="nav-link"
          type="button"
          :class="{ active: replyTab === 'quick' }"
          @click="selectQuickReply"
        >
          {{ quickReplyLabel }}
        </button>
        <button
          class="nav-link"
          type="button"
          :class="{ active: replyTab === 'detailed' }"
          @click="selectDetailedReply"
        >
          <i class="fas fa-pen me-1"></i>
          {{ detailedReplyLabel }}
        </button>
      </div>

      <!-- 回覆輸入框 -->
      <QuickReplyBox
        v-if="showReply && canReplyHere"
        v-show="showQuickReply"
        :content-id="props.post.id"
        @submit="submitReply"
        @cancel="cancelReply"
        @textarea-input="autoResizeTextarea"
        class="mt-3 pt-3"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, nextTick } from "vue";
import { useRouter } from "vue-router";
import HtmlIframe from "@/components/HtmlIframe.vue";
import contentApi from "@/api/contentApi.js";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import InteractionButtons from "@/components/Contents/InteractionButtons.vue";
import QuickReplyBox from "@/components/Contents/QuickReplyBox.vue";
import UserHoverName from "@/components/UserHoverName.vue";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { toAbsoluteUrl } from "@/utils/contentBody.js";
import Swal from "sweetalert2";

const forumStore = useForumStore();
const router = useRouter();

const getAvatarUrl = (name, explicitUrl = "") => {
  if (explicitUrl) return explicitUrl;
  const safeName = String(name || "User").trim() || "User";
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(safeName)}&background=random`;
};

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
  showParentLink: {
    type: Boolean,
    default: true,
  },
});

const showReply = ref(false);
const isPreviewExpanded = ref(false);
const showQuickReply = ref(false);
const replyTab = ref(null);
const previewWrapper = ref(null);
const previewToggleEnabled = ref(false);
const PREVIEW_COLLAPSED_MAX_HEIGHT = 180;
const PREVIEW_MIN_HEIGHT = 60;
const PREVIEW_TEXT_COLLAPSE_LENGTH = 300;
const savedScrollPosition = ref(0);

// 巢狀回覆展開/收起狀態
const isNestedExpanded = ref(false);
const isNestedFullyExpanded = ref(false);
const nestedRepliesWrapper = ref(null);
const nestedToggleEnabled = ref(false);
const NESTED_COLLAPSED_MAX_HEIGHT = 200;

// 檢查是否為正在回覆的目標（檢查所有草稿）
const isReplyingTarget = computed(() => {
  return forumStore.drafts.some(
    (draft) => draft.replyingToContentId === props.post.id,
  );
});

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
  props.post.type === CONTENT_TYPES.REPLY ? "留言" : "回覆",
);

const canReply = computed(
  () => props.post.type !== CONTENT_TYPES.COMMENT_REPLY,
);

// 巢狀子層(展開留言/回覆內)不顯示回覆按鈕
const canReplyHere = computed(() => canReply.value && !props.isReply);
const canHaveNestedReplies = computed(
  () => !props.hideNestedReplies && canReply.value && !props.isReply,
);

const quickReplyLabel = computed(() =>
  props.post.type === CONTENT_TYPES.REPLY ? "留言" : "回覆",
);

const detailedReplyLabel = computed(() =>
  props.post.type === CONTENT_TYPES.REPLY ? "詳細留言" : "詳細回覆",
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

const nestedRepliesState = computed(
  () => forumStore.childRepliesByParentId[props.post.id] || null,
);

const nestedReplies = computed(() => {
  if (nestedRepliesState.value?.items) return nestedRepliesState.value.items;
  if (Array.isArray(props.post.replies)) return props.post.replies;
  return [];
});

const nestedRepliesCount = computed(() => {
  if (nestedRepliesState.value?.loaded) {
    return nestedRepliesState.value.totalCount ?? nestedReplies.value.length;
  }
  return Array.isArray(props.post.replies) ? props.post.replies.length : 0;
});

const nestedRepliesLoading = computed(
  () => nestedRepliesState.value?.isLoading ?? false,
);

const nestedRepliesLoaded = computed(
  () => nestedRepliesState.value?.loaded ?? false,
);

const nestedCountLabel = computed(() => {
  // 優先顯示預加載的數量
  const preloadedData = forumStore.childRepliesCountByParentId[props.post.id];
  if (
    preloadedData &&
    !preloadedData.isLoading &&
    preloadedData.totalCount !== null
  ) {
    return String(preloadedData.totalCount);
  }

  if (nestedRepliesLoading.value) return "...";
  if (nestedRepliesLoaded.value) return String(nestedRepliesCount.value);
  if (nestedRepliesCount.value > 0) return String(nestedRepliesCount.value);
  return "?";
});

const shouldShowNestedToggleButton = computed(() => {
  // 優先檢查預加載的計數數據
  const preloadedData = forumStore.childRepliesCountByParentId[props.post.id];
  if (preloadedData) {
    // 如果正在加載或已加載且有數據，顯示按鈕
    if (preloadedData.isLoading) return true;
    if (preloadedData.totalCount !== null && preloadedData.totalCount > 0) {
      return true;
    }
  }

  // 檢查實際加載的回覆數據或預先設定的 replies
  if (nestedRepliesCount.value > 0) return true;

  // 其他情況不顯示按鈕
  return false;
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

const parentContentId = computed(
  () =>
    props.post.parentContentId ??
    props.post.parent_content_id ??
    props.post.parentId ??
    props.post.parent_id ??
    null,
);

const shouldShowParentLink = computed(
  () =>
    props.showParentLink &&
    props.post.type === CONTENT_TYPES.REPLY &&
    parentContentId.value,
);

watch(
  () => [
    props.post?.id,
    props.post?.type,
    props.post?.parentContentId,
    props.post?.parent_content_id,
    props.post?.parentId,
    props.post?.parent_id,
  ],
  () => {
    console.log("[ThreadPost] parent link check", {
      id: props.post?.id,
      type: props.post?.type,
      parentContentId: props.post?.parentContentId,
      parent_content_id: props.post?.parent_content_id,
      parentId: props.post?.parentId,
      parent_id: props.post?.parent_id,
      resolvedParentId: parentContentId.value,
      showParentLinkProp: props.showParentLink,
      shouldShowParentLink: shouldShowParentLink.value,
    });
  },
  { immediate: true },
);

const escapeHtml = (value) =>
  String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");

const safeContent = computed(() => {
  const rawHtml = props.post.contentHtml ?? props.post.content ?? "";
  const { sanitizedHtml } = normalizeRichContent(rawHtml, toAbsoluteUrl);
  const titleText = String(props.post.title ?? "").trim();

  if (props.post.type !== CONTENT_TYPES.REPLY || !titleText) {
    return sanitizedHtml;
  }

  const titleHtml = `<h6 class="reply-title">${escapeHtml(titleText)}</h6>`;
  if (!sanitizedHtml) return titleHtml;
  return `${titleHtml}<div class="reply-body">${sanitizedHtml}</div>`;
});

const hasInlineImages = computed(() => {
  const rawHtml = props.post.contentHtml ?? props.post.content ?? "";
  return /<img\s/i.test(rawHtml);
});

const contentSummary = computed(() => {
  const base = props.post.contentText ?? props.post.content ?? "";
  return typeof base === "string" ? base : String(base ?? "");
});

const buildReplyTitle = (parentTitle) => {
  const base = String(parentTitle ?? "").trim();
  return base ? `Re: ${base}` : "Re:";
};

const previewImages = computed(() => {
  const images = props.post.imageUrls ?? [];
  return Array.isArray(images) ? images.slice(0, 3) : [];
});

const baseIframeCss =
  "body { white-space: pre-wrap; } h6.reply-title { margin: 0 0 6px; font-size: 1rem; font-weight: 600; color: #145369; } .reply-body { margin: 0; } img:not([style*='width']) { width: 100%; } img { max-width: 100%; max-height: 320px; object-fit: contain; } @media (max-width: 768px) { img { max-height: 240px; } }";
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

const nestedChildType = computed(() => resolveReplyContentType().type);

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
  () => previewToggleEnabled.value && !isPreviewExpanded.value,
);

const showExpandedIframe = computed(
  () => !previewToggleEnabled.value || isPreviewExpanded.value,
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
    const needsToggleByText =
      contentSummary.value.length > PREVIEW_TEXT_COLLAPSE_LENGTH;
    const shouldEnable =
      needsToggleByHeight || needsToggleByImages || needsToggleByText;
    previewToggleEnabled.value = shouldEnable;
    if (!shouldEnable) {
      isPreviewExpanded.value = false;
    }
  });
};

onMounted(() => {
  updatePreviewToggleState();

  // 預加載子回覆的數量（不加載詳細內容）
  if (canHaveNestedReplies.value) {
    forumStore.preloadChildRepliesCount(props.post.id, nestedChildType.value);
  }
});

watch(
  [safeContent, previewImages, hasInlineImages, () => props.previewOnly],
  () => {
    updatePreviewToggleState();
  },
);

// ========== 巢狀回覆展開/收起邏輯 ==========
function toggleNestedReplies() {
  isNestedExpanded.value = !isNestedExpanded.value;
  if (isNestedExpanded.value) {
    forumStore.ensureChildRepliesLoaded(props.post.id, nestedChildType.value);
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
    !nestedReplies.value ||
    nestedReplies.value.length === 0
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
  },
);

watch(
  () => nestedReplies.value.length,
  () => {
    if (isNestedExpanded.value) {
      updateNestedToggleState();
    }
  },
);

function autoResizeTextarea(event) {
  const textarea = event?.target;
  if (!textarea) return;
  textarea.style.height = "auto";
  textarea.style.height = `${textarea.scrollHeight}px`;
}

function toggleReply() {
  showReply.value = !showReply.value;
  if (!showReply.value) {
    forumStore.cancelQuickReply(props.post.id);
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

  // 檢查是否已經有針對這個帖文的詳細回覆草稿
  const existingDraft = forumStore.drafts.find(
    (draft) => draft.replyingToContentId === props.post.id,
  );

  if (existingDraft) {
    // 如果已存在草稿，先最小化其他所有的draft
    forumStore.drafts.forEach((draft) => {
      if (draft.id !== existingDraft.id && !draft.isMinimized) {
        forumStore.toggleDraftMinimize(draft.id, true);
      }
    });
    // 再展開這個草稿
    forumStore.toggleDraftMinimize(existingDraft.id, false);
  } else {
    // 如果不存在草稿，先最小化所有已展開的draft
    forumStore.drafts.forEach((draft) => {
      if (!draft.isMinimized) {
        forumStore.toggleDraftMinimize(draft.id, true);
      }
    });
    // 然後創建新的草稿
    openDetailedReplyModal();
  }
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
    (cat) => String(getCategoryIdValue(cat)) === String(categoryId),
  );

  const parentCategoryId = matchedCategory
    ? getParentCategoryIdValue(matchedCategory)
    : null;

  const parentCategory = parentCategoryId
    ? categories.find(
        (cat) => String(getCategoryIdValue(cat)) === String(parentCategoryId),
      )
    : null;

  const categorySearchName = parentCategory
    ? getCategoryName(parentCategory)
    : matchedCategory
      ? getCategoryName(matchedCategory)
      : "";

  const replySetting = resolveReplyContentType();
  const replyTitle =
    replySetting.type === CONTENT_TYPES.COMMENT ||
    replySetting.type === CONTENT_TYPES.COMMENT_REPLY
      ? buildReplyTitle(props.post.title || contentSummary.value)
      : "";

  // 創建新的草稿
  forumStore.createDraft({
    title: replyTitle,
    body: "",
    categoryId: categoryId,
    parentContentId: props.post.id,
    categorySearch: categorySearchName,
    mode: replySetting.mode,
    contentType: replySetting.type,
    replyingToContentId: props.post.id,
    replyingToContentData: {
      id: props.post.id,
      title: props.post.title || "",
      author: props.post.author,
      createdAt: props.post.createdAt,
      contentText: contentSummary.value,
    },
    replyingToThreadId: forumStore.selectedThread,
  });
};

async function submitReply() {
  const replyContent = forumStore.getQuickReplyContent(props.post.id);

  if (!replyContent.trim()) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請輸入回覆內容",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return;
  }

  const isSubmitting = forumStore.getQuickReplySubmitting(props.post.id);
  if (isSubmitting) return;

  forumStore.setQuickReplySubmitting(props.post.id, true);

  const dto = {
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId: props.post.categoryId, // 使用被回覆內容的 categoryId（後端會自動覆蓋）
    parentContentId: props.post.id, // 回覆到當前貼文/留言
    title: buildReplyTitle(props.post.title || contentSummary.value),
    body: replyContent.trim(),
    type: resolveReplyContentType().type, // 根據被回覆內容決定類型
  };

  try {
    await contentApi.createContent(dto);
    await Swal.fire({
      icon: "success",
      title: "已送出！",
      text: "簡易留言已成功送出",
      timer: 1500,
      showConfirmButton: false,
    });
    forumStore.clearQuickReply(props.post.id);
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
    forumStore.setQuickReplySubmitting(props.post.id, false);
  }
}

function cancelReply() {
  showReply.value = false;
  forumStore.cancelQuickReply(props.post.id);
  showQuickReply.value = false;
  replyTab.value = null;
}

/**
 * 點擊發文時間跳轉到該篇內容的詳細頁面
 */
function goToPostDetail(event) {
  // 阻止事件冒泡，避免觸發其他點擊事件
  if (event) {
    event.stopPropagation();
  }

  // 所有類型的 content 都可以點擊進入詳情頁
  router.push({ name: "forum-thread", params: { id: props.post.id } });
}

function goToParentThread(event) {
  if (event) {
    event.stopPropagation();
  }

  if (!parentContentId.value) return;
  router.push({ name: "forum-thread", params: { id: parentContentId.value } });
}

/**
 * 點擊高亮的"正在回覆此內容"切換對應草稿的展開/最小化狀態
 */
function handleReplyingTargetClick() {
  if (!isReplyingTarget.value) return;

  // 找到正在回覆此內容的草稿
  const targetDraft = forumStore.drafts.find(
    (draft) => draft.replyingToContentId === props.post.id,
  );

  if (targetDraft) {
    // Toggle 該草稿的展開/最小化狀態
    forumStore.toggleDraftMinimize(targetDraft.id, !targetDraft.isMinimized);
  }
}

/**
 * 處理互動成功
 */
async function handleInteractionSuccess(data) {
  // 隱藏功能的特殊處理
  if (data.categoryId === 2 && data.categoryName === "hide") {
    // 重新載入主題列表以反映後端狀態
    // fetchTopics 會更新分類統計
    await forumStore.fetchTopics();

    // 如果在詳情視圖，也重新載入線程詳情以更新回文列表
    if (forumStore.threadDetail?.id) {
      forumStore.fetchThreadDetail(forumStore.threadDetail.id);
    }
  }
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

/**
 * 處理編輯內容
 */
function handleEditContent(contentId) {
  // 開啟編輯模式 - 使用 NewTopicModal 或 NewTopicSidebar 進行編輯
  forumStore.openEditMode(contentId, props.post);
}

/**
 * 處理刪除內容
 */
async function handleDeleteContent(contentId) {
  // 刪除成功後重新載入數據
  // fetchTopics 會更新分類統計
  await forumStore.fetchTopics();

  if (forumStore.threadDetail.id) {
    forumStore.fetchThreadDetail(forumStore.threadDetail.id);
  }
}
</script>

<style scoped>
/* 正在回覆的目標內容高亮效果 */
.thread-post .card.replying-target {
  border: 2px solid #145369;
  box-shadow: 0 0 20px rgba(20, 83, 105, 0.3);
  background: linear-gradient(
    135deg,
    rgba(20, 83, 105, 0.08) 0%,
    rgba(20, 83, 105, 0.03) 100%
  );
  position: relative;
  animation: pulse-border 2s ease-in-out infinite;
  cursor: pointer;
  transition: all 0.3s ease;
}

.thread-post .card.replying-target:hover {
  box-shadow: 0 0 30px rgba(20, 83, 105, 0.5);
  background: linear-gradient(
    135deg,
    rgba(20, 83, 105, 0.12) 0%,
    rgba(20, 83, 105, 0.06) 100%
  );
}

.thread-post .card.replying-target::before {
  content: "✍️ 正在回覆此內容";
  position: absolute;
  top: -12px;
  left: 12px;
  background: linear-gradient(135deg, #145369 0%, #0f3c4d 100%);
  color: #fff0dd;
  padding: 2px 12px;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(20, 83, 105, 0.4);
  z-index: 7;
}

@keyframes pulse-border {
  0%,
  100% {
    box-shadow: 0 0 20px rgba(20, 83, 105, 0.3);
  }
  50% {
    box-shadow: 0 0 30px rgba(20, 83, 105, 0.5);
  }
}

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
  position: relative;
  z-index: 4;
}

.parent-link-btn {
  height: 22px;
  min-height: 22px;
  padding: 0 0.5rem;
  font-size: 0.75rem;
  line-height: 1;
  border-color: #86b0bd;
  color: #145369;
  background: transparent;
}

.parent-link-btn:hover {
  background-color: #fff0dd;
  border-color: #145369;
  color: #145369;
}

.badge.bg-secondary-light {
  background-color: #d0e8f2 !important;
  color: #145369;
}

.post-time {
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  line-height: 1;
  position: relative;
  z-index: 5;
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
  padding-top: 1rem; /* 上 */
  padding-right: 1rem; /* 右 */
  padding-left: 1rem; /* 左 */
  padding-bottom: 0; /* 下 */
}

.quick-reply-box textarea.form-control {
  resize: none;
  overflow: hidden;
  min-height: 38px;
  line-height: 1.4;
  transition: height 0.12s ease;
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
  z-index: 6;
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
