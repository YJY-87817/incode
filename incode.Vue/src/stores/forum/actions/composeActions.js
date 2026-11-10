import contentApi from "@/api/contentApi.js";
import { useAuthStore } from "@/stores/users/auth.js";
import { CONTENT_TYPES } from "@/stores/forum/constants.js";
import { clearTopicPageCache } from "@/stores/forum/topicQuery.js";

export const composeActions = {
  // ========== 新主題管理 ==========
  toggleNewTopicBox() {
    this.showNewTopicBox = !this.showNewTopicBox;
  },

  async submitNewTopic() {
    if (!this.newTopic.title.trim()) {
      alert("請輸入標題");
      return;
    }
    if (!this.newTopic.body.trim()) {
      alert("請輸入內容");
      return;
    }

    // 若有 parentContentId，不需檢查 categoryId（會使用 parent 的 category）
    // 若無 parentContentId，需檢查 categoryId
    if (!this.newTopic.parentContentId && !this.newTopic.categoryId) {
      alert("請選擇分類");
      return;
    }

    if (this.isSubmittingNewTopic) return;
    this.isSubmittingNewTopic = true;

    const authStore = useAuthStore();
    const currentUserId = Number(authStore.user?.id);
    if (!Number.isFinite(currentUserId) || currentUserId <= 0) {
      alert("請先登入後再發表");
      this.isSubmittingNewTopic = false;
      return;
    }

    const dto = {
      userId: currentUserId,
      categoryId: this.newTopic.categoryId,
      parentContentId: this.newTopic.parentContentId,
      title: this.newTopic.title.trim(),
      body: this.newTopic.body.trim(),
      type: this.newTopic.parentContentId
        ? CONTENT_TYPES.REPLY
        : CONTENT_TYPES.POST,
    };

    try {
      await contentApi.createContent(dto);
      alert("發表成功！");
      this.newTopic.title = "";
      this.newTopic.body = "";
      this.showNewTopicBox = false;
      this.pagination.currentPage = 1;
      clearTopicPageCache();
      await this.fetchTopics();
    } catch (error) {
      console.error("發表失敗:", error);
      alert("發表失敗，請稍後再試");
    } finally {
      this.isSubmittingNewTopic = false;
    }
  },

  cancelNewTopic() {
    this.newTopic.title = "";
    this.newTopic.body = "";
    this.newTopic.mode = "post";
    this.newTopic.contentType = CONTENT_TYPES.POST;
    this.newTopic.showEntrustMission = false;
    this.newTopic.entrustMission = {
      title: "",
      detail: "",
      rewardPoints: "",
      category: "",
      deadline: "",
    };
    this.showNewTopicBox = false;
  },

  updateNewTopicCategory(categoryId) {
    this.newTopic.categoryId = categoryId;
  },

  // ========== 多個草稿管理 ==========
  createDraft(options = {}) {
    const draftId = this.nextDraftId++;
    const newDraft = {
      id: draftId,
      title: options.title || "",
      body: options.body || "",
      categoryId: options.categoryId || null,
      parentContentId: options.parentContentId || null,
      categorySearch: options.categorySearch || "",
      mode: options.mode || "post",
      contentType: options.contentType || CONTENT_TYPES.POST,
      editingContentId: options.editingContentId || null,
      isMinimized: false,
      replyingToContentId: options.replyingToContentId || null,
      replyingToContentData: options.replyingToContentData || null,
      replyingToThreadId: options.replyingToThreadId || null,
      showEntrustMission: Boolean(options.showEntrustMission),
      entrustMission: options.entrustMission || {
        title: "",
        detail: "",
        rewardPoints: "",
        category: "",
        deadline: "",
      },
    };
    this.drafts.push(newDraft);
    return draftId;
  },

  updateDraft(draftId, updates) {
    const draft = this.drafts.find((d) => d.id === draftId);
    if (draft) {
      Object.assign(draft, updates);
    }
  },

  removeDraft(draftId) {
    const index = this.drafts.findIndex((d) => d.id === draftId);
    if (index !== -1) {
      this.drafts.splice(index, 1);
    }
  },

  toggleDraftMinimize(draftId, minimized) {
    const draft = this.drafts.find((d) => d.id === draftId);
    if (draft) {
      draft.isMinimized = minimized;
    }
  },

  // ========== 舊有功能保留 ==========
  toggleNewTopicModal(value) {
    this.showNewTopicModal = value;
  },

  toggleNewTopicSidebar(
    value,
    contentId = null,
    contentData = null,
    threadId = null,
    clearReplyInfo = true,
  ) {
    this.showNewTopicSidebar = value;
    if (value) {
      // 只有在提供了新的 contentId 時才更新回覆信息
      if (contentId !== null) {
        this.replyingToContentId = contentId;
        this.replyingToContentData = contentData;
        this.replyingToThreadId =
          threadId ?? this.selectedThread ?? contentId;
      }
      this.isNewTopicSidebarMinimized = false;
    } else {
      // 只有在 clearReplyInfo 為 true 時才清空回覆信息
      if (clearReplyInfo) {
        this.replyingToContentId = null;
        this.replyingToContentData = null;
        this.replyingToThreadId = null;
        this.pendingScrollTargetId = null;
      }
      this.isNewTopicSidebarMinimized = false;
    }
  },

  toggleNewTopicSidebarMinimize(minimized) {
    this.isNewTopicSidebarMinimized = minimized;
  },
};
