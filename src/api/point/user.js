import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:7190", // 從 .env 讀
  withCredentials: true,
});

// 使用 session cookies 認證，不需要 JWT token
// 移除原本的 JWT interceptor 以避免干擾 session cookie 認證

// 取得使用者點數紀錄
export const fetchPointLogs = () => api.get("/api/PointLogApi/user/my-logs");
