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
    const response = await fetch(url, { method: "GET" });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status} when fetching ${url}`);
    }
    return await response.text();
  } catch (error) {
    console.error("讀取內容 HTML 失敗:", error);
    return "";
  }
};
