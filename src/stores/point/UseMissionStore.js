import { defineStore } from "pinia";
import axios from "@/api/point/axios";
import router from "@/router";

axios.defaults.withCredentials = true;

export const useMissionStore = defineStore("mission", {
  state: () => ({
    myMissions: [],
    loading: false,
    error: null,
  }),

  getters: {
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
        const [platformRes, entrustRes] = await Promise.all([
          axios.get("/api/MissionProgressApi/my-missions"),
          axios.get("/api/EntrustMissionProgressApi/user/my-tasks"),
        ]);

        this.myMissions = [
          ...(platformRes.data || []).map((item) => ({
            ...item,
            isEntrust: false,
          })),
          ...(entrustRes.data || []).map((item) => ({
            ...item,
            isEntrust: true,
          })),
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
     * 領取獎勵
     * @param {number} progressId - 進度表主鍵 ID (missionProgressId 或 entrustMissionProgressId)
     * @param {boolean} isEntrust - 是否為委託任務
     */
    async claimReward(progressId, isEntrust = false) {
      try {
        let url;
        let payload;

        if (isEntrust) {
          url = "/api/EntrustMissionProgressApi/claim-reward";
          payload = { progressId }; // 委託用 entrustMissionProgressId
        } else {
          url = "/api/MissionProgressApi/claim-reward";
          payload = { progressId }; // 平台用 missionProgressId
        }

        console.log("發送領獎請求：", {
          url,
          payload,
          isEntrust,
        });

        const response = await axios.post(url, payload);

        console.log("領獎回應：", response.data);

        if (!response.data.ok) {
          throw new Error(response.data.message || "領取失敗");
        }

        await this.fetchMyMissions();

        return this.myMissions.find((m) =>
          isEntrust
            ? m.entrustMissionProgressId === progressId
            : m.missionProgressId === progressId,
        );
      } catch (err) {
        console.error("領獎錯誤：", err.response?.data || err.message);
        this.handleError(err);
        throw new Error(
          err.response?.data?.message || "領取獎勵失敗，請稍後再試",
        );
      }
    },

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
