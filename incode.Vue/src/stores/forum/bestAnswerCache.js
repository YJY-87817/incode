import { BEST_ANSWER_CACHE_KEY } from "@/stores/forum/caches.js";

export const readBestAnswerCache = () => {
  if (typeof window === "undefined") return {};
  try {
    const raw = window.localStorage.getItem(BEST_ANSWER_CACHE_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
};

export const writeBestAnswerCache = (payload) => {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(BEST_ANSWER_CACHE_KEY, JSON.stringify(payload));
  } catch {
    // ignore write failure
  }
};

export const toBestAnswerCacheKey = ({ entrustMissionId, threadId }) => {
  const missionNumericId = Number(entrustMissionId);
  if (Number.isFinite(missionNumericId) && missionNumericId > 0) {
    return `m:${missionNumericId}`;
  }

  const threadNumericId = Number(threadId);
  if (Number.isFinite(threadNumericId) && threadNumericId > 0) {
    return `t:${threadNumericId}`;
  }

  return null;
};

export const getCachedBestAnswerContentId = ({ entrustMissionId, threadId }) => {
  const key = toBestAnswerCacheKey({ entrustMissionId, threadId });
  if (!key) return null;
  const cache = readBestAnswerCache();
  const value = Number(cache[key]);
  return Number.isFinite(value) && value > 0 ? value : null;
};

export const setCachedBestAnswerContentId = ({
  entrustMissionId,
  threadId,
  bestAnswerContentId,
}) => {
  const key = toBestAnswerCacheKey({ entrustMissionId, threadId });
  const numericContentId = Number(bestAnswerContentId);
  if (!key || !Number.isFinite(numericContentId) || numericContentId <= 0) {
    return;
  }
  const cache = readBestAnswerCache();
  cache[key] = numericContentId;
  writeBestAnswerCache(cache);
};

export const clearCachedBestAnswerContentId = ({ entrustMissionId, threadId }) => {
  const key = toBestAnswerCacheKey({ entrustMissionId, threadId });
  if (!key) return;
  const cache = readBestAnswerCache();
  if (!(key in cache)) return;
  delete cache[key];
  writeBestAnswerCache(cache);
};
