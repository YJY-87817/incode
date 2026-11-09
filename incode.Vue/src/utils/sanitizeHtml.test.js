import { describe, it, expect } from "vitest";
import { normalizeRichContent } from "./sanitizeHtml.js";

describe("normalizeRichContent", () => {
  it("空輸入", () => {
    expect(normalizeRichContent("")).toEqual({ sanitizedHtml: "", textContent: "", imageUrls: [] });
    expect(normalizeRichContent(null).imageUrls).toEqual([]);
  });

  it("移除 script 與事件屬性", () => {
    const { sanitizedHtml } = normalizeRichContent(
      '<p onclick="x()">a</p><script>alert(1)</script><img src="/a.png" onerror="x()">',
    );
    expect(sanitizedHtml).not.toContain("script");
    expect(sanitizedHtml).not.toContain("onclick");
    expect(sanitizedHtml).not.toContain("onerror");
  });

  it("圖片網址正規化並去除重複", () => {
    const { imageUrls, sanitizedHtml } = normalizeRichContent(
      '<img src="/a.png"><img src="/a.png"><img src="/b.png">',
      (u) => `https://h${u}`,
    );
    expect(imageUrls).toEqual(["https://h/a.png", "https://h/b.png"]);
    expect(sanitizedHtml.match(/<img/g)).toHaveLength(2);
  });

  it("連結加上 noopener noreferrer 與 target=_blank", () => {
    const { sanitizedHtml } = normalizeRichContent('<a href="https://e.com">x</a>');
    expect(sanitizedHtml).toContain("noopener");
    expect(sanitizedHtml).toContain("noreferrer");
    expect(sanitizedHtml).toContain('target="_blank"');
  });
});
