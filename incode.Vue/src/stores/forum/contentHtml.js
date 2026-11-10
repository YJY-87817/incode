import { fetchBodyHtml, toAbsoluteUrl } from "@/utils/contentBody.js";
import { contentBodyCache, contentBodyPending } from "@/stores/forum/caches.js";

export const isHtml = (value) => /<[^>]+>/.test(value || "");

export const escapeHtml = (value) =>
  String(value ?? "").replace(/[&<>"']/g, (char) => {
    switch (char) {
      case "&":
        return "&amp;";
      case "<":
        return "&lt;";
      case ">":
        return "&gt;";
      case '"':
        return "&quot;";
      case "'":
        return "&#39;";
      default:
        return char;
    }
  });

export const buildPreviewHtml = (textContent, imageUrls = []) => {
  const safeText = escapeHtml(textContent).replace(/\n/g, "<br />");
  const images = (Array.isArray(imageUrls) ? imageUrls : [])
    .filter(Boolean)
    .map((url) => `<img src="${escapeHtml(url)}" alt="preview" />`)
    .join("");
  const result =
    safeText && images
      ? `${safeText}<br />${images}`
      : safeText || images || "";
  return result;
};

export const fetchBodyHtmlCached = async (body) => {
  if (!body) {
    return "";
  }
  if (isHtml(body)) {
    return body;
  }

  const url = toAbsoluteUrl(body);
  if (!url) {
    return "";
  }

  if (contentBodyCache.has(url)) {
    return contentBodyCache.get(url);
  }

  if (contentBodyPending.has(url)) {
    return contentBodyPending.get(url);
  }

  const request = (async () => {
    const html = await fetchBodyHtml(body);
    contentBodyCache.set(url, html);
    return html;
  })();

  contentBodyPending.set(url, request);
  try {
    const result = await request;
    return result;
  } finally {
    contentBodyPending.delete(url);
  }
};
