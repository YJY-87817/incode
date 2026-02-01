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
  modalMode.value === "post" ? "發表" : "送出"
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
            放大編輯
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
    <div class="card-body">
      <div class="mb-3">
        <label class="form-label">標題</label>
        <input v-model="titleModel" class="form-control" />
      </div>
      <div class="mb-3">
        <label class="form-label">內容</label>
        <TiptapEditor v-model="bodyModel" placeholder="請輸入貼文內容..." />
      </div>
      <div class="d-flex justify-content-end gap-2">
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
.new-topic-sidebar :deep(.editor-content) {
  min-height: 180px;
}

.new-topic-sidebar {
  position: sticky;
  top: 1rem;
  z-index: 2;
}
</style>
