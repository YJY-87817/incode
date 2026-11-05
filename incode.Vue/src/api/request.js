import axios from "axios";
import { installAuthInterceptor } from "@/api/authInterceptor.js";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
  // 在這裡改timeout時間
  timeout: 500000,
  headers: { "Content-Type": "application/json" },
  // 帶上登入 Cookie (後端圖片上傳等 API 需要登入)；
  // axios.create 建立時就複製預設值，不會吃到 main.js 之後才設定的 axios.defaults.withCredentials
  withCredentials: true,
});

// 401：登入逾時時清除登入狀態並導向登入頁（未登入時交給呼叫端，見 authInterceptor.js）
installAuthInterceptor(apiClient);

// 回傳 response.data（使用 apiClient 的呼叫端直接拿到 body）
apiClient.interceptors.response.use((response) => response.data);

export default apiClient;
