import apiClient from "@/api/request.js";

export const getContentBaseUrl = () =>
  (apiClient.defaults?.baseURL || "").replace(/\/api$/, "");

export const toAbsoluteUrl = (url) => {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;
  const baseUrl = getContentBaseUrl();
  if (url.startsWith("/")) return `${baseUrl}${url}`;
  return `${baseUrl}/${url}`;
};

const looksLikeHtml = (value) => /<[^>]+>/.test(value || "");

export const fetchBodyHtml = async (body) => {
  if (!body) return "";
  if (looksLikeHtml(body)) return body;

  const url = toAbsoluteUrl(body);
  try {
    const response = await apiClient.get(url, { responseType: "text" });
    return response;
  } catch (error) {
    console.error("讀取內容 HTML 失敗:", error);
    return "";
  }
};

/**
 * 解析貼文內容，提取文字和圖片URLs
 *
 * 功能：
 * 1. 使用正則表達式萃取 HTTP/HTTPS 圖片URL
 * 2. 支援 .png, .jpg, .jpeg, .gif, .webp, .bmp 格式
 * 3. 移除重複的URL
 * 4. 清理純文字部分 (移除URL、多餘換行)
 * 5. 若無文字但有圖片，顯示 "(已附圖片)"
 *
 * @param {string} body - 貼文原始內容
 * @returns {Object} { textContent, imageUrls }
 */
export const parseBodyContent = (body) => {
  const raw = body || "";

  // 正則：完整的 http/https 圖片 URL
  // 支援的格式：.png, .jpe?g, .gif, .webp, .bmp
  const absolutePattern =
    /https?:\/\/[^\s()\[\]]+\.(?:png|jpe?g|gif|webp|bmp)/gi;
  const rootRelativePattern =
    /(?:^|\s)(\/[^\s()\[\]]+\.(?:png|jpe?g|gif|webp|bmp))/gi;

  const absoluteMatches = raw.match(absolutePattern) || [];
  const rootMatches = Array.from(raw.matchAll(rootRelativePattern)).map(
    (match) => match[1],
  );
  const matches = [...absoluteMatches, ...rootMatches];

  // 去重：保留唯一的圖片URL
  const uniqueImages = Array.from(new Set(matches));
  const imageUrls = uniqueImages
    .map((url) => url.trim())
    .filter(Boolean)
    .map(toAbsoluteUrl);

  // 清理文字：移除URL、多餘換行
  const textContent = raw
    .replace(absolutePattern, "")
    .replace(rootRelativePattern, " ")
    .replace(/\n{2,}/g, "\n")
    .trim();

  // 若無文字但有圖片，顯示提示
  const displayText = textContent || (imageUrls.length ? "(已附圖片)" : "");

  return {
    textContent: displayText,
    imageUrls,
  };
};

/**
 * [已棄用 - 僅作為 fallback] 計算貼文的回應統計資訊
 *
 * 注意：後端現在直接在 ContentDto 中提供這些統計資訊（replyCount, lastReplyTime, lastReplyUser, lastReplyUserId）
 * 前端應優先使用後端提供的資料，此函數僅作為備用方案保留
 *
 * 蒐集該貼文的所有回應，包括：
 * - type=2: 回文
 * - type=3: 留言
 * - type=4: 留言回覆
 *
 * 並計算最新回應的時間和作者
 *
 * @deprecated 優先使用後端 API 提供的統計資訊
 * @param {number} postId - 貼文ID
 * @param {Array} allContents - 所有內容列表
 * @returns {Object} { replyCount, lastReplyTime, lastReplyUser, lastReplyUserId }
 */
export const calculateReplyStats = (postId, allContents) => {
  // 找出該貼文的所有回應
  // 條件：parentContentId === postId AND (type=2 OR type=3 OR type=4)
  const replies = allContents.filter(
    (c) =>
      c.parentContentId === postId &&
      (c.type === 2 || c.type === 3 || c.type === 4),
  );

  if (replies.length === 0) {
    return {
      replyCount: 0,
      lastReplyTime: null,
      lastReplyUser: null,
      lastReplyUserId: null,
    };
  }

  // 按時間排序（由新至舊），取得最新的回應
  const sortedReplies = replies.sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );
  const lastReply = sortedReplies[0];

  return {
    replyCount: replies.length,
    lastReplyTime: lastReply.createdAt,
    lastReplyUser: lastReply.username,
    lastReplyUserId: lastReply.userId ?? lastReply.user_id ?? null,
  };
};

/**
 * 時間格式化 - 顯示相對時間 (如「2小時前」)
 *
 * @param {string|Date} date - ISO日期字串或Date物件
 * @returns {string} 人類易讀的相對時間
 */
export const formatTimeAgo = (date) => {
  const now = new Date();
  const d = new Date(date);
  const diff = now.getTime() - d.getTime();

  const seconds = Math.floor(diff / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);

  if (days > 0) return `${days} 天前`;
  if (hours > 0) return `${hours} 小時前`;
  if (minutes > 0) return `${minutes} 分鐘前`;
  return "剛剛";
};

/**
 * 日期格式化 - 顯示完整日期時間
 *
 * @param {string|Date} date - ISO日期字串或Date物件
 * @returns {string} YYYY-MM-DD HH:MM 格式
 */
export const formatDate = (date) => {
  if (!date) return "";
  const d = new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${year}-${month}-${day} ${hours}:${minutes}`;
};

/**
 * 附加URLs到內容
 * 用於圖片上傳完成後追加URL到貼文/留言內容
 *
 * @param {string} original - 原始內容
 * @param {string|Array} urls - 要附加的URL(們)
 * @returns {string} 合併後的內容
 */
export const appendUrls = (original, urls) => {
  const list = Array.isArray(urls) ? urls : [urls];
  const chunk = list.filter(Boolean).join("\n");
  if (!chunk) return original;
  return [original.trim(), chunk].filter(Boolean).join("\n");
};

/**
 * 計算分頁顯示
 *
 * 根據目前頁碼和總頁數，計算應顯示的頁碼範圍
 * 最多顯示5個頁碼 (包括省略號)
 *
 * @param {number} currentPage - 目前頁碼
 * @param {number} totalPages - 總頁數
 * @param {number} maxDisplay - 最多顯示幾個頁碼 (預設5)
 * @returns {Array<number>} 應顯示的頁碼陣列
 */
export const calculateDisplayPages = (
  currentPage,
  totalPages,
  maxDisplay = 5,
) => {
  const pages = [];

  if (totalPages <= 1) return pages;

  let start = Math.max(1, currentPage - Math.floor(maxDisplay / 2));
  let end = Math.min(totalPages, start + maxDisplay - 1);

  if (end - start < maxDisplay - 1) {
    start = Math.max(1, end - maxDisplay + 1);
  }

  for (let i = start; i <= end; i++) {
    pages.push(i);
  }

  return pages;
};
