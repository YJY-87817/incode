import { afterEach, describe, expect, it, vi } from "vitest";
import { topicPageCache } from "@/stores/forum/caches.js";
import {
  TOPIC_SORT_OPTION_TO_API,
  buildTopicsCacheKey,
  clearTopicPageCache,
  getCachedTopicPage,
  normalizeTopicSortOption,
} from "@/stores/forum/topicQuery.js";

afterEach(() => {
  clearTopicPageCache();
  vi.useRealTimers();
});

describe("normalizeTopicSortOption", () => {
  it.each([
    ["latest", "latest"],
    ["Most-Likes", "most-likes"],
    ["  latest-reply ", "latest-reply"],
    ["most-replies", "most-replies"],
    ["createdAt_desc", "latest"], // 舊名稱
    ["CREATEDAT_DESC", "latest"],
  ])("%s → %s", (input, expected) => {
    expect(normalizeTopicSortOption(input)).toBe(expected);
  });

  it("未知或空值回到 latest", () => {
    expect(normalizeTopicSortOption("whatever")).toBe("latest");
    expect(normalizeTopicSortOption("")).toBe("latest");
    expect(normalizeTopicSortOption(null)).toBe("latest");
    expect(normalizeTopicSortOption(undefined)).toBe("latest");
  });
});

describe("TOPIC_SORT_OPTION_TO_API", () => {
  it("每個正規化後的排序選項都對得到後端的 sortBy", () => {
    for (const input of ["latest", "latest-reply", "most-replies", "most-likes", "createdAt_desc", "unknown", ""]) {
      expect(TOPIC_SORT_OPTION_TO_API[normalizeTopicSortOption(input)]).toBeTruthy();
    }
  });

  it("舊名稱與 latest 會產生相同的快取 key（不會重複請求同一份資料）", () => {
    const base = { categoryId: 1, keyword: "", page: 1, pageSize: 10 };
    expect(buildTopicsCacheKey({ ...base, sortOption: normalizeTopicSortOption("createdAt_desc") })).toBe(
      buildTopicsCacheKey({ ...base, sortOption: normalizeTopicSortOption("latest") }),
    );
  });
});

describe("buildTopicsCacheKey", () => {
  const base = { categoryId: 3, keyword: "Vue", page: 2, pageSize: 10, sortOption: "latest" };

  it("關鍵字忽略大小寫與前後空白", () => {
    expect(buildTopicsCacheKey({ ...base, keyword: "  vUE " })).toBe(buildTopicsCacheKey(base));
  });

  it("沒有分類與關鍵字時以 null / 空字串表示", () => {
    expect(JSON.parse(buildTopicsCacheKey({ page: 1, pageSize: 10, sortOption: "latest" }))).toEqual({
      categoryId: null,
      keyword: "",
      page: 1,
      pageSize: 10,
      sortOption: "latest",
    });
  });

  it("任何一個條件不同就是不同的 key", () => {
    const key = buildTopicsCacheKey(base);
    expect(buildTopicsCacheKey({ ...base, page: 3 })).not.toBe(key);
    expect(buildTopicsCacheKey({ ...base, categoryId: 4 })).not.toBe(key);
    expect(buildTopicsCacheKey({ ...base, sortOption: "most-likes" })).not.toBe(key);
  });
});

describe("getCachedTopicPage", () => {
  it("30 秒內回傳快取，過期後清除並回傳 null", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-01-01T00:00:00Z"));
    topicPageCache.set("k", { fetchedAt: Date.now(), data: { topics: [1] } });

    expect(getCachedTopicPage("k")).toEqual({ topics: [1] });

    vi.setSystemTime(new Date("2026-01-01T00:00:31Z"));
    expect(getCachedTopicPage("k")).toBeNull();
    expect(topicPageCache.has("k")).toBe(false);
  });

  it("沒有快取回傳 null", () => {
    expect(getCachedTopicPage("missing")).toBeNull();
  });
});
