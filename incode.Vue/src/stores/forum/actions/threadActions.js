import contentApi from "@/api/contentApi.js";
import contentInteractionApi from "@/api/contentInteractionApi.js";
import { toAbsoluteUrl } from "@/utils/contentBody.js";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { resolveAuthorAvatar } from "@/stores/forum/avatars.js";
import { clearCachedBestAnswerContentId, getCachedBestAnswerContentId, setCachedBestAnswerContentId } from "@/stores/forum/bestAnswerCache.js";
import { CONTENT_TYPES } from "@/stores/forum/constants.js";
import { fetchBodyHtmlCached } from "@/stores/forum/contentHtml.js";
import { fetchEntrustMissionMetaByContentId, normalizeEntrustMissionMeta } from "@/stores/forum/entrustMeta.js";
import { buildThreadItem, extractLikeCount, extractReplyStats } from "@/stores/forum/normalizers.js";

export const threadActions = {
  /**
   * [遺留方法 - 作為 fallback] 更新主題的回覆統計資訊
   *
   * 注意：後端現在直接在 ContentDto 中提供這些統計資訊
   * 此方法僅在 replyStatsFallbackEnabled=true 且後端未提供統計時使用
   *
   * @param {number} threadId - 主題 ID
   * @param {Array} replies - 回覆列表
   * @param {number} totalCount - 總回覆數
   */
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
      lastReplyUserAvatar: resolveAuthorAvatar(lastReply),
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
      await this.enrichTopicAvatarFields(mapped);

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
      await this.enrichTopicAvatarFields(mapped);

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
      await this.enrichTopicAvatarFields(mapped);
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

  /**
   * [遺留方法 - 作為 fallback] 獲取主題的回覆統計資訊
   *
   * 注意：後端現在直接在 ContentDto 中提供這些統計資訊
   * 此方法僅在 replyStatsFallbackEnabled=true 且後端未提供統計時使用
   *
   * @param {Array} threadIds - 主題 ID 列表
   * @param {Object} options - 選項 { forceRefresh }
   */
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
            // 後端每頁最多 100 筆；回覆依時間由新到舊，最新回覆一定在第一頁，總數另由 totalCount 提供
            pageSize: 100,
          });
          const replies = Array.isArray(res?.items) ? res.items : [];
          this.updateThreadReplyStats(id, replies, res?.totalCount);
        } catch (error) {
          console.error(`載入主題 ${id} 回應統計失敗:`, error);
        }
      }),
    );
  },

  // ========== 帖文詳情 ==========
  /**
   * 【信息流複雜點】取得線程詳情
   *
   * 此方法是整個論壇系統中複雜性最高的方法之一。信息流如下：
   * 1. 調用 contentApi.getContent() 取回線程詳情
   * 2. 從詳情中萃取 body（原始 HTML）→ 經由 fetchBodyHtmlCached() 取回及轉換
   * 3. 萃取回覆統計（replyCount、lastReplyTime 等）
   * 4. 【多來源】萃取「委託任務」資訊：
   *    - 直接從 api 回應中
   *    - 或透過 fetchEntrustMissionMetaByContentId() 另行取得
   * 5. 萃取「最佳解」：
   *    - 檢查本地快取（localStorage）
   *    - 調用 contentInteractionApi.getBestAnswerResolution()
   * 6. 填充 state.threadDetail 物件
   * 7. 最後，並行取得回覆及留言分頁資訊
   *
   * 流程中有 3 個外部 API 調用 + 快取讀取，需特別注意。
   */
  async fetchThreadDetail(threadId) {
    try {
      // 新人提醒：每次切 thread 都先重置分頁，避免上一個 thread 的資料殘留。
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

        // 提取後端提供的回覆統計資訊
        const stats = extractReplyStats(detail);
        const detailEntrustMeta = normalizeEntrustMissionMeta(detail);
        const fallbackEntrustMeta = await fetchEntrustMissionMetaByContentId(
          detail.contentId ?? threadId,
        );
        // 這裡是多來源整併：優先用 detail 直接給的欄位，不足再補 fallback API。
        // 你若要追「委託任務資料怪怪的」，先從這段開始比對來源。
        const entrustMissionMeta = {
          entrustMissionId:
            detailEntrustMeta.entrustMissionId ??
            fallbackEntrustMeta?.entrustMissionId ??
            null,
          entrustMissionUserId:
            detailEntrustMeta.entrustMissionUserId ??
            fallbackEntrustMeta?.entrustMissionUserId ??
            null,
          rewardPoints:
            detailEntrustMeta.rewardPoints > 0
              ? detailEntrustMeta.rewardPoints
              : Number(fallbackEntrustMeta?.rewardPoints || 0),
          bestAnswerContentId:
            detailEntrustMeta.bestAnswerContentId ??
            fallbackEntrustMeta?.bestAnswerContentId ??
            null,
        };

        const threadContentId = detail.contentId ?? threadId;
        const cachedBestAnswerContentId = getCachedBestAnswerContentId({
          entrustMissionId: entrustMissionMeta.entrustMissionId,
          threadId: threadContentId,
        });

        const resolvedBestAnswer =
          await contentInteractionApi.getBestAnswerResolution({
            entrustMissionId: entrustMissionMeta.entrustMissionId,
            threadContentId,
            fallbackBestAnswerContentId:
              entrustMissionMeta.bestAnswerContentId ??
              cachedBestAnswerContentId,
          });
        // 新人提醒：最佳解不是只看一個欄位，這裡有 API + localStorage 的容錯鏈。
        // 目的：避免後端回傳暫時缺漏時，前端整個「掉最佳解狀態」。

        const resolvedBestAnswerContentId =
          resolvedBestAnswer?.contentId ?? null;
        const finalBestAnswerContentId = resolvedBestAnswer?.isExplicit
          ? resolvedBestAnswerContentId
          : (resolvedBestAnswerContentId ?? cachedBestAnswerContentId);

        // 從主題列表中查找對應主題以獲取 isHot 和 categoryName
        let isHot = (stats.replyCount ?? 0) > 10;
        let categoryName = detail.categoryName ?? null;
        let hasEntrustMission =
          detail.hasEntrustMission ?? detail.has_entrust_mission ?? false;

        if (this.topics && this.topics.length > 0) {
          const matchedTopic = this.topics.find(
            (t) => String(t.id) === String(threadId),
          );
          if (matchedTopic) {
            isHot = matchedTopic.isHot ?? isHot;
            categoryName = matchedTopic.categoryName ?? categoryName;
            hasEntrustMission =
              matchedTopic.hasEntrustMission ?? hasEntrustMission;
          }
        }

        this.threadDetail = {
          id: detail.contentId,
          userId: detail.userId ?? detail.user_id ?? null,
          title: detail.title,
          author: detail.username,
          authorAvatar: resolveAuthorAvatar(detail),
          createdAt: detail.createdAt,
          content: textContent,
          contentHtml: sanitizedHtml,
          contentText: textContent,
          imageUrls,
          categoryId: categoryId,
          categoryName: categoryName,
          parentContentId:
            detail.parentContentId ??
            detail.parent_content_id ??
            detail.parentId ??
            detail.parent_id ??
            null,
          type: detail.type,
          isHot: isHot,
          hasEntrustMission: Boolean(hasEntrustMission),
          likeCount: extractLikeCount(detail),
          // 添加回覆統計資訊
          replyCount: stats.replyCount ?? 0,
          lastReplyTime: stats.lastReplyTime ?? null,
          lastReplyUser: stats.lastReplyUser ?? null,
          lastReplyUserId: stats.lastReplyUserId ?? null,
          lastReplyUserAvatar: stats.lastReplyUserAvatar ?? "",
          entrustMissionId: entrustMissionMeta.entrustMissionId,
          entrustMissionUserId: entrustMissionMeta.entrustMissionUserId,
          rewardPoints: entrustMissionMeta.rewardPoints,
          bestAnswerContentId: finalBestAnswerContentId,
          bestAnswerSelected: Boolean(finalBestAnswerContentId),
        };

        if (!this.threadDetail.authorAvatar && this.threadDetail.author) {
          this.threadDetail.authorAvatar = await this.resolveUserAvatar(
            this.threadDetail.userId,
            this.threadDetail.author,
          );
        }

        if (
          this.threadDetail.lastReplyUser &&
          !this.threadDetail.lastReplyUserAvatar
        ) {
          this.threadDetail.lastReplyUserAvatar =
            await this.resolveUserAvatar(
              this.threadDetail.lastReplyUserId,
              this.threadDetail.lastReplyUser,
            );
        }

        if (finalBestAnswerContentId) {
          setCachedBestAnswerContentId({
            entrustMissionId: entrustMissionMeta.entrustMissionId,
            threadId: threadContentId,
            bestAnswerContentId: finalBestAnswerContentId,
          });
        } else if (resolvedBestAnswer?.isExplicit) {
          clearCachedBestAnswerContentId({
            entrustMissionId: entrustMissionMeta.entrustMissionId,
            threadId: threadContentId,
          });
        }

        // 更新快取的統計資訊
        if (detail.contentId && stats.replyCount !== null) {
          this.threadReplyStats[detail.contentId] = {
            replyCount: stats.replyCount ?? 0,
            lastReplyTime: stats.lastReplyTime ?? null,
            lastReplyUser: stats.lastReplyUser ?? null,
            lastReplyUserId: stats.lastReplyUserId ?? null,
            lastReplyUserAvatar: stats.lastReplyUserAvatar ?? "",
            fetchedAt: Date.now(),
          };
        }
      }

      await Promise.all([
        this.fetchThreadRepliesPage(1, { threadId }),
        this.fetchThreadCommentsPage(1, { threadId }),
      ]);
    } catch (error) {
      console.error("獲取帖文詳情失敗:", error);
    }
  },

  /**
   * 【入口點】打開線程詳情
   *
   * 此方法由 ForumView.vue 的 watch(route.params.id) 中調用（見 ForumView.vue L134）
   * 流程：
   * 1. 記錄選中的線程 ID 到 state.selectedThread
   * 2. 調用 fetchThreadDetail() 取回線程詳情及其回覆列表
   * 3. TopicListView.vue 會根據 state.threadDetail 的更新自動在詳情視圖中展示內容
   */
  openThread(topicId) {
    this.selectedThread = topicId;
    // 這裡不 await 是刻意的：讓 UI 先切到詳情畫面，再由 loading 狀態補資料。
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
      categoryName: null,
      parentContentId: null,
      type: null,
      isHot: false,
      hasEntrustMission: false,
      likeCount: 0,
      replyCount: 0,
      lastReplyTime: null,
      lastReplyUser: null,
      lastReplyUserId: null,
      entrustMissionId: null,
      entrustMissionUserId: null,
      rewardPoints: 0,
      bestAnswerContentId: null,
      bestAnswerSelected: false,
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

  cacheBestAnswerSelection({
    entrustMissionId = null,
    threadId = null,
    bestAnswerContentId = null,
  } = {}) {
    setCachedBestAnswerContentId({
      entrustMissionId,
      threadId,
      bestAnswerContentId,
    });
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
};
