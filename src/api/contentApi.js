import apiClient from "./request";

const contentApi = {
  getContents(params = {}) {
    return apiClient.get("/ContentApi", { params });
  },

  getContent(contentId) {
    return apiClient.get(`/ContentApi/${contentId}`);
  },

  getChildren(contentId, { types, page = 1, pageSize = 20 } = {}) {
    const params = { page, pageSize };
    if (types) params.types = types;
    return apiClient.get(`/ContentApi/${contentId}/children`, { params });
  },

  createContent(payload) {
    return apiClient.post("/ContentApi", payload);
  },

  updateContent(contentId, payload) {
    return apiClient.put(`/ContentApi/${contentId}`, payload);
  },

  deleteContent(contentId) {
    return apiClient.delete(`/ContentApi/${contentId}`);
  },
};

export default contentApi;
