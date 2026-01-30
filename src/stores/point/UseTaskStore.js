import { defineStore } from "pinia";
import service from "@/api/point/axios.js"; // 暫時註解

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
    activeTab: "mission",
  }),

  getters: {
    // 現在直接使用 state.activeTab，不再需要傳參數
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

      // 時長
      if (state.selectedDuration !== "不限時間") {
        result = result.filter(
          (item) => item.duration === state.selectedDuration,
        );
      }

      // 排序
      return result.sort((a, b) => {
        if (state.sortOption === "reward") {
          return (b.reward || b.price || 0) - (a.reward || a.price || 0);
        }
        if (state.sortOption === "popular") {
          return (
            (b.participants || b.applicants || b.views || 0) -
            (a.participants || a.applicants || a.views || 0)
          );
        }
        if (state.sortOption === "urgent") {
          return b.urgent === a.urgent ? 0 : b.urgent ? -1 : 1;
        }
        // 預設排序
        if (a.featured !== b.featured) return b.featured ? 1 : -1;
        return b.id - a.id;
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
      // 可選擇是否在切換 tab 時重置其他篩選
      // this.resetFilters();
    },

    resetFilters() {
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
      try {
        const [missionRes, entrustRes] = await Promise.all([
          service.get("/api/MissionApi"), // 取得所有啟用平台任務
          service.get("/api/EntrustMissionApi"), // 取得開放中的委託任務
        ]);

        // 平台任務：直接使用後端回傳格式（或輕微轉換）
        this.missions = (missionRes.data || []).map((item) => ({
          // 統一前端使用的欄位名稱（可選，如果你想保持舊名稱）
          id: item.missionId,
          title: item.missionName,
          desc: item.missionDetail, // 原 desc 對應 detail
          reward: item.rewardPoints,
          exp: item.rewardExp, // 如果前端要顯示經驗值
          category: item.category,
          difficulty: item.difficulty,
          duration: item.estimatedDuration || item.resetType || "每日", // 後備方案
          participants: item.participantCount || 0,
          featured: item.isFeatured || false,
          resetType: item.resetType,
          isActive: item.isActive,
          createdAt: item.createdAt,
          // 如果有其他需要的前端欄位，可在此補充
        }));

        // 委託任務：同樣映射
        this.entrustMissions = (entrustRes.data || []).map((item) => ({
          id: item.entrustMissionId,
          title: item.title || "未命名委託",
          desc: item.detail || "無詳細說明",
          price: item.rewardPoints || 0,
          category: item.category || "其他", // ← 修正 null 問題
          deadline: item.deadline
            ? new Date(item.deadline).toLocaleString("zh-TW", {
                dateStyle: "medium",
                timeStyle: "short",
              })
            : "無期限",
          maxParticipants: item.maxParticipants || 1,
          applicants: item.currentParticipants ?? 0,
          urgent: !!item.isUrgent, // 轉 boolean，更嚴謹
          status: item.status || "open",
          requester: item.requesterName || "匿名影友",
          createdAt: item.createdAt,
        }));

        console.log("任務資料載入完成", {
          missionsCount: this.missions.length,
          entrustCount: this.entrustMissions.length,
        });
      } catch (err) {
        console.error("載入任務失敗", err);

        // 錯誤時才 fallback 到假資料（避免開發時一直看到空列表）
        this.missions = [
          {
            id: 1,
            title: "每日登入",
            desc: "今天也要看看有什麼新電影！",
            reward: 10,
            category: "日常",
            difficulty: "簡單",
            duration: "每日",
            participants: 0,
            featured: true,
          },
          // 更多假資料...
        ];

        this.entrustMissions = [
          {
            id: 101,
            title: "電影字幕校對",
            desc: "協助校對電影字幕中的錯字與時間軸",
            price: 120,
            category: "字幕",
            deadline: "3天內",
            applicants: 0,
            maxParticipants: 1,
            urgent: true,
          },
          // 更多...
        ];
      } finally {
        this.isLoading = false;
      }
    },

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

    clearFilters() {
      this.searchQuery = "";
      this.selectedCategory = "全部";
      this.selectedDifficulty = "不限難度";
      this.selectedDuration = "不限時間";
      this.sortOption = "default";
      this.showAdvancedFilters = false; // 順便關閉高級面板
    },

    // 如果你原本有 resetFilters，也可以保留或改名
    resetFilters() {
      this.clearFilters(); // 統一呼叫
    },
  },
});
