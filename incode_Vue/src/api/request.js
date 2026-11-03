import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
  // 在這裡改timeout時間
  timeout: 500000,
  headers: { "Content-Type": "application/json" },
  // 帶上登入 Cookie (後端圖片上傳等 API 需要登入)；
  // axios.create 建立時就複製預設值，不會吃到 main.js 之後才設定的 axios.defaults.withCredentials
  withCredentials: true,
});

apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

apiClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response && error.response.status === 401) {
      console.error("登入逾時，請重新登入");
    }
    return Promise.reject(error);
  },
);

export default apiClient;
