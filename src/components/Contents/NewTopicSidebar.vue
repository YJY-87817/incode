<script setup>
import { computed, watch } from "vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";
import { useForumPost } from "@/composables/useForumPost";
import Swal from "sweetalert2";

const forumStore = useForumStore();
const { getModeTitle, publishPost, hasDraftContent } = useForumPost();

// 獲取所有草稿
const allDrafts = computed(() => forumStore.drafts || []);

const submitLabel = "發布";

const getDefaultEntrustMission = () => ({
  title: "",
  detail: "",
  rewardPoints: "",
  category: "",
  deadline: "",
});

const getDraftEntrustMission = (draft) =>
  draft?.entrustMission
    ? { ...draft.entrustMission }
    : getDefaultEntrustMission();

const getDraftExtraValues = (draft) => {
  if (!draft?.showEntrustMission) return [];
  const data = getDraftEntrustMission(draft);
  return [
    data.title,
    data.detail,
    data.rewardPoints,
    data.category,
    data.deadline,
  ];
};

const updateDraftEntrustMission = (draft, updates) => {
  const current = getDraftEntrustMission(draft);
  forumStore.updateDraft(draft.id, {
    entrustMission: {
      ...current,
      ...updates,
    },
  });
};

const canUseEntrustMission = (draft) =>
  draft?.mode === "post" && !draft?.parentContentId;

const toggleEntrustMission = (draft) => {
  if (!canUseEntrustMission(draft)) return;
  const next = !draft?.showEntrustMission;
  forumStore.updateDraft(draft.id, {
    showEntrustMission: next,
    entrustMission: next
      ? getDraftEntrustMission(draft)
      : getDefaultEntrustMission(),
  });
};

const validateEntrustMission = async (draft) => {
  if (!draft?.showEntrustMission) return true;

  const data = getDraftEntrustMission(draft);
  const title = String(data.title || "").trim();
  const detail = String(data.detail || "").trim();
  const category = String(data.category || "").trim();
  const rewardPoints = Number(data.rewardPoints);

  if (!title || !detail || !category || !Number.isFinite(rewardPoints)) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請完整填寫委託任務資訊",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return false;
  }

  if (rewardPoints <= 0) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "獎勵點數需大於 0",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return false;
  }

  return true;
};

const buildEntrustMissionPayload = (draft) => {
  if (!draft?.showEntrustMission) return null;
  const data = getDraftEntrustMission(draft);
  const deadline = String(data.deadline || "").trim();
  const rewardPoints = Number(data.rewardPoints);

  return {
    title: String(data.title || "").trim(),
    detail: String(data.detail || "").trim(),
    rewardPoints: Number.isFinite(rewardPoints) ? rewardPoints : 0,
    category: String(data.category || "").trim(),
    deadline: deadline || null,
  };
};

const buildReplyTitle = (baseTitle) => {
  const base = String(baseTitle || "").trim();
  return base ? `Re: ${base}` : "Re:";
};

const isCommentLikeDraft = (draft) => {
  const type = draft?.contentType;
  return type === CONTENT_TYPES.COMMENT || type === CONTENT_TYPES.COMMENT_REPLY;
};

const getReplyBaseTitle = (draft) =>
  draft?.replyingToContentData?.title ||
  draft?.replyingToContentData?.contentText ||
  "";

const syncDraftReplyTitle = (draft) => {
  if (!draft || !isCommentLikeDraft(draft)) return;
  const targetTitle = buildReplyTitle(getReplyBaseTitle(draft));
  if (targetTitle && draft.title !== targetTitle) {
    forumStore.updateDraft(draft.id, { title: targetTitle });
  }
};

// 處理草稿操作
const handleCancelClick = (draft) => {
  if (hasDraftContent(draft.title, draft.body, getDraftExtraValues(draft))) {
    // 如果有內容，最小化
    forumStore.toggleDraftMinimize(draft.id, true);
  } else {
    // 如果沒有內容，直接刪除
    forumStore.removeDraft(draft.id);
  }
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
  forumStore.newTopic.editingContentId = draft.editingContentId || null;
  forumStore.newTopic.showEntrustMission = Boolean(draft.showEntrustMission);
  forumStore.newTopic.entrustMission = getDraftEntrustMission(draft);
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
  const isEditMode = draft.mode === "edit" && draft.editingContentId;
  if (draft.showEntrustMission && !(await validateEntrustMission(draft))) {
    return;
  }

  const entrustMissionPayload = canUseEntrustMission(draft)
    ? buildEntrustMissionPayload(draft)
    : null;

  const success = await publishPost(
    {
      title: draft.title,
      body: draft.body,
      categoryId: draft.categoryId,
      parentContentId: draft.parentContentId,
      contentType: draft.contentType,
      editingContentId: draft.editingContentId,
      entrustMission: entrustMissionPayload,
    },
    Boolean(isEditMode),
  );

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

watch(
  () => allDrafts.value,
  (drafts) => {
    (drafts || []).forEach((draft) => syncDraftReplyTitle(draft));
  },
  { immediate: true, deep: true },
);
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
            <div v-if="!isCommentLikeDraft(draft)" class="mb-3">
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
            <div v-if="canUseEntrustMission(draft)" class="mb-3">
              <button
                type="button"
                class="btn btn-outline-primary btn-entrust-toggle"
                @click="toggleEntrustMission(draft)"
              >
                {{ draft.showEntrustMission ? "取消委託任務" : "發佈委託任務" }}
              </button>
            </div>
            <div
              v-if="draft.showEntrustMission"
              class="entrust-mission-panel border rounded p-3 mb-3"
            >
              <h6 class="mb-3">委託任務資訊</h6>
              <div class="row g-3">
                <div class="col-12 col-md-6">
                  <label class="form-label">任務標題</label>
                  <input
                    :value="draft.entrustMission?.title || ''"
                    class="form-control"
                    placeholder="請輸入任務標題"
                    @input="
                      updateDraftEntrustMission(draft, {
                        title: $event.target.value,
                      })
                    "
                  />
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">任務分類</label>
                  <input
                    :value="draft.entrustMission?.category || ''"
                    class="form-control"
                    placeholder="請輸入任務分類"
                    @input="
                      updateDraftEntrustMission(draft, {
                        category: $event.target.value,
                      })
                    "
                  />
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">獎勵點數</label>
                  <input
                    :value="draft.entrustMission?.rewardPoints || ''"
                    type="number"
                    min="1"
                    step="1"
                    class="form-control"
                    placeholder="請輸入獎勵點數"
                    @input="
                      updateDraftEntrustMission(draft, {
                        rewardPoints: $event.target.value,
                      })
                    "
                  />
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label">截止日期</label>
                  <input
                    :value="draft.entrustMission?.deadline || ''"
                    type="date"
                    class="form-control"
                    @input="
                      updateDraftEntrustMission(draft, {
                        deadline: $event.target.value,
                      })
                    "
                  />
                </div>
                <div class="col-12">
                  <label class="form-label">任務說明</label>
                  <textarea
                    :value="draft.entrustMission?.detail || ''"
                    class="form-control"
                    rows="4"
                    placeholder="請輸入任務內容"
                    @input="
                      updateDraftEntrustMission(draft, {
                        detail: $event.target.value,
                      })
                    "
                  ></textarea>
                </div>
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
  </div>
</template>

<style scoped>
/* 編輯器容器 */
.new-topic-sidebar-stack {
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 0;
  height: 100%;
  overflow: visible;
}

.editors-container {
  position: relative;
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

/* 完整編輯器樣式 */
.new-topic-sidebar :deep(.tiptap-editor-wrapper) {
  display: flex;
  flex-direction: column;
  flex: 0 0 auto;
  min-height: 0;
  max-height: none;
  overflow: visible;
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
  position: relative;
  height: auto;
  max-height: none;
  z-index: 60;
  display: flex;
  flex-direction: column;
  margin-bottom: 1rem;
  background: white;
  border: 1px solid #dee2e6;
}

.new-topic-sidebar .card-header {
  padding-bottom: 0;
}

.new-topic-sidebar .sidebar-body {
  flex: 0 0 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: visible;
}

.new-topic-sidebar .editor-section {
  flex: 0 0 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.new-topic-sidebar .editor-wrapper {
  flex: 0 0 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
  max-height: none;
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

.new-topic-sidebar .btn-entrust-toggle {
  border-color: #145369;
  color: #145369;
}

.new-topic-sidebar .btn-entrust-toggle:hover {
  background-color: #eaf2f4;
  color: #0f4456;
}

.new-topic-sidebar .btn-entrust-toggle:focus,
.new-topic-sidebar .btn-entrust-toggle:active,
.new-topic-sidebar .btn-entrust-toggle.active {
  background-color: #eaf2f4;
  border-color: #145369;
  color: #0f4456;
  box-shadow: none;
}

.new-topic-sidebar .entrust-mission-panel {
  background: #f6f8f9;
  border-color: #d9e2e6;
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
