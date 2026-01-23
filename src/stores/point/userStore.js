//載入並儲存使用者個人資料的 Pinia Store
import { defineStore } from 'pinia';
import { fetchUserProfile } from '@/api/point/user.js';


export const useUserStore = defineStore('user', {
  state: () => ({
    userProfile: {
      name: "載入中...",
      level: 0,
      id: "-",
      title: "-",
      exp: 0,
    },
    isLoading: false,
    fetchError: null
  }),
  actions: {
    async fetchUserProfile() {
      if (this.isLoading) return // 避免重複請求


      this.isLoading = true
      this.fetchError = null


      try {
        // 建議：如果是正式開發，API URL 應寫在 .env 檔案
        const response = await fetchUserProfile();
        const data  = response.data;


        // 重要：根據你後端實際回傳欄位做 mapping
        this.userProfile = {
          name: data.username || data.name || data.displayName || "未知使用者",
          level: data.level ?? 1,
          id: data.userId || data.id || data.accountId || "-",
          title: data.title || data.role || "新手冒險者",
          exp: data.expPercent ?? data.experience ?? 0
        }
      } catch (err) {
        console.error('取得個人資料失敗', err)
        this.fetchError = err.response?.data?.message || '伺服器錯誤，請稍後再試'
      } finally {
        this.isLoading = false
      }
    }
  }
});

