import axios from "axios";
import { installAuthInterceptor } from "@/api/authInterceptor.js";

const service = axios.create({
  baseURL: import.meta.env.VITE_BASE_URL, // 後端網址 (.env)；呼叫路徑自帶 /api
  // 在這裡改timeout時間
  timeout: 500000, // 超時設定
  withCredentials: true, // 關鍵！讓 Session Cookie 自動帶上
  headers: {
    Accept: "application/json",
    "Content-Type": "application/json",
  },
});

// 401：登入逾時時清除登入狀態並導向登入頁（未登入時交給呼叫端，見 authInterceptor.js）
installAuthInterceptor(service);

export default service;
