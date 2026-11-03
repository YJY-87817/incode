import { defineStore } from "pinia";
import service from "@/api/pointClient.js";

export const useTaskStore = defineStore("task", {
  state: () => ({
    missions: [], // 平台任務
    entrustMissions: [], // 委託任務
    searchQuery: "",
    selectedCategory: "全部",
    selectedDifficulty: "不限難度",
    selectedDuration: "不限時間",
    sortOption: "default",
    showAdvancedFilters: false,
    isLoading: false,
    error: null, // error 狀態
    activeTab: "mission",
  }),

  getters: {
    currentCategories: (state) => {
      if (state.activeTab === "mission") {
        return [
          "全部",
          "基礎",
          "內容創作",
          "社群互動",
          "觀影實踐",
          "電影知識",
          "其他",
        ];
      } else {
        return [
          "全部",
          "影音",
          "跑腿",
          "翻譯",
          "設計",
          "諮詢",
          "技術",
          "資料",
          "其他",
        ];
      }
    },

    difficultyOptions: () => ["不限難度", "簡單", "中等", "高難度"],
    durationOptions: () => [
      "不限時間",
      "1-3 天",
      "3-7 天",
      "7-14 天",
      "14 天以上",
    ],

    filteredList: (state) => {
      const isMission = state.activeTab === "mission";
      const source =
        state.activeTab === "mission" ? state.missions : state.entrustMissions;

      let result = [...source];

      // 搜尋
      if (state.searchQuery.trim()) {
        const q = state.searchQuery.toLowerCase().trim();
        result = result.filter((item) =>
          (item.title || "").toLowerCase().includes(q),
        );
      }

      // 類別
      if (state.selectedCategory !== "全部") {
        result = result.filter(
          (item) => item.category === state.selectedCategory,
        );
      }

      // 難度（僅 mission 適用）
      if (
        state.activeTab === "mission" &&
        state.selectedDifficulty !== "不限難度"
      ) {
        result = result.filter(
          (item) => item.difficulty === state.selectedDifficulty,
        );
      }

      if (state.selectedDuration !== "不限時間") {
        if (isMission) {
          // 平台任務：匹配字串 (如 "1-3 天")
          result = result.filter(
            (item) => item.duration === state.selectedDuration,
          );
        } else {
          // 委託任務：根據 deadline 日期計算
          const now = new Date();
          result = result.filter((item) => {
            if (!item.rawDeadline) return true; // 無期限則不濾除
            const deadlineDate = new Date(item.rawDeadline);
            const diffDays = (deadlineDate - now) / (1000 * 60 * 60 * 24);

            if (state.selectedDuration === "1-3 天")
              return diffDays > 0 && diffDays <= 3;
            if (state.selectedDuration === "3-7 天")
              return diffDays > 3 && diffDays <= 7;
            if (state.selectedDuration === "7-14 天")
              return diffDays > 7 && diffDays <= 14;
            if (state.selectedDuration === "14 天以上") return diffDays > 14;
            return true;
          });
        }
      }

      // 排序（統一處理欄位差異）
      return [...result].sort((a, b) => {
        const isMission = state.activeTab === "mission";

        // 1. 獎勵/價格排序
        if (state.sortOption === "reward") {
          const aVal = isMission ? a.reward || 0 : a.price || 0;
          const bVal = isMission ? b.reward || 0 : b.price || 0;
          return bVal - aVal;
        }

        // 2. 人氣排序 (修正 0 的讀取問題)
        if (state.sortOption === "popular") {
          // 使用 nullish coalescing (??) 確保 0 不會被視為 false
          const aPop = isMission ? (a.participants ?? 0) : (a.applicants ?? 0);
          const bPop = isMission ? (b.participants ?? 0) : (b.applicants ?? 0);
          return bPop - aPop;
        }

        // 3. 緊急程度排序 (委託模式限定)
        if (state.sortOption === "urgent") {
          return (b.urgent === true) - (a.urgent === true);
        }

        // 4. 預設排序 (精選優先，其次 ID 倒序)
        if (a.featured !== b.featured) {
          return (b.featured ? 1 : 0) - (a.featured ? 1 : 0);
        }

        // 確保 ID 比較時型別正確
        return Number(b.id || 0) - Number(a.id || 0);
      });
    },

    filteredMissions: (state) => {
      let result = [...(state.missions || [])];

      // 搜尋
      if (state.searchQuery.trim()) {
        const q = state.searchQuery.toLowerCase().trim();
        result = result.filter((item) =>
          (item.title || "").toLowerCase().includes(q),
        );
      }

      // 類別
      if (state.selectedCategory !== "全部") {
        result = result.filter(
          (item) => item.category === state.selectedCategory,
        );
      }

      // 難度
      if (state.selectedDifficulty !== "不限難度") {
        result = result.filter(
          (item) => item.difficulty === state.selectedDifficulty,
        );
      }

      // 時間 (平台任務是匹配字串)
      if (state.selectedDuration !== "不限時間") {
        result = result.filter(
          (item) => item.duration === state.selectedDuration,
        );
      }

      // 排序
      return result.sort((a, b) => {
        if (state.sortOption === "reward")
          return (b.reward || 0) - (a.reward || 0);
        if (state.sortOption === "popular")
          return (b.participants ?? 0) - (a.participants ?? 0);
        if (a.featured !== b.featured)
          return (b.featured ? 1 : 0) - (a.featured ? 1 : 0);
        return Number(b.id || 0) - Number(a.id || 0);
      });
    },

    // 2. 委託任務專用過濾
    filteredEntrusts: (state) => {
      let result = [...(state.entrustMissions || [])];

      // 搜尋
      if (state.searchQuery.trim()) {
        const q = state.searchQuery.toLowerCase().trim();
        result = result.filter((item) =>
          (item.title || "").toLowerCase().includes(q),
        );
      }

      // 類別
      if (state.selectedCategory !== "全部") {
        result = result.filter(
          (item) => item.category === state.selectedCategory,
        );
      }

      // 時間 (委託任務是根據日期計算)
      if (state.selectedDuration !== "不限時間") {
        const now = new Date();
        result = result.filter((item) => {
          if (!item.rawDeadline) return true;
          const diffDays =
            (new Date(item.rawDeadline) - now) / (1000 * 60 * 60 * 24);
          if (state.selectedDuration === "1-3 天")
            return diffDays > 0 && diffDays <= 3;
          if (state.selectedDuration === "3-7 天")
            return diffDays > 3 && diffDays <= 7;
          if (state.selectedDuration === "7-14 天")
            return diffDays > 7 && diffDays <= 14;
          if (state.selectedDuration === "14 天以上") return diffDays > 14;
          return true;
        });
      }

      // 排序
      return result.sort((a, b) => {
        if (state.sortOption === "reward")
          return (b.price || 0) - (a.price || 0);
        if (state.sortOption === "popular")
          return (b.applicants ?? 0) - (a.applicants ?? 0);
        if (state.sortOption === "urgent")
          return (b.urgent === true) - (a.urgent === true);
        return Number(b.id || 0) - Number(a.id || 0);
      });
    },
  },

  actions: {
    setActiveTab(newTab) {
      this.activeTab = newTab;
      this.selectedCategory = "全部";
      if (newTab !== "mission") {
        this.selectedDifficulty = "不限難度";
      }
      // 可選擇是否重置其他篩選
      // this.clearFilters();
    },

    clearFilters() {
      this.searchQuery = "";
      this.selectedCategory = "全部";
      this.selectedDifficulty = "不限難度";
      this.selectedDuration = "不限時間";
      this.sortOption = "default";
      this.showAdvancedFilters = false;
    },

    toggleAdvancedFilters() {
      this.showAdvancedFilters = !this.showAdvancedFilters;
    },

    async fetchTasks() {
      this.isLoading = true;
      this.error = null;
      try {
        const [missionRes, entrustRes] = await Promise.all([
          service.get("/api/MissionApi"),
          service.get("/api/EntrustMissionApi"),
        ]);

        this.missions = (missionRes.data || []).map((item) => ({
          id: item.missionId,
          title: item.missionName,
          desc: item.missionDetail,
          reward: item.rewardPoints,
          exp: item.rewardExp,
          category: item.category,
          difficulty: item.difficulty,
          duration: item.estimatedDuration || item.resetType || "每日",
          participants: item.participantCount || 0,
          featured: item.isFeatured || false,
          resetType: item.resetType,
          isActive: item.isActive,
          createdAt: item.createdAt,
          claimed: false, // 假設後端有，回傳時設置
          completed: false,
        }));

        this.entrustMissions = (entrustRes.data || []).map((item) => ({
          id: item.entrustMissionId,
          contentId: item.contentId,
          username: item.username,
          imageUrl: item.imageUrl,
          title: item.title || "未命名委託",
          desc: item.detail || "無詳細說明",
          price: item.rewardPoints || 0,
          category: item.category || "其他",
          rawDeadline: item.deadline,
          //顯示用：格式化日期
          deadline: item.deadline
            ? new Date(item.deadline).toLocaleString("zh-TW", {
                dateStyle: "medium",
                timeStyle: "short",
              })
            : "無期限",
          maxParticipants: item.maxParticipants || 1,
          // 確保人數被正確轉為數字，避免計算時變字串相加
          applicants: Number(item.currentParticipants ?? 0),
          urgent: !!item.isUrgent,
          status: item.status || "open",
          createdAt: item.createdAt,
          claimed: false,
          completed: false,
        }));
        console.log(
          `載入完成：平台 ${this.missions.length} 項，委託 ${this.entrustMissions.length} 項`,
        );
      } catch (err) {
        console.error("載入任務失敗", err);
        this.error =
          err.response?.data?.message ||
          err.message ||
          "無法載入任務，請稍後再試";
      } finally {
        this.isLoading = false;
      }
    },

    async claimTask(type, taskId) {
      // 移除 return，改為根據 type 決定 endpoint
      try {
        const isPlatform = type === "platform";
        const endpoint = isPlatform
          ? `/api/MissionApi/${taskId}/claim`
          : `/api/EntrustMissionApi/${taskId}/entrustMissionClaim`;

        const response = await service.post(endpoint);
        const result = response.data;

        // 只要後端回傳 ok 或成功狀態
        if (result.ok && result.data) {
          const listKey = isPlatform ? "missions" : "entrustMissions";
          const taskIndex = this[listKey].findIndex((t) => t.id === taskId);

          if (taskIndex !== -1) {
            // 1. 取得舊任務並建立副本 (確保響應式渲染)
            const task = { ...this[listKey][taskIndex] };

            // 2. 更新狀態
            task.claimed = true;

            // 3. 更新人數 (處理不同的欄位名稱)
            if (isPlatform) {
              // 平台任務用 participantCount
              task.participants =
                result.data.participantCount ?? (task.participants || 0) + 1;
            } else {
              // 委託任務用 currentParticipants
              task.applicants =
                result.data.CurrentParticipants ?? (task.applicants || 0) + 1;
            }

            // 4. 寫回 Store 並觸發 UI 刷新
            this[listKey][taskIndex] = task;
            this[listKey] = [...this[listKey]];
          }
        }
        return result;
      } catch (err) {
        console.error(
          `領取 ${type} 請求失敗：`,
          err.response?.data || err.message,
        );
        const msg = err.response?.data?.message
          ? `${err.response.data.message} (錯誤碼: ${err.response.data.code || err.response.status})`
          : "領取失敗，請稍後再試";
        throw new Error(msg);
      }
    }, //之後任務完成邏輯

    getCategoryIcon(category) {
      const icons = {
        基礎: "fas fa-home",
        內容創作: "fas fa-pen-nib",
        社群互動: "fas fa-users",
        觀影實踐: "fas fa-ticket-alt",
        電影知識: "fas fa-book",
        影音: "fas fa-ticket-alt",
        跑腿: "fas fa-closed-captioning",
        翻譯: "fas fa-search",
        設計: "fas fa-book-open",
        諮詢: "fas fa-cloud-upload-alt",
        技術: "fas fa-user-friends",
        資料: "fas fa-file-alt",
        其他: "fas fa-file-alt",
      };
      return icons[category] || "fas fa-film";
    },

    getDifficultyColor(difficulty) {
      const colors = {
        簡單: "bg-gradient-success",
        中等: "bg-gradient-info",
        高難度: "bg-gradient-warning",
      };
      return colors[difficulty] || "bg-gradient-secondary";
    },
  },
});
