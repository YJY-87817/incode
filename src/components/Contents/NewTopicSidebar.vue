<script setup>
import { computed, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";
import Swal from "sweetalert2";

const forumStore = useForumStore();
const router = useRouter();
const route = useRoute();

const modalMode = computed(() => forumStore.newTopic?.mode || "post");
const isSupportedMode = computed(() =>
  ["comment", "reply", "commentReply"].includes(modalMode.value)
);

const modalTitle = computed(() => {
  switch (modalMode.value) {
    case "comment":
      return "詳細留言";
    case "reply":
      return "詳細回文";
    case "commentReply":
      return "詳細回覆";
    default:
      return "詳細發布";
  }
});

const submitLabel = computed(() =>
  modalMode.value === "post" ? "發布" : "發布"
);

const titleModel = computed({
  get: () => forumStore.newTopic.title,
  set: (value) => {
    forumStore.newTopic.title = value;
  },
});

const bodyModel = computed({
  get: () => forumStore.newTopic.body,
  set: (value) => {
    forumStore.newTopic.body = value;
  },
});

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

const scrollToReplyTarget = async () => {
  const targetId =
    replyingToContent.value?.id ?? forumStore.replyingToContentId ?? null;

  let threadId = forumStore.replyingToThreadId ?? forumStore.selectedThread;

  console.log("[NewTopicSidebar] 回到原文點擊", {
    targetId,
    threadId,
    replyingToContentId: forumStore.replyingToContentId,
    replyingToContentData: forumStore.replyingToContentData,
    selectedThread: forumStore.selectedThread,
    currentRoute: route.fullPath,
  });

  if (!targetId) return;

  if (!threadId) {
    console.log("[NewTopicSidebar] threadId 為空，嘗試解析", { targetId });
    threadId = await resolveThreadIdByContentId(targetId);
    console.log("[NewTopicSidebar] 解析結果", { targetId, threadId });
    if (threadId) {
      forumStore.replyingToThreadId = threadId;
    }
  }

  if (!threadId) return;

  forumStore.pendingScrollTargetId = targetId;

  console.log("[NewTopicSidebar] 設定 pendingScrollTargetId", {
    pendingScrollTargetId: forumStore.pendingScrollTargetId,
  });

  const currentThreadId = route.params?.id ?? null;
  if (String(currentThreadId ?? "") !== String(threadId)) {
    console.log("[NewTopicSidebar] 導向 forum-thread", {
      currentThreadId,
      targetThreadId: threadId,
    });
    router.push({ name: "forum-thread", params: { id: threadId } });
    return;
  }

  nextTick(() => {
    const targetEl = document.getElementById(`content-${targetId}`);
    console.log("[NewTopicSidebar] 同頁查找目標", {
      targetId,
      found: Boolean(targetEl),
    });
    if (targetEl) {
      targetEl.scrollIntoView({ behavior: "smooth", block: "center" });
      forumStore.pendingScrollTargetId = null;
      console.log("[NewTopicSidebar] 已捲動並清除 pendingScrollTargetId");
    }
  });
};

const closeSidebar = () => {
  forumStore.toggleNewTopicSidebar(false);
};

const handleCancelClick = () => {
  // 檢查標題和內容是否有非空白文字
  const hasTitle = titleModel.value && titleModel.value.trim().length > 0;
  const hasContent = bodyModel.value && bodyModel.value.trim().length > 0;

  if (hasTitle || hasContent) {
    // 如果有內容，最小化 sidebar
    forumStore.toggleNewTopicSidebarMinimize(true);
  } else {
    // 如果沒有內容，直接關閉
    closeSidebar();
  }
};

const expandSidebar = () => {
  forumStore.toggleNewTopicSidebarMinimize(false);
};

const openLargeEditor = () => {
  forumStore.toggleNewTopicModal(true);
  forumStore.toggleNewTopicSidebar(false);
};

const resetNewTopicDraft = () => {
  forumStore.newTopic.title = "";
  forumStore.newTopic.body = "";
  forumStore.newTopic.categoryId = null;
  forumStore.newTopic.parentContentId = null;
  forumStore.newTopic.categorySearch = "";
  forumStore.newTopic.mode = "post";
  forumStore.newTopic.contentType = CONTENT_TYPES.POST;
};

const submitSidebar = async () => {
  if (!titleModel.value || !bodyModel.value) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請輸入標題與內容",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return;
  }

  const parentContentId = forumStore.newTopic.parentContentId ?? null;
  const categoryId = forumStore.newTopic.categoryId ?? null;

  if (!parentContentId && !categoryId) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請選擇分類",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return;
  }

  const resolvedContentType =
    forumStore.newTopic?.contentType ??
    (parentContentId ? CONTENT_TYPES.REPLY : CONTENT_TYPES.POST);

  const dto = {
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId,
    parentContentId,
    title: titleModel.value,
    body: bodyModel.value,
    type: resolvedContentType,
  };

  try {
    await apiClient.post("/ContentApi", dto);
    await Swal.fire({
      icon: "success",
      title: "發表成功",
      showConfirmButton: false,
      timer: 1500,
    });

    forumStore.pagination.currentPage = 1;
    await forumStore.fetchTopics();

    if (parentContentId && forumStore.selectedThread) {
      await forumStore.fetchThreadDetail(forumStore.selectedThread);
    }

    resetNewTopicDraft();
    forumStore.toggleNewTopicSidebar(false);
  } catch (error) {
    console.error("[NewTopicSidebar] submit - 發表失敗:", error);
    await Swal.fire({
      icon: "error",
      title: "發表失敗",
      text: "發表失敗，請稍後再試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
  }
};
</script>

<template>
  <!-- 最小化的 banner -->
  <div
    v-if="
      forumStore.showNewTopicSidebar &&
      forumStore.isNewTopicSidebarMinimized &&
      isSupportedMode
    "
    class="new-topic-banner"
    @click="expandSidebar"
  >
    <div class="banner-content">
      <div class="banner-jump" @click.stop="scrollToReplyTarget">回到原文</div>
      <div class="banner-left">
        <div class="banner-title">
          <span v-if="titleModel.trim()">{{ titleModel }}</span>
          <span v-else class="text-muted">未命名的貼文</span>
        </div>
      </div>
      <div class="banner-right">
        <button
          type="button"
          class="btn btn-sm banner-close-btn"
          @click.stop="closeSidebar"
        >
          <i class="fa-solid fa-xmark"></i>
        </button>
      </div>
    </div>
  </div>

  <!-- 完整的編輯器 -->
  <div
    v-if="
      forumStore.showNewTopicSidebar &&
      !forumStore.isNewTopicSidebarMinimized &&
      isSupportedMode
    "
    class="card shadow-sm mb-4 new-topic-sidebar"
  >
    <div class="card-header bg-white">
      <div class="d-flex align-items-start justify-content-between gap-2">
        <div>
          <h6 class="mb-1">{{ modalTitle }}</h6>
        </div>
        <div class="d-flex align-items-center gap-2">
          <button
            type="button"
            class="btn btn-sm btn-outline-primary"
            @click="openLargeEditor"
          >
            放大
          </button>
          <button
            type="button"
            class="btn btn-sm btn-outline-secondary"
            @click="handleCancelClick"
          >
            關閉
          </button>
        </div>
      </div>
    </div>
    <div class="card-body pt-0 pb-0 sidebar-body">
      <div class="mb-3">
        <label class="form-label">標題</label>
        <input v-model="titleModel" class="form-control" />
      </div>
      <div class="mb-3 editor-section">
        <label class="form-label">內容</label>
        <div class="editor-wrapper">
          <TiptapEditor v-model="bodyModel" placeholder="請輸入貼文內容..." />
        </div>
      </div>
      <div class="d-flex justify-content-end gap-2 sidebar-actions">
        <button
          type="button"
          class="btn btn-outline-secondary"
          @click="handleCancelClick"
        >
          取消
        </button>
        <button type="button" class="btn btn-primary" @click="submitSidebar">
          {{ submitLabel }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 最小化的 banner 樣式 */
.new-topic-banner {
  position: sticky;
  top: 0;
  bottom: auto;
  left: 0;
  right: 0;
  z-index: 2;
  background: white;
  border: 1px solid #d2d6da;
  border-radius: 0.375rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  /* padding: 0.75rem 1rem; */
  margin-top: 0;
  margin-bottom: 0.25rem;
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
  background-color: #e8f0f3;
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
  top: 0;
  bottom: 0;
  height: 100vh;
  z-index: 2;
  display: flex;
  flex-direction: column;
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
