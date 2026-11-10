import contentInteractionApi from "@/api/contentInteractionApi.js";
import { useAuthStore } from "@/stores/users/auth.js";
import { interactionInitPending } from "@/stores/forum/caches.js";
import { extractUserInteractions, toInteractionCategoryId } from "@/stores/forum/normalizers.js";
import { clearTopicPageCache } from "@/stores/forum/topicQuery.js";

export const interactionActions = {
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

  async initializeUserInteractionState(contentId, { force = false } = {}) {
    const targetContentId = Number(contentId);
    if (!Number.isFinite(targetContentId) || targetContentId <= 0) return;

    this.setCurrentInteractionContent(targetContentId, null);

    const authStore = useAuthStore();
    const isLoggedIn = Boolean(
      authStore.isAuthenticated && authStore.user?.id,
    );

    if (!isLoggedIn) {
      this.interactionStates[targetContentId] = {
        1: false,
        2: false,
        3: false,
        4: false,
      };
      this.interactionStateInitialized[targetContentId] = false;
      return;
    }

    if (!force && this.interactionStateInitialized[targetContentId]) {
      return;
    }

    if (interactionInitPending.has(targetContentId)) {
      await interactionInitPending.get(targetContentId);
      return;
    }

    const request = (async () => {
      try {
        const response =
          await contentInteractionApi.getUserInteraction(targetContentId);
        const interactions = extractUserInteractions(response);
        const activeCategories = new Set(
          interactions.map(toInteractionCategoryId).filter(Boolean),
        );

        this.interactionStates[targetContentId] = {
          1: activeCategories.has(1),
          2: activeCategories.has(2),
          3: activeCategories.has(3),
          4: activeCategories.has(4),
        };
        this.interactionStateInitialized[targetContentId] = true;
      } catch (error) {
        if (error?.response?.status === 401) {
          this.interactionStates[targetContentId] = {
            1: false,
            2: false,
            3: false,
            4: false,
          };
          this.interactionStateInitialized[targetContentId] = false;
          return;
        }

        console.error("初始化使用者互動狀態失敗:", error);
      } finally {
        interactionInitPending.delete(targetContentId);
      }
    })();

    interactionInitPending.set(targetContentId, request);
    await request;
  },

  async initializeUserInteractionStates(
    contentIds,
    { force = false, batchSize = null } = {},
  ) {
    const normalizedIds = Array.from(
      new Set(
        (Array.isArray(contentIds) ? contentIds : [])
          .map((id) => Number(id))
          .filter((id) => Number.isFinite(id) && id > 0),
      ),
    );

    if (!normalizedIds.length) return;

    const authStore = useAuthStore();
    const isLoggedIn = Boolean(
      authStore.isAuthenticated && authStore.user?.id,
    );

    if (!isLoggedIn) {
      normalizedIds.forEach((id) => {
        this.setCurrentInteractionContent(id, null);
        this.interactionStates[id] = {
          1: false,
          2: false,
          3: false,
          4: false,
        };
        this.interactionStateInitialized[id] = false;
      });
      return;
    }

    const targetIds = force
      ? normalizedIds
      : normalizedIds.filter((id) => !this.interactionStateInitialized[id]);

    if (!targetIds.length) return;

    const defaultBatchSize = Math.max(
      1,
      Number(this.interactionInitBatchSize) || 1,
    );
    const resolvedBatchSize = Math.max(
      1,
      Number(batchSize ?? defaultBatchSize) || defaultBatchSize,
    );

    for (
      let index = 0;
      index < targetIds.length;
      index += resolvedBatchSize
    ) {
      const chunk = targetIds.slice(index, index + resolvedBatchSize);
      await Promise.all(
        chunk.map((id) => this.initializeUserInteractionState(id, { force })),
      );
    }
  },

  setInteractionInitBatchSize(size) {
    const nextSize = Number(size);
    if (!Number.isFinite(nextSize)) return;

    this.interactionInitBatchSize = Math.min(20, Math.max(1, nextSize));
  },

  getContentLikeCount(contentId) {
    const targetId = Number(contentId);
    if (!Number.isFinite(targetId) || targetId <= 0) return 0;

    const findCount = (items = []) => {
      const matched = items.find((item) => Number(item?.id) === targetId);
      if (!matched) return null;
      const count = Number(matched?.likeCount);
      return Number.isFinite(count) ? Math.max(0, count) : 0;
    };

    if (Number(this.threadDetail?.id) === targetId) {
      const detailCount = Number(this.threadDetail?.likeCount ?? 0);
      if (Number.isFinite(detailCount)) return Math.max(0, detailCount);
    }

    const topicCount = findCount(this.topics);
    if (topicCount !== null) return topicCount;

    const postCount = findCount(this.threadPosts);
    if (postCount !== null) return postCount;

    const commentCount = findCount(this.threadComments);
    if (commentCount !== null) return commentCount;

    const childReplyCollections = Object.values(this.childRepliesByParentId);
    for (const payload of childReplyCollections) {
      const childCount = findCount(payload?.items || []);
      if (childCount !== null) return childCount;
    }

    return 0;
  },

  setContentLikeCount(contentId, likeCount) {
    const targetId = Number(contentId);
    const nextCount = Number(likeCount);
    if (!Number.isFinite(targetId) || targetId <= 0) return;
    if (!Number.isFinite(nextCount)) return;

    const normalizedCount = Math.max(0, nextCount);
    const updateCollection = (items = []) =>
      items.map((item) =>
        Number(item?.id) === targetId
          ? { ...item, likeCount: normalizedCount }
          : item,
      );

    this.topics = updateCollection(this.topics);
    this.threadPosts = updateCollection(this.threadPosts);
    this.threadComments = updateCollection(this.threadComments);

    if (Number(this.threadDetail?.id) === targetId) {
      this.threadDetail = {
        ...this.threadDetail,
        likeCount: normalizedCount,
      };
    }

    Object.entries(this.childRepliesByParentId || {}).forEach(
      ([parentId, payload]) => {
        this.childRepliesByParentId[parentId] = {
          ...payload,
          items: updateCollection(payload?.items || []),
        };
      },
    );
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

    const previousState = Boolean(
      this.interactionStates[contentId][categoryId],
    );
    this.interactionLoadingStates[contentId][categoryId] = true;

    try {
      const response = await contentInteractionApi.postInteraction({
        interactionCategoryId: categoryId,
        contentId: contentId,
      });

      const explicitRemoved =
        response?.isRemoved ??
        response?.is_removed ??
        response?.removed ??
        response?.isDeleted ??
        response?.is_deleted;

      const isRemoved =
        typeof explicitRemoved === "boolean"
          ? explicitRemoved
          : previousState;

      this.interactionStates[contentId][categoryId] = !isRemoved;

      if (categoryId === 1) {
        const currentLikeCount = this.getContentLikeCount(contentId);
        const nextLikeCount = isRemoved
          ? currentLikeCount - 1
          : currentLikeCount + 1;
        this.setContentLikeCount(contentId, nextLikeCount);
      }

      // 隱藏功能的特殊處理：清除緩存以便立即刷新列表
      if (categoryId === 2) {
        clearTopicPageCache();
      }

      if (isRemoved) {
        return { success: true, isRemoved: true, message: "互動已移除" };
      }

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
};
