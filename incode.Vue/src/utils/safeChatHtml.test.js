import { describe, it, expect } from "vitest";
import { toSafeHtml } from "./safeChatHtml.js";

describe("toSafeHtml", () => {
  it("空訊息回傳空字串", () => {
    expect(toSafeHtml(null)).toBe("");
    expect(toSafeHtml({ role: "ai", text: "" })).toBe("");
  });

  it("使用者輸入一律跳脫為純文字", () => {
    const out = toSafeHtml({ role: "user", text: '<img src=x onerror="alert(1)"><b>hi</b>' });
    expect(out).not.toContain("<img");
    expect(out).not.toContain("<b>");
    expect(out).toContain("&lt;img");
  });

  it("AI 訊息保留格式但移除腳本與事件屬性", () => {
    const out = toSafeHtml({
      role: "ai",
      text: '<p>ok <strong>bold</strong></p><script>alert(1)</script><img src="x" onerror="alert(1)">',
    });
    expect(out).toContain("<strong>bold</strong>");
    expect(out).not.toContain("<script");
    expect(out).not.toContain("onerror");
  });

  it("移除 javascript: 連結", () => {
    const out = toSafeHtml({ role: "admin", text: '<a href="javascript:alert(1)">x</a>' });
    expect(out).not.toContain("javascript:");
  });
});
