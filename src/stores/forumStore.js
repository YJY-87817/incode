import { defineStore } from 'pinia'
import apiClient from '@/api/request.js'
import contentInteractionApi from '@/api/contentInteractionApi.js'
import { parseBodyContent, calculateReplyStats } from '@/components/Contents/Forum/forumUtils'

// Content types according to database schema
export const CONTENT_TYPES = {
  POST: 1,           // 貼文(出現在貼文版面)
  REPLY: 2,          // 回文(出現在貼文版面)
  COMMENT: 3,        // 留言
  COMMENT_REPLY: 4   // 留言回覆
}

export const useForumStore = defineStore('forum', {
  state: () => ({
    // ========== 全局狀態 ==========
    windowWidth: typeof window !== 'undefined' ? window.innerWidth : 1024,

    // ========== 分類相關 ==========
    categories: [],
    selectedCategory: null,

    // ========== 主題列表 ==========
    topics: [],
    isLoading: false,
    searchKeyword: '',
    pagination: {
      currentPage: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    },

    // ========== 帖文詳情 ==========
    selectedThread: null,
    threadDetail: {
      id: null,
      title: '',
      author: '',
      createdAt: '',
      content: '',
      imageUrls: [],
      categoryId: null,
      type: null,
    },
    threadPosts: [],
    threadComments: [],
    threadCurrentPage: 1,
    threadItemsPerPage: 5,

    // ========== 新主題表單 ==========
    showNewTopicBox: false,
    newTopic: {
      title: '',
      body: '',
      categoryId: null,
    },
    isSubmittingNewTopic: false,
    isUploadingNewTopicImages: false,

    // ========== 其他 ==========
    showNewTopicModal: false,
    lastRefreshTime: 0,
  }),

  getters: {
    /**
     * 計算帖文詳情的總頁數
     */
    threadTotalPages: (state) => {
      return Math.ceil(state.threadPosts.length / state.threadItemsPerPage)
    },

    /**
     * 當前頁的回文列表
     */
    paginatedThreadPosts: (state) => {
      const start = (state.threadCurrentPage - 1) * state.threadItemsPerPage
      return state.threadPosts.slice(start, start + state.threadItemsPerPage)
    },

    /**
     * 分頁顯示計算
     */
    displayPages: (state) => {
      const { currentPage, totalPages } = state.pagination
      const pages = []
      const maxDisplay = 5

      if (totalPages <= 1) return pages

      let start = Math.max(1, currentPage - Math.floor(maxDisplay / 2))
      let end = Math.min(totalPages, start + maxDisplay - 1)

      if (end - start < maxDisplay - 1) {
        start = Math.max(1, end - maxDisplay + 1)
      }

      for (let i = start; i <= end; i++) pages.push(i)
      return pages
    },

    /**
     * 目前選擇的分類名稱
     */
    selectedCategoryName: (state) => {
      const category = state.categories.find(c => c.id === state.selectedCategory)
      return category?.name || null
    },
  },

  actions: {
    // ========== 視窗寬度 ==========
    updateWindowWidth(width) {
      this.windowWidth = width
    },

    // ========== 分類管理 ==========
    async fetchCategories() {
      this.isLoading = true
      try {
        const data = await apiClient.get('/CategoryApi')
        console.log('分類資料:', data)
        this.categories = data

        // 預設選擇第一個分類
        if (!this.newTopic.categoryId && data.length > 0) {
          this.newTopic.categoryId = this.selectedCategory || data[0].id
        }
      } catch (error) {
        console.error('載入分類失敗:', error)
        alert('載入分類失敗')
      } finally {
        this.isLoading = false
      }
    },

    selectCategory(categoryId) {
      this.selectedCategory = this.selectedCategory === categoryId ? null : categoryId
      // 同步更新新主題的分類
      this.newTopic.categoryId = this.selectedCategory
      this.pagination.currentPage = 1
      this.fetchTopics()
    },

    // ========== 主題列表 ==========
    async fetchTopics(categoryId = null, page = null) {
      const targetCategory = categoryId !== null ? categoryId : this.selectedCategory
      const targetPage = page !== null ? page : this.pagination.currentPage

      this.isLoading = true

      try {
        // 1. 獲取所有內容
        const allContentParams = { pageSize: 9999 }
        if (targetCategory) allContentParams.categoryId = targetCategory

        const allContentData = await apiClient.get('/ContentApi', { params: allContentParams })
        const allContents = allContentData.items || []

        // 2. 過濾 type=1(貼文) 和 type=2(回文)
        let listContents = allContents
          .filter(c => c.type === CONTENT_TYPES.POST || c.type === CONTENT_TYPES.REPLY)

        // 3. 搜尋過濾 (搜尋 title 和 body)
        if (this.searchKeyword.trim()) {
          const keyword = this.searchKeyword.trim().toLowerCase()
          listContents = listContents.filter(c => {
            const titleMatch = c.title?.toLowerCase().includes(keyword)
            const bodyMatch = c.body?.toLowerCase().includes(keyword)
            return titleMatch || bodyMatch
          })
        }

        listContents.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))

        // 4. 前端分頁
        const pageSize = this.pagination.pageSize
        const totalCount = listContents.length
        const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
        const currentPage = Math.min(Math.max(1, targetPage), totalPages)
        const start = (currentPage - 1) * pageSize
        const end = start + pageSize
        const pageItems = listContents.slice(start, end)

        this.pagination = {
          currentPage,
          pageSize,
          totalCount,
          totalPages,
        }

        // 5. 組合顯示資料
        this.topics = pageItems.map(item => {
          const { textContent, imageUrls } = parseBodyContent(item.body)
          const replyStats = calculateReplyStats(item.contentId, allContents)

          return {
            id: item.contentId,
            title: item.title,
            content: textContent,
            imageUrls,
            author: item.username,
            authorAvatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(item.username)}&background=random`,
            categoryName: item.categoryName,
            createdAt: item.createdAt,
            type: item.type,
            replyCount: replyStats.replyCount,
            lastReplyTime: replyStats.lastReplyTime,
            lastReplyUser: replyStats.lastReplyUser,
            viewCount: item.viewCount ?? 0,
            isPinned: item.isPinned ?? false,
            isHot: replyStats.replyCount > 10,
          }
        })
      } catch (error) {
        console.error('載入主題失敗:', error)
        if (error.response) {
          alert(error.response.data.message || '載入主題失敗')
        } else if (error.request) {
          alert('無法連接到伺服器，請檢查網路連線')
        } else {
          alert('發生未知錯誤')
        }
        this.topics = []
      } finally {
        this.isLoading = false
      }
    },

    changePage(page) {
      if (page < 1 || page > this.pagination.totalPages) return
      this.pagination.currentPage = page
      this.fetchTopics()
    },

    // ========== 帖文詳情 ==========
    async fetchThreadDetail(threadId) {
      try {
        const [detailRes, commentsRes] = await Promise.all([
          apiClient.get(`/ContentApi/${threadId}`),
          contentInteractionApi.getComments(threadId),
        ])

        // 處理主題詳情
        const detail = detailRes?.contentId ? detailRes : detailRes?.data
        if (detail && detail.contentId) {
          const { textContent, imageUrls } = parseBodyContent(detail.body)
          this.threadDetail = {
            id: detail.contentId,
            title: detail.title,
            author: detail.username,
            createdAt: detail.createdAt,
            content: textContent,
            imageUrls,
            categoryId: detail.categoryId,
            type: detail.type,
          }
        }

        // 處理留言
        const commentsData = commentsRes?.comments || []
        this.threadComments = commentsData.map(comment => {
          const { textContent, imageUrls } = parseBodyContent(comment.body)
          return {
            id: comment.contentId,
            author: comment.username,
            createdAt: comment.createdAt,
            content: textContent,
            imageUrls,
            type: CONTENT_TYPES.COMMENT,
            parentContentId: comment.parentContentId,
            replies: [],
          }
        })

        // 獲取回文
        const repliesRes = await apiClient.get('/ContentApi', {
          params: { parentContentId: threadId },
        })
        const replyItems = Array.isArray(repliesRes?.items) ? repliesRes.items : []

        this.threadPosts = replyItems
          .filter(i => i.type === CONTENT_TYPES.REPLY)
          .map(i => {
            const { textContent, imageUrls } = parseBodyContent(i.body)
            return {
              id: i.contentId,
              author: i.username,
              createdAt: i.createdAt,
              content: textContent,
              imageUrls,
              type: i.type,
              parentContentId: i.parentContentId,
              replies: [],
            }
          })
          .sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt))

        this.threadComments.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt))
        this.threadCurrentPage = 1
      } catch (error) {
        console.error('獲取帖文詳情失敗:', error)
      }
    },

    openThread(topicId) {
      this.selectedThread = topicId
      this.fetchThreadDetail(topicId)
    },

    closeThread() {
      this.selectedThread = null
      this.threadDetail = {
        id: null,
        title: '',
        author: '',
        createdAt: '',
        content: '',
        imageUrls: [],
        categoryId: null,
        type: null,
      }
      this.threadPosts = []
      this.threadComments = []
    },

    changeThreadPage(page) {
      if (page >= 1 && page <= this.threadTotalPages) {
        this.threadCurrentPage = page
      }
    },

    // ========== 新主題管理 ==========
    toggleNewTopicBox() {
      this.showNewTopicBox = !this.showNewTopicBox
    },

    async submitNewTopic() {
      if (!this.newTopic.title.trim()) {
        alert('請輸入標題')
        return
      }
      if (!this.newTopic.body.trim()) {
        alert('請輸入內容')
        return
      }

      // 若有 parentContentId，不需檢查 categoryId（會使用 parent 的 category）
      // 若無 parentContentId，需檢查 categoryId
      if (!this.newTopic.parentContentId && !this.newTopic.categoryId) {
        alert('請選擇分類')
        return
      }

      if (this.isSubmittingNewTopic) return
      this.isSubmittingNewTopic = true

      const dto = {
        userId: 1,
        categoryId: this.newTopic.categoryId,
        parentContentId: this.newTopic.parentContentId,
        title: this.newTopic.title.trim(),
        body: this.newTopic.body.trim(),
        type: this.newTopic.parentContentId ? CONTENT_TYPES.REPLY : CONTENT_TYPES.POST,
      }

      try {
        console.log('提交DTO:', dto)
        await apiClient.post('/ContentApi', dto)
        alert('發表成功！')
        this.newTopic.title = ''
        this.newTopic.body = ''
        this.showNewTopicBox = false
        this.pagination.currentPage = 1
        await this.fetchTopics()
      } catch (error) {
        console.error('發表失敗:', error)
        alert('發表失敗，請稍後再試')
      } finally {
        this.isSubmittingNewTopic = false
      }
    },

    cancelNewTopic() {
      this.newTopic.title = ''
      this.newTopic.body = ''
      this.showNewTopicBox = false
    },

    updateNewTopicCategory(categoryId) {
      this.newTopic.categoryId = categoryId
    },

    // ========== 舊有功能保留 ==========
    toggleNewTopicModal(value) {
      this.showNewTopicModal = value
    },

    triggerTopicRefresh() {
      this.lastRefreshTime = Date.now()
    },

    // ========== 搜尋功能 ==========
    setSearchKeyword(keyword) {
      this.searchKeyword = keyword
      this.pagination.currentPage = 1
      this.fetchTopics()
    },

    clearSearch() {
      this.searchKeyword = ''
      this.pagination.currentPage = 1
      this.fetchTopics()
    }
  },

  persist: true
})
