import { defineStore } from "pinia";
import apiClient from "@/api/request.js";
import contentApi from "@/api/contentApi.js";
import contentInteractionApi from "@/api/contentInteractionApi.js";
import {
  fetchBodyHtml,
  parseBodyContent,
  toAbsoluteUrl,
} from "@/utils/contentBody.js";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";

// Content types according to database schema
export const CONTENT_TYPES = {
  POST: 1, // 貼文(出現在貼文版面)
  REPLY: 2, // 回文(出現在貼文版面)
  COMMENT: 3, // 留言
  COMMENT_REPLY: 4, // 留言回覆
};

const TOPIC_PAGE_CACHE_TTL_MS = 30000;
const topicPageCache = new Map();
const topicPagePending = new Map();
const contentBodyCache = new Map();
const contentBodyPending = new Map();

const buildTopicsCacheKey = ({
  categoryId,
  keyword,
  page,
  pageSize,
  sortOption,
}) =>
  JSON.stringify({
    categoryId: categoryId ?? null,
    keyword: String(keyword || "")
      .trim()
      .toLowerCase(),
    page,
    pageSize,
    sortOption,
  });

const getCachedTopicPage = (cacheKey) => {
  const cached = topicPageCache.get(cacheKey);
  if (!cached) return null;
  if (Date.now() - cached.fetchedAt > TOPIC_PAGE_CACHE_TTL_MS) {
    topicPageCache.delete(cacheKey);
    return null;
  }
  return cached.data;
};

const clearTopicPageCache = () => {
  topicPageCache.clear();
  topicPagePending.clear();
};

const isHtml = (value) => /<[^>]+>/.test(value || "");

const escapeHtml = (value) =>
  String(value ?? "").replace(/[&<>"']/g, (char) => {
    switch (char) {
      case "&":
        return "&amp;";
      case "<":
        return "&lt;";
      case ">":
        return "&gt;";
      case '"':
        return "&quot;";
      case "'":
        return "&#39;";
      default:
        return char;
    }
  });

const buildPreviewHtml = (textContent, imageUrls = []) => {
  const safeText = escapeHtml(textContent).replace(/\n/g, "<br />");
  const images = (Array.isArray(imageUrls) ? imageUrls : [])
    .filter(Boolean)
    .map((url) => `<img src="${escapeHtml(url)}" alt="preview" />`)
    .join("");
  const result =
    safeText && images
      ? `${safeText}<br />${images}`
      : safeText || images || "";
  console.log("[buildPreviewHtml]", {
    textContent: textContent?.substring(0, 50),
    imagesCount: imageUrls.length,
    resultLength: result.length,
  });
  return result;
};

const fetchBodyHtmlCached = async (body) => {
  if (!body) {
    console.log("[fetchBodyHtmlCached] body is empty");
    return "";
  }
  if (isHtml(body)) {
    console.log(
      "[fetchBodyHtmlCached] body is HTML (direct)",
      body.substring(0, 100),
    );
    return body;
  }

  const url = toAbsoluteUrl(body);
  console.log("[fetchBodyHtmlCached] Fetching from URL:", url);
  if (!url) {
    console.log("[fetchBodyHtmlCached] url is empty");
    return "";
  }

  if (contentBodyCache.has(url)) {
    console.log("[fetchBodyHtmlCached] Returning from cache");
    return contentBodyCache.get(url);
  }

  if (contentBodyPending.has(url)) {
    console.log("[fetchBodyHtmlCached] Waiting for pending request");
    return contentBodyPending.get(url);
  }

  const request = (async () => {
    console.log("[fetchBodyHtmlCached] Making fetch request for:", body);
    const html = await fetchBodyHtml(body);
    console.log(
      "[fetchBodyHtmlCached] Received content length:",
      html?.length || 0,
    );
    contentBodyCache.set(url, html);
    return html;
  })();

  contentBodyPending.set(url, request);
  try {
    const result = await request;
    console.log(
      "[fetchBodyHtmlCached] Final result length:",
      result?.length || 0,
    );
    return result;
  } finally {
    contentBodyPending.delete(url);
  }
};

const buildThreadItem = async (item, fallbackType) => {
  const bodyHtml = await fetchBodyHtmlCached(item.body);
  const { sanitizedHtml, textContent, imageUrls } = normalizeRichContent(
    bodyHtml,
    toAbsoluteUrl,
  );
  return {
    id: item.contentId ?? item.content_id ?? item.id,
    userId: item.userId ?? item.user_id ?? null,
    author: item.username ?? item.userName ?? item.user_name ?? "",
    createdAt: item.createdAt ?? item.created_at ?? null,
    content: textContent,
    contentHtml: sanitizedHtml,
    contentText: textContent,
    imageUrls,
    type: item.type ?? fallbackType,
    parentContentId: item.parentContentId ?? item.parent_content_id ?? null,
    categoryId: item.categoryId ?? item.category_id ?? null,
    title: item.title ?? "",
  };
};

const extractReplyStats = (item) => {
  const stats = item?.replyStats ?? item?.reply_stats ?? null;
  const replyCount =
    item?.replyCount ??
    item?.reply_count ??
    stats?.replyCount ??
    stats?.reply_count ??
    null;
  const lastReplyTime =
    item?.lastReplyTime ??
    item?.last_reply_time ??
    stats?.lastReplyTime ??
    stats?.last_reply_time ??
    null;
  const lastReplyUser =
    item?.lastReplyUser ??
    item?.last_reply_user ??
    stats?.lastReplyUser ??
    stats?.last_reply_user ??
    null;
  const lastReplyUserId =
    item?.lastReplyUserId ??
    item?.last_reply_user_id ??
    stats?.lastReplyUserId ??
    stats?.last_reply_user_id ??
    null;

  return {
    replyCount,
    lastReplyTime,
    lastReplyUser,
    lastReplyUserId,
  };
};

const extractPreviewPayload = (item) => {
  const previewHtml =
    item?.previewHtml ??
    item?.preview_html ??
    item?.bodyPreviewHtml ??
    item?.body_preview_html ??
    item?.bodyHtml ??
    item?.body_html ??
    null;
  const previewText =
    item?.previewText ??
    item?.preview_text ??
    item?.bodyPreviewText ??
    item?.body_preview_text ??
    item?.bodyText ??
    item?.body_text ??
    null;
  const previewImages =
    item?.previewImages ??
    item?.preview_images ??
    item?.imageUrls ??
    item?.image_urls ??
    null;

  if (!previewHtml && !previewText && !previewImages) return null;

  return {
    previewHtml,
    previewText,
    previewImages,
  };
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
    replyStatsFallbackEnabled: false,
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

    updateThreadReplyStats(threadId, replies, totalCount = null) {
      const items = Array.isArray(replies) ? replies : [];
      const replyCount = Number.isFinite(totalCount)
        ? totalCount
        : items.length;
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

    resetThreadPagingState() {
      this.threadPosts = [];
      this.threadComments = [];
      this.threadCurrentPage = 1;
      this.threadCommentsCurrentPage = 1;
      this.threadRepliesPagination = {
        currentPage: 1,
        pageSize: this.threadItemsPerPage,
        totalCount: 0,
        totalPages: 0,
        isLoading: false,
      };
      this.threadCommentsPagination = {
        currentPage: 1,
        pageSize: this.threadCommentsItemsPerPage,
        totalCount: 0,
        totalPages: 0,
        isLoading: false,
      };
      this.childRepliesByParentId = {};
      this.childRepliesCountByParentId = {};
    },

    async fetchThreadRepliesPage(page = 1, options = {}) {
      const threadId = options.threadId ?? this.selectedThread;
      if (!threadId) return;

      const pageSize = this.threadRepliesPagination.pageSize;
      this.threadRepliesPagination.isLoading = true;

      try {
        const res = await contentApi.getChildren(threadId, {
          types: String(CONTENT_TYPES.REPLY),
          page,
          pageSize,
        });
        const items = Array.isArray(res?.items) ? res.items : [];
        const mapped = await Promise.all(
          items.map((item) => buildThreadItem(item, CONTENT_TYPES.REPLY)),
        );

        const totalCount = res?.totalCount ?? mapped.length;
        const totalPages =
          res?.totalPages ??
          (totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize));
        const currentPage = res?.currentPage ?? res?.page ?? page;

        this.threadPosts = mapped;
        this.threadRepliesPagination = {
          currentPage,
          pageSize,
          totalCount,
          totalPages,
          isLoading: false,
        };
        this.threadCurrentPage = currentPage;
      } catch (error) {
        console.error("載入回文分頁失敗:", error);
        this.threadRepliesPagination.isLoading = false;
      }
    },

    async fetchThreadCommentsPage(page = 1, options = {}) {
      const threadId = options.threadId ?? this.selectedThread;
      if (!threadId) return;

      const pageSize = this.threadCommentsPagination.pageSize;
      this.threadCommentsPagination.isLoading = true;

      try {
        const res = await contentApi.getChildren(threadId, {
          types: String(CONTENT_TYPES.COMMENT),
          page,
          pageSize,
        });
        const items = Array.isArray(res?.items) ? res.items : [];
        const mapped = await Promise.all(
          items.map((item) => buildThreadItem(item, CONTENT_TYPES.COMMENT)),
        );

        const totalCount = res?.totalCount ?? mapped.length;
        const totalPages =
          res?.totalPages ??
          (totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize));
        const currentPage = res?.currentPage ?? res?.page ?? page;

        this.threadComments = mapped;
        this.threadCommentsPagination = {
          currentPage,
          pageSize,
          totalCount,
          totalPages,
          isLoading: false,
        };
        this.threadCommentsCurrentPage = currentPage;
      } catch (error) {
        console.error("載入留言分頁失敗:", error);
        this.threadCommentsPagination.isLoading = false;
      }
    },

    async preloadChildRepliesCount(parentId, expectedType) {
      if (!parentId) return;
      const current = this.childRepliesCountByParentId[parentId];
      if (current) return; // 已經預加載過，不再重複加載

      this.childRepliesCountByParentId[parentId] = {
        totalCount: null,
        isLoading: true,
      };

      try {
        const res = await contentApi.getChildren(parentId, {
          types: expectedType ? String(expectedType) : undefined,
          page: 1,
          pageSize: 1, // 只獲取一條用於統計總數
        });
        const totalCount = res?.totalCount ?? 0;
        this.childRepliesCountByParentId[parentId] = {
          totalCount,
          isLoading: false,
        };
      } catch (error) {
        console.error(`預加載內容 ${parentId} 子回覆數量失敗:`, error);
        this.childRepliesCountByParentId[parentId] = {
          totalCount: 0,
          isLoading: false,
        };
      }
    },

    async ensureChildRepliesLoaded(parentId, expectedType, options = {}) {
      if (!parentId) return;
      const current = this.childRepliesByParentId[parentId];
      if (current?.loaded || current?.isLoading) return;

      const pageSize = options.pageSize ?? 20;
      this.childRepliesByParentId[parentId] = {
        items: current?.items ?? [],
        currentPage: 1,
        pageSize,
        totalCount: current?.totalCount ?? 0,
        totalPages: current?.totalPages ?? 0,
        isLoading: true,
        loaded: false,
      };

      try {
        const res = await contentApi.getChildren(parentId, {
          types: expectedType ? String(expectedType) : undefined,
          page: 1,
          pageSize,
        });
        const items = Array.isArray(res?.items) ? res.items : [];
        const mapped = await Promise.all(
          items.map((item) => buildThreadItem(item, expectedType)),
        );
        const totalCount = res?.totalCount ?? mapped.length;
        const totalPages =
          res?.totalPages ??
          (totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize));

        this.childRepliesByParentId[parentId] = {
          items: mapped,
          currentPage: 1,
          pageSize,
          totalCount,
          totalPages,
          isLoading: false,
          loaded: true,
        };
      } catch (error) {
        console.error(`載入內容 ${parentId} 子回覆失敗:`, error);
        this.childRepliesByParentId[parentId] = {
          items: [],
          currentPage: 1,
          pageSize,
          totalCount: 0,
          totalPages: 0,
          isLoading: false,
          loaded: true,
        };
      }
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
            const res = await contentApi.getChildren(id, {
              types: `${CONTENT_TYPES.REPLY},${CONTENT_TYPES.COMMENT},${CONTENT_TYPES.COMMENT_REPLY}`,
              page: 1,
              pageSize: 200,
            });
            const replies = Array.isArray(res?.items) ? res.items : [];
            this.updateThreadReplyStats(id, replies, res?.totalCount);
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

      const pageSize = this.pagination.pageSize;
      const keyword = this.searchKeyword.trim();
      const cacheKey = buildTopicsCacheKey({
        categoryId: targetCategory,
        keyword,
        page: targetPage,
        pageSize,
        sortOption: this.sortOption,
      });

      const cached = getCachedTopicPage(cacheKey);
      if (cached) {
        this.pagination = cached.pagination;
        this.topics = cached.topics;
        return;
      }

      if (topicPagePending.has(cacheKey)) {
        await topicPagePending.get(cacheKey);
        const pendingCached = getCachedTopicPage(cacheKey);
        if (pendingCached) {
          this.pagination = pendingCached.pagination;
          this.topics = pendingCached.topics;
        }
        return;
      }

      this.isLoading = true;

      try {
        const params = {
          page: targetPage,
          pageSize,
          types: `${CONTENT_TYPES.POST},${CONTENT_TYPES.REPLY}`,
          sortBy: "latest",
        };

        if (targetCategory) params.categoryId = targetCategory;
        if (keyword) params.keyword = keyword;

        const requestPromise = (async () => {
          const data = await contentApi.getContents(params);
          const items = Array.isArray(data?.items) ? data.items : [];

          // Log first 2 items completely
          if (items.length > 0) {
            console.log(
              "[fetchTopics] API Response - First item structure:",
              JSON.stringify(items[0], null, 2),
            );
            if (items.length > 1) {
              console.log(
                "[fetchTopics] API Response - Second item structure:",
                JSON.stringify(items[1], null, 2),
              );
            }
          }

          let serverReplyStatsProvided = Boolean(data?.replyStatsIncluded);
          const totalCount = data?.totalCount ?? items.length;
          const totalPages =
            data?.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize));
          const currentPage = data?.currentPage ?? data?.page ?? targetPage;

          const topics = await Promise.all(
            items.map(async (item) => {
              console.log(
                "[fetchTopics] Processing item:",
                item.contentId || item.id,
                "| title:",
                item.title,
              );
              const previewPayload = extractPreviewPayload(item);
              const previewText = previewPayload?.previewText ?? "";
              const previewImages = Array.isArray(previewPayload?.previewImages)
                ? previewPayload.previewImages.map(toAbsoluteUrl)
                : [];
              const previewHtml = previewPayload?.previewHtml ?? "";

              // Only treat as real preview if previewHtml exists + non-empty,
              // or if previewText is substantial (not just title) + previewHtml is explicitly set
              const isSubstantialPreview =
                String(previewHtml).trim().length > 0 ||
                (String(previewText).trim().length > 50 &&
                  previewPayload?.previewHtml !== null);

              const hasPreviewPayload =
                Boolean(previewPayload) &&
                (isSubstantialPreview || previewImages.length > 0);
              console.log(
                "[fetchTopics] item",
                item.contentId || item.id,
                "| hasPreviewPayload:",
                hasPreviewPayload,
                "| body:",
                (item.body || "").substring(0, 100),
                "| item keys:",
                Object.keys(item || {}),
              );

              let bodyText = String(previewText || "");
              let bodyImages = previewImages;
              let bodySanitizedHtml = "";

              if (String(previewHtml || "").trim()) {
                const { sanitizedHtml, textContent, imageUrls } =
                  normalizeRichContent(previewHtml, toAbsoluteUrl);
                bodySanitizedHtml = sanitizedHtml;
                if (!bodyText) bodyText = textContent;
                if (bodyImages.length === 0) bodyImages = imageUrls;
              }

              if (!hasPreviewPayload) {
                const bodyHtml = await fetchBodyHtmlCached(item.body);
                if (isHtml(bodyHtml)) {
                  const { sanitizedHtml, textContent, imageUrls } =
                    normalizeRichContent(bodyHtml, toAbsoluteUrl);
                  bodyText = textContent;
                  bodyImages = imageUrls;
                  bodySanitizedHtml = sanitizedHtml;
                } else {
                  const { textContent, imageUrls } = parseBodyContent(bodyHtml);
                  const previewHtml = buildPreviewHtml(textContent, imageUrls);
                  const {
                    sanitizedHtml,
                    textContent: normalizedText,
                    imageUrls: normalizedImages,
                  } = normalizeRichContent(previewHtml, toAbsoluteUrl);
                  bodyText = normalizedText || textContent;
                  bodyImages = normalizedImages.length
                    ? normalizedImages
                    : imageUrls;
                  bodySanitizedHtml = sanitizedHtml;
                }
              }

              if (!bodySanitizedHtml && (bodyText || bodyImages.length > 0)) {
                const previewHtml = buildPreviewHtml(bodyText, bodyImages);
                const { sanitizedHtml, textContent, imageUrls } =
                  normalizeRichContent(previewHtml, toAbsoluteUrl);
                bodySanitizedHtml = sanitizedHtml;
                if (!bodyText) bodyText = textContent;
                if (bodyImages.length === 0) bodyImages = imageUrls;
              }

              if (!bodyText && bodyImages.length > 0) {
                bodyText = "(已附圖片)";
              }

              const stats = extractReplyStats(item);
              if (
                !serverReplyStatsProvided &&
                (stats.replyCount !== null ||
                  stats.lastReplyTime ||
                  stats.lastReplyUser)
              ) {
                serverReplyStatsProvided = true;
              }
              const contentId = item.contentId ?? item.content_id ?? item.id;

              if (contentId && stats.replyCount !== null) {
                this.threadReplyStats[contentId] = {
                  replyCount: stats.replyCount ?? 0,
                  lastReplyTime: stats.lastReplyTime ?? null,
                  lastReplyUser: stats.lastReplyUser ?? null,
                  lastReplyUserId: stats.lastReplyUserId ?? null,
                  fetchedAt: Date.now(),
                };
              }

              const fallbackStats =
                contentId && this.threadReplyStats[contentId]
                  ? this.threadReplyStats[contentId]
                  : {};

              console.log(
                "[fetchTopics] Final result for",
                contentId,
                "| contentText:",
                (bodyText || "").substring(0, 50),
                "| contentHtml:",
                (bodySanitizedHtml || "").substring(0, 50),
              );
              return {
                id: contentId,
                userId: item.userId ?? item.user_id ?? null,
                title: item.title,
                content: bodyText ?? "",
                contentText: bodyText ?? "",
                contentHtml: bodySanitizedHtml ?? "",
                imageUrls: bodyImages ?? [],
                author: item.username,
                authorAvatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(item.username)}&background=random`,
                categoryName: item.categoryName,
                categoryId: item.categoryId ?? item.category_id ?? null,
                createdAt: item.createdAt,
                type: item.type,
                replyCount: stats.replyCount ?? fallbackStats.replyCount ?? 0,
                lastReplyTime:
                  stats.lastReplyTime ?? fallbackStats.lastReplyTime ?? null,
                lastReplyUser:
                  stats.lastReplyUser ?? fallbackStats.lastReplyUser ?? null,
                lastReplyUserId:
                  stats.lastReplyUserId ??
                  fallbackStats.lastReplyUserId ??
                  null,
                viewCount: item.viewCount ?? 0,
                isPinned: item.isPinned ?? false,
                isHot: (stats.replyCount ?? fallbackStats.replyCount ?? 0) > 10,
              };
            }),
          );

          return {
            pagination: {
              currentPage,
              pageSize,
              totalCount,
              totalPages,
            },
            topics,
            serverReplyStatsProvided,
          };
        })();

        topicPagePending.set(cacheKey, requestPromise);
        const result = await requestPromise;
        topicPageCache.set(cacheKey, {
          fetchedAt: Date.now(),
          data: result,
        });

        this.pagination = result.pagination;
        this.topics = result.topics;

        const shouldFetchFallbackStats =
          this.replyStatsFallbackEnabled &&
          !result.serverReplyStatsProvided &&
          result.topics.some(
            (topic) => topic.replyCount === 0 && !topic.lastReplyTime,
          );

        if (shouldFetchFallbackStats) {
          const threadIds = result.topics
            .map((item) => item.id)
            .filter(Boolean);
          await this.fetchThreadReplyStats(threadIds);
        }
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
        topicPagePending.delete(cacheKey);
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
        this.resetThreadPagingState();

        const detailRes = await contentApi.getContent(threadId);

        const detail = detailRes?.contentId ? detailRes : detailRes?.data;

        if (detail && detail.contentId) {
          const detailBodyHtml = await fetchBodyHtmlCached(detail.body);
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

        await Promise.all([
          this.fetchThreadRepliesPage(1, { threadId }),
          this.fetchThreadCommentsPage(1, { threadId }),
        ]);
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
      this.threadRepliesPagination = {
        currentPage: 1,
        pageSize: this.threadItemsPerPage,
        totalCount: 0,
        totalPages: 0,
        isLoading: false,
      };
      this.threadCommentsPagination = {
        currentPage: 1,
        pageSize: this.threadCommentsItemsPerPage,
        totalCount: 0,
        totalPages: 0,
        isLoading: false,
      };
      this.childRepliesByParentId = {};
      this.childRepliesCountByParentId = {};
    },

    changeThreadPage(page) {
      if (page >= 1 && page <= this.threadTotalPages) {
        this.fetchThreadRepliesPage(page);
      }
    },

    changeThreadCommentsPage(page) {
      if (page >= 1 && page <= this.threadCommentsTotalPages) {
        this.fetchThreadCommentsPage(page);
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
      clearTopicPageCache();
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
