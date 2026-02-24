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
          payload = { progressId };
        } else {
          url = "/api/MissionProgressApi/claim-reward";
          payload = { progressId };
        }

        console.log("發送領獎請求：", { url, payload, isEntrust });

        const response = await axios.post(url, payload);

        // 這裡通常 axios 若回傳 400 會直接跳進 catch，
        // 但如果後端 ok: false 且 status 是 200，則在此處處理
        if (!response.data.ok) {
          throw new Error(response.data.message || "領取失敗");
        }

        // 成功領取後，務必重新獲取最新清單
        await this.fetchMyMissions();

        return this.myMissions.find((m) =>
          isEntrust
            ? m.entrustMissionProgressId === progressId
            : m.missionProgressId === progressId,
        );
      } catch (err) {
        const errorMsg = err.response?.data?.message || err.message;
        console.error("領獎錯誤詳情：", errorMsg);

        // ✨ 同步 UI 狀態的核心邏輯
        // 如果後端說「已領取過」(400 Bad Request)，代表前端狀態已過時
        if (err.response?.status === 400 || errorMsg.includes("已領取過")) {
          console.warn("偵測到狀態不同步，正在強制刷新任務清單...");
          await this.fetchMyMissions();
        }

        // 統一處理登入過期等通用錯誤
        this.handleError(err);

        // 拋出錯誤給 UI 層 (SweetAlert) 顯示
        throw new Error(errorMsg);
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
