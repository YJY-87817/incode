// 大腦與倉庫 (pointStore.js)
// State (倉庫)：就像銀行的保險箱，裡面存著「原始帳單紀錄」、「使用者現在查什麼關鍵字」。
// Getters (計算器)：就像銀行的會計，它會自動幫你算：「保險箱裡正值的加起來是多少（累計獲得）」、「負值的加起來是多少（累計消耗）」。
// Actions (辦事員)：負責去外面（API/資料庫）把資料搬回來放進保險箱。
import { defineStore } from "pinia";
// import axios from "axios";
import service from "@/api/pointClient.js";

export const usePointStore = defineStore("usePoint", {
  state: () => ({
    rawLogs: [], // 存放後端回傳的所有原始紀錄
    isLoading: false, // 全域載入狀態
    searchQuery: "", // 搜尋字串
    filterType: "全部類型", // 篩選類別
  }),

  getters: {
    // 核心邏輯：根據搜尋框與選單過濾資料
    filteredLogs: (state) => {
      let result = state.rawLogs;

      if (state.searchQuery.trim()) {
        const q = state.searchQuery.trim().toLowerCase();
        result = result.filter(
          (log) =>
            log.item?.toLowerCase().includes(q) ||
            log.date?.includes(q) ||
            log.rawType?.toLowerCase().includes(q) ||
            log.displayName?.toLowerCase().includes(q),
        );
      }

      if (state.filterType !== "全部類型") {
        result = result.filter((log) => log.category === state.filterType);
      }

      return result;
    },

    // 計算累計獲得
    totalGained: (state) => {
      return state.rawLogs
        .filter((log) => log.points > 0)
        .reduce((sum, log) => sum + log.points, 0);
    },

    // 計算累計消耗
    totalSpent: (state) => {
      return state.rawLogs
        .filter((log) => log.points < 0)
        .reduce((sum, log) => sum + Math.abs(log.points), 0);
    },

    currentBalance: (state) =>
      state.rawLogs.length > 0 ? (state.rawLogs[0].balance ?? 0) : 0,
  },

  actions: {
    async fetchLogs() {
      this.isLoading = true;
      try {
        const response = await service.get("/api/PointLogApi/user/my-logs");
        const apiData = response.data?.items ?? response.data;

        this.rawLogs = apiData.map((pl) => ({
          id: pl.pointLogId,
          date: new Date(pl.createdAt).toLocaleString("zh-TW", {
            year: "numeric",
            month: "2-digit",
            day: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          }),
          item: pl.displayName || "系統調整",
          subTitle: pl.displaySourceSubTitle || "",
          category: this.mapCategory(pl.sourceType),
          points: pl.changeAmount,
          balance: pl.balanceAfter,
          status: pl.status === "success" ? "已完成" : "處理中",
          rawType: pl.sourceType,
        }));

        // 確保最新在最上面
        this.rawLogs.sort((a, b) => new Date(b.date) - new Date(a.date));
      } catch (error) {
        console.error("載入失敗:", error);
        // 假資料（更新為 2026 年範例）
        this.rawLogs = [
          {
            id: 1,
            date: "2026/01/28 12:42",
            item: "[預扣] 電影字幕校對",
            category: "委託任務",
            points: -222,
            balance: 578,
            rawType: "entrust_mission_point_reserve",
          },
          {
            id: 2,
            date: "2026/01/28 12:38",
            item: "商城商品兌換",
            category: "商城商品兌換",
            points: -500,
            balance: 800,
            rawType: "order",
          },
          {
            id: 3,
            date: "2026/01/28 12:38",
            item: "點數儲值",
            category: "點數儲值",
            points: 1000,
            balance: 1300,
            rawType: "deposit",
          },
          // ... 其他
        ];
      } finally {
        this.isLoading = false;
      }
    },
    // 輔助方法：統一後端標籤轉中文
    mapCategory(type) {
      if (!type) return "其他調整";

      const t = type.trim().toLowerCase();

      if (t === "mission") return "平台任務";
      if (t === "entrust_mission") return "委託任務";

      // 加強判斷預扣相關的各種可能寫法
      if (
        t.includes("reserve") ||
        t === "entrust_mission_point_reserve" ||
        t === "entrustmissionpointreserve"
      ) {
        return "預扣";
      }

      if (t === "point_recharge" || t === "deposit") return "儲值";
      if (t.includes("order") || t === "orders") return "商城";
      if (t.includes("tip")) return "打賞";
      if (t.includes("refund")) return "退款/修正";
      if (t === "system") return "系統調整";

      return "其他調整";
    },

    // 可選：清除搜尋與篩選狀態
    resetFilters() {
      this.searchQuery = "";
      this.filterType = "全部類型";
    },
  },
});
