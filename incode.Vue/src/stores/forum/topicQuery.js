import { TOPIC_PAGE_CACHE_TTL_MS, topicPageCache, topicPagePending } from "@/stores/forum/caches.js";

export const TOPIC_SORT_OPTION_TO_API = {
  createdAt_desc: "latest",
  latest: "latest",
  "latest-reply": "latest-reply",
  "most-replies": "most-replies",
  "most-likes": "most-likes",
};

export const normalizeTopicSortOption = (option) => {
  const normalized = String(option || "")
    .trim()
    .toLowerCase();
  return TOPIC_SORT_OPTION_TO_API[normalized] ? normalized : "latest";
};

export const buildTopicsCacheKey = ({
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

export const getCachedTopicPage = (cacheKey) => {
  const cached = topicPageCache.get(cacheKey);
  if (!cached) return null;
  if (Date.now() - cached.fetchedAt > TOPIC_PAGE_CACHE_TTL_MS) {
    topicPageCache.delete(cacheKey);
    return null;
  }
  return cached.data;
};

export const clearTopicPageCache = () => {
  topicPageCache.clear();
  topicPagePending.clear();
};
