import apiClient from "./request";

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
   * GET /InteractionCategoryApi
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
    return apiClient.get("/InteractionCategoryApi");
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

  /**
   * Fetch comments for a specific content item.
   *
   * GET /ContentApi/{contentId}/comments
   *
   * @param {number} contentId - The ID of the content to fetch comments for
   * @returns {Promise<Object>} Object containing:
   *   {
   *     contentId: number,
   *     comments: Array,
   *     count: number
   *   }
   */
  getComments(contentId) {
    return apiClient.get(`/ContentApi/${contentId}/comments`);
  },

  /**
   * Update (edit) a content item.
   *
   * PUT /ContentApi/{contentId}
   *
   * Request body (JSON):
   * {
   *   "title": string,      // 新的標題
   *   "body": string,       // 新的內容
   * }
   *
   * @param {number} contentId - The ID of the content to update
   * @param {Object} payload - The updated content data
   * @param {string} payload.title - New title
   * @param {string} payload.body - New body content
   * @returns {Promise<Object>} The updated content record
   */
  updateContent(contentId, { title, body }) {
    return apiClient.put(`/ContentApi/${contentId}`, {
      title,
      body,
    });
  },

  /**
   * Delete (soft delete) a content item.
   *
   * DELETE /ContentApi/{contentId}
   *
   * This performs a soft delete by setting is_active to 2 (hidden).
   * The content will still exist in the database but won't be displayed.
   *
   * @param {number} contentId - The ID of the content to delete
   * @returns {Promise<Object>} Success response
   */
  deleteContent(contentId) {
    return apiClient.delete(`/ContentApi/${contentId}`);
  },
};

export default contentInteractionApi;
