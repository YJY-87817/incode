import { describe, expect, it } from "vitest";
import { buildPreviewHtml, escapeHtml, fetchBodyHtmlCached, isHtml } from "@/stores/forum/contentHtml.js";

describe("escapeHtml", () => {
  it("跳脫 HTML 特殊字元", () => {
    expect(escapeHtml(`<script>alert("x") & 'y'</script>`)).toBe(
      "&lt;script&gt;alert(&quot;x&quot;) &amp; &#39;y&#39;&lt;/script&gt;",
    );
  });

  it("null / undefined 變成空字串，數字轉字串", () => {
    expect(escapeHtml(null)).toBe("");
    expect(escapeHtml(undefined)).toBe("");
    expect(escapeHtml(42)).toBe("42");
  });
});

describe("isHtml", () => {
  it("有標籤才算 HTML", () => {
    expect(isHtml("<p>hi</p>")).toBe(true);
    expect(isHtml("/contents/a.html")).toBe(false);
    expect(isHtml("")).toBe(false);
    expect(isHtml(null)).toBe(false);
  });
});

describe("buildPreviewHtml", () => {
  it("文字會跳脫並把換行轉成 <br />", () => {
    expect(buildPreviewHtml("a<b\nc")).toBe("a&lt;b<br />c");
  });

  it("文字加圖片，圖片網址也會跳脫", () => {
    expect(buildPreviewHtml("hi", ['/a.png?x="1"'])).toBe(
      'hi<br /><img src="/a.png?x=&quot;1&quot;" alt="preview" />',
    );
  });

  it("只有圖片或什麼都沒有", () => {
    expect(buildPreviewHtml("", ["/a.png"])).toBe('<img src="/a.png" alt="preview" />');
    expect(buildPreviewHtml("", [])).toBe("");
    expect(buildPreviewHtml("", null)).toBe("");
    expect(buildPreviewHtml("x", [null, ""])).toBe("x");
  });
});

describe("fetchBodyHtmlCached", () => {
  it("沒有內容回傳空字串，已經是 HTML 直接回傳（不發請求）", async () => {
    expect(await fetchBodyHtmlCached("")).toBe("");
    expect(await fetchBodyHtmlCached(undefined)).toBe("");
    expect(await fetchBodyHtmlCached("<p>x</p>")).toBe("<p>x</p>");
  });
});
