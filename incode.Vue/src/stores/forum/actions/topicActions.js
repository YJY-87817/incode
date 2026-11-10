import contentApi from "@/api/contentApi.js";
import { parseBodyContent, toAbsoluteUrl } from "@/utils/contentBody.js";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { resolveAuthorAvatar } from "@/stores/forum/avatars.js";
import { contentBodyCache, contentBodyPending, entrustMissionByContentCache, entrustMissionByContentPending, topicPageCache, topicPagePending } from "@/stores/forum/caches.js";
import { CONTENT_TYPES } from "@/stores/forum/constants.js";
import { buildPreviewHtml, fetchBodyHtmlCached, isHtml } from "@/stores/forum/contentHtml.js";
import { extractLikeCount, extractPreviewPayload, extractReplyStats } from "@/stores/forum/normalizers.js";
import { TOPIC_SORT_OPTION_TO_API, buildTopicsCacheKey, clearTopicPageCache, getCachedTopicPage, normalizeTopicSortOption } from "@/stores/forum/topicQuery.js";

export const topicActions = {
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
        sortBy:
          TOPIC_SORT_OPTION_TO_API[
            normalizeTopicSortOption(this.sortOption)
          ] ?? "latest",
      };

      if (targetCategory) params.categoryId = targetCategory;
      if (keyword) params.keyword = keyword;

      const requestPromise = (async () => {
        const data = await contentApi.getContents(params);
        const items = Array.isArray(data?.items) ? data.items : [];

        let serverReplyStatsProvided = Boolean(data?.replyStatsIncluded);
        const totalCount = data?.totalCount ?? items.length;
        const totalPages =
          data?.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize));
        const currentPage = data?.currentPage ?? data?.page ?? targetPage;

        const topics = await Promise.all(
          items.map(async (item) => {
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
                lastReplyUserAvatar: stats.lastReplyUserAvatar ?? "",
                fetchedAt: Date.now(),
              };
            }

            const fallbackStats =
              contentId && this.threadReplyStats[contentId]
                ? this.threadReplyStats[contentId]
                : {};

            return {
              id: contentId,
              userId: item.userId ?? item.user_id ?? null,
              title: item.title,
              content: bodyText ?? "",
              contentText: bodyText ?? "",
              contentHtml: bodySanitizedHtml ?? "",
              imageUrls: bodyImages ?? [],
              author: item.username,
              authorAvatar: resolveAuthorAvatar(item),
              categoryName: item.categoryName,
              categoryId: item.categoryId ?? item.category_id ?? null,
              parentContentId:
                item.parentContentId ??
                item.parent_content_id ??
                item.parentId ??
                item.parent_id ??
                null,
              createdAt: item.createdAt,
              type: item.type,
              likeCount: extractLikeCount(item),
              replyCount: stats.replyCount ?? fallbackStats.replyCount ?? 0,
              lastReplyTime:
                stats.lastReplyTime ?? fallbackStats.lastReplyTime ?? null,
              lastReplyUser:
                stats.lastReplyUser ?? fallbackStats.lastReplyUser ?? null,
              lastReplyUserId:
                stats.lastReplyUserId ??
                fallbackStats.lastReplyUserId ??
                null,
              lastReplyUserAvatar:
                stats.lastReplyUserAvatar ??
                fallbackStats.lastReplyUserAvatar ??
                "",
              viewCount: item.viewCount ?? 0,
              isHot: (stats.replyCount ?? fallbackStats.replyCount ?? 0) > 10,
              hasEntrustMission:
                item.hasEntrustMission ?? item.has_entrust_mission ?? false,
            };
          }),
        );

        await this.enrichTopicAvatarFields(topics);

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

      const topicContentIds = result.topics
        .map((topic) => Number(topic?.id))
        .filter((id) => Number.isFinite(id) && id > 0);

      this.initializeUserInteractionStates(topicContentIds).catch((error) => {
        console.error("批次初始化互動狀態失敗:", error);
      });

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

  triggerTopicRefresh() {
    this.lastRefreshTime = Date.now();
    clearTopicPageCache();
  },

  clearContentCaches() {
    clearTopicPageCache();
    contentBodyCache.clear();
    contentBodyPending.clear();
    entrustMissionByContentCache.clear();
    entrustMissionByContentPending.clear();
    entrustMissionByContentCache.clear();
    entrustMissionByContentPending.clear();
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
    this.sortOption = normalizeTopicSortOption(option);
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
};
