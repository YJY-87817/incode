import axios from "axios";

const service = axios.create({
  baseURL: "https://localhost:7190", // 你的後端 API 網址
  // 在這裡改timeout時間
  timeout: 500000, // 超時設定
  withCredentials: true, // 關鍵！讓 Session Cookie 自動帶上
  headers: {
    Accept: "application/json",
    "Content-Type": "application/json",
  },
});

export default service;
