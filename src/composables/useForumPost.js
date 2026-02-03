import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import Swal from "sweetalert2";

/**
 * Forum 發文相關的共用邏輯
 * 用於 NewTopicModal 和 NewTopicSidebar 組件
 */
export function useForumPost() {
  const forumStore = useForumStore();

  /**
   * 獲取模式對應的標題
   * @param {string} mode - 模式 ('post', 'comment', 'reply', 'commentReply')
   * @returns {string} 對應的標題文字
   */
  const getModeTitle = (mode) => {
    switch (mode) {
      case "comment":
        return "詳細留言";
      case "reply":
        return "詳細回文";
      case "commentReply":
        return "詳細回覆";
      default:
        return "發佈新主題";
    }
  };

  /**
   * 解析內容類型
   * @param {number|null} parentContentId - 父內容 ID
   * @param {number|null} contentType - 指定的內容類型
   * @returns {number} 解析後的內容類型
   */
  const resolveContentType = (parentContentId, contentType = null) => {
    return (
      contentType ??
      (parentContentId ? CONTENT_TYPES.REPLY : CONTENT_TYPES.POST)
    );
  };

  /**
   * 驗證發文內容
   * @param {string} title - 標題
   * @param {string} body - 內容
   * @param {number|null} categoryId - 分類 ID
   * @param {number|null} parentContentId - 父內容 ID
   * @returns {Promise<boolean>} 是否驗證通過
   */
  const validatePost = async (title, body, categoryId, parentContentId) => {
    // 檢查標題和內容
    if (!title || !body) {
      await Swal.fire({
        icon: "warning",
        title: "提醒",
        text: "請輸入標題與內容",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });
      return false;
    }

    // 如果不是回文，需要檢查分類
    if (!parentContentId && !categoryId) {
      await Swal.fire({
        icon: "warning",
        title: "提醒",
        text: "請選擇分類",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });
      return false;
    }

    return true;
  };

  /**
   * 提交發文
   * @param {Object} params - 發文參數
   * @param {string} params.title - 標題
   * @param {string} params.body - 內容
   * @param {number|null} params.categoryId - 分類 ID
   * @param {number|null} params.parentContentId - 父內容 ID
   * @param {number|null} params.contentType - 內容類型
   * @returns {Promise<{success: boolean, error?: any}>} 提交結果
   */
  const submitPost = async ({
    title,
    body,
    categoryId,
    parentContentId,
    contentType,
  }) => {
    const resolvedType = resolveContentType(parentContentId, contentType);

    const dto = {
      userId: 1, // TODO: 改成實際使用者 ID
      categoryId,
      parentContentId,
      title,
      body,
      type: resolvedType,
    };

    try {
      await apiClient.post("/ContentApi", dto);
      return { success: true };
    } catch (error) {
      console.error("[useForumPost] 發佈失敗:", error);
      return { success: false, error };
    }
  };

  /**
   * 發文成功後刷新資料
   * @param {number|null} parentContentId - 父內容 ID
   */
  const refreshAfterPost = async (parentContentId) => {
    // 重置分頁並重新載入主題列表
    forumStore.pagination.currentPage = 1;
    await forumStore.fetchTopics();

    // 如果是回文，刷新當前討論串
    if (parentContentId && forumStore.selectedThread) {
      await forumStore.fetchThreadDetail(forumStore.selectedThread);
    }
  };

  /**
   * 顯示發佈成功訊息
   */
  const showSuccessMessage = async () => {
    await Swal.fire({
      icon: "success",
      title: "發佈成功",
      showConfirmButton: false,
      timer: 1500,
      position: "center",
      allowOutsideClick: true,
      allowEscapeKey: true,
    });
  };

  /**
   * 顯示發佈失敗訊息
   */
  const showErrorMessage = async () => {
    await Swal.fire({
      icon: "error",
      title: "發佈失敗",
      text: "發佈失敗，請稍後再試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
  };

  /**
   * 檢查是否有草稿內容
   * @param {string} title - 標題
   * @param {string} body - 內容
   * @returns {boolean} 是否有內容
   */
  const hasDraftContent = (title, body) => {
    const hasTitle = title && title.trim().length > 0;

    if (!body || !body.trim()) {
      return hasTitle;
    }

    const raw = body.trim();
    const looksLikeHtml = /<[^>]+>/.test(raw);

    if (!looksLikeHtml) {
      const text = raw.replace(/\u00a0/g, " ").trim();
      return hasTitle || text.length > 0;
    }

    const parser = new DOMParser();
    const doc = parser.parseFromString(raw, "text/html");
    const text = (doc.body.textContent || "").replace(/\u00a0/g, " ").trim();
    const hasMedia =
      doc.body.querySelector("img, video, audio, iframe") !== null;

    return hasTitle || text.length > 0 || hasMedia;
  };

  /**
   * 完整的發文流程（含驗證、提交、刷新、提示）
   * @param {Object} params - 發文參數
   * @returns {Promise<boolean>} 是否發佈成功
   */
  const publishPost = async (params) => {
    // 驗證
    const isValid = await validatePost(
      params.title,
      params.body,
      params.categoryId,
      params.parentContentId,
    );

    if (!isValid) {
      return false;
    }

    // 提交
    const result = await submitPost(params);

    if (!result.success) {
      await showErrorMessage();
      return false;
    }

    // 刷新資料
    await refreshAfterPost(params.parentContentId);

    // 顯示成功訊息
    await showSuccessMessage();

    return true;
  };

  return {
    getModeTitle,
    resolveContentType,
    validatePost,
    submitPost,
    refreshAfterPost,
    showSuccessMessage,
    showErrorMessage,
    hasDraftContent,
    publishPost,
  };
}
