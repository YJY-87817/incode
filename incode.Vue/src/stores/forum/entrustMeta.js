import apiClient from "@/api/request.js";
import { entrustMissionByContentCache, entrustMissionByContentPending } from "@/stores/forum/caches.js";

export const normalizeEntrustMissionMeta = (detail = {}) => {
  const mission =
    detail?.entrustMission ??
    detail?.entrust_mission ??
    detail?.task ??
    detail?.mission ??
    null;

  const entrustMissionId =
    detail?.entrustMissionId ??
    detail?.entrust_mission_id ??
    detail?.missionId ??
    detail?.mission_id ??
    detail?.taskId ??
    detail?.task_id ??
    mission?.entrustMissionId ??
    mission?.entrust_mission_id ??
    mission?.missionId ??
    mission?.mission_id ??
    mission?.taskId ??
    mission?.task_id ??
    mission?.id ??
    null;

  const rewardPoints =
    detail?.rewardPoints ??
    detail?.reward_points ??
    mission?.rewardPoints ??
    mission?.reward_points ??
    0;

  const missionOwnerUserId =
    mission?.userId ??
    mission?.user_id ??
    mission?.ownerUserId ??
    mission?.owner_user_id ??
    mission?.publisherUserId ??
    mission?.publisher_user_id ??
    mission?.creatorUserId ??
    mission?.creator_user_id ??
    detail?.entrustMissionUserId ??
    detail?.entrust_mission_user_id ??
    detail?.ownerUserId ??
    detail?.owner_user_id ??
    detail?.publisherUserId ??
    detail?.publisher_user_id ??
    detail?.userId ??
    detail?.user_id ??
    null;

  const bestAnswerContentId =
    detail?.bestAnswerContentId ??
    detail?.best_answer_content_id ??
    mission?.bestAnswerContentId ??
    mission?.best_answer_content_id ??
    null;

  return {
    entrustMissionId: entrustMissionId ? Number(entrustMissionId) : null,
    rewardPoints: Number(rewardPoints || 0),
    entrustMissionUserId: missionOwnerUserId
      ? Number(missionOwnerUserId)
      : null,
    bestAnswerContentId: bestAnswerContentId
      ? Number(bestAnswerContentId)
      : null,
  };
};

export const normalizeEntrustMissionRecord = (item = {}) => ({
  entrustMissionId:
    item?.entrustMissionId ??
    item?.entrust_mission_id ??
    item?.missionId ??
    item?.mission_id ??
    item?.taskId ??
    item?.task_id ??
    item?.id ??
    null,
  entrustMissionUserId:
    item?.userId ??
    item?.user_id ??
    item?.ownerUserId ??
    item?.owner_user_id ??
    item?.publisherUserId ??
    item?.publisher_user_id ??
    item?.creatorUserId ??
    item?.creator_user_id ??
    null,
  rewardPoints: item?.rewardPoints ?? item?.reward_points ?? 0,
  bestAnswerContentId:
    item?.bestAnswerContentId ?? item?.best_answer_content_id ?? null,
});

/**
 * 【輔助函數】 依據內容 ID 獲取委託任務資訊
 *
 * 異步後調用 /EntrustMissionApi 對接 → 返回任務情報
 * 然後結合內容資訊（實際上某些特定 API 輸出結構可能包含）
 */
export const fetchEntrustMissionMetaByContentId = async (contentId) => {
  const targetId = Number(contentId);
  if (!Number.isFinite(targetId)) return null;

  if (entrustMissionByContentCache.has(targetId)) {
    return entrustMissionByContentCache.get(targetId);
  }

  if (entrustMissionByContentPending.has(targetId)) {
    return entrustMissionByContentPending.get(targetId);
  }

  const request = (async () => {
    try {
      const response = await apiClient.get("/EntrustMissionApi");
      const records = Array.isArray(response)
        ? response
        : Array.isArray(response?.data)
          ? response.data
          : [];

      records.forEach((record) => {
        const normalizedRecord = normalizeEntrustMissionRecord(record);
        const linkedIds = [
          record?.contentId,
          record?.content_id,
          record?.postContentId,
          record?.post_content_id,
          record?.parentContentId,
          record?.parent_content_id,
        ]
          .map((value) => Number(value))
          .filter((value) => Number.isFinite(value));

        linkedIds.forEach((linkedContentId) => {
          entrustMissionByContentCache.set(linkedContentId, normalizedRecord);
        });
      });

      return entrustMissionByContentCache.get(targetId) ?? null;
    } catch (error) {
      console.error("載入委託任務資訊失敗:", error);
      return null;
    } finally {
      entrustMissionByContentPending.delete(targetId);
    }
  })();

  entrustMissionByContentPending.set(targetId, request);
  return request;
};
