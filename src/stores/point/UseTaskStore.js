import { defineStore } from "pinia";
import service from "@/api/point/axios.js"; // 假設這是你的 API 服務

// const USE_MOCK = import.meta.env.DEV && true; // 開發模式下使用 mock 資料（可從 .env 控制）

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
      }
      return [
        "全部",
        "票務",
        "字幕",
        "尋片",
        "解析",
        "資源",
        "社交",
        "內容",
        "其他",
      ];
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
      const source =
        state.activeTab === "mission" ? state.missions : state.entrustMissions;

      let result = source;

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

      // 時長（僅 mission 適用）
      if (
        state.activeTab === "mission" &&
        state.selectedDuration !== "不限時間"
      ) {
        result = result.filter(
          (item) => item.duration === state.selectedDuration,
        );
      }

      // 排序（統一處理欄位差異）
      return [...result].sort((a, b) => {
        if (state.sortOption === "reward") {
          const aVal =
            state.activeTab === "mission" ? a.reward || 0 : a.price || 0;
          const bVal =
            state.activeTab === "mission" ? b.reward || 0 : b.price || 0;
          return bVal - aVal;
        }
        if (state.sortOption === "popular") {
          const aPop =
            state.activeTab === "mission"
              ? a.participants || 0
              : a.applicants || a.views || 0;
          const bPop =
            state.activeTab === "mission"
              ? b.participants || 0
              : b.applicants || b.views || 0;
          return bPop - aPop;
        }
        if (state.sortOption === "urgent") {
          const aUrgent = state.activeTab === "entrust" ? !!a.urgent : false;
          const bUrgent = state.activeTab === "entrust" ? !!b.urgent : false;
          return bUrgent === aUrgent ? 0 : bUrgent ? -1 : 1;
        }
        // 預設排序
        if (a.featured !== b.featured) return b.featured ? -1 : 1;
        return (b.id || 0) - (a.id || 0);
      });
    },

    totalTasks: (state) => {
      return state.activeTab === "mission"
        ? state.missions.length
        : state.entrustMissions.length;
    },

    totalParticipants: (state) => {
      return state.activeTab === "mission"
        ? state.missions.reduce((sum, m) => sum + (m.participants || 0), 0)
        : state.entrustMissions.reduce(
            (sum, c) => sum + (c.applicants || 0),
            0,
          );
    },

    totalRewardPool: (state) => {
      return state.activeTab === "mission"
        ? state.missions.reduce((sum, m) => sum + (m.reward || 0), 0)
        : state.entrustMissions.reduce((sum, c) => sum + (c.price || 0), 0);
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
      // if (USE_MOCK) {
      //   // mock 資料
      //   this.missions = [
      //     {
      //       id: 1,
      //       title: "每日登入",
      //       desc: "今天也要看看有什麼新電影！",
      //       reward: 10,
      //       category: "基礎",
      //       difficulty: "簡單",
      //       duration: "1-3 天",
      //       participants: 0,
      //       featured: true,
      //       claimed: false,
      //       completed: false,
      //     },
      //     {
      //       id: 2,
      //       title: "分享電影心得",
      //       desc: "發表一篇電影評論",
      //       reward: 50,
      //       category: "內容創作",
      //       difficulty: "中等",
      //       duration: "3-7 天",
      //       participants: 5,
      //       featured: false,
      //       claimed: false,
      //       completed: false,
      //     },
      //     // 更多...
      //   ];
      //   this.entrustMissions = [
      //     {
      //       id: 101,
      //       title: "電影字幕校對",
      //       desc: "協助校對電影字幕中的錯字與時間軸",
      //       price: 120,
      //       category: "字幕",
      //       deadline: "3天內",
      //       applicants: 0,
      //       maxParticipants: 1,
      //       urgent: true,
      //       claimed: false,
      //       completed: false,
      //     },
      //     {
      //       id: 102,
      //       title: "尋找稀有電影資源",
      //       desc: "幫忙找到特定電影的下載連結",
      //       price: 200,
      //       category: "資源",
      //       deadline: "7天內",
      //       applicants: 2,
      //       maxParticipants: 3,
      //       urgent: false,
      //       claimed: false,
      //       completed: false,
      //     },
      //     // 更多...
      //   ];
      //   this.isLoading = false;
      //   this.error = null;
      //   return;
      // }

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
          title: item.title || "未命名委託",
          desc: item.detail || "無詳細說明",
          price: item.rewardPoints || 0,
          category: item.category || "其他",
          deadline: item.deadline
            ? new Date(item.deadline).toLocaleString("zh-TW", {
                dateStyle: "medium",
                timeStyle: "short",
              })
            : "無期限",
          maxParticipants: item.maxParticipants || 1,
          applicants: item.currentParticipants ?? 0,
          urgent: !!item.isUrgent,
          status: item.status || "open",
          requester: item.requesterName || "匿名影友",
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
      if (type !== "platform") return;

      try {
        console.log(`開始領取任務：MissionId = ${taskId}`);
        const response = await service.post(`/api/MissionApi/claim/${taskId}`);

        console.log("HTTP 狀態碼：", response.status);
        console.log("完整回應資料：", response.data); // 印出整個 response.data
        console.log(
          "回傳的 participantCount：",
          response.data?.data?.participantCount,
        );

        const result = response.data;

        if (result.ok && result.data) {
          const mission = this.missions.find((m) => m.id === taskId);
          if (mission) {
            mission.claimed = true;
            mission.participants =
              result.data.participantCount ?? (mission.participants || 0) + 1;
            this.missions = [...this.missions];
            console.log(`前端更新後 participants: ${mission.participants}`);
          }
        }

        return result;
      } catch (err) {
        console.error("領取請求失敗：");
        console.error("錯誤訊息：", err.message);
        console.error("後端回傳錯誤：", err.response?.data); // ← 關鍵！印出後端錯誤訊息
        console.error("HTTP 狀態碼：", err.response?.status);

        const msg = err.response?.data?.message || "領取失敗，請檢查網路或後端";
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
        票務: "fas fa-ticket-alt",
        字幕: "fas fa-closed-captioning",
        尋片: "fas fa-search",
        解析: "fas fa-book-open",
        資源: "fas fa-cloud-upload-alt",
        社交: "fas fa-user-friends",
        內容: "fas fa-file-alt",
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
