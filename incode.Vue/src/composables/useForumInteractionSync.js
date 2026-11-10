import { watch } from "vue";
import { useForumStore } from "@/stores/forumStore.js";
import { useLoginGuardAlert } from "@/composables/useLoginGuardAlert";
import { useThreadPagination } from "@/composables/useThreadPagination.js";

/** 列表 / 詳情畫面上出現的內容 ID 一有變化（或登入狀態改變），就向後端同步目前會員的互動狀態（按讚、收藏…） */
export function useForumInteractionSync() {
  const forumStore = useForumStore();
  const { isLoggedIn, getCurrentUserId } = useLoginGuardAlert();
  const { paginatedThreadPosts, paginatedThreadComments } =
    useThreadPagination();

  watch(
    () => forumStore.topics.map((topic) => Number(topic?.id)).join(","),
    async () => {
      const topicContentIds = forumStore.topics
        .map((topic) => Number(topic?.id))
        .filter((id) => Number.isFinite(id) && id > 0);

      await forumStore.initializeUserInteractionStates(topicContentIds);
    },
    { immediate: true },
  );

  const collectThreadInteractionContentIds = () => {
    const ids = [
      Number(forumStore.threadDetail?.id),
      ...paginatedThreadPosts.value.map((item) => Number(item?.id)),
      ...paginatedThreadComments.value.map((item) => Number(item?.id)),
    ].filter((id) => Number.isFinite(id) && id > 0);

    // 收集所有巢狀回覆（留言和回覆）的 ID
    const nestedRepliesIds = [];
    Object.values(forumStore.childRepliesByParentId || {}).forEach((payload) => {
      const items = payload?.items || [];
      items.forEach((item) => {
        const itemId = Number(item?.id);
        if (Number.isFinite(itemId) && itemId > 0) {
          nestedRepliesIds.push(itemId);
        }
      });
    });

    return Array.from(new Set([...ids, ...nestedRepliesIds]));
  };

  watch(
    () => [
      forumStore.selectedThread,
      Number(forumStore.threadDetail?.id || 0),
      paginatedThreadPosts.value.map((item) => Number(item?.id || 0)).join(","),
      paginatedThreadComments.value
        .map((item) => Number(item?.id || 0))
        .join(","),
      // 監聽巢狀回覆的變化
      JSON.stringify(
        Object.entries(forumStore.childRepliesByParentId || {}).map(
          ([parentId, payload]) => [
            parentId,
            (payload?.items || []).map((item) => Number(item?.id || 0)),
          ],
        ),
      ),
    ],
    async ([selectedThread]) => {
      if (!selectedThread) return;
      const threadContentIds = collectThreadInteractionContentIds();
      await forumStore.initializeUserInteractionStates(threadContentIds);
    },
    { immediate: true },
  );

  watch(
    () => [isLoggedIn(), getCurrentUserId()],
    async () => {
      const topicContentIds = forumStore.topics
        .map((topic) => Number(topic?.id))
        .filter((id) => Number.isFinite(id) && id > 0);

      const threadContentIds = collectThreadInteractionContentIds();

      await Promise.all([
        forumStore.initializeUserInteractionStates(topicContentIds, {
          force: true,
        }),
        forumStore.initializeUserInteractionStates(threadContentIds, {
          force: true,
        }),
      ]);
    },
  );

}
