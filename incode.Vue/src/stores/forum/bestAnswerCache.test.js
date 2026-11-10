import { beforeEach, describe, expect, it } from "vitest";
import { BEST_ANSWER_CACHE_KEY } from "@/stores/forum/caches.js";
import {
  clearCachedBestAnswerContentId,
  getCachedBestAnswerContentId,
  setCachedBestAnswerContentId,
  toBestAnswerCacheKey,
} from "@/stores/forum/bestAnswerCache.js";

beforeEach(() => {
  window.localStorage.clear();
});

describe("toBestAnswerCacheKey", () => {
  it("優先使用委託 ID，其次主題 ID", () => {
    expect(toBestAnswerCacheKey({ entrustMissionId: 5, threadId: 9 })).toBe("m:5");
    expect(toBestAnswerCacheKey({ entrustMissionId: 0, threadId: 9 })).toBe("t:9");
    expect(toBestAnswerCacheKey({ entrustMissionId: null, threadId: "12" })).toBe("t:12");
  });

  it("兩個 ID 都無效時回傳 null", () => {
    expect(toBestAnswerCacheKey({ entrustMissionId: "abc", threadId: -1 })).toBeNull();
    expect(toBestAnswerCacheKey({})).toBeNull();
  });
});

describe("best answer cache", () => {
  it("寫入後可以讀回，且以不同 key 分開存放", () => {
    setCachedBestAnswerContentId({ entrustMissionId: 5, bestAnswerContentId: 100 });
    setCachedBestAnswerContentId({ threadId: 9, bestAnswerContentId: 200 });

    expect(getCachedBestAnswerContentId({ entrustMissionId: 5 })).toBe(100);
    expect(getCachedBestAnswerContentId({ threadId: 9 })).toBe(200);
    expect(JSON.parse(window.localStorage.getItem(BEST_ANSWER_CACHE_KEY))).toEqual({ "m:5": 100, "t:9": 200 });
  });

  it("無效的內容 ID 不會寫入", () => {
    setCachedBestAnswerContentId({ entrustMissionId: 5, bestAnswerContentId: 0 });
    setCachedBestAnswerContentId({ entrustMissionId: 5, bestAnswerContentId: "x" });
    setCachedBestAnswerContentId({ bestAnswerContentId: 10 }); // 沒有 key

    expect(window.localStorage.getItem(BEST_ANSWER_CACHE_KEY)).toBeNull();
  });

  it("清除只影響指定的項目", () => {
    setCachedBestAnswerContentId({ entrustMissionId: 5, bestAnswerContentId: 100 });
    setCachedBestAnswerContentId({ entrustMissionId: 6, bestAnswerContentId: 101 });

    clearCachedBestAnswerContentId({ entrustMissionId: 5 });

    expect(getCachedBestAnswerContentId({ entrustMissionId: 5 })).toBeNull();
    expect(getCachedBestAnswerContentId({ entrustMissionId: 6 })).toBe(101);
  });

  it("localStorage 內容損毀時當作沒有快取", () => {
    window.localStorage.setItem(BEST_ANSWER_CACHE_KEY, "{not json");
    expect(getCachedBestAnswerContentId({ entrustMissionId: 5 })).toBeNull();

    window.localStorage.setItem(BEST_ANSWER_CACHE_KEY, JSON.stringify("string"));
    expect(getCachedBestAnswerContentId({ entrustMissionId: 5 })).toBeNull();
  });
});
