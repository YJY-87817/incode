import { describe, expect, it } from "vitest";
import {
  POSTER_PLACEHOLDER,
  formatReleaseDate,
  formatRuntime,
  releaseLabel,
  tmdbImage,
  youtubeEmbedUrl,
} from "@/utils/movie.js";

describe("tmdbImage", () => {
  it("組出完整的 TMDB 圖片網址", () => {
    expect(tmdbImage("/abc.jpg", "w500")).toBe(
      "https://image.tmdb.org/t/p/w500/abc.jpg",
    );
    expect(tmdbImage("/abc.jpg")).toBe(
      "https://image.tmdb.org/t/p/w342/abc.jpg",
    );
  });
  it("沒有圖片時回傳預設圖", () => {
    expect(tmdbImage(null)).toBe(POSTER_PLACEHOLDER);
    expect(tmdbImage("")).toBe(POSTER_PLACEHOLDER);
  });
});

describe("formatReleaseDate / releaseLabel", () => {
  it("格式化日期，無效值回空字串", () => {
    expect(formatReleaseDate("2026-12-05")).toBe("2026/12/05");
    expect(formatReleaseDate("2026-12-05T00:00:00")).toBe("2026/12/05");
    expect(formatReleaseDate(null)).toBe("");
    expect(formatReleaseDate("abc")).toBe("");
  });
  it("今天之後為即將上映，當天與之前為上映中", () => {
    const today = new Date(2026, 10, 27, 15, 30);
    expect(releaseLabel("2026-11-28", today)).toBe("即將上映");
    expect(releaseLabel("2026-11-27", today)).toBe("上映中");
    expect(releaseLabel("2026-10-01", today)).toBe("上映中");
    expect(releaseLabel(null, today)).toBe("");
  });
});

describe("formatRuntime", () => {
  it("轉成小時與分鐘", () => {
    expect(formatRuntime(125)).toBe("2 小時 5 分");
    expect(formatRuntime(120)).toBe("2 小時");
    expect(formatRuntime(45)).toBe("45 分");
  });
  it("沒有資料回空字串", () => {
    expect(formatRuntime(null)).toBe("");
    expect(formatRuntime(0)).toBe("");
  });
});

describe("youtubeEmbedUrl", () => {
  it("只接受合法的影片代碼", () => {
    expect(youtubeEmbedUrl("dQw4w9WgXcQ")).toBe(
      "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ",
    );
    expect(youtubeEmbedUrl('x"><script>')).toBe("");
    expect(youtubeEmbedUrl(null)).toBe("");
  });
});
