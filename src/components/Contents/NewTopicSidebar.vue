<script setup>
import { computed, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";
import Swal from "sweetalert2";
import { useForumPost } from "@/composables/useForumPost";

const forumStore = useForumStore();
const router = useRouter();
const route = useRoute();
const { getModeTitle, publishPost, hasDraftContent } = useForumPost();

// 獲取所有草稿
const allDrafts = computed(() => forumStore.drafts || []);

const submitLabel = "發布";

// 滾動到回覆目標的函數
const scrollToReplyTarget = async (draft) => {
  const targetId = draft.replyingToContentId;
  let threadId = draft.replyingToThreadId;

  if (!targetId) return;

  if (!threadId) {
    threadId = await resolveThreadIdByContentId(targetId);
    if (threadId) {
      forumStore.updateDraft(draft.id, { replyingToThreadId: threadId });
    }
  }

  if (!threadId) return;

  forumStore.pendingScrollTargetId = targetId;

  const currentThreadId = route.params?.id ?? null;
  if (String(currentThreadId ?? "") !== String(threadId)) {
    router.push({ name: "forum-thread", params: { id: threadId } });
    return;
  }

  nextTick(() => {
    const targetEl = document.getElementById(`content-${targetId}`);
    if (targetEl) {
      const offsetTop =
        targetEl.getBoundingClientRect().top + window.scrollY - 60;
      window.scrollTo({ top: offsetTop, behavior: "smooth" });
      forumStore.pendingScrollTargetId = null;
    }
  });
};

const replyingToContent = computed(() => forumStore.replyingToContentData);

const fetchContentDetail = async (contentId) => {
  const res = await apiClient.get(`/ContentApi/${contentId}`);
  return res?.contentId ? res : (res?.data ?? res);
};

const resolveThreadIdByContentId = async (contentId) => {
  try {
    const detail = await fetchContentDetail(contentId);
    if (!detail) return null;

    const type = detail.type ?? detail.contentType ?? detail.content_type;
    const currentId = detail.contentId ?? detail.id ?? detail.content_id;
    const parentId =
      detail.parentContentId ?? detail.parent_content_id ?? detail.parentId;

    if (type === CONTENT_TYPES.POST || !parentId) return currentId;
    if (type === CONTENT_TYPES.REPLY) return parentId;

    // COMMENT or COMMENT_REPLY: need to walk up
    const parent = await fetchContentDetail(parentId);
    if (!parent) return parentId;

    const parentType = parent.type ?? parent.contentType ?? parent.content_type;
    const parentParentId =
      parent.parentContentId ?? parent.parent_content_id ?? parent.parentId;

    if (parentType === CONTENT_TYPES.REPLY) {
      return parentParentId ?? parentId;
    }

    if (parentType === CONTENT_TYPES.POST) {
      return parentId;
    }

    if (parentType === CONTENT_TYPES.COMMENT) {
      if (!parentParentId) return parentId;
      const grandParent = await fetchContentDetail(parentParentId);
      if (!grandParent) return parentParentId;

      const gpType =
        grandParent.type ?? grandParent.contentType ?? grandParent.content_type;
      const gpParentId =
        grandParent.parentContentId ??
        grandParent.parent_content_id ??
        grandParent.parentId;

      if (gpType === CONTENT_TYPES.REPLY) {
        return gpParentId ?? parentParentId;
      }

      if (gpType === CONTENT_TYPES.POST) {
        return parentParentId;
      }
    }

    return parentId;
  } catch (error) {
    console.error("[NewTopicSidebar] resolveThreadIdByContentId 失敗", error);
    return null;
  }
};

// 處理草稿操作
const handleCancelClick = (draft) => {
  if (hasDraftContent(draft.title, draft.body)) {
    // 如果有內容，最小化
    forumStore.toggleDraftMinimize(draft.id, true);
  } else {
    // 如果沒有內容，直接刪除
    forumStore.removeDraft(draft.id);
  }
};

const expandDraft = (draft) => {
  // 最小化所有其他展開的編輯器
  allDrafts.value.forEach((d) => {
    if (d.id !== draft.id && !d.isMinimized) {
      forumStore.toggleDraftMinimize(d.id, true);
    }
  });

  // 展開當前編輯器
  forumStore.toggleDraftMinimize(draft.id, false);
};

const closeDraft = (draft) => {
  forumStore.removeDraft(draft.id);
};

const hasVisibleDraftContent = (draft) => {
  return hasDraftContent(draft.title, draft.body);
};

const openLargeEditor = (draft) => {
  // 將草稿內容複製到 newTopic，然後打開 modal
  forumStore.newTopic.title = draft.title;
  forumStore.newTopic.body = draft.body;
  forumStore.newTopic.categoryId = draft.categoryId;
  forumStore.newTopic.parentContentId = draft.parentContentId;
  forumStore.newTopic.categorySearch = draft.categorySearch;
  forumStore.newTopic.mode = draft.mode;
  forumStore.newTopic.contentType = draft.contentType;
  forumStore.replyingToContentId = draft.replyingToContentId;
  forumStore.replyingToContentData = draft.replyingToContentData;
  forumStore.replyingToThreadId = draft.replyingToThreadId;

  // 記錄當前編輯的草稿 ID，以便從 modal 回來時同步
  forumStore.currentEditingDraftId = draft.id;

  forumStore.toggleNewTopicModal(true);
  // 暫時最小化這個草稿
  forumStore.toggleDraftMinimize(draft.id, true);
};

const submitDraft = async (draft) => {
  // 使用 composable 的 publishPost 進行驗證和提交
  const success = await publishPost({
    title: draft.title,
    body: draft.body,
    categoryId: draft.categoryId,
    parentContentId: draft.parentContentId,
    contentType: draft.contentType,
  });

  if (!success) {
    return;
  }

  // 刪除這個草稿
  forumStore.removeDraft(draft.id);
};

// 更新草稿內容的函數
const updateDraftTitle = (draft, value) => {
  forumStore.updateDraft(draft.id, { title: value });
};

const updateDraftBody = (draft, value) => {
  forumStore.updateDraft(draft.id, { body: value });
};
</script>

<template>
  <div class="new-topic-sidebar-stack">
    <!-- 所有展開的編輯器 -->
    <div class="editors-container">
      <template v-for="draft in allDrafts" :key="`editor-${draft.id}`">
        <div
          v-if="!draft.isMinimized"
          class="card shadow-sm mb-4 new-topic-sidebar"
        >
          <div class="card-header bg-white">
            <div class="d-flex align-items-start justify-content-between gap-2">
              <div>
                <h6 class="mb-1">{{ getModeTitle(draft.mode) }}</h6>
              </div>
              <div class="d-flex align-items-center gap-2">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-primary"
                  @click="openLargeEditor(draft)"
                >
                  放大
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-secondary"
                  @click="handleCancelClick(draft)"
                >
                  關閉
                </button>
              </div>
            </div>
          </div>
          <div class="card-body pt-0 pb-0 sidebar-body">
            <div class="mb-3">
              <label class="form-label">標題</label>
              <input
                :value="draft.title"
                @input="updateDraftTitle(draft, $event.target.value)"
                class="form-control"
              />
            </div>
            <div class="mb-3 editor-section">
              <label class="form-label">內容</label>
              <div class="editor-wrapper">
                <TiptapEditor
                  :modelValue="draft.body"
                  @update:modelValue="updateDraftBody(draft, $event)"
                  placeholder="請輸入貼文內容..."
                />
              </div>
            </div>
            <div class="d-flex justify-content-end gap-2 sidebar-actions">
              <button
                type="button"
                class="btn btn-outline-secondary"
                @click="handleCancelClick(draft)"
              >
                取消
              </button>
              <button
                type="button"
                class="btn btn-primary"
                @click="submitDraft(draft)"
              >
                {{ submitLabel }}
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- 所有最小化的 banner 放在底部固定容器中 -->
    <div
      v-if="allDrafts.some((d) => d.isMinimized && hasVisibleDraftContent(d))"
      class="banners-container"
    >
      <template v-for="draft in allDrafts" :key="`banner-${draft.id}`">
        <div
          v-if="draft.isMinimized && hasVisibleDraftContent(draft)"
          class="new-topic-banner"
          @click="expandDraft(draft)"
        >
          <div class="banner-content">
            <!-- 雙向繫結，帶我回去 banner-jump -->
            <div
              v-if="draft.replyingToContentId || draft.parentContentId"
              class="banner-jump"
              @click.stop="scrollToReplyTarget(draft)"
            >
              <i class="fa-solid fa-people-pulling"></i>
            </div>
            <div class="banner-left">
              <div class="banner-title">
                <span v-if="draft.title.trim()">{{ draft.title }}</span>
                <span v-else class="text-muted">未命名的草稿</span>
              </div>
            </div>
            <div class="banner-right">
              <button
                type="button"
                class="btn btn-sm banner-close-btn"
                @click.stop="closeDraft(draft)"
              >
                <i class="fa-solid fa-xmark"></i>
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
/* Banner 容器：固定在底部 */
.new-topic-sidebar-stack {
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 0;
  height: 100%;
  overflow: visible;
}

.banners-container {
  position: sticky;
  bottom: 0;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  z-index: 50;
  background: transparent;
  margin-top: auto;
}

.editors-container {
  position: relative;
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

/* 最小化的 banner 樣式 */
.new-topic-banner {
  position: relative;
  background: transparent;
  border: 1px solid #d2d6da;
  border-radius: 0.375rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  animation: slideUp 0.3s ease-out;
}

@keyframes slideUp {
  from {
    transform: translateY(100%);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.banner-content {
  display: flex;
  align-items: stretch;
  justify-content: space-between;
  gap: 0;
  max-width: 100%;
}

.banner-left {
  flex: 1;
  min-width: 0;
  display: flex;
}

.banner-jump {
  flex: 0 0 20%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem 0.5rem;
  border-right: 1px solid #d2d6da;
  font-weight: 600;
  color: #145369;
  user-select: none;
  cursor: pointer;
}

.banner-right {
  flex: 0 0 20%;
  display: flex;
  align-items: stretch;
}

.banner-title {
  flex: 1;
  min-width: 0;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  padding: 0.25rem;
  display: flex;
  align-items: center;
}

.banner-title .text-muted {
  color: #6c757d;
  font-style: italic;
}

.new-topic-banner {
  cursor: pointer;
  transition:
    background-color 0.2s ease,
    border-color 0.2s ease;
}

.new-topic-banner:hover {
  background-color: #f0f5f8;
  border-color: #145369;
}

.new-topic-banner .banner-jump:hover {
  background-color: #145369;
  color: #fff;
}

.new-topic-banner .btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
  white-space: nowrap;
}

.new-topic-banner .banner-close-btn {
  width: 100%;
  height: 100%;
  flex-shrink: 0;
  padding: 0 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  border-radius: 0;
  border-left: 1px solid #d2d6da;
}

.new-topic-banner .banner-close-btn:hover {
  background-color: #dc3545;
  border-color: #dc3545;
  color: #fff;
}

/* 完整編輯器樣式 */
.new-topic-sidebar :deep(.tiptap-editor-wrapper) {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  max-height: 100%;
  overflow: auto;
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.new-topic-sidebar :deep(.tiptap-editor-wrapper)::-webkit-scrollbar {
  width: 0;
  height: 0;
}

.new-topic-sidebar :deep(.editor-content) {
  min-height: 180px;
  flex: 1;
}

.new-topic-sidebar {
  position: sticky;
  height: auto;
  max-height: calc(100vh - 120px);
  z-index: 60;
  display: flex;
  flex-direction: column;
  top: 0;
  margin-bottom: 1rem;
  background: white;
  border: 1px solid #dee2e6;
}

.new-topic-sidebar .card-header {
  padding-bottom: 0;
}

.new-topic-sidebar .sidebar-body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.new-topic-sidebar .editor-section {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.new-topic-sidebar .editor-wrapper {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  max-height: 100%;
}

.new-topic-sidebar .btn-primary {
  background-color: #145369;
  border-color: #145369;
}

.new-topic-sidebar .btn-primary:hover {
  background-color: #0f3c4d;
  border-color: #0f3c4d;
}

.new-topic-sidebar .btn-outline-primary {
  color: #145369;
  border-color: #145369;
}

.new-topic-sidebar .btn-outline-primary:hover {
  background-color: #145369;
  border-color: #145369;
  color: white;
}

.new-topic-sidebar .sidebar-actions {
  margin-top: auto;
}

.new-topic-sidebar .form-control {
  border: 1px solid #d2d6da;
  border-radius: 0.375rem;
}

/* banner 中的按鈕樣式 */
.new-topic-banner .btn-primary {
  background-color: #145369;
  border-color: #145369;
}

.new-topic-banner .btn-primary:hover {
  background-color: #0f3c4d;
  border-color: #0f3c4d;
}
</style>
