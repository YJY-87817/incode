// 管理登入狀態、token、是否已登入的 Pinia Store
import { defineStore } from 'pinia'
import axios from 'axios'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('access_token') || null,
    user: null,
    isAuthenticated: false
  }),

  actions: {
    async login(username, password) {
      try {
        const response = await axios.post('https://localhost:7190/api/Auth/login', {
          username,
          password
        })

        const token = response.data.token || response.data.accessToken
        if (!token) throw new Error('沒有收到 token')

        this.token = token
        localStorage.setItem('access_token', token)
        this.isAuthenticated = true

        // 可選：立即取使用者資料
        await useUserStore().fetchUserProfile()

        return true
      } catch (err) {
        console.error('登入失敗', err)
        throw err
      }
    },

    logout() {
      this.token = null
      this.user = null
      this.isAuthenticated = false
      localStorage.removeItem('access_token')
      // 可導向登入頁
    },

    // 給 axios 用，自動加 header 的 config
    getAxiosConfig() {
      return {
        headers: this.token ? { Authorization: `Bearer ${this.token}` } : {}
      }
    }
  },

  persist: {
    paths: ['token', 'isAuthenticated']
  }
})