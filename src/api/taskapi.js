import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7190/api", // 替換為你的後端 Port
});

export const taskApi = {
  // 官方任務
  getMission: () => api.get("/MissionApi"),
  // 影友委託
  getEntrustMission: () => api.get("/EntrustMissionApi"),
  // 領取任務 (新增進度)
  claimMissionProgress: (data) => api.post("/MissionProgressApi", data),
  claimEntrustProgress: (data) => api.post("/EntrustMissionProgressApi", data),
};
