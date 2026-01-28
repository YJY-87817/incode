import axios from "axios";

const service = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:7190", // 你的後端 API 網址
  timeout: 5000, // 超時設定
  withCredentials: true,
});

export default service;
