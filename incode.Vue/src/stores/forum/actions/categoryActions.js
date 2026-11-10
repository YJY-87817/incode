import apiClient from "@/api/request.js";
import { fetchUserAvatarFromProfile } from "@/stores/forum/avatars.js";
import { normalizeCategory } from "@/stores/forum/normalizers.js";

export const categoryActions = {
  async resolveUserAvatar(userId, username) {
    return await fetchUserAvatarFromProfile({ userId, username });
  },

  async enrichTopicAvatarFields(topics = []) {
    const items = Array.isArray(topics) ? topics : [];
    if (items.length === 0) return;

    await Promise.all(
      items.map(async (topic) => {
        if (!topic) return;

        if (!topic.authorAvatar && topic.author) {
          const authorAvatar = await this.resolveUserAvatar(
            topic.userId,
            topic.author,
          );
          if (authorAvatar) {
            topic.authorAvatar = authorAvatar;
          }
        }

        if (topic.lastReplyUser && !topic.lastReplyUserAvatar) {
          const replyAvatar = await this.resolveUserAvatar(
            topic.lastReplyUserId,
            topic.lastReplyUser,
          );
          if (replyAvatar) {
            topic.lastReplyUserAvatar = replyAvatar;
          }
        }
      }),
    );
  },

  // ========== 視窗寬度 ==========
  updateWindowWidth(width) {
    this.windowWidth = width;
  },

  // ========== 分類管理 ==========
  async fetchCategories(options = {}) {
    const { forceRefresh = false } = options;

    this.isLoading = true;
    try {
      // 添加時間戳防止緩存
      const params = forceRefresh ? { _t: Date.now() } : {};
      const data = await apiClient.get("/CategoryApi", { params });
      this.categories = data;

      // 不再預設選擇第一個分類
      // 只有在已經有選擇的分類時才同步到 newTopic
      if (this.selectedCategory && data.length > 0) {
        this.newTopic.categoryId = this.selectedCategory;
      }
    } catch (error) {
      console.error("載入分類失敗:", error);
      alert("載入分類失敗");
    } finally {
      this.isLoading = false;
    }
  },

  async createCategory(
    categoryName,
    description = null,
    parentCategoryId = null,
  ) {
    const name = String(categoryName ?? "").trim();
    if (!name) {
      throw new Error("分類名稱不可為空");
    }

    try {
      const payload = {
        parentCategoryId,
        name,
        description: description ?? null,
      };
      const created = await apiClient.post("/CategoryApi", payload);
      const normalized = normalizeCategory(created, name);

      await this.fetchCategories();

      if (normalized?.id) return normalized;
      return this.categories.find((c) => c.name === name) || null;
    } catch (error) {
      console.error("新增分類失敗:", error);
      throw error;
    }
  },
};
