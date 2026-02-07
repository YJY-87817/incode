import { defineStore } from "pinia";
import apiClient from "@/api/request.js";
import contentInteractionApi from "@/api/contentInteractionApi.js";
import { fetchBodyHtml, toAbsoluteUrl } from "@/utils/contentBody.js";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";

// Content types according to database schema
export const CONTENT_TYPES = {
  POST: 1, // 貼文(出現在貼文版面)
  REPLY: 2, // 回文(出現在貼文版面)
  COMMENT: 3, // 留言
  COMMENT_REPLY: 4, // 留言回覆
};

const normalizeCategory = (raw, fallbackName = "") => {
  if (!raw) return null;
  const source = raw?.data ?? raw;
  if (Array.isArray(source)) return null;

  const id =
    source.id ??
    source.content_category_id ??
    source.categoryId ??
    source.category_id;
  const name =
    source.name ?? source.categoryName ?? source.category_name ?? fallbackName;

  if (!id && !name) return null;

  return {
    id,
    name,
    description: source.description ?? null,
    topicCount: source.topicCount ?? source.topic_count ?? 0,
    postCount: source.postCount ?? source.post_count ?? 0,
  };
};

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
    isLoading: false,
    searchKeyword: "",
    pagination: {
      currentPage: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    },
    sortOption: "createdAt_desc",

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
      type: null,
    },
    threadPosts: [],
    threadComments: [],
    threadCurrentPage: 1,
    threadItemsPerPage: 5,
    threadCommentsCurrentPage: 1,
    threadCommentsItemsPerPage: 5,

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
    },
    isSubmittingNewTopic: false,
    isUploadingNewTopicImages: false,

    // ========== 多個草稿相關 ==========
    drafts: [], // { id, title, body, categoryId, parentContentId, categorySearch, mode, contentType, isMinimized, replyingToContentId, replyingToContentData, replyingToThreadId }
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
      return Math.ceil(state.threadPosts.length / state.threadItemsPerPage);
    },

    /**
     * 計算留言的總頁數
     */
    threadCommentsTotalPages: (state) => {
      return Math.ceil(
        state.threadComments.length / state.threadCommentsItemsPerPage,
      );
    },

    /**
     * 當前頁的回文列表
     */
    paginatedThreadPosts: (state) => {
      const start = (state.threadCurrentPage - 1) * state.threadItemsPerPage;
      return state.threadPosts.slice(start, start + state.threadItemsPerPage);
    },

    /**
     * 當前頁的留言列表
     */
    paginatedThreadComments: (state) => {
      const start =
        (state.threadCommentsCurrentPage - 1) *
        state.threadCommentsItemsPerPage;
      return state.threadComments.slice(
        start,
        start + state.threadCommentsItemsPerPage,
      );
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
        topicCount: category.topicCount ?? 0,
        postCount: category.postCount ?? 0,
      }));
    },
  },

  actions: {
    // ========== 視窗寬度 ==========
    updateWindowWidth(width) {
      this.windowWidth = width;
    },

    // ========== 分類管理 ==========
    async fetchCategories(options = {}) {
      const { forceRefresh = false } = options;

      this.isLoading = true;
      try {
        // 添加時間戳防止緩存
        const params = forceRefresh ? { _t: Date.now() } : {};
        const data = await apiClient.get("/CategoryApi", { params });
        this.categories = data;

        // 不再預設選擇第一個分類
        // 只有在已經有選擇的分類時才同步到 newTopic
        if (this.selectedCategory && data.length > 0) {
          this.newTopic.categoryId = this.selectedCategory;
        }

        console.log("分類數據已更新:", data);
      } catch (error) {
        console.error("載入分類失敗:", error);
        alert("載入分類失敗");
      } finally {
        this.isLoading = false;
      }
    },

    updateThreadReplyStats(threadId, replies) {
      const items = Array.isArray(replies) ? replies : [];
      const replyCount = items.length;
      const sorted = items.slice().sort((a, b) => {
        const aTime = new Date(a.createdAt).getTime();
        const bTime = new Date(b.createdAt).getTime();
        return bTime - aTime;
      });
      const lastReply = sorted[0];

      this.threadReplyStats[threadId] = {
        replyCount,
        lastReplyTime: lastReply?.createdAt ?? null,
        lastReplyUser: lastReply?.username ?? null,
        lastReplyUserId: lastReply?.userId ?? lastReply?.user_id ?? null,
        fetchedAt: Date.now(),
      };
    },

    async fetchThreadReplyStats(threadIds = [], options = {}) {
      const { forceRefresh = false } = options;
      const ids = Array.from(new Set(threadIds.filter(Boolean)));
      if (ids.length === 0) return;

      const now = Date.now();
      const ttl = this.replyStatsCacheTtlMs;
      const pendingIds = ids.filter((id) => {
        if (forceRefresh) return true;
        const cached = this.threadReplyStats[id];
        if (!cached) return true;
        const fetchedAt = cached.fetchedAt || 0;
        return now - fetchedAt > ttl;
      });

      if (pendingIds.length === 0) return;

      await Promise.all(
        pendingIds.map(async (id) => {
          try {
            const res = await contentInteractionApi.getComments(id);
            const replies = Array.isArray(res?.comments) ? res.comments : [];
            this.updateThreadReplyStats(id, replies);
          } catch (error) {
            console.error(`載入主題 ${id} 回應統計失敗:`, error);
          }
        }),
      );
    },

    selectCategory(categoryId, options = {}) {
      const { clearSearch = false } = options || {};
      this.selectedCategory =
        this.selectedCategory === categoryId ? null : categoryId;
      // 同步更新新主題的分類
      this.newTopic.categoryId = this.selectedCategory;
      this.pagination.currentPage = 1;
      if (clearSearch) {
        this.searchKeyword = "";
      }
      this.fetchTopics();
    },

    async createCategory(
      categoryName,
      description = null,
      parentCategoryId = null,
    ) {
      const name = String(categoryName ?? "").trim();
      if (!name) {
        throw new Error("分類名稱不可為空");
      }

      try {
        const payload = {
          parentCategoryId,
          name,
          description: description ?? null,
        };
        const created = await apiClient.post("/CategoryApi", payload);
        const normalized = normalizeCategory(created, name);

        await this.fetchCategories();

        if (normalized?.id) return normalized;
        return this.categories.find((c) => c.name === name) || null;
      } catch (error) {
        console.error("新增分類失敗:", error);
        throw error;
      }
    },

    // ========== 主題列表 ==========
    async fetchTopics(categoryId = null, page = null) {
      const targetCategory =
        categoryId !== null ? categoryId : this.selectedCategory;
      const targetPage = page !== null ? page : this.pagination.currentPage;

      this.isLoading = true;

      try {
        const pageSize = this.pagination.pageSize;
        const keyword = this.searchKeyword.trim();

        const params = {
          page: targetPage,
          pageSize,
          types: `${CONTENT_TYPES.POST},${CONTENT_TYPES.REPLY}`,
          sortBy: "latest",
        };

        if (targetCategory) params.categoryId = targetCategory;
        if (keyword) params.keyword = keyword;

        const data = await apiClient.get("/ContentApi", { params });
        const items = Array.isArray(data?.items) ? data.items : [];
        const totalCount = data?.totalCount ?? items.length;
        const totalPages =
          data?.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize));
        const currentPage = data?.currentPage ?? targetPage;

        this.pagination = {
          currentPage,
          pageSize,
          totalCount,
          totalPages,
        };

        const itemsWithHtml = await Promise.all(
          items.map(async (item) => {
            const bodyHtml = await fetchBodyHtml(item.body);
            const { sanitizedHtml, textContent, imageUrls } =
              normalizeRichContent(bodyHtml, toAbsoluteUrl);
            return {
              ...item,
              bodyHtml,
              bodyText: textContent,
              bodyImages: imageUrls,
              bodySanitizedHtml: sanitizedHtml,
            };
          }),
        );

        const threadIds = itemsWithHtml
          .map((item) => item.contentId)
          .filter(Boolean);
        await this.fetchThreadReplyStats(threadIds);

        this.topics = itemsWithHtml.map((item) => {
          const stats = this.threadReplyStats[item.contentId] || {};
          return {
            id: item.contentId,
            userId: item.userId ?? item.user_id ?? null,
            title: item.title,
            content: item.bodyText ?? "",
            imageUrls: item.bodyImages ?? [],
            author: item.username,
            authorAvatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(item.username)}&background=random`,
            categoryName: item.categoryName,
            categoryId: item.categoryId ?? item.category_id ?? null,
            createdAt: item.createdAt,
            type: item.type,
            replyCount: stats.replyCount ?? 0,
            lastReplyTime: stats.lastReplyTime ?? null,
            lastReplyUser: stats.lastReplyUser ?? null,
            lastReplyUserId: stats.lastReplyUserId ?? null,
            viewCount: item.viewCount ?? 0,
            isPinned: item.isPinned ?? false,
            isHot: (stats.replyCount ?? 0) > 10,
          };
        });
      } catch (error) {
        console.error("載入主題失敗:", error);
        if (error.response) {
          alert(error.response.data.message || "載入主題失敗");
        } else if (error.request) {
          alert("無法連接到伺服器，請檢查網路連線");
        } else {
          alert("發生未知錯誤");
        }
        this.topics = [];
      } finally {
        this.isLoading = false;
      }
    },

    changePage(page) {
      if (page < 1 || page > this.pagination.totalPages) return;
      this.pagination.currentPage = page;
      this.fetchTopics();
    },

    // ========== 帖文詳情 ==========
    async fetchThreadDetail(threadId) {
      try {
        const [detailRes, commentsRes] = await Promise.all([
          apiClient.get(`/ContentApi/${threadId}`),
          contentInteractionApi.getComments(threadId),
        ]);

        const detail = detailRes?.contentId ? detailRes : detailRes?.data;

        if (detail && detail.contentId) {
          const detailBodyHtml = await fetchBodyHtml(detail.body);
          const { sanitizedHtml, textContent, imageUrls } =
            normalizeRichContent(detailBodyHtml, toAbsoluteUrl);

          let categoryId =
            detail.categoryId ??
            detail.category_id ??
            detail.cateId ??
            detail.cate_id ??
            null;

          if (!categoryId && this.topics && this.topics.length > 0) {
            const matchedTopic = this.topics.find(
              (t) => String(t.id) === String(threadId),
            );
            if (matchedTopic) {
              categoryId =
                matchedTopic.categoryId ?? matchedTopic.category_id ?? null;
            }
          }

          this.threadDetail = {
            id: detail.contentId,
            userId: detail.userId ?? detail.user_id ?? null,
            title: detail.title,
            author: detail.username,
            createdAt: detail.createdAt,
            content: textContent,
            contentHtml: sanitizedHtml,
            contentText: textContent,
            imageUrls,
            categoryId: categoryId,
            type: detail.type,
          };
        }

        const directChildren = Array.isArray(commentsRes?.comments)
          ? commentsRes.comments
          : [];

        this.updateThreadReplyStats(threadId, directChildren);

        const loadContentWithHtml = async (item, fallbackType) => {
          const bodyHtml = await fetchBodyHtml(item.body);
          const { sanitizedHtml, textContent, imageUrls } =
            normalizeRichContent(bodyHtml, toAbsoluteUrl);
          return {
            id: item.contentId,
            userId: item.userId ?? item.user_id ?? null,
            author: item.username,
            createdAt: item.createdAt,
            content: textContent,
            contentHtml: sanitizedHtml,
            contentText: textContent,
            imageUrls,
            type: item.type ?? fallbackType,
            parentContentId: item.parentContentId,
          };
        };

        const fetchChildComments = async (parentId, expectedType) => {
          try {
            const res = await contentInteractionApi.getComments(parentId);
            const items = Array.isArray(res?.comments) ? res.comments : [];
            const filtered = expectedType
              ? items.filter((i) => i.type === expectedType || !i.type)
              : items;
            return await Promise.all(
              filtered.map((item) => loadContentWithHtml(item, expectedType)),
            );
          } catch (error) {
            console.error(`獲取內容 ${parentId} 的子留言失敗:`, error);
            return [];
          }
        };

        const threadCommentsBase = directChildren.filter(
          (c) => c.type === CONTENT_TYPES.COMMENT || !c.type,
        );

        this.threadComments = await Promise.all(
          threadCommentsBase.map(async (comment) => {
            const base = await loadContentWithHtml(
              comment,
              CONTENT_TYPES.COMMENT,
            );
            const replies = await fetchChildComments(
              comment.contentId,
              CONTENT_TYPES.COMMENT_REPLY,
            );
            return {
              ...base,
              replies: replies.sort(
                (a, b) => new Date(a.createdAt) - new Date(b.createdAt),
              ),
            };
          }),
        );

        const threadRepliesBase = directChildren.filter(
          (c) => c.type === CONTENT_TYPES.REPLY,
        );

        this.threadPosts = await Promise.all(
          threadRepliesBase.map(async (reply) => {
            const base = await loadContentWithHtml(reply, CONTENT_TYPES.REPLY);
            const replyComments = await fetchChildComments(
              reply.contentId,
              CONTENT_TYPES.COMMENT,
            );

            const replyCommentsWithReplies = await Promise.all(
              replyComments.map(async (rc) => {
                const replies = await fetchChildComments(
                  rc.id,
                  CONTENT_TYPES.COMMENT_REPLY,
                );
                return {
                  ...rc,
                  replies: replies.sort(
                    (a, b) => new Date(a.createdAt) - new Date(b.createdAt),
                  ),
                };
              }),
            );

            return {
              ...base,
              replies: replyCommentsWithReplies.sort(
                (a, b) => new Date(a.createdAt) - new Date(b.createdAt),
              ),
            };
          }),
        );

        this.threadPosts.sort(
          (a, b) => new Date(a.createdAt) - new Date(b.createdAt),
        );
        this.threadComments.sort(
          (a, b) => new Date(a.createdAt) - new Date(b.createdAt),
        );
        this.threadCurrentPage = 1;
        this.threadCommentsCurrentPage = 1;
      } catch (error) {
        console.error("獲取帖文詳情失敗:", error);
      }
    },

    openThread(topicId) {
      this.selectedThread = topicId;
      this.fetchThreadDetail(topicId);
    },

    closeThread() {
      this.selectedThread = null;
      this.threadDetail = {
        id: null,
        title: "",
        author: "",
        createdAt: "",
        content: "",
        contentHtml: "",
        contentText: "",
        imageUrls: [],
        categoryId: null,
        type: null,
      };
      this.threadPosts = [];
      this.threadComments = [];
      this.threadCurrentPage = 1;
      this.threadCommentsCurrentPage = 1;
    },

    changeThreadPage(page) {
      if (page >= 1 && page <= this.threadTotalPages) {
        this.threadCurrentPage = page;
      }
    },

    changeThreadCommentsPage(page) {
      if (page >= 1 && page <= this.threadCommentsTotalPages) {
        this.threadCommentsCurrentPage = page;
      }
    },

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

      const dto = {
        userId: 1,
        categoryId: this.newTopic.categoryId,
        parentContentId: this.newTopic.parentContentId,
        title: this.newTopic.title.trim(),
        body: this.newTopic.body.trim(),
        type: this.newTopic.parentContentId
          ? CONTENT_TYPES.REPLY
          : CONTENT_TYPES.POST,
      };

      try {
        await apiClient.post("/ContentApi", dto);
        alert("發表成功！");
        this.newTopic.title = "";
        this.newTopic.body = "";
        this.showNewTopicBox = false;
        this.pagination.currentPage = 1;
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
        isMinimized: false,
        replyingToContentId: options.replyingToContentId || null,
        replyingToContentData: options.replyingToContentData || null,
        replyingToThreadId: options.replyingToThreadId || null,
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

    triggerTopicRefresh() {
      this.lastRefreshTime = Date.now();
    },

    // ========== 搜尋功能 ==========
    setSearchKeyword(keyword) {
      this.searchKeyword = keyword;
      this.pagination.currentPage = 1;
      this.fetchTopics();
    },

    clearSearch() {
      this.searchKeyword = "";
      this.pagination.currentPage = 1;
      this.fetchTopics();
    },

    setSortOption(option) {
      this.sortOption = option === "createdAt_desc" ? option : "createdAt_desc";
      this.pagination.currentPage = 1;
      this.fetchTopics();
    },

    // ========== 編輯模式 ==========
    /**
     * 開啟編輯模式
     * @param {number} contentId - 要編輯的內容 ID
     * @param {object} postData - 內容數據
     */
    openEditMode(contentId, postData) {
      this.newTopic.editingContentId = contentId;
      this.newTopic.title = postData.title || "";
      this.newTopic.body = postData.contentHtml || postData.body || "";
      this.newTopic.categoryId = postData.categoryId || null;
      this.newTopic.parentContentId = postData.parentContentId || null;
      this.newTopic.mode = "edit";
      this.newTopic.contentType = postData.type || CONTENT_TYPES.POST;
      this.toggleNewTopicModal(true);
    },

    /**
     * 清除編輯模式
     */
    clearEditMode() {
      this.newTopic.editingContentId = null;
      this.newTopic.mode = "post";
    },

    // ========== 互動按鈕相關 ==========
    /**
     * 設置當前互動的內容
     * @param {number} contentId - 內容 ID
     * @param {number} authorUserId - 作者用戶 ID
     */
    setCurrentInteractionContent(contentId, authorUserId) {
      this.currentInteractionContentId = contentId;
      this.currentInteractionAuthorUserId = authorUserId;

      // 初始化互動狀態
      if (!this.interactionStates[contentId]) {
        this.interactionStates[contentId] = {
          1: false, // like
          2: false, // hide
          3: false, // bookmark
          4: false, // block
        };
      }

      // 初始化載入狀態
      if (!this.interactionLoadingStates[contentId]) {
        this.interactionLoadingStates[contentId] = {
          1: false,
          2: false,
          3: false,
          4: false,
        };
      }
    },

    /**
     * 執行互動
     * @param {number} contentId - 內容 ID
     * @param {number} categoryId - 互動分類 ID
     */
    async handleInteraction(contentId, categoryId) {
      if (!this.interactionLoadingStates[contentId]) {
        this.setCurrentInteractionContent(contentId, null);
      }

      if (this.interactionLoadingStates[contentId][categoryId]) return;

      this.interactionLoadingStates[contentId][categoryId] = true;

      try {
        const response = await contentInteractionApi.postInteraction({
          interactionCategoryId: categoryId,
          contentId: contentId,
        });

        if (response.isRemoved) {
          this.interactionStates[contentId][categoryId] = false;
          return { success: true, isRemoved: true, message: "互動已移除" };
        } else {
          this.interactionStates[contentId][categoryId] = true;
          const categoryNames = {
            1: "讚",
            2: "隱藏",
            3: "收藏",
            4: "檢舉",
          };
          return {
            success: true,
            isRemoved: false,
            message: `已${categoryNames[categoryId]}`,
          };
        }
      } catch (error) {
        console.error("互動操作失敗:", error);
        return { success: false, error: error.message };
      } finally {
        this.interactionLoadingStates[contentId][categoryId] = false;
      }
    },

    /**
     * 獲取互動狀態
     * @param {number} contentId - 內容 ID
     * @param {number} categoryId - 互動分類 ID
     */
    getInteractionState(contentId, categoryId) {
      return this.interactionStates[contentId]?.[categoryId] ?? false;
    },

    /**
     * 獲取載入狀態
     * @param {number} contentId - 內容 ID
     * @param {number} categoryId - 互動分類 ID
     */
    getInteractionLoadingState(contentId, categoryId) {
      return this.interactionLoadingStates[contentId]?.[categoryId] ?? false;
    },

    // ========== 快速回覆相關 ==========
    /**
     * 初始化快速回覆狀態
     * @param {number} contentId - 內容 ID
     */
    initQuickReply(contentId) {
      if (!this.quickReplies[contentId]) {
        this.quickReplies[contentId] = {
          content: "",
          isSubmitting: false,
          show: false,
        };
      }
    },

    /**
     * 設置快速回覆內容
     * @param {number} contentId - 內容 ID
     * @param {string} content - 回覆內容
     */
    setQuickReplyContent(contentId, content) {
      this.initQuickReply(contentId);
      this.quickReplies[contentId].content = content;
    },

    /**
     * 獲取快速回覆內容
     * @param {number} contentId - 內容 ID
     */
    getQuickReplyContent(contentId) {
      return this.quickReplies[contentId]?.content ?? "";
    },

    /**
     * 設置快速回覆提交狀態
     * @param {number} contentId - 內容 ID
     * @param {boolean} isSubmitting - 是否正在提交
     */
    setQuickReplySubmitting(contentId, isSubmitting) {
      this.initQuickReply(contentId);
      this.quickReplies[contentId].isSubmitting = isSubmitting;
    },

    /**
     * 獲取快速回覆提交狀態
     * @param {number} contentId - 內容 ID
     */
    getQuickReplySubmitting(contentId) {
      return this.quickReplies[contentId]?.isSubmitting ?? false;
    },

    /**
     * 顯示快速回覆框
     * @param {number} contentId - 內容 ID
     */
    showQuickReply(contentId) {
      this.initQuickReply(contentId);
      this.quickReplies[contentId].show = true;
    },

    /**
     * 隱藏快速回覆框
     * @param {number} contentId - 內容 ID
     */
    hideQuickReply(contentId) {
      if (this.quickReplies[contentId]) {
        this.quickReplies[contentId].show = false;
      }
    },

    /**
     * 取消快速回覆
     * @param {number} contentId - 內容 ID
     */
    cancelQuickReply(contentId) {
      if (this.quickReplies[contentId]) {
        this.quickReplies[contentId].content = "";
        this.quickReplies[contentId].show = false;
      }
    },

    /**
     * 清除快速回覆
     * @param {number} contentId - 內容 ID
     */
    clearQuickReply(contentId) {
      if (this.quickReplies[contentId]) {
        this.quickReplies[contentId].content = "";
      }
    },

    // ========== 更多菜單管理 ==========
    /**
     * 切換"更多"菜單
     * @param {number} contentId - 內容 ID
     */
    toggleMoreMenu(contentId) {
      if (this.openMoreMenuId === contentId) {
        // 如果當前菜單已打開，關閉它
        this.openMoreMenuId = null;
      } else {
        // 打開新菜單，自動關閉其他菜單
        this.openMoreMenuId = contentId;
      }
    },

    /**
     * 關閉"更多"菜單
     */
    closeMoreMenu() {
      this.openMoreMenuId = null;
    },

    /**
     * 檢查是否為指定內容的菜單打開
     * @param {number} contentId - 內容 ID
     */
    isMoreMenuOpen(contentId) {
      return this.openMoreMenuId === contentId;
    },
  },

  persist: true,
});
