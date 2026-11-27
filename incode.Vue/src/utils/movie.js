// TMDB 圖片：後端只回相對路徑（例如 /abc.jpg），在這裡組成完整網址
const TMDB_IMAGE_BASE = "https://image.tmdb.org/t/p";

export const POSTER_PLACEHOLDER =
  "data:image/svg+xml;utf8," +
  encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="342" height="513" viewBox="0 0 342 513">' +
      '<rect width="342" height="513" fill="#e9ecef"/>' +
      '<text x="171" y="262" font-size="20" text-anchor="middle" fill="#6c757d" font-family="sans-serif">No Image</text>' +
      "</svg>",
  );

/** size 例如 w185 / w342 / w500 / w1280；沒有圖片回傳預設圖 */
export const tmdbImage = (path, size = "w342") =>
  path ? `${TMDB_IMAGE_BASE}/${size}${path}` : POSTER_PLACEHOLDER;

const parseDate = (value) => {
  if (!value) return null;
  const [y, m, d] = String(value).slice(0, 10).split("-").map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
};

/** 2026-12-05 → 2026/12/05（解析失敗回空字串） */
export const formatReleaseDate = (value) => {
  const date = parseDate(value);
  if (!date) return "";
  const pad = (n) => String(n).padStart(2, "0");
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())}`;
};

/** 上映日在今天之後 → 「即將上映」，其餘「上映中」；沒有日期回空字串 */
export const releaseLabel = (value, today = new Date()) => {
  const date = parseDate(value);
  if (!date) return "";
  const start = new Date(
    today.getFullYear(),
    today.getMonth(),
    today.getDate(),
  );
  return date > start ? "即將上映" : "上映中";
};

/** 125 → 2 小時 5 分；沒有資料回空字串 */
export const formatRuntime = (minutes) => {
  const total = Number(minutes);
  if (!Number.isFinite(total) || total <= 0) return "";
  const h = Math.floor(total / 60);
  const m = total % 60;
  if (h === 0) return `${m} 分`;
  return m === 0 ? `${h} 小時` : `${h} 小時 ${m} 分`;
};

/** YouTube 影片代碼只接受英數、底線與連字號（後端已驗證，前端再擋一次才串進網址） */
export const youtubeEmbedUrl = (key) =>
  key && /^[A-Za-z0-9_-]{6,20}$/.test(key)
    ? `https://www.youtube-nocookie.com/embed/${key}`
    : "";
