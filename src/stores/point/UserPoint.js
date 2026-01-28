// 大腦與倉庫 (UserPoint.js)
// 這是你系統的靈魂。我們不把資料放在頁面裡，而是放在一個獨立的檔案 UserPoint.js。

// State (倉庫)：就像銀行的保險箱，裡面存著「原始帳單紀錄」、「使用者現在查什麼關鍵字」。

// Getters (計算器)：就像銀行的會計，它會自動幫你算：「保險箱裡正值的加起來是多少（累計獲得）」、「負值的加起來是多少（累計消耗）」。

// Actions (辦事員)：負責去外面（API/資料庫）把資料搬回來放進保險箱。
import { defineStore } from "pinia";
// import axios from "axios";
import service from "@/api/point/axios.js";

export const useUserPointStore = defineStore("userPoint", {
  state: () => ({
    rawLogs: [], // 存放後端回傳的所有原始紀錄
    isLoading: false, // 全域載入狀態
    searchQuery: "", // 搜尋字串
    filterType: "全部類型", // 篩選類別
  }),

  getters: {
    // 核心邏輯：根據搜尋框與選單過濾資料
    filteredLogs: (state) => {
      return state.rawLogs.filter((log) => {
        const matchesSearch =
          state.searchQuery === "" ||
          log.item.toLowerCase().includes(state.searchQuery.toLowerCase()) ||
          log.date.includes(state.searchQuery);

        const matchesFilter =
          state.filterType === "全部類型" || log.category === state.filterType;

        return matchesSearch && matchesFilter;
      });
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
  },

  actions: {
    // 模擬從後端抓取資料
    async fetchLogs() {
      this.isLoading = true;
      try {
        const response = await service.get("/api/PointLogApi/user/my-logs");

        // 注意：axios 回傳的資料是在 .data 屬性裡
        const apiData = response.data;

        // 定義類別對照表 (可以寫在外面或 Store 裡)
        const typeMap = {
          recharge: "帳戶儲值",
          task: "平台任務",
          exchange: "商品兌換",
          refund: "退款修正",
        };

        this.rawLogs = apiData.map((pl) => {
          return {
            id: pl.pointLogId,
            date: new Date(pl.createdAt).toLocaleString("zh-TW", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
            }),
            /// 1. 這裡直接使用後端已經根據 SourceType 查表解算出來的名稱
            item: pl.displayName || "系統調整",

            // 2. 修正：類別轉換應該根據 sourceType (如 mission, recharge)
            category: this.mapCategory(pl.sourceType),

            points: pl.changeAmount,

            // 3. 核心：C 方案的精髓，不進行前端計算，直接顯示快照餘額
            balance: pl.balanceAfter,

            status: pl.status === "success" ? "已完成" : "處理中",

            // 4. 多傳一個原始類型，方便給 Badge 顏色判斷使用
            rawType: pl.sourceType,
          };
        });
      } catch (error) {
        console.error("API 抓取失敗，改用預設資料:", error);
        alert("API 抓取失敗，改用預設資料");
        //如果 API 沒開，可以用這組假資料測試畫面
        this.rawLogs = [
          {
            id: 1,
            date: "2024/05/15 10:30",
            item: "每日登入簽到",
            category: "平台任務",
            points: 100,
            balance: 1100,
          },
          {
            id: 2,
            date: "2024/05/14 18:45",
            item: "購買電影兌換",
            category: "商品兌換",
            points: -200,
            balance: 900,
          },
          {
            id: 3,
            date: "2024/05/13 09:15",
            item: "完成新手教學",
            category: "平台任務",
            points: 50,
            balance: 950,
          },
          {
            id: 4,
            date: "2024/05/12 14:20",
            item: "活動獎勵",
            category: "系統贈送",
            points: 150,
            balance: 1100,
          },
          {
            id: 5,
            date: "2024/05/11 11:30",
            item: "商品退款",
            category: "退款/修正",
            points: 200,
            balance: 1300,
          },
        ];
      } finally {
        this.isLoading = false;
      }
    },
    // 輔助方法：統一後端標籤轉中文
    mapCategory(type) {
      const map = {
        // 任務類
        mission: "平台任務",
        entrust_mission: "委託任務",
        entrust_mission_point_reserve: "(預扣)委託任務", // ← 重要！預扣也算委託任務類別

        // 儲值 / 充值類
        recharge: "點數儲值",
        point_recharge: "點數儲值",
        deposit: "點數儲值", // ← 新增或替換

        // 消耗類
        order: "商城商品兌換",
        exchange: "商品兌換", // 如果有舊資料用這個

        // 其他
        system: "系統調整",
        refund: "退款修正",

        // 如果有更多 sourceType，可繼續加
      };
      return map[type] || "其他調整"; // 最後 fallback 改成更中性一點，避免誤導
    },
  },
});
