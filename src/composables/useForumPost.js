import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import contentApi from "@/api/contentApi.js";
import Swal from "sweetalert2";

/**
 * Forum 發文相關的共用邏輯
 * 用於 NewTopicModal 和 NewTopicSidebar 組件
 */
export function useForumPost() {
  const forumStore = useForumStore();

  /**
   * 獲取模式對應的標題
   * @param {string} mode - 模式 ('post', 'comment', 'reply', 'commentReply', 'edit')
   * @returns {string} 對應的標題文字
   */
  const getModeTitle = (mode) => {
    switch (mode) {
      case "edit":
        return "編輯內容";
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
   * @param {boolean} isEdit - 是否編輯模式
   * @returns {Promise<boolean>} 是否驗證通過
   */
  const validatePost = async (
    title,
    body,
    categoryId,
    parentContentId,
    isEdit = false,
  ) => {
    // 檢查標題和內容
    if (!title || !body) {
      // 临时下陋 modal 的 z-index
      const modalBackdrop = document.querySelector(".custom-modal-backdrop");
      if (modalBackdrop) modalBackdrop.style.zIndex = "500";

      await Swal.fire({
        icon: "warning",
        title: "提醒",
        text: "請輸入標題與內容",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });

      // 恢複 modal 的 z-index
      if (modalBackdrop) modalBackdrop.style.zIndex = "2000";
      return false;
    }

    // 編輯模式下不需要驗證分類，因為編輯時分類已經綁定
    if (isEdit) {
      return true;
    }

    // 如果不是回文，需要檢查分類
    if (!parentContentId && !categoryId) {
      // 临时下陋 modal 的 z-index
      const modalBackdrop = document.querySelector(".custom-modal-backdrop");
      if (modalBackdrop) modalBackdrop.style.zIndex = "500";

      await Swal.fire({
        icon: "warning",
        title: "提醒",
        text: "請選擇分類",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });

      // 恢複 modal 的 z-index
      if (modalBackdrop) modalBackdrop.style.zIndex = "2000";
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
   * @param {Object|null} params.entrustMission - 委託任務資料
   * @returns {Promise<{success: boolean, error?: any}>} 提交結果
   */
  const submitPost = async ({
    title,
    body,
    categoryId,
    parentContentId,
    contentType,
    entrustMission = null,
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
      if (entrustMission) {
        await contentApi.createContentWithEntrustMission({
          parentContentId,
          categoryId,
          title,
          body,
          type: resolvedType,
          entrustMission,
        });
      } else {
        await contentApi.createContent(dto);
      }
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
    // 清除緩存以確保獲取最新數據
    forumStore.clearContentCaches();

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
    // 临时下陋 modal 的 z-index
    const modalBackdrop = document.querySelector(".custom-modal-backdrop");
    if (modalBackdrop) modalBackdrop.style.zIndex = "500";

    await Swal.fire({
      icon: "success",
      title: "發佈成功",
      showConfirmButton: false,
      timer: 1500,
      position: "center",
      allowOutsideClick: true,
      allowEscapeKey: true,
    });

    // 恢複 modal 的 z-index
    if (modalBackdrop) modalBackdrop.style.zIndex = "2000";
  };

  /**
   * 顯示發佈失敗訊息
   */
  const showErrorMessage = async () => {
    // 临时下陋 modal 的 z-index
    const modalBackdrop = document.querySelector(".custom-modal-backdrop");
    if (modalBackdrop) modalBackdrop.style.zIndex = "500";

    await Swal.fire({
      icon: "error",
      title: "發佈失敗",
      text: "發佈失敗，請稍後再試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });

    // 恢複 modal 的 z-index
    if (modalBackdrop) modalBackdrop.style.zIndex = "2000";
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
   * @param {boolean} isEdit - 是否為編輯模式
   * @returns {Promise<boolean>} 是否發佈成功
   */
  const publishPost = async (params, isEdit = false) => {
    // 如果是編輯模式，調用編輯 API
    if (isEdit && params.editingContentId) {
      return await updatePost(params);
    }

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

  /**
   * 更新內容（編輯模式）
   * @param {Object} params - 編輯參數
   * @returns {Promise<boolean>} 是否更新成功
   */
  const updatePost = async (params) => {
    // 驗證 - 編輯模式下不驗證分類
    const isValid = await validatePost(
      params.title,
      params.body,
      params.categoryId,
      params.parentContentId,
      true, // isEdit = true
    );

    if (!isValid) {
      return false;
    }

    try {
      await contentApi.updateContent(params.editingContentId, {
        title: params.title,
        body: params.body,
      });

      forumStore.clearContentCaches();

      // 刷新資料
      await refreshAfterPost(params.parentContentId);

      const selectedThreadId = forumStore.selectedThread;

      // 如果當前在查看討論串詳情，無論編輯的是主文還是嵌套內容，都需要刷新
      if (selectedThreadId) {
        await forumStore.fetchThreadDetail(selectedThreadId);
      }

      // 临时下陋 modal 的 z-index
      const modalBackdrop = document.querySelector(".custom-modal-backdrop");
      if (modalBackdrop) modalBackdrop.style.zIndex = "500";

      // 顯示成功訊息
      await Swal.fire({
        icon: "success",
        title: "更新成功",
        showConfirmButton: false,
        timer: 1500,
        position: "center",
        allowOutsideClick: true,
        allowEscapeKey: true,
      });

      // 恢複 modal 的 z-index
      if (modalBackdrop) modalBackdrop.style.zIndex = "2000";

      // 清除編輯模式
      forumStore.clearEditMode();

      return true;
    } catch (error) {
      console.error("[useForumPost] 更新失敗:", error);
      // 临时下陋 modal 的 z-index
      const modalBackdrop = document.querySelector(".custom-modal-backdrop");
      if (modalBackdrop) modalBackdrop.style.zIndex = "500";

      await Swal.fire({
        icon: "error",
        title: "更新失敗",
        text: "更新失敗，請稍後再試",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });

      // 恢複 modal 的 z-index
      if (modalBackdrop) modalBackdrop.style.zIndex = "2000";
      return false;
    }
  };

  return {
    getModeTitle,
    resolveContentType,
    validatePost,
    submitPost,
    updatePost,
    refreshAfterPost,
    showSuccessMessage,
    showErrorMessage,
    hasDraftContent,
    publishPost,
  };
}
