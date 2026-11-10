import { TOPIC_PAGE_CACHE_TTL_MS, topicPageCache, topicPagePending } from "@/stores/forum/caches.js";

// key 一律小寫（normalizeTopicSortOption 比對前會轉小寫），value 是送給後端的 sortBy
export const TOPIC_SORT_OPTION_TO_API = {
  createdat_desc: "latest", // 舊名稱，等同「最新發佈時間」
  latest: "latest",
  "latest-reply": "latest-reply",
  "most-replies": "most-replies",
  "most-likes": "most-likes",
};

export const normalizeTopicSortOption = (option) => {
  const normalized = String(option || "")
    .trim()
    .toLowerCase();
  // 回傳正規化後的排序選項：舊名稱（createdAt_desc）會換成 latest，不認得的值也是 latest
  return TOPIC_SORT_OPTION_TO_API[normalized] ?? "latest";
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
