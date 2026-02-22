import { defineStore } from "pinia";
import axios from "@/api/point/axios";
import router from "@/router"; // 直接匯入 router 實例

axios.defaults.withCredentials = true;

export const useMissionStore = defineStore("mission", {
  state: () => ({
    myMissions: [], // 這裡後端會回傳混合了平台與委託任務的 List
    loading: false,
    error: null,
  }),

  getters: {
    // 檢查是否有任何任務可以領獎 (包含平台 completed 或 委託 approved)
    hasClaimable: (state) =>
      state.myMissions.some(
        (m) => m.status === "completed" || m.status === "approved",
      ),
  },

  actions: {
    async fetchMyMissions() {
      this.loading = true;
      this.error = null;

      try {
        // 使用 Promise.all 同時發送兩個請求，提高效率
        const [platformRes, entrustRes] = await Promise.all([
          axios.get("/api/MissionProgressApi/my-missions"),
          axios.get("/api/EntrustMissionProgressApi/user/my-tasks"), // 呼叫你提供的 C# API
        ]);

        // 這裡將兩份資料合併
        // 建議加上標記以便前端邏輯判斷，雖然你的 DTO 欄位已有區分
        this.myMissions = [
          ...(platformRes.data || []),
          ...(entrustRes.data || []),
        ];

        console.log("合併後的任務清單:", this.myMissions);
      } catch (err) {
        console.error("載入我的任務失敗", err);
        this.handleError(err);
      } finally {
        this.loading = false;
      }
    },

    /**
     * 領獎邏輯
     * @param {number} progressId - 進度表的主鍵 ID
     * @param {boolean} isEntrust - 是否為委託任務
     */
    async claimReward(progressId, isEntrust = false) {
      try {
        // 根據類型決定打哪支 API，或是在同一支 API 用參數區分
        const url = isEntrust
          ? "/api/EntrustMissionApi/claim-reward"
          : "/api/MissionProgressApi/claim-reward";

        await axios.post(url, { progressId });

        // 領取成功後，重新整理列表以確保數據同步
        await this.fetchMyMissions();

        // 回傳更新後的那一筆資料
        return this.myMissions.find((m) =>
          isEntrust
            ? m.entrustMissionProgressId === progressId
            : m.missionProgressId === progressId,
        );
      } catch (err) {
        this.handleError(err);
        throw new Error(err.response?.data?.message || "領取獎勵失敗");
      }
    },

    // 統一處理錯誤
    handleError(err) {
      if (err.response?.status === 401) {
        this.error = "登入已過期，請重新登入";
        router.push("/login");
      } else {
        this.error = err.response?.data?.message || "操作失敗，請稍後再試";
      }
    },
  },
});
