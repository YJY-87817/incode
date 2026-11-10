import apiClient from "@/api/request.js";
import { toAbsoluteUrl } from "@/utils/contentBody.js";
import { userAvatarCache, userAvatarPending } from "@/stores/forum/caches.js";

export const resolveAuthorAvatar = (item = {}) => {
  const rawAvatar =
    item?.authorAvatar ??
    item?.author_avatar ??
    item?.avatarUrl ??
    item?.avatar_url ??
    item?.imageUrl ??
    item?.image_url ??
    item?.userAvatar ??
    item?.user_avatar ??
    item?.userImageUrl ??
    item?.user_image_url ??
    item?.userImage ??
    item?.user_image ??
    item?.profileImageUrl ??
    item?.profile_image_url ??
    item?.profileImage ??
    item?.profile_image ??
    item?.user?.avatarUrl ??
    item?.user?.avatar_url ??
    item?.user?.imageUrl ??
    item?.user?.image_url ??
    item?.userProfile?.avatarUrl ??
    item?.userProfile?.avatar_url ??
    item?.userProfile?.imageUrl ??
    item?.userProfile?.image_url ??
    "";

  return toAbsoluteUrl(rawAvatar);
};

export const normalizeUsername = (value) => String(value || "").trim();

export const toUserAvatarCacheKeys = ({ userId, username } = {}) => {
  const keys = [];
  const numericUserId = Number(userId);
  const normalizedName = normalizeUsername(username);

  if (Number.isFinite(numericUserId) && numericUserId > 0) {
    keys.push(`id:${numericUserId}`);
  }

  if (normalizedName) {
    keys.push(`name:${normalizedName.toLowerCase()}`);
  }

  return keys;
};

export const getCachedUserAvatar = ({ userId, username } = {}) => {
  const keys = toUserAvatarCacheKeys({ userId, username });
  for (const key of keys) {
    if (userAvatarCache.has(key)) {
      return userAvatarCache.get(key) || "";
    }
  }
  return "";
};

export const setCachedUserAvatar = ({ userId, username, avatarUrl } = {}) => {
  const keys = toUserAvatarCacheKeys({ userId, username });
  if (keys.length === 0) return;

  const normalizedAvatar = toAbsoluteUrl(avatarUrl || "");
  keys.forEach((key) => userAvatarCache.set(key, normalizedAvatar));
};

export const extractAvatarFromProfilePayload = (payload = {}) => {
  const source = payload?.data ?? payload;
  return toAbsoluteUrl(
    source?.imageUrl ??
      source?.ImageUrl ??
      source?.avatarUrl ??
      source?.avatar_url ??
      source?.avatar ??
      source?.profileImageUrl ??
      source?.profile_image_url ??
      "",
  );
};

export const fetchUserAvatarFromProfile = async ({ userId, username } = {}) => {
  const normalizedName = normalizeUsername(username);
  const cached = getCachedUserAvatar({ userId, username: normalizedName });
  if (cached) return cached;
  if (!normalizedName) return "";

  const pendingKey = `name:${normalizedName.toLowerCase()}`;
  if (userAvatarPending.has(pendingKey)) {
    return userAvatarPending.get(pendingKey);
  }

  const request = (async () => {
    try {
      const profile = await apiClient.get("/User/profile", {
        params: {
          username: normalizedName,
          t: Date.now(),
        },
      });
      const avatar = extractAvatarFromProfilePayload(profile);
      setCachedUserAvatar({
        userId,
        username: normalizedName,
        avatarUrl: avatar,
      });
      return avatar;
    } catch (error) {
      console.warn("載入使用者頭像失敗:", normalizedName, error);
      setCachedUserAvatar({ userId, username: normalizedName, avatarUrl: "" });
      return "";
    } finally {
      userAvatarPending.delete(pendingKey);
    }
  })();

  userAvatarPending.set(pendingKey, request);
  return request;
};
