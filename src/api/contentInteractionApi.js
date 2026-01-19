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
   *   "category_id": number,   // ID from interaction_category
   *   "content_id": number,    // ID of the content (post)
   *   // user_id is inferred from the auth token on the server side
   *   // optional note field can be added if needed
   * }
   *
   * @param {Object} payload
   * @param {number} payload.category_id
   * @param {number} payload.content_id
   * @returns {Promise<Object>} The created content_interaction record
   */
  postInteraction({ category_id, content_id }) {
    return apiClient.post("/ContentInteractionApi", {
      category_id,
      content_id,
    });
  },

  /**
   * Convenience helpers for common actions.
   * These simply call postInteraction with the appropriate category ID.
   * Adjust the IDs to match your database seed data.
   */
  like(contentId) {
    return this.postInteraction({ category_id: 1, content_id: contentId });
  },
  
  bookmark(contentId) {
    return this.postInteraction({ category_id: 3, content_id: contentId });
  },
  block(contentId) {
    return this.postInteraction({ category_id: 4, content_id: contentId });
  },
  hide(contentId) {
    return this.postInteraction({ category_id: 5, content_id: contentId });
  },
};

export default contentInteractionApi;
