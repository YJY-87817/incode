import apiClient from "@/api/request";

const BEST_ANSWER_NAME_REGEX = /(best[-_ ]?answer|最佳解答|最佳解)/i;

/**
 * 【字段映射幫手】將任何值轉換為數字或 null
 * 此幫手用於處理 API 回應中可能是字符串或數字的字段
 */
const toNumberOrNull = (value) => {
  const numericValue = Number(value);
  return Number.isFinite(numericValue) ? numericValue : null;
};

/**
 * 【字段映射幫手】確保有效的陣列結構
 *
 * 不同的 API 端點可能將陣列包裹在不同的字段中：
 * - data, interactions, Interactions, items, list, result
 * - data.interactions, data.Interactions
 *
 * 此函數統一提取真正的陣列，以便後續處理
 */
const ensureArray = (payload) => {
  if (Array.isArray(payload)) return payload;
  if (Array.isArray(payload?.data)) return payload.data;
  if (Array.isArray(payload?.interactions)) return payload.interactions;
  if (Array.isArray(payload?.Interactions)) return payload.Interactions;
  if (Array.isArray(payload?.items)) return payload.items;
  if (Array.isArray(payload?.list)) return payload.list;
  if (payload && Array.isArray(payload?.result)) return payload.result;
  if (Array.isArray(payload?.data?.interactions))
    return payload.data.interactions;
  if (Array.isArray(payload?.data?.Interactions))
    return payload.data.Interactions;
  return [];
};

const extractMissionIdFromValue = (value) => {
  const parsed = toNumberOrNull(value);
  if (parsed) return parsed;

  const text = String(value || "");
  const matched = text.match(
    /(?:entrustMissionId|entrust_mission_id|missionId|mission_id)\D*(\d+)/i,
  );
  if (!matched?.[1]) return null;
  return toNumberOrNull(matched[1]);
};

/**
 * 【委託任務ID萃取】從記錄中找出委託任務 ID
 *
 * 委託任務 ID 可能來自多個位置：
 * 1. 直接字段：entrustMissionId, entrust_mission_id, missionId, mission_id 等
 * 2. 注解/備註欄：note, description（如 \"missionId: 123\"）
 *
 * 此函數依序檢查所有可能的位置
 */
const extractMissionIdFromRecord = (record = {}) => {
  const directMissionId =
    record?.entrustMissionId ??
    record?.entrust_mission_id ??
    record?.missionId ??
    record?.mission_id ??
    record?.taskId ??
    record?.task_id ??
    null;

  const parsedDirectMissionId = extractMissionIdFromValue(directMissionId);
  if (parsedDirectMissionId) return parsedDirectMissionId;

  return extractMissionIdFromValue(record?.note ?? record?.description ?? "");
};

const extractContentIdFromRecord = (record = {}) => {
  return toNumberOrNull(
    record?.bestAnswerContentId ??
      record?.best_answer_content_id ??
      record?.contentId ??
      record?.content_id ??
      null,
  );
};

const isBestAnswerRecord = (record = {}, bestAnswerCategoryId = null) => {
  const categoryId = toNumberOrNull(
    record?.interactionCategoryId ??
      record?.interaction_category_id ??
      record?.categoryId ??
      record?.category_id ??
      null,
  );

  if (bestAnswerCategoryId && categoryId === bestAnswerCategoryId) {
    return true;
  }

  const categoryName = String(
    record?.interactionCategoryName ??
      record?.interaction_category_name ??
      record?.categoryName ??
      record?.category_name ??
      record?.name ??
      record?.type ??
      "",
  ).trim();

  if (BEST_ANSWER_NAME_REGEX.test(categoryName)) {
    return true;
  }

  const hintText = String(record?.note ?? record?.description ?? "");
  return BEST_ANSWER_NAME_REGEX.test(hintText);
};

const extractBestAnswerContentId = (payload) => {
  const directId =
    payload?.bestAnswerContentId ??
    payload?.best_answer_content_id ??
    payload?.contentId ??
    payload?.content_id ??
    null;

  const parsedDirectId = toNumberOrNull(directId);
  if (parsedDirectId) return parsedDirectId;

  const collections = [
    payload?.data,
    payload?.result,
    payload?.item,
    payload?.record,
  ];

  for (const entry of collections) {
    const parsedEntryId = toNumberOrNull(
      entry?.bestAnswerContentId ??
        entry?.best_answer_content_id ??
        entry?.contentId ??
        entry?.content_id ??
        null,
    );
    if (parsedEntryId) return parsedEntryId;
  }

  return null;
};

const hasExplicitBestAnswerField = (payload) => {
  if (!payload || typeof payload !== "object") return false;

  if (
    Object.prototype.hasOwnProperty.call(payload, "bestAnswerContentId") ||
    Object.prototype.hasOwnProperty.call(payload, "best_answer_content_id")
  ) {
    return true;
  }

  const nestedCandidates = [
    payload?.data,
    payload?.result,
    payload?.item,
    payload?.record,
  ];

  return nestedCandidates.some(
    (entry) =>
      entry &&
      typeof entry === "object" &&
      (Object.prototype.hasOwnProperty.call(entry, "bestAnswerContentId") ||
        Object.prototype.hasOwnProperty.call(entry, "best_answer_content_id")),
  );
};

/**
 * Content Interaction API module
 *
 * Provides functions to interact with content (posts) such as like, dislike,
 * bookmark, block, or hide. The backend schema defines two tables:
 *
 *   - interaction_category (id, name, description, note)
 *   - content_interaction (id, category_id, content_id, user_id, created_at, note)
 *
 * The API follows a simple REST convention.
 */
const contentInteractionApi = {
  /**
   * Fetch all interaction categories.
   *
   * GET /ContentInteractionApi/category
   *
   * @returns {Promise<Array>} Array of categories, each containing:
   *   {
   *     interaction_category_id: number,
   *     name: string,
   *     description: string,
   *     note: string
   *   }
   */
  getCategories() {
    return apiClient.get("/ContentInteractionApi/category");
  },

  /**
   * Submit a user interaction for a specific content item.
   *
   * POST /ContentInteractionApi
   *
   * Request body (JSON):
   * {
   *   "interactionCategoryId": number,   // ID from interaction_category
   *   "contentId": number,    // ID of the content (post)
   *   // user_id is inferred from the auth token on the server side
   * }
   *
   * @param {Object} payload
   * @param {number} payload.interactionCategoryId
   * @param {number} payload.contentId
   * @returns {Promise<Object>} The created content_interaction record or null if removed
   */
  postInteraction({ interactionCategoryId, contentId }) {
    return apiClient.post("/ContentInteractionApi", {
      interactionCategoryId,
      contentId,
    });
  },

  /**
   * Convenience helpers for common actions.
   * These simply call postInteraction with the appropriate category ID.
   * Category IDs: 1=Like, 2=Hide, 3=Bookmark, 4=Report
   */
  like(contentId) {
    return this.postInteraction({ interactionCategoryId: 1, contentId });
  },

  bookmark(contentId) {
    return this.postInteraction({ interactionCategoryId: 3, contentId });
  },
  block(contentId) {
    return this.postInteraction({ interactionCategoryId: 4, contentId });
  },
  hide(contentId) {
    return this.postInteraction({ interactionCategoryId: 2, contentId });
  },

  bestAnswer({ entrustMissionId, contentId }) {
    return apiClient.post(
      "/contentinteractionapi/best-answer",
      {
        entrustMissionId,
        contentId,
      },
      {
        withCredentials: true,
      },
    );
  },

  getInteractions(params = {}) {
    return apiClient.get("/ContentInteractionApi", { params });
  },

  getUserInteraction(contentId) {
    return apiClient.get(`/ContentInteractionApi/user/${contentId}`);
  },

  async getBestAnswerResolution({
    entrustMissionId = null,
    threadContentId = null,
    fallbackBestAnswerContentId = null,
  } = {}) {
    const parsedFallback = toNumberOrNull(fallbackBestAnswerContentId);
    const parsedThreadContentId = toNumberOrNull(threadContentId);

    const endpointCandidates = [];
    if (entrustMissionId) {
      endpointCandidates.push(
        {
          path: "/contentinteractionapi/best-answer",
          params: { entrustMissionId },
        },
        {
          path: "/ContentInteractionApi/best-answer",
          params: { entrustMissionId },
        },
        {
          path: `/contentinteractionapi/best-answer/${entrustMissionId}`,
          params: null,
        },
      );
    }

    for (const candidate of endpointCandidates) {
      try {
        const payload = await apiClient.get(candidate.path, {
          params: candidate.params || undefined,
        });
        const resolvedId = extractBestAnswerContentId(payload);
        if (hasExplicitBestAnswerField(payload)) {
          return { contentId: resolvedId, isExplicit: true };
        }
        if (resolvedId) return { contentId: resolvedId, isExplicit: false };
      } catch {
        // ignore and try next endpoint candidate
      }
    }

    let bestAnswerCategoryId = null;
    try {
      const categoriesPayload = await this.getCategories();
      const categories = ensureArray(categoriesPayload);
      const bestAnswerCategory = categories.find((item) => {
        const categoryName = String(item?.name || "").trim();
        return BEST_ANSWER_NAME_REGEX.test(categoryName);
      });
      bestAnswerCategoryId = toNumberOrNull(
        bestAnswerCategory?.interaction_category_id ??
          bestAnswerCategory?.interactionCategoryId ??
          bestAnswerCategory?.id ??
          null,
      );
    } catch {
      bestAnswerCategoryId = null;
    }

    if (!bestAnswerCategoryId) {
      return { contentId: parsedFallback, isExplicit: false };
    }

    const interactionQueryCandidates = [];
    if (entrustMissionId) {
      interactionQueryCandidates.push({
        interactionCategoryId: bestAnswerCategoryId,
        entrustMissionId,
      });
    }
    interactionQueryCandidates.push({
      interactionCategoryId: bestAnswerCategoryId,
    });

    const interactionRecords = [];
    for (const params of interactionQueryCandidates) {
      try {
        const payload = await this.getInteractions(params);
        interactionRecords.push(...ensureArray(payload));
      } catch {
        // ignore and continue
      }
    }

    if (!interactionRecords.length) {
      return { contentId: parsedFallback, isExplicit: false };
    }

    const findMatch = (predicate) =>
      interactionRecords.find((record) => {
        const contentId = extractContentIdFromRecord(record);
        if (!contentId) return false;
        return predicate(record, contentId);
      });

    const matchedByMissionAndCategory = findMatch((record) => {
      if (!entrustMissionId) return false;
      const missionId = extractMissionIdFromRecord(record);
      if (missionId !== Number(entrustMissionId)) return false;
      return isBestAnswerRecord(record, bestAnswerCategoryId);
    });
    if (matchedByMissionAndCategory) {
      return {
        contentId:
          extractContentIdFromRecord(matchedByMissionAndCategory) ||
          parsedFallback,
        isExplicit: false,
      };
    }

    const matchedByCategoryAndThread = findMatch((record, contentId) => {
      if (!isBestAnswerRecord(record, bestAnswerCategoryId)) return false;
      if (!parsedThreadContentId) return true;
      const parentId = toNumberOrNull(
        record?.parentContentId ?? record?.parent_content_id ?? null,
      );
      return (
        parentId === parsedThreadContentId ||
        contentId === parsedThreadContentId
      );
    });

    return {
      contentId:
        extractContentIdFromRecord(matchedByCategoryAndThread) ||
        parsedFallback,
      isExplicit: false,
    };
  },

  async getBestAnswerContentId(options = {}) {
    const result = await this.getBestAnswerResolution(options);
    return result?.contentId ?? null;
  },

  // Content CRUD and child pagination are handled by contentApi.
};

export default contentInteractionApi;
