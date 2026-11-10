import { describe, expect, it } from "vitest";
import { getAvatarUrl } from "@/utils/avatar.js";

describe("getAvatarUrl", () => {
  it("有指定網址時直接使用", () => {
    expect(getAvatarUrl("小明", "https://example.com/a.png")).toBe("https://example.com/a.png");
  });

  it("沒有網址時以名字產生頭像，名字會做 URL 編碼", () => {
    expect(getAvatarUrl("小明")).toBe("https://ui-avatars.com/api/?name=%E5%B0%8F%E6%98%8E&background=random");
    expect(getAvatarUrl("A&B C")).toContain("name=A%26B%20C");
  });

  it("名字空白或沒有時用 User", () => {
    for (const name of [undefined, null, "", "   "]) {
      expect(getAvatarUrl(name)).toContain("name=User&");
    }
  });
});
