import { defineStore } from 'pinia'

// Content types according to database schema
export const CONTENT_TYPES = {
  POST: 1,           // 貼文(出現在貼文版面)
  REPLY: 2,          // 回文(出現在貼文版面)
  COMMENT: 3,        // 留言
  COMMENT_REPLY: 4   // 留言回覆
}

export const useForumStore = defineStore('forum', {
  state: () => ({
    showNewTopicModal: false,
    lastRefreshTime: 0  // 新增刷新時間戳記狀態
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
  persist: true
})
