import axios from 'axios'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  // 在這裡改timeout時間 
  timeout: 50000,
  headers: { 'Content-Type': 'application/json' }
})

apiClient.interceptors.request.use(config => {
  const token = localStorage.getItem('token') 
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
}, error => Promise.reject(error))

apiClient.interceptors.response.use(
  response => response.data, 
  error => {
    if (error.response && error.response.status === 401) {
      console.error('登入逾時，請重新登入')
    }
    return Promise.reject(error)
  }
)

export default apiClient