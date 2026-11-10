import { describe, expect, it } from "vitest";
import {
  extractLikeCount,
  extractLikeCountFromInteractions,
  extractUserInteractions,
  normalizeCategory,
  toInteractionCategoryId,
} from "@/stores/forum/normalizers.js";
import { normalizeEntrustMissionMeta } from "@/stores/forum/entrustMeta.js";

describe("extractLikeCount", () => {
  it.each([
    [{ likeCount: 5 }, 5],
    [{ like_count: "7" }, 7],
    [{ likesCount: 2 }, 2],
    [{ total_likes: 9 }, 9],
    [{ interaction_like_count: 4 }, 4],
  ])("從各種欄位名稱取得按讚數 %j", (item, expected) => {
    expect(extractLikeCount(item)).toBe(expected);
  });

  it("沒有按讚欄位（null / 空字串）時改用互動資料計算，而不是直接當成 0", () => {
    const interactions = [{ interactionCategoryId: 1 }, { interactionCategoryId: 1 }, { categoryId: 3 }];
    expect(extractLikeCount({ interactions })).toBe(2);
    expect(extractLikeCount({ likeCount: null, interactions })).toBe(2);
    expect(extractLikeCount({ likeCount: "", interactions })).toBe(2);
  });

  it("有按讚欄位時以欄位為準，包含 0", () => {
    expect(extractLikeCount({ likeCount: 0, interactions: [{ interactionCategoryId: 1 }] })).toBe(0);
  });

  it("欄位不是數字時也會改用互動資料", () => {
    expect(extractLikeCount({ likeCount: "abc", interactions: [{ interactionCategoryId: 1 }] })).toBe(1);
  });

  it("負數視為 0", () => {
    expect(extractLikeCount({ likeCount: -3 })).toBe(0);
  });

  it("沒有資料回傳 0", () => {
    expect(extractLikeCount({})).toBe(0);
    expect(extractLikeCount(null)).toBe(0);
  });
});

describe("extractLikeCountFromInteractions", () => {
  it("只計算按讚（分類 1）", () => {
    const item = {
      interactions: [
        { interactionCategoryId: 1 },
        { interaction_category_id: 1 },
        { categoryId: 3 },
        { category_id: 1 },
      ],
    };
    expect(extractLikeCountFromInteractions(item)).toBe(3);
  });

  it("不是陣列回傳 null", () => {
    expect(extractLikeCountFromInteractions({})).toBeNull();
    expect(extractLikeCountFromInteractions({ interactions: "x" })).toBeNull();
  });
});

describe("toInteractionCategoryId / extractUserInteractions", () => {
  it("取得互動分類 ID", () => {
    expect(toInteractionCategoryId({ interactionCategoryId: 2 })).toBe(2);
    expect(toInteractionCategoryId({ category_id: "4" })).toBe(4);
    expect(toInteractionCategoryId({ categoryId: "abc" })).toBeNull();
  });

  it("從各種回應格式取出互動清單", () => {
    const list = [{ id: 1 }];
    expect(extractUserInteractions(list)).toBe(list);
    expect(extractUserInteractions({ Interactions: list })).toBe(list);
    expect(extractUserInteractions({ interactions: list })).toBe(list);
    expect(extractUserInteractions({ data: { Interactions: list } })).toBe(list);
    expect(extractUserInteractions({ data: { interactions: list } })).toBe(list);
    expect(extractUserInteractions({ data: {} })).toEqual([]);
    expect(extractUserInteractions(null)).toEqual([]);
  });
});

describe("normalizeCategory", () => {
  it("統一欄位名稱", () => {
    expect(normalizeCategory({ content_category_id: 3, categoryName: "問答", post_count: 8 })).toEqual({
      id: 3,
      name: "問答",
      description: null,
      postCount: 8,
    });
  });

  it("接受包在 data 裡的回應與 fallback 名稱", () => {
    expect(normalizeCategory({ data: { id: 1 } }, "預設")).toEqual({
      id: 1,
      name: "預設",
      description: null,
      postCount: 0,
    });
  });

  it("空值、陣列、沒有 id 與名稱時回傳 null", () => {
    expect(normalizeCategory(null)).toBeNull();
    expect(normalizeCategory([])).toBeNull();
    expect(normalizeCategory({})).toBeNull();
  });
});

describe("normalizeEntrustMissionMeta", () => {
  it("從委託物件取出 ID、賞金、委託人與最佳解答", () => {
    expect(
      normalizeEntrustMissionMeta({
        entrustMission: { id: "5", rewardPoints: "100", userId: "8", best_answer_content_id: "30" },
      }),
    ).toEqual({ entrustMissionId: 5, rewardPoints: 100, entrustMissionUserId: 8, bestAnswerContentId: 30 });
  });

  it("頂層欄位優先，snake_case 也支援", () => {
    expect(
      normalizeEntrustMissionMeta({ entrust_mission_id: 2, reward_points: 50, ownerUserId: 3, bestAnswerContentId: 9 }),
    ).toEqual({ entrustMissionId: 2, rewardPoints: 50, entrustMissionUserId: 3, bestAnswerContentId: 9 });
  });

  it("沒有委託時全部是空值", () => {
    expect(normalizeEntrustMissionMeta({})).toEqual({
      entrustMissionId: null,
      rewardPoints: 0,
      entrustMissionUserId: null,
      bestAnswerContentId: null,
    });
    expect(normalizeEntrustMissionMeta()).toEqual({
      entrustMissionId: null,
      rewardPoints: 0,
      entrustMissionUserId: null,
      bestAnswerContentId: null,
    });
  });
});
