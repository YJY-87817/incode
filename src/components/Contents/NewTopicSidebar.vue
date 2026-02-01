<script setup>
import { computed } from "vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";
import Swal from "sweetalert2";

const forumStore = useForumStore();

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

const closeSidebar = () => {
  forumStore.toggleNewTopicSidebar(false);
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
  <div
    v-if="forumStore.showNewTopicSidebar && isSupportedMode"
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
            @click="closeSidebar"
          >
            關閉
          </button>
        </div>
      </div>
    </div>
    <div class="card-body pt-0 pb-0 sidebar-body">
      <!-- 顯示正在回覆的內容資訊 -->
      <div v-if="replyingToContent" class="replying-to-info mb-3">
        <div class="replying-to-header">
          <i class="fas fa-reply me-2"></i>
          <span>正在回覆</span>
        </div>
        <div class="replying-to-content">
          <div class="d-flex align-items-start gap-2">
            <div class="flex-grow-1">
              <div class="text-muted small mb-1">
                <strong>{{ replyingToContent.author }}</strong>
                <span class="mx-1">·</span>
                <span>{{ replyingToContent.createdAt }}</span>
              </div>
              <div class="replying-to-text">
                {{ replyingToContent.contentText || replyingToContent.title }}
              </div>
            </div>
          </div>
        </div>
      </div>

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
          @click="closeSidebar"
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
.replying-to-info {
  background: linear-gradient(
    135deg,
    rgba(233, 30, 99, 0.08) 0%,
    rgba(233, 30, 99, 0.03) 100%
  );
  border: 2px solid #e91e63;
  border-radius: 8px;
  padding: 12px;
  margin-top: 12px;
}

.replying-to-header {
  font-weight: 600;
  color: #e91e63;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  font-size: 0.9rem;
}

.replying-to-content {
  padding: 8px;
  background: white;
  border-radius: 6px;
  border-left: 3px solid #e91e63;
}

.replying-to-text {
  color: #145369;
  font-size: 0.875rem;
  line-height: 1.5;
  max-height: 80px;
  overflow-y: auto;
  word-wrap: break-word;
  white-space: pre-wrap;
}

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

.new-topic-sidebar .sidebar-actions {
  margin-top: auto;
}

.new-topic-sidebar .form-control {
  border: 1px solid #d2d6da;
  border-radius: 0.375rem;
}

.new-topic-sidebar .form-control:focus {
  border-color: #e91e63;
  box-shadow: 0 0 0 0.2rem rgba(233, 30, 99, 0.25);
}
</style>
