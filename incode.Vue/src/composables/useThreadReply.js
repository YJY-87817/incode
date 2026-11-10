import { computed, ref } from "vue";
import Swal from "sweetalert2";
import apiClient from "@/api/request.js";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import { useLoginGuardAlert } from "@/composables/useLoginGuardAlert";

/** 詳情頁的留言 / 回文：簡易留言框、詳細留言 / 回文草稿、「正在回覆此內容」高亮 */
export function useThreadReply() {
  const forumStore = useForumStore();
  const { ensureLoggedIn, getCurrentUserId } = useLoginGuardAlert();

  // ========== 留言/回文輸入狀態 ==========
  const showQuickReply = ref(false);

  const replyTab = ref(null);

  /**
   * 留言/回文輸入框相關
   */
  const selectQuickReply = async () => {
    const canContinue = await ensureLoggedIn("請先登入後操作");
    if (!canContinue) return;

    // Toggle功能：如果已經是簡易留言模式，則切換展開/縮合狀態
    if (replyTab.value === "quick" && showQuickReply.value) {
      showQuickReply.value = false;
      replyTab.value = null;
    } else {
      replyTab.value = "quick";
      showQuickReply.value = true;
    }
  };

  const selectDetailedComment = async () => {
    const canContinue = await ensureLoggedIn("請先登入後操作");
    if (!canContinue) return;

    replyTab.value = "detailed";
    if (showQuickReply.value && forumStore.threadDetail?.id) {
      forumStore.clearQuickReply(forumStore.threadDetail.id);
    }
    showQuickReply.value = false;
    openDetailedComment();
  };

  const closeQuickReply = () => {
    showQuickReply.value = false;
    replyTab.value = null;
    if (forumStore.threadDetail?.id) {
      forumStore.cancelQuickReply(forumStore.threadDetail.id);
    }
  };

  const submitQuickReply = async () => {
    if (!forumStore.threadDetail?.id) {
      await Swal.fire({
        icon: "warning",
        title: "提醒",
        text: "未找到對應的主題，請重新嘗試",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });
      return;
    }

    const quickReplyContent = forumStore.getQuickReplyContent(
      forumStore.threadDetail.id,
    );

    if (!quickReplyContent.trim()) {
      await Swal.fire({
        icon: "warning",
        title: "提醒",
        text: "請輸入內容",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });
      return;
    }

    const isSubmitting = forumStore.getQuickReplySubmitting(
      forumStore.threadDetail.id,
    );
    if (isSubmitting) return;

    const canContinue = await ensureLoggedIn("請先登入後操作");
    if (!canContinue) return;
    const currentUserId = getCurrentUserId();
    if (!currentUserId) return;

    forumStore.setQuickReplySubmitting(forumStore.threadDetail.id, true);

    const dto = {
      userId: currentUserId,
      categoryId: forumStore.threadDetail.categoryId,
      parentContentId: forumStore.threadDetail.id,
      title: buildReplyTitle(forumStore.threadDetail.title),
      body: quickReplyContent.trim(),
      type: CONTENT_TYPES.COMMENT,
    };

    try {
      await apiClient.post("/ContentApi", dto);
      await Swal.fire({
        icon: "success",
        title: "已送出！",
        text: "簡易留言已成功送出",
        timer: 1500,
        showConfirmButton: false,
      });

      // 直接调用 store 重新获取详情
      if (forumStore.selectedThread) {
        await forumStore.fetchThreadDetail(forumStore.selectedThread);
      }

      forumStore.clearQuickReply(forumStore.threadDetail.id);
      showQuickReply.value = false;
      replyTab.value = null;
    } catch (error) {
      console.error("送出留言/回文失敗:", error);
      await Swal.fire({
        icon: "error",
        title: "送出失敗",
        text: "送出失敗，請稍後再試",
        confirmButtonText: "確定",
        confirmButtonColor: "#145369",
      });
    } finally {
      forumStore.setQuickReplySubmitting(forumStore.threadDetail.id, false);
    }
  };

  function autoResizeTextarea(event) {
    const textarea = event?.target;
    if (!textarea) return;
    textarea.style.height = "auto";
    textarea.style.height = `${textarea.scrollHeight}px`;
  }

  const getCategoryIdValue = (cat) =>
    cat?.id ??
    cat?.content_category_id ??
    cat?.categoryId ??
    cat?.category_id ??
    null;

  const getParentCategoryIdValue = (cat) =>
    cat?.parentCategoryId ??
    cat?.parent_category_id ??
    cat?.parentCategoryID ??
    cat?.parent_id ??
    null;

  const getCategoryName = (cat) =>
    cat?.name ?? cat?.categoryName ?? cat?.category_name ?? "";

  const buildReplyTitle = (parentTitle) => {
    const base = String(parentTitle ?? "").trim();
    return base ? `Re: ${base}` : "Re:";
  };

  const openDetailedComment = () => {
    if (!forumStore.threadDetail?.id) {
      console.error("[openDetailedComment] No threadDetail.id found");
      return;
    }

    // 檢查是否已經有針對這個主文的詳細留言草稿
    const existingDraft = forumStore.drafts.find(
      (draft) =>
        draft.replyingToContentId === forumStore.threadDetail.id &&
        draft.mode === "comment",
    );

    if (existingDraft) {
      // 如果已存在草稿，先最小化其他所有的draft
      forumStore.drafts.forEach((draft) => {
        if (draft.id !== existingDraft.id && !draft.isMinimized) {
          forumStore.toggleDraftMinimize(draft.id, true);
        }
      });
      // 再展開這個草稿
      forumStore.toggleDraftMinimize(existingDraft.id, false);
    } else {
      // 如果不存在草稿，先最小化所有已展開的draft
      forumStore.drafts.forEach((draft) => {
        if (!draft.isMinimized) {
          forumStore.toggleDraftMinimize(draft.id, true);
        }
      });

      // 如果不存在草稿，創建新的草稿
      const threadCategoryId = forumStore.threadDetail.categoryId ?? null;

      const categories = forumStore.categories || [];

      const matchedCategory = categories.find(
        (cat) => String(getCategoryIdValue(cat)) === String(threadCategoryId),
      );

      const parentCategoryId = matchedCategory
        ? getParentCategoryIdValue(matchedCategory)
        : null;

      const parentCategory = parentCategoryId
        ? categories.find(
            (cat) => String(getCategoryIdValue(cat)) === String(parentCategoryId),
          )
        : null;

      const categorySearchName = parentCategory
        ? getCategoryName(parentCategory)
        : matchedCategory
          ? getCategoryName(matchedCategory)
          : "";

      forumStore.selectedCategory = threadCategoryId;

      // 創建新的草稿
      forumStore.createDraft({
        title: buildReplyTitle(forumStore.threadDetail.title),
        body: "",
        categoryId: threadCategoryId,
        parentContentId: forumStore.threadDetail.id,
        categorySearch: categorySearchName,
        mode: "comment",
        contentType: CONTENT_TYPES.COMMENT,
        replyingToContentId: forumStore.threadDetail.id,
        replyingToContentData: {
          id: forumStore.threadDetail.id,
          title: forumStore.threadDetail.title,
          author: forumStore.threadDetail.author,
          createdAt: forumStore.threadDetail.createdAt,
          contentText: forumStore.threadDetail.contentText,
        },
        replyingToThreadId: forumStore.threadDetail.id,
      });
    }
  };

  const openDetailedReply = () => {
    if (!forumStore.threadDetail?.id) {
      console.error("[openDetailedReply] No threadDetail.id found");
      return;
    }

    // 檢查是否已經有針對這個主文的詳細回文草稿
    const existingDraft = forumStore.drafts.find(
      (draft) =>
        draft.replyingToContentId === forumStore.threadDetail.id &&
        draft.mode === "reply",
    );

    if (existingDraft) {
      // 如果已存在草稿，先最小化其他所有的draft
      forumStore.drafts.forEach((draft) => {
        if (draft.id !== existingDraft.id && !draft.isMinimized) {
          forumStore.toggleDraftMinimize(draft.id, true);
        }
      });
      // 再展開這個草稿
      forumStore.toggleDraftMinimize(existingDraft.id, false);
    } else {
      // 如果不存在草稿，先最小化所有已展開的draft
      forumStore.drafts.forEach((draft) => {
        if (!draft.isMinimized) {
          forumStore.toggleDraftMinimize(draft.id, true);
        }
      });

      // 如果不存在草稿，創建新的草稿
      const threadCategoryId = forumStore.threadDetail.categoryId ?? null;
      const categories = forumStore.categories || [];

      const matchedCategory = categories.find(
        (cat) => String(getCategoryIdValue(cat)) === String(threadCategoryId),
      );

      const parentCategoryId = matchedCategory
        ? getParentCategoryIdValue(matchedCategory)
        : null;

      const parentCategory = parentCategoryId
        ? categories.find(
            (cat) => String(getCategoryIdValue(cat)) === String(parentCategoryId),
          )
        : null;

      const categorySearchName = parentCategory
        ? getCategoryName(parentCategory)
        : matchedCategory
          ? getCategoryName(matchedCategory)
          : "";

      forumStore.selectedCategory = threadCategoryId;

      // 創建新的草稿
      forumStore.createDraft({
        title: "",
        body: "",
        categoryId: threadCategoryId,
        parentContentId: forumStore.threadDetail.id,
        categorySearch: categorySearchName,
        mode: "reply",
        contentType: CONTENT_TYPES.REPLY,
        replyingToContentId: forumStore.threadDetail.id,
        replyingToContentData: {
          id: forumStore.threadDetail.id,
          title: forumStore.threadDetail.title,
          author: forumStore.threadDetail.author,
          createdAt: forumStore.threadDetail.createdAt,
          contentText: forumStore.threadDetail.contentText,
        },
        replyingToThreadId: forumStore.threadDetail.id,
      });
    }
  };

  // 主文是否為正在回覆的目標（檢查所有草稿）
  const isThreadReplyingTarget = computed(() => {
    const targetId = forumStore.threadDetail?.id;
    if (!targetId) return false;
    return forumStore.drafts.some(
      (draft) => draft.replyingToContentId === targetId,
    );
  });

  /**
   * 過濾掉已隱藏的主題
   */
  // 點擊高亮的"正在回覆此內容"切換對應草稿的展開/最小化狀態
  function handleThreadReplyingTargetClick() {
    if (!isThreadReplyingTarget.value) return;

    const targetId = forumStore.threadDetail?.id;
    if (!targetId) return;

    const targetDraft = forumStore.drafts.find(
      (draft) => draft.replyingToContentId === targetId,
    );

    if (targetDraft) {
      forumStore.toggleDraftMinimize(targetDraft.id, !targetDraft.isMinimized);
    }
  }

  return {
    showQuickReply,
    replyTab,
    selectQuickReply,
    selectDetailedComment,
    closeQuickReply,
    submitQuickReply,
    autoResizeTextarea,
    openDetailedReply,
    isThreadReplyingTarget,
    handleThreadReplyingTargetClick,
  };
}
