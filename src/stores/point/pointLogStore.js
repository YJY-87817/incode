// 管理使用者點數紀錄的 Pinia Store
import { defineStore } from 'pinia';
import { fetchPointLogs } from '@/api/point/user.js';

export const usePointLogStore = defineStore('pointLog', {
  state: () => ({
    pointLogs: [], // 存放點數紀錄陣列
    isLoading: false,
    fetchError: null
  }),
  actions: {
    async fetchPointLogs() {
      if (this.isLoading) return // 避免重複請求

      this.isLoading = true;
      this.fetchError = null;

      try {
        const response = await fetchPointLogs();
        // API 回傳的是點數紀錄陣列，直接存入 state
        this.pointLogs = response.data || [];

        // 根據需要可以加入資料轉換邏輯
        // 例如：格式化時間、計算總點數等
        if (this.pointLogs.length > 0) {
          console.log('成功取得點數紀錄:', this.pointLogs.length, '筆');
        }

      } catch (err) {
        console.error('取得點數紀錄失敗', err);
        this.fetchError = err.response?.data?.message || '伺服器錯誤，請稍後再試';
      } finally {
        this.isLoading = false;
      }
    },

    // 輔助方法：計算總點數變化
    getTotalPoints() {
      return this.pointLogs.reduce((total, log) => total + (log.changeAmount || 0), 0);
    },

    // 輔助方法：根據來源過濾紀錄
    getLogsBySource(sourceType) {
      return this.pointLogs.filter(log => log.sourceType === sourceType);
    }
  }
});