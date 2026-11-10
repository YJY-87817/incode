import { toAbsoluteUrl } from "@/utils/contentBody.js";
import { normalizeRichContent } from "@/utils/sanitizeHtml.js";
import { resolveAuthorAvatar } from "@/stores/forum/avatars.js";
import { fetchBodyHtmlCached } from "@/stores/forum/contentHtml.js";

/**
 * 【數據轉換層】 API 回應 → 處理後的內部數據結構
 *
 * 此函數是整個帖文中數據流的一個關鍵轉換點。
 * 之所以複雜是因為後端的 API 回應字段格式不統一：
 * - 有些 camelCase: contentId, userId, createdAt
 * - 有些 snake_case: content_id, user_id, created_at
 * 此函數統一化這些候選形式，確保下游組件使用一致的 camelCase 。
 */
export const buildThreadItem = async (item, fallbackType) => {
  // 【字段映射】 將 API 回應的各種字段映射到下游的內部統一 id
  const bodyHtml = await fetchBodyHtmlCached(item.body);
  const { sanitizedHtml, textContent, imageUrls } = normalizeRichContent(
    bodyHtml,
    toAbsoluteUrl,
  );
  // [字段映射] userId: item.userId 或 item.user_id
  // [字段映射] createdAt: item.createdAt 或 item.created_at
  // [字段映射] id: item.contentId 或 item.content_id 或 item.id
  const resolvedLikeCount = extractLikeCount(item);

  return {
    id: item.contentId ?? item.content_id ?? item.id,
    userId: item.userId ?? item.user_id ?? null,
    author: item.username ?? item.userName ?? item.user_name ?? "",
    authorAvatar: resolveAuthorAvatar(item),
    createdAt: item.createdAt ?? item.created_at ?? null,
    content: textContent,
    contentHtml: sanitizedHtml,
    contentText: textContent,
    imageUrls,
    type: item.type ?? fallbackType,
    parentContentId: item.parentContentId ?? item.parent_content_id ?? null,
    categoryId: item.categoryId ?? item.category_id ?? null,
    title: item.title ?? "",
    likeCount: resolvedLikeCount,
  };
};

/**
 * 【數據提取】 從 API 回應中提取回覆統計資訊
 *
 * 負責: 從 API 回應中提取下列信息：
 * - replyCount: 此主題的回覆數量
 * - lastReplyTime: 最後回覆日期
 * - lastReplyUser: 最後回覆的使用者名稱
 * - lastReplyUserId: 最後回覆的使用者 ID
 * 此信息可能來自：
 * 1. 後端直接提供（版本 5.0+）
 * 2. 解析出來的 interactions 陣列（舊版）
 */
export const extractReplyStats = (item) => {
  // 優先使用後端直接提供的統計資訊（camelCase）
  const replyCount = item?.replyCount ?? null;
  const lastReplyTime = item?.lastReplyTime ?? null;
  const lastReplyUser = item?.lastReplyUser ?? null;
  const lastReplyUserId = item?.lastReplyUserId ?? null;
  const lastReplyUserAvatar = toAbsoluteUrl(
    item?.lastReplyUserAvatar ??
      item?.last_reply_user_avatar ??
      item?.lastReplyAvatar ??
      item?.last_reply_avatar ??
      item?.lastReplyUserImageUrl ??
      item?.last_reply_user_image_url ??
      item?.lastReplyImageUrl ??
      item?.last_reply_image_url ??
      item?.lastReplyImage ??
      item?.last_reply_image ??
      item?.lastReplyUserImage ??
      item?.last_reply_user_image ??
      item?.lastReplyUser?.imageUrl ??
      item?.lastReplyUser?.avatarUrl ??
      "",
  );

  return {
    replyCount,
    lastReplyTime,
    lastReplyUser,
    lastReplyUserId,
    lastReplyUserAvatar,
  };
};

export const extractLikeCountFromInteractions = (item) => {
  const interactions =
    item?.interactions ??
    item?.Interactions ??
    item?.contentInteractions ??
    item?.content_interactions ??
    null;

  if (!Array.isArray(interactions)) return null;

  const likeCount = interactions.filter((interaction) => {
    const categoryId = Number(
      interaction?.interactionCategoryId ??
        interaction?.interaction_category_id ??
        interaction?.categoryId ??
        interaction?.category_id,
    );
    return categoryId === 1;
  }).length;

  return Math.max(0, likeCount);
};

/**
 * 【數據提取 #3】 按讚數提取（複雜的 fallback 鏈）
 *
 * 負責: 從不同的 API 回應格式中提取按讚數；
 * 但不同的各個 API 回應中，該數字段位置各不相同（字段名稱不一致、嵌套位置不一致）。
 * 此函數專門針對這種差異。
 */
export const extractLikeCount = (item) => {
  // 嘗試撈取所有 API 回應可能的候選字段名（camelCase 及 snake_case 一起）
  const candidate =
    item?.likeCount ??
    item?.like_count ??
    item?.likesCount ??
    item?.likes_count ??
    item?.likes ??
    item?.totalLikes ??
    item?.total_likes ??
    item?.thumbsUpCount ??
    item?.thumbs_up_count ??
    item?.interactionLikeCount ??
    item?.interaction_like_count ??
    null;

  // 欄位不存在（null / 空字串）時不能直接 Number()：Number(null) 是 0，會讓下面的備援永遠不會執行
  if (candidate !== null && candidate !== "") {
    const value = Number(candidate);
    if (Number.isFinite(value)) return Math.max(0, value);
  }

  // 若 straightforward 方法不可行，當作最後的 fallback 兜底策略：
  // 依據使用者互動資料一筆一筆計算點讚數。
  const fromInteractions = extractLikeCountFromInteractions(item);
  if (Number.isFinite(fromInteractions)) return Math.max(0, fromInteractions);

  return 0;
};

export const extractPreviewPayload = (item) => {
  const previewHtml =
    item?.previewHtml ??
    item?.preview_html ??
    item?.bodyPreviewHtml ??
    item?.body_preview_html ??
    item?.bodyHtml ??
    item?.body_html ??
    null;
  const previewText =
    item?.previewText ??
    item?.preview_text ??
    item?.bodyPreviewText ??
    item?.body_preview_text ??
    item?.bodyText ??
    item?.body_text ??
    null;
  const previewImages =
    item?.previewImages ??
    item?.preview_images ??
    item?.imageUrls ??
    item?.image_urls ??
    null;

  if (!previewHtml && !previewText && !previewImages) return null;

  return {
    previewHtml,
    previewText,
    previewImages,
  };
};

export const normalizeCategory = (raw, fallbackName = "") => {
  if (!raw) return null;
  const source = raw?.data ?? raw;
  if (Array.isArray(source)) return null;

  const id =
    source.id ??
    source.content_category_id ??
    source.categoryId ??
    source.category_id;
  const name =
    source.name ?? source.categoryName ?? source.category_name ?? fallbackName;

  if (!id && !name) return null;

  return {
    id,
    name,
    description: source.description ?? null,
    postCount: source.postCount ?? source.post_count ?? 0,
  };
};

export const toInteractionCategoryId = (record) => {
  const categoryId = Number(
    record?.interactionCategoryId ??
      record?.interaction_category_id ??
      record?.categoryId ??
      record?.category_id ??
      null,
  );
  return Number.isFinite(categoryId) ? categoryId : null;
};

export const extractUserInteractions = (payload) => {
  if (Array.isArray(payload)) return payload;
  if (Array.isArray(payload?.Interactions)) return payload.Interactions;
  if (Array.isArray(payload?.interactions)) return payload.interactions;
  if (Array.isArray(payload?.data?.Interactions))
    return payload.data.Interactions;
  if (Array.isArray(payload?.data?.interactions))
    return payload.data.interactions;
  return [];
};
