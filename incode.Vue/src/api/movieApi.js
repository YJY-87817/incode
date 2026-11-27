import apiClient from "@/api/request.js";

// 電影資料來自後端轉呼叫 TMDB（後端有快取，Token 不會外露）
// 回應為 ResultDTO：{ ok, code, message, data }；apiClient 已回傳 response.data

/** 近期上映（上個月 1 日～下個月月底，依上映日由新到舊） */
export const getRecentMovies = () => apiClient.get("/MovieApi/recent");

/** 單部電影詳細資料（含導演、演員、預告片）；找不到時 HTTP 404 */
export const getMovie = (id) =>
  apiClient.get(`/MovieApi/${encodeURIComponent(id)}`);
