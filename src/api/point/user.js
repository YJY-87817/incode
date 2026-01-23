import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:7190', // 從 .env 讀
  withCredentials: true
})

// 自動帶 token（登入後會存到 localStorage）
api.interceptors.request.use(config => {
  const token = localStorage.getItem('access_token') // 或你的 token key
  console.log('[Axios] Token from localStorage:', token ? '存在' : '不存在')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  console.log('[Axios] 已加上 Authorization header')
  } else {
    console.log('[Axios] 沒有 token，不加 header')
  }
  return config
}, error => Promise.reject(error))

// 你的 fetchUserProfile 改用 api 實例
export const fetchUserProfile = () => api.get('/api/PointLogApi/user/my-logs');

