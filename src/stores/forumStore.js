import { defineStore } from 'pinia'
import apiClient from '@/api/request.js'
import contentInteractionApi from '@/api/contentInteractionApi.js'
import { calculateReplyStats } from '@/components/Contents/Forum/forumUtils'
import { normalizeRichContent } from '@/utils/sanitizeHtml.js'
import { fetchBodyHtml, toAbsoluteUrl } from '@/utils/contentBody.js'

// Content types according to database schema
export const CONTENT_TYPES = {
  POST: 1,           // 貼文(出現在貼文版面)
  REPLY: 2,          // 回文(出現在貼文版面)
  COMMENT: 3,        // 留言
  COMMENT_REPLY: 4   // 留言回覆
}

const normalizeCategory = (raw, fallbackName = '') => {
  if (!raw) return null
  const source = raw?.data ?? raw
  if (Array.isArray(source)) return null

  const id = source.id ?? source.content_category_id ?? source.categoryId ?? source.category_id
  const name = source.name ?? source.categoryName ?? source.category_name ?? fallbackName

  if (!id && !name) return null

  return {
    id,
    name,
    description: source.description ?? null,
    topicCount: source.topicCount ?? source.topic_count ?? 0,
    postCount: source.postCount ?? source.post_count ?? 0,
  }
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
    sortOption: 'createdAt_desc',

    // ========== 帖文詳情 ==========
    selectedThread: null,
    threadDetail: {
      id: null,
      title: '',
      author: '',
      createdAt: '',
      content: '',
      contentHtml: '',
      contentText: '',
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
      parentContentId: null,
      categorySearch: '',
      mode: 'post',
      contentType: CONTENT_TYPES.POST,
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

    selectCategory(categoryId, options = {}) {
      const { clearSearch = false } = options || {}
      this.selectedCategory = this.selectedCategory === categoryId ? null : categoryId
      // 同步更新新主題的分類
      this.newTopic.categoryId = this.selectedCategory
      this.pagination.currentPage = 1
      if (clearSearch) {
        this.searchKeyword = ''
      }
      this.fetchTopics()
    },

    async createCategory(categoryName, description = null, parentCategoryId = null) {
      const name = String(categoryName ?? '').trim()
      if (!name) {
        throw new Error('分類名稱不可為空')
      }

      try {
        const payload = {
          parentCategoryId,
          name,
          description: description ?? null,
        }
        const created = await apiClient.post('/CategoryApi', payload)
        const normalized = normalizeCategory(created, name)

        await this.fetchCategories()

        if (normalized?.id) return normalized
        return this.categories.find(c => c.name === name) || null
      } catch (error) {
        console.error('新增分類失敗:', error)
        throw error
      }
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

        const contentsWithHtml = await Promise.all(
          allContents.map(async (item) => {
            const bodyHtml = await fetchBodyHtml(item.body)
            const { textContent, imageUrls } = normalizeRichContent(bodyHtml, toAbsoluteUrl)
            return {
              ...item,
              bodyHtml,
              bodyText: textContent,
              bodyImages: imageUrls,
            }
          })
        )

        // 2. 過濾 type=1(貼文) 和 type=2(回文) - 只顯示主貼文和回文，留言應在貼文詳情頁面顯示
        let listContents = contentsWithHtml
          .filter(c => c.type === CONTENT_TYPES.POST || c.type === CONTENT_TYPES.REPLY)

        // 3. 搜尋過濾 (搜尋 title 和 body)
        if (this.searchKeyword.trim()) {
          const keyword = this.searchKeyword.trim().toLowerCase()
          listContents = listContents.filter(c => {
            const titleMatch = c.title?.toLowerCase().includes(keyword)
            const bodyMatch = c.bodyText?.toLowerCase().includes(keyword)
            return titleMatch || bodyMatch
          })
        }

        const contentsWithStats = listContents.map(item => {
          const replyStats = calculateReplyStats(item.contentId, contentsWithHtml)
          return {
            ...item,
            _replyStats: replyStats,
          }
        })

        const parseDateValue = (value) => {
          if (!value) return 0
          const time = new Date(value).getTime()
          return Number.isNaN(time) ? 0 : time
        }

        const sortOption = this.sortOption || 'createdAt_desc'

        contentsWithStats.sort((a, b) => {
          if (sortOption === 'lastReply_desc') {
            const aLast = parseDateValue(a._replyStats?.lastReplyTime || a.createdAt)
            const bLast = parseDateValue(b._replyStats?.lastReplyTime || b.createdAt)
            return bLast - aLast
          }

          if (sortOption === 'replyCount_desc') {
            const aCount = Number(a._replyStats?.replyCount || 0)
            const bCount = Number(b._replyStats?.replyCount || 0)
            if (bCount !== aCount) return bCount - aCount
            return parseDateValue(b.createdAt) - parseDateValue(a.createdAt)
          }

          return parseDateValue(b.createdAt) - parseDateValue(a.createdAt)
        })

        // 4. 前端分頁
        const pageSize = this.pagination.pageSize
        const totalCount = contentsWithStats.length
        const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
        const currentPage = Math.min(Math.max(1, targetPage), totalPages)
        const start = (currentPage - 1) * pageSize
        const end = start + pageSize
        const pageItems = contentsWithStats.slice(start, end)

        this.pagination = {
          currentPage,
          pageSize,
          totalCount,
          totalPages,
        }

        // 5. 組合顯示資料
        this.topics = pageItems.map(item => {
          const { textContent, imageUrls } = normalizeRichContent(item.bodyHtml, toAbsoluteUrl)
          const replyStats = item._replyStats || calculateReplyStats(item.contentId, contentsWithHtml)

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
        const [detailRes, commentsRes, allContentsRes] = await Promise.all([
          apiClient.get(`/ContentApi/${threadId}`),
          contentInteractionApi.getComments(threadId),
          apiClient.get('/ContentApi', { params: { pageSize: 9999 } })
        ])

        // 處理主題詳情
        const detail = detailRes?.contentId ? detailRes : detailRes?.data
        console.log('[fetchThreadDetail] Raw detail response:', detail);
        
        if (detail && detail.contentId) {
          const detailBodyHtml = await fetchBodyHtml(detail.body)
          const { sanitizedHtml, textContent, imageUrls } = normalizeRichContent(
            detailBodyHtml,
            toAbsoluteUrl
          )
          
          // 嘗試多個可能的 categoryId 字段名
          let categoryId = detail.categoryId ?? detail.category_id ?? detail.cateId ?? detail.cate_id ?? null;
          
          // 如果 detail 中沒有 categoryId，嘗試從主題列表中查找
          if (!categoryId && this.topics && this.topics.length > 0) {
            const matchedTopic = this.topics.find(t => String(t.id) === String(threadId));
            if (matchedTopic) {
              categoryId = matchedTopic.categoryId ?? matchedTopic.category_id ?? null;
              console.log('[fetchThreadDetail] Found categoryId from topics list:', categoryId);
            }
          }
          
          // 如果仍然沒有，嘗試從 allContentsRes 中查找
          if (!categoryId && allContentsRes?.items) {
            const allItems = Array.isArray(allContentsRes.items) ? allContentsRes.items : [];
            const matchedItem = allItems.find(i => String(i.contentId) === String(threadId));
            if (matchedItem) {
              categoryId = matchedItem.categoryId ?? matchedItem.category_id ?? null;
              console.log('[fetchThreadDetail] Found categoryId from allContentsRes:', categoryId);
            }
          }
          
          console.log('[fetchThreadDetail] Final categoryId:', categoryId, 'from detail:', {
            categoryId: detail.categoryId,
            category_id: detail.category_id,
            cateId: detail.cateId,
            cate_id: detail.cate_id
          });
          
          this.threadDetail = {
            id: detail.contentId,
            title: detail.title,
            author: detail.username,
            createdAt: detail.createdAt,
            content: textContent,
            contentHtml: sanitizedHtml,
            contentText: textContent,
            imageUrls,
            categoryId: categoryId,
            type: detail.type,
          }
        }

        // 處理留言 - 只保留 COMMENT 類型（不包含其他類型）
        const commentsData = commentsRes?.comments || []
        this.threadComments = await Promise.all(
          commentsData
            .filter(c => c.type === CONTENT_TYPES.COMMENT || !c.type) // 信任 API 返回的類型，或默認為 COMMENT
            .map(async (comment) => {
              const commentHtml = await fetchBodyHtml(comment.body)
              const { sanitizedHtml, textContent, imageUrls } = normalizeRichContent(
                commentHtml,
                toAbsoluteUrl
              )
              return {
                id: comment.contentId,
                author: comment.username,
                createdAt: comment.createdAt,
                content: textContent,
                contentHtml: sanitizedHtml,
                contentText: textContent,
                imageUrls,
                type: CONTENT_TYPES.COMMENT,
                parentContentId: comment.parentContentId,
                replies: [],
              }
            })
        )

        // 獲取回文 - 改為獲取所有內容然後過濾
        const normalizedThreadId = Number(threadId)

        const allItems = Array.isArray(allContentsRes?.items)
          ? allContentsRes.items
          : (Array.isArray(allContentsRes) ? allContentsRes : [])


        // 過濾出 parent_content_id 等於當前主題 ID 的內容
        const replyItems = allItems.filter(i =>
          i.parentContentId !== null &&
          Number(i.parentContentId) === normalizedThreadId
        )

        // 只過濾出回文類型，排除留言和留言回覆，並且排除已經在留言中出現過的ID
        const commentIds = new Set(this.threadComments.map(c => c.id))
        const replyList = replyItems.filter(i =>
          i.type === CONTENT_TYPES.REPLY &&
          !commentIds.has(i.contentId)
        )

        // 獲取所有回文的子留言
        this.threadPosts = await Promise.all(
          replyList.map(async (i) => {
            const replyHtml = await fetchBodyHtml(i.body)
            const { sanitizedHtml, textContent, imageUrls } = normalizeRichContent(
              replyHtml,
              toAbsoluteUrl
            )

            // 為每個回文獲取其子留言 (type=3 且 parent_content_id 為該回文 ID)
            const replyId = i.contentId
            const replyCommentList = allItems.filter(rc =>
              rc.type === CONTENT_TYPES.COMMENT &&
              Number(rc.parentContentId) === replyId
            )

            let replyComments = []
            try {
              replyComments = await Promise.all(
                replyCommentList.map(async (rc) => {
                  const rcHtml = await fetchBodyHtml(rc.body)
                  const { sanitizedHtml: rcSanitizedHtml, textContent: rcTextContent, imageUrls: rcImageUrls } = normalizeRichContent(
                    rcHtml,
                    toAbsoluteUrl
                  )
                  return {
                    id: rc.contentId,
                    author: rc.username,
                    createdAt: rc.createdAt,
                    content: rcTextContent,
                    contentHtml: rcSanitizedHtml,
                    contentText: rcTextContent,
                    imageUrls: rcImageUrls,
                    type: CONTENT_TYPES.COMMENT,
                    parentContentId: rc.parentContentId,
                  }
                })
              )
            } catch (error) {
              console.error(`獲取回文 ${i.contentId} 的子留言失敗:`, error)
            }

            return {
              id: i.contentId,
              author: i.username,
              createdAt: i.createdAt,
              content: textContent,
              contentHtml: sanitizedHtml,
              contentText: textContent,
              imageUrls,
              type: i.type,
              parentContentId: i.parentContentId,
              replies: replyComments.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt)),
            }
          })
        )

        this.threadPosts.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt))
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
        contentHtml: '',
        contentText: '',
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
      this.newTopic.mode = 'post'
      this.newTopic.contentType = CONTENT_TYPES.POST
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
    },

    setSortOption(option) {
      this.sortOption = option
      this.pagination.currentPage = 1
      this.fetchTopics()
    }
  },

  persist: true
})
