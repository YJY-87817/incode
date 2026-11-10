import { useRouter } from "vue-router";
import { useForumStore } from "@/stores/forumStore.js";

/** 論壇頁面導航：列表 ⇄ 詳情、列表換頁 */
export function useForumNavigation() {
  const forumStore = useForumStore();
  const router = useRouter();

  /**
   * 【信息流跳轉點】從主題列表打開詳情視圖
   *
   * 流程（詳見 ForumView.vue L134）：
   * 1. 用戶點擊主題 → 此函數被調用→ router.push({ name: 'forum-thread', params: { id: threadId } })
   * 2. route.params.id 改變 → ForumView.vue 中的 watch 被觸發
   * 3. watch 調用 forumStore.openThread(threadId)
   * 4. openThread() 呼叫 fetchThreadDetail() → 向後端取得線程詳情及回覆列表
   * 5. threadDetail 及 threadPosts/threadComments 更新 → 此組件（TopicListView）偵測到更新 → 自動渲染詳情視圖
   *
   * 注意：此組件無 props，完全依賴 forumStore 進行通訊，所有狀態變化都在 store 中同步。
   */
  const goToThread = (threadId) => {
    if (!threadId) return;
    router.push({ name: "forum-thread", params: { id: threadId } });
  };

  const goToList = () => {
    router.push({ name: "forum" });
  };

  /**
   * 頁面導航
   */
  const goToPage = (page) => {
    forumStore.changePage(page);
  };

  return { goToThread, goToList, goToPage };
}
