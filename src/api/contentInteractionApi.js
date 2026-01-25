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
   * Category IDs: 1=Like, 3=Bookmark, 4=Block, 5=Hide
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
    return this.postInteraction({ interactionCategoryId: 5, contentId });
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
};

export default contentInteractionApi;
