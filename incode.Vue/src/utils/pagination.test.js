import { describe, expect, it } from "vitest";
import { buildDisplayPages } from "@/utils/pagination.js";

describe("buildDisplayPages", () => {
  it("只有一頁（或沒有）時不顯示頁碼", () => {
    expect(buildDisplayPages(1, 1)).toEqual([]);
    expect(buildDisplayPages(1, 0)).toEqual([]);
  });

  it("總頁數不超過上限時全部顯示", () => {
    expect(buildDisplayPages(1, 3)).toEqual([1, 2, 3]);
    expect(buildDisplayPages(2, 5)).toEqual([1, 2, 3, 4, 5]);
  });

  it("目前頁盡量置中", () => {
    expect(buildDisplayPages(5, 10)).toEqual([3, 4, 5, 6, 7]);
  });

  it("靠近開頭或結尾時視窗貼齊邊界，仍維持最大顯示數", () => {
    expect(buildDisplayPages(1, 10)).toEqual([1, 2, 3, 4, 5]);
    expect(buildDisplayPages(2, 10)).toEqual([1, 2, 3, 4, 5]);
    expect(buildDisplayPages(10, 10)).toEqual([6, 7, 8, 9, 10]);
    expect(buildDisplayPages(9, 10)).toEqual([6, 7, 8, 9, 10]);
  });

  it("可以指定最大顯示數", () => {
    expect(buildDisplayPages(5, 10, 3)).toEqual([4, 5, 6]);
    expect(buildDisplayPages(1, 10, 3)).toEqual([1, 2, 3]);
    expect(buildDisplayPages(10, 10, 3)).toEqual([8, 9, 10]);
  });
});
