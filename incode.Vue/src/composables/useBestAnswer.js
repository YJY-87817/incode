import { computed, ref, watch } from "vue";
import Swal from "sweetalert2";
import { useForumStore } from "@/stores/forumStore.js";

/** 委託任務的最佳解答狀態：目前主題的委託資訊、選定後的樂觀更新 */
export function useBestAnswer() {
  const forumStore = useForumStore();

  const optimisticBestAnswerContentId = ref(null);

  const activeEntrustMissionId = computed(() => {
    return forumStore.threadDetail?.entrustMissionId ?? null;
  });

  const activeMissionOwnerUserId = computed(() => {
    return (
      forumStore.threadDetail?.entrustMissionUserId ??
      forumStore.threadDetail?.userId ??
      null
    );
  });

  const activeRewardPoints = computed(() => {
    return Number(forumStore.threadDetail?.rewardPoints || 0);
  });

  const activeBestAnswerContentId = computed(() => {
    if (optimisticBestAnswerContentId.value) {
      return optimisticBestAnswerContentId.value;
    }
    return forumStore.threadDetail?.bestAnswerContentId ?? null;
  });

  const canEnableBestAnswer = computed(() => {
    return Boolean(activeEntrustMissionId.value);
  });

  const handleBestAnswerSelected = async (payload) => {
    const selectedContentId =
      payload?.bestAnswerContentId ?? payload?.contentId ?? null;
    if (!selectedContentId) return;

    optimisticBestAnswerContentId.value = Number(selectedContentId);

    forumStore.threadDetail = {
      ...forumStore.threadDetail,
      bestAnswerContentId: Number(selectedContentId),
      bestAnswerSelected: true,
    };

    forumStore.cacheBestAnswerSelection({
      entrustMissionId:
        payload?.entrustMissionId ?? forumStore.threadDetail?.entrustMissionId,
      threadId: forumStore.threadDetail?.id,
      bestAnswerContentId: Number(selectedContentId),
    });

    await Swal.fire({
      icon: "success",
      title: "最佳解答已選定",
      text: `已發放 ${Number(payload?.rewardPoints || activeRewardPoints.value || 0)} 點給回覆作者`,
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
  };

  watch(
    () => forumStore.threadDetail?.id,
    () => {
      optimisticBestAnswerContentId.value = null;
    },
    { immediate: true },
  );

  return {
    activeEntrustMissionId,
    activeMissionOwnerUserId,
    activeRewardPoints,
    activeBestAnswerContentId,
    canEnableBestAnswer,
    handleBestAnswerSelected,
  };
}
