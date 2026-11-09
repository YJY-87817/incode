import { defineStore } from "pinia";
import axios from "@/api/pointClient";
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
          axios.get("/api/EntrustMissionApi/my-entrusts"),
        ]);

        this.myMissions = [
          ...(platformRes.data || []).map((item) => ({
            ...item,
            isEntrust: false,
          })),
          ...(entrustRes.data || []).map((item) => ({
            ...item,
            isEntrust: true,
            entrustMissionId: item.entrustMissionId,
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
      // 如果是委託任務，直接返回（因為後端已自動發放）
      if (isEntrust) {
        console.warn("委託任務獎勵已自動發放，無需手動領取");
        return;
      }

      try {
        const url = "/api/MissionProgressApi/claim-reward";
        const payload = { progressId };

        console.log("發送系統任務領獎請求：", { url, payload });

        const response = await axios.post(url, payload);

        if (!response.data.ok) {
          throw new Error(response.data.message || "領取失敗");
        }

        // 成功後重新載入清單
        await this.fetchMyMissions();

        return this.myMissions.find((m) => m.missionProgressId === progressId);
      } catch (err) {
        const errorMsg = err.response?.data?.message || err.message;
        console.error("領獎錯誤：", errorMsg);

        if (err.response?.status === 400 || errorMsg.includes("已領取過")) {
          console.warn("狀態不同步，強制刷新...");
          await this.fetchMyMissions();
        }

        this.handleError(err);
        throw new Error(errorMsg);
      }
    },

    /**
     * 取消自己發起的委託（POST api/EntrustMissionApi/{id}/cancel），預扣點數會退回。
     * 只有徵求中、且還沒有其他會員回覆的委託可以取消；失敗時丟出後端訊息
     */
    async cancelEntrust(entrustMissionId) {
      try {
        const { data } = await axios.post(
          `/api/EntrustMissionApi/${entrustMissionId}/cancel`,
        );
        await this.fetchMyMissions();
        return data;
      } catch (err) {
        // 狀態已變更（已有人回覆、已結案、已取消）時也重新整理清單
        if (err.response?.status === 409) await this.fetchMyMissions();
        throw new Error(err.response?.data?.message || "取消委託失敗，請稍後再試");
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
