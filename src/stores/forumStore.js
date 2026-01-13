import { defineStore } from 'pinia'

export const useForumStore = defineStore('forum', {
  state: () => ({
    showNewTopicModal: false,
    // 其他論壇相關狀態
  }),
  actions: {
    toggleNewTopicModal(value) {
      this.showNewTopicModal = value
    }
  },
  persist: true
})
