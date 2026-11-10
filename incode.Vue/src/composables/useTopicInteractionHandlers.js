import Swal from "sweetalert2";
import { useForumStore } from "@/stores/forumStore.js";
import { useForumNavigation } from "@/composables/useForumNavigation.js";

/** InteractionButtons 的事件處理（列表與詳情共用）：互動成功 / 失敗、編輯、刪除 */
export function useTopicInteractionHandlers() {
  const forumStore = useForumStore();
  const { goToList } = useForumNavigation();

  /**
   * 處理主題互動成功
   */
  const handleTopicInteractionSuccess = async (data) => {
    // 隱藏功能的特殊處理
    if (data.categoryId === 2 && data.categoryName === "hide") {
      // 立即從列表中移除隱藏的主題
      forumStore.topics = forumStore.topics.filter(
        (topic) => topic.id !== data.contentId,
      );

      // 隱藏或解除隱藏後，重新載入主題列表以反映後端狀態
      // fetchTopics 會更新分類統計
      await forumStore.fetchTopics();

      if (forumStore.selectedThread) {
        // 在詳情視圖中，隱藏後返回列表
        goToList();
      }
    }
  };

  /**
   * 處理主題互動錯誤
   */
  const handleTopicInteractionError = async (data) => {
    console.error("主題互動失敗:", data);
    await Swal.fire({
      icon: "error",
      title: "互動失敗",
      text: data.error || "操作失敗，請稍後再試",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
  };

  /**
   * 處理主題編輯操作
   */
  const handleTopicEditContent = (contentId) => {
    // 尋找對應的主題或回文進行編輯
    const targetContent =
      forumStore.threadDetail?.id === contentId
        ? forumStore.threadDetail
        : forumStore.topics.find((t) => t.id === contentId);

    if (targetContent) {
      forumStore.openEditMode(contentId, targetContent);
    }
  };

  /**
   * 處理主題刪除操作
   */
  const handleTopicDeleteContent = async (contentId) => {
    // 刪除成功後重新載入數據
    // fetchTopics 會更新分類統計
    await forumStore.fetchTopics();

    if (forumStore.threadDetail?.id === contentId) {
      // 如果刪除的是當前詳情中的主題，返回列表
      goToList();
    } else if (forumStore.threadDetail?.id) {
      // 否則重新載入詳情
      forumStore.fetchThreadDetail(forumStore.threadDetail.id);
    }
  };

  return {
    handleTopicInteractionSuccess,
    handleTopicInteractionError,
    handleTopicEditContent,
    handleTopicDeleteContent,
  };
}
