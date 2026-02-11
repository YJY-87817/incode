import { defineStore } from "pinia";
import { useRouter } from "vue-router";
import axios from "@/api/point/axios";

axios.defaults.withCredentials = true; // 讓 Cookie 自動帶上

export const useMissionStore = defineStore("mission", {
  state: () => ({
    myMissions: [],
    loading: false,
    error: null,
  }),

  getters: {
    hasClaimable: (state) => state.myMissions.some((m) => m.canClaimReward),
  },

  actions: {
    async fetchMyMissions() {
      const router = useRouter();
      this.loading = true;
      this.error = null;

      try {
        const res = await axios.get("/api/MissionProgressApi/my-missions");
        this.myMissions = res.data || [];
      } catch (err) {
        console.error("載入我的任務失敗", err);

        if (err.response?.status === 401) {
          this.error = "登入已過期，請重新登入";
          router.push("/login");
        } else {
          this.error = err.response?.data?.message || "載入失敗，請稍後再試";
        }
      } finally {
        this.loading = false;
      }
    },

    // 領獎並回傳更新後的 mission
    async claimReward(missionId) {
      try {
        await axios.post("/api/MissionProgressApi/claim-reward", { missionId });

        // 與其手動計算 idx，不如直接重新獲取最新狀態
        await this.fetchMyMissions();

        // 如果需要回傳給組件做其他處理
        return this.myMissions.find((m) => m.missionId === missionId);
      } catch (err) {
        if (err.response?.status === 401) router.push("/login");
        throw new Error(err.response?.data?.message || "領取獎勵失敗");
      }
    },

    async updateProgress(missionId, increment = 1) {
      try {
        const res = await axios.post(
          `/api/MissionProgressApi/${missionId}/increment`,
          { increment },
        );

        // 找到 Store 裡對應的那筆任務
        const idx = this.myMissions.findIndex((m) => m.missionId === missionId);
        if (idx !== -1) {
          // 使用 API 回傳的最新數據更新（如截圖中的 data.currentCount）
          this.myMissions[idx].currentCount = res.data.data.currentCount;
          this.myMissions[idx].status = res.data.data.status;

          // 💡 關鍵：確保響應式觸發
          this.myMissions = [...this.myMissions];
        }
      } catch (err) {
        console.error("更新進度失敗", err);
      }
    },
  },
});
