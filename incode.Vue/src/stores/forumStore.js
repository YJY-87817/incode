import { defineStore } from "pinia";
import { CONTENT_TYPES } from "@/stores/forum/constants.js";

import { categoryActions } from "@/stores/forum/actions/categoryActions.js";
import { topicActions } from "@/stores/forum/actions/topicActions.js";
import { threadActions } from "@/stores/forum/actions/threadActions.js";
import { composeActions } from "@/stores/forum/actions/composeActions.js";
import { interactionActions } from "@/stores/forum/actions/interactionActions.js";
import { uiActions } from "@/stores/forum/actions/uiActions.js";

// 其他檔案從這裡匯入 CONTENT_TYPES（定義在 forum/constants.js）
export { CONTENT_TYPES } from "@/stores/forum/constants.js";

export const useForumStore = defineStore("forum", {
  state: () => ({
    // ========== 全局狀態 ==========
    windowWidth: typeof window !== "undefined" ? window.innerWidth : 1024,

    // ========== 分類相關 ==========
    categories: [],
    selectedCategory: null,

    // ========== 主題列表 ==========
    topics: [],
    threadReplyStats: {}, // { [threadId]: { replyCount, lastReplyTime, lastReplyUser, lastReplyUserId, fetchedAt } }
    replyStatsCacheTtlMs: 60000,
    replyStatsFallbackEnabled: false,
    isLoading: false,
    searchKeyword: "",
    pagination: {
      currentPage: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    },
    sortOption: "latest",

    // ========== 帖文詳情 ==========
    selectedThread: null,
    threadDetail: {
      id: null,
      title: "",
      author: "",
      createdAt: "",
      content: "",
      contentHtml: "",
      contentText: "",
      imageUrls: [],
      categoryId: null,
      categoryName: null,
      parentContentId: null,
      type: null,
      isHot: false,
      hasEntrustMission: false,
      likeCount: 0,
      entrustMissionId: null,
      entrustMissionUserId: null,
      rewardPoints: 0,
      bestAnswerContentId: null,
      bestAnswerSelected: false,
    },
    threadPosts: [],
    threadComments: [],
    threadCurrentPage: 1,
    threadItemsPerPage: 5,
    threadCommentsCurrentPage: 1,
    threadCommentsItemsPerPage: 5,
    threadRepliesPagination: {
      currentPage: 1,
      pageSize: 5,
      totalCount: 0,
      totalPages: 0,
      isLoading: false,
    },
    threadCommentsPagination: {
      currentPage: 1,
      pageSize: 5,
      totalCount: 0,
      totalPages: 0,
      isLoading: false,
    },
    childRepliesByParentId: {}, // { [parentId]: { items, currentPage, pageSize, totalCount, totalPages, isLoading, loaded } }
    childRepliesCountByParentId: {}, // { [parentId]: { totalCount, isLoading } } 用於預加載數量

    // ========== 新主題表單 ==========
    showNewTopicBox: false,
    newTopic: {
      title: "",
      body: "",
      categoryId: null,
      parentContentId: null,
      categorySearch: "",
      mode: "post",
      contentType: CONTENT_TYPES.POST,
      editingContentId: null, // 新增：正在編輯的內容 ID
      showEntrustMission: false,
      entrustMission: {
        title: "",
        detail: "",
        rewardPoints: "",
        category: "",
        deadline: "",
      },
    },
    isSubmittingNewTopic: false,
    isUploadingNewTopicImages: false,

    // ========== 多個草稿相關 ==========
    drafts: [], // { id, title, body, categoryId, parentContentId, categorySearch, mode, contentType, isMinimized, replyingToContentId, replyingToContentData, replyingToThreadId, showEntrustMission, entrustMission }
    nextDraftId: 1,
    currentEditingDraftId: null, // 記錄當前正在 modal 中編輯的草稿 ID

    // ========== 其他 ==========
    showNewTopicModal: false,
    showNewTopicSidebar: false,
    isNewTopicSidebarMinimized: false,
    lastRefreshTime: 0,

    // ========== 視覺連結 ==========
    replyingToContentId: null,
    replyingToContentData: null,
    replyingToThreadId: null,
    pendingScrollTargetId: null,

    // ========== 互動按鈕相關 ==========
    currentInteractionContentId: null,
    currentInteractionAuthorUserId: null,
    interactionStates: {}, // { contentId: { 1: false, 2: false, 3: false, 4: false } }
    interactionLoadingStates: {}, // { contentId: { 1: false, 2: false, 3: false, 4: false } }
    interactionStateInitialized: {}, // { contentId: true/false }
    interactionInitBatchSize: 5,

    // ========== 快速回覆相關 ==========
    quickReplies: {}, // { contentId: { content: "", isSubmitting: false, show: false } }

    // ========== 更多菜單相關 ==========
    openMoreMenuId: null, // 當前打開的"更多"菜單的 contentId
  }),

  getters: {
    /**
     * 計算帖文詳情的總頁數
     */
    threadTotalPages: (state) => {
      return state.threadRepliesPagination.totalPages || 0;
    },

    /**
     * 計算留言的總頁數
     */
    threadCommentsTotalPages: (state) => {
      return state.threadCommentsPagination.totalPages || 0;
    },

    /**
     * 當前頁的回文列表
     */
    paginatedThreadPosts: (state) => {
      return state.threadPosts;
    },

    /**
     * 當前頁的留言列表
     */
    paginatedThreadComments: (state) => {
      return state.threadComments;
    },

    /**
     * 分頁顯示計算
     */
    displayPages: (state) => {
      const { currentPage, totalPages } = state.pagination;
      const pages = [];
      const maxDisplay = 5;

      if (totalPages <= 1) return pages;

      let start = Math.max(1, currentPage - Math.floor(maxDisplay / 2));
      let end = Math.min(totalPages, start + maxDisplay - 1);

      if (end - start < maxDisplay - 1) {
        start = Math.max(1, end - maxDisplay + 1);
      }

      for (let i = start; i <= end; i++) pages.push(i);
      return pages;
    },

    /**
     * 目前選擇的分類名稱
     */
    selectedCategoryName: (state) => {
      const category = state.categories.find(
        (c) => c.id === state.selectedCategory,
      );
      return category?.name || null;
    },

    /**
     * 帶實時統計的分類列表
     * 根據所有可見的主題自動計算每個分類的主題數和回文數
     */
    categoriesWithStats: (state) => {
      if (!state.categories || state.categories.length === 0) {
        return [];
      }

      return state.categories.map((category) => ({
        ...category,
        postCount: category.postCount ?? 0,
      }));
    },
  },

  actions: {
    ...categoryActions,
    ...topicActions,
    ...threadActions,
    ...composeActions,
    ...interactionActions,
    ...uiActions,
  },

  persist: true,
});
