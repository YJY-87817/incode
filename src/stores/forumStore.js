import { defineStore } from 'pinia'

export const useForumStore = defineStore('forum', {
  state: () => ({
    showNewTopicModal: false,
    // 其他論壇相關狀態
  }),
  actions: {
    toggleNewTopicModal(value) {
      this.showNewTopicModal = value
    },
    triggerTopicRefresh() {
      // 觸發主題刷新旗標
      this.lastRefreshTime = Date.now()
    }
  },
  state: () => ({
    showNewTopicModal: false,
    lastRefreshTime: 0  // 新增刷新時間戳記狀態
  }),
  persist: true
})
