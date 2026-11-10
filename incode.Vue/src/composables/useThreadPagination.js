import { computed } from "vue";
import { useForumStore } from "@/stores/forumStore.js";
import { buildDisplayPages } from "@/utils/pagination.js";

/** 詳情頁的回文 / 留言分頁 */
export function useThreadPagination() {
  const forumStore = useForumStore();

  /**
   * 執行線程分頁
   */
  const threadTotalPages = computed(() => forumStore.threadTotalPages);

  const paginatedThreadPosts = computed(() => forumStore.paginatedThreadPosts);

  const threadCommentsTotalPages = computed(
    () => forumStore.threadCommentsTotalPages,
  );

  const paginatedThreadComments = computed(
    () => forumStore.paginatedThreadComments,
  );

  const threadDisplayPages = computed(() =>
    buildDisplayPages(forumStore.threadCurrentPage, threadTotalPages.value),
  );

  const threadCommentsDisplayPages = computed(() =>
    buildDisplayPages(
      forumStore.threadCommentsCurrentPage,
      threadCommentsTotalPages.value,
    ),
  );

  const goToThreadPage = (page) => {
    forumStore.changeThreadPage(page);
  };

  const goToCommentPage = (page) => {
    forumStore.changeThreadCommentsPage(page);
  };

  return {
    threadTotalPages,
    paginatedThreadPosts,
    threadCommentsTotalPages,
    paginatedThreadComments,
    threadDisplayPages,
    threadCommentsDisplayPages,
    goToThreadPage,
    goToCommentPage,
  };
}
