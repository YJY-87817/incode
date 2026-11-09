import DOMPurify from "dompurify";

const escapeHtml = (text) =>
  String(text ?? "").replace(
    /[&<>"']/g,
    (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c],
  );

// 聊天訊息顯示前（v-html）一律經過這裡：
// - 使用者輸入一律當純文字（跳脫 HTML）
// - AI / 客服 / 系統訊息允許格式（Markdown 轉出的 HTML），但先用 DOMPurify 移除 script、事件屬性、javascript: 連結等
export const toSafeHtml = (msg) => {
  if (!msg?.text) return "";
  if (msg.role === "user") return escapeHtml(msg.text);
  return DOMPurify.sanitize(msg.text, {
    USE_PROFILES: { html: true },
    ADD_ATTR: ["target"],
  });
};
