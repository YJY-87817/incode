export const uiActions = {
  // ========== 快速回覆相關 ==========
  /**
   * 初始化快速回覆狀態
   * @param {number} contentId - 內容 ID
   */
  initQuickReply(contentId) {
    if (!this.quickReplies[contentId]) {
      this.quickReplies[contentId] = {
        content: "",
        isSubmitting: false,
        show: false,
      };
    }
  },

  /**
   * 設置快速回覆內容
   * @param {number} contentId - 內容 ID
   * @param {string} content - 回覆內容
   */
  setQuickReplyContent(contentId, content) {
    this.initQuickReply(contentId);
    this.quickReplies[contentId].content = content;
  },

  /**
   * 獲取快速回覆內容
   * @param {number} contentId - 內容 ID
   */
  getQuickReplyContent(contentId) {
    return this.quickReplies[contentId]?.content ?? "";
  },

  /**
   * 設置快速回覆提交狀態
   * @param {number} contentId - 內容 ID
   * @param {boolean} isSubmitting - 是否正在提交
   */
  setQuickReplySubmitting(contentId, isSubmitting) {
    this.initQuickReply(contentId);
    this.quickReplies[contentId].isSubmitting = isSubmitting;
  },

  /**
   * 獲取快速回覆提交狀態
   * @param {number} contentId - 內容 ID
   */
  getQuickReplySubmitting(contentId) {
    return this.quickReplies[contentId]?.isSubmitting ?? false;
  },

  /**
   * 顯示快速回覆框
   * @param {number} contentId - 內容 ID
   */
  showQuickReply(contentId) {
    this.initQuickReply(contentId);
    this.quickReplies[contentId].show = true;
  },

  /**
   * 隱藏快速回覆框
   * @param {number} contentId - 內容 ID
   */
  hideQuickReply(contentId) {
    if (this.quickReplies[contentId]) {
      this.quickReplies[contentId].show = false;
    }
  },

  /**
   * 取消快速回覆
   * @param {number} contentId - 內容 ID
   */
  cancelQuickReply(contentId) {
    if (this.quickReplies[contentId]) {
      this.quickReplies[contentId].content = "";
      this.quickReplies[contentId].show = false;
    }
  },

  /**
   * 清除快速回覆
   * @param {number} contentId - 內容 ID
   */
  clearQuickReply(contentId) {
    if (this.quickReplies[contentId]) {
      this.quickReplies[contentId].content = "";
    }
  },

  // ========== 更多菜單管理 ==========
  /**
   * 切換"更多"菜單
   * @param {number} contentId - 內容 ID
   */
  toggleMoreMenu(contentId) {
    if (this.openMoreMenuId === contentId) {
      // 如果當前菜單已打開，關閉它
      this.openMoreMenuId = null;
    } else {
      // 打開新菜單，自動關閉其他菜單
      this.openMoreMenuId = contentId;
    }
  },

  /**
   * 關閉"更多"菜單
   */
  closeMoreMenu() {
    this.openMoreMenuId = null;
  },

  /**
   * 檢查是否為指定內容的菜單打開
   * @param {number} contentId - 內容 ID
   */
  isMoreMenuOpen(contentId) {
    return this.openMoreMenuId === contentId;
  },
};
