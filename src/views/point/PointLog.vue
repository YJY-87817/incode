<script setup>
import { ref, computed, onMounted } from "vue";
import axios from "axios";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

// --- 1. 全域配置與狀態 ---
const API_BASE_URL = "https://localhost:7190/api/PointLogApi"; // 建議移至 .env
const currentUserId = ref(1); // 模擬目前登入者 ID，實戰中應從 Pinia/LocalStorage 取得

// 使用者 UI 資訊
const user = computed(() => ({
  name: "Gemini User",
  id: currentUserId.value,
  level: 12,
  levelName: "白銀領主",
  avatar:
    "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?ixlib=rb-1.2.1&auto=format&fit=crop&w=200&q=80",
  exp: 64,
}));

// 資料狀態
const currentBalance = ref(0); // 餘額
const logs = ref([]); // 原始資料
const isLoading = ref(false); // 讀取狀態
const stats = ref({ monthlyEarned: 0, monthlySpent: 0 }); // 統計

// 搜尋與篩選條件
const searchQuery = ref("");
const selectedCategory = ref("全部");
const categories = ["全部", "任務", "商店", "儲值", "活動"];

// --- 2. API 串接與資料處理邏輯 ---

/**
 * 從 API 獲取資料並進行清洗轉換
 */
const fetchPointLogs = async () => {
  if (isLoading.value) return;
  isLoading.value = true;

  try {
    const response = await axios.get(
      `${API_BASE_URL}/user/${currentUserId.value}`,
    );

    // 將後端 DTO 映射為前端需要的介面格式
    logs.value = response.data.map((item) => ({
      id: item.pointLogId,
      date: formatDate(item.createdAt),
      action: getActionName(item.pointTypeDefinitionId),
      amount: item.changeAmount,
      type: item.changeAmount >= 0 ? "plus" : "minus",
      category: getCategoryName(item.pointTypeDefinitionId),
      status: item.status === 1 ? "成功" : "處理中",
      afterBalance: item.balanceAfter ?? 0, // ← 防 null
      rawDate: new Date(item.createdAt), // 存一份 Date 物件方便後續計算
    }));

    // 更新目前總餘額 (API 已按時間降序排列，故取第一筆最準確)
    if (logs.value.length > 0) {
      currentBalance.value = logs.value[0].afterBalance;
    } else {
      currentBalance.value = 0;
    }

    calculateMonthlyStats();
  } catch (error) {
    console.error("點數 API 錯誤：", error.response?.data || error.message);
    alert(`無法載入點數資料\n錯誤：${error.message}`);
  } finally {
    isLoading.value = false;
  }
};

/**
 * 統計邏輯：計算當月份的累積賺取與消耗
 */
const calculateMonthlyStats = () => {
  const now = new Date();
  const currentMonth = now.getMonth();
  const currentYear = now.getFullYear();

  // 使用 reduce 一次性跑完加總，提升效能
  const monthStats = logs.value.reduce(
    (acc, log) => {
      if (
        log.rawDate.getMonth() === currentMonth &&
        log.rawDate.getFullYear() === currentYear
      ) {
        if (log.amount > 0) acc.earned += log.amount;
        else acc.spent += Math.abs(log.amount);
      }
      return acc;
    },
    { earned: 0, spent: 0 },
  );

  stats.value.monthlyEarned = monthStats.earned;
  stats.value.monthlySpent = monthStats.spent;
};

// --- 3. 輔助工具函式 ---

const formatDate = (dateStr) => {
  if (!dateStr) return "";
  const d = new Date(dateStr);
  const pad = (n) => n.toString().padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(
    d.getHours(),
  )}:${pad(d.getMinutes())}`;
};

const getActionName = (id) => {
  const map = { 1: "每日簽到獎勵", 2: "購買神秘寶箱", 3: "線上儲值" };
  return map[id] || "系統異動";
};

const getCategoryName = (id) => {
  const map = { 1: "任務", 2: "商店", 3: "儲值" };
  return map[id] || "活動";
};

const getStatusClass = (status) => {
  return status === "成功" ? "bg-gradient-success" : "bg-gradient-secondary";
};

// --- 4. 生命週期與計算屬性 ---

onMounted(fetchPointLogs);

// 前端即時過濾邏輯
const filteredLogs = computed(() => {
  return logs.value.filter((log) => {
    const matchesSearch = log.action
      .toLowerCase()
      .includes(searchQuery.value.toLowerCase());
    const matchesCategory =
      selectedCategory.value === "全部" ||
      log.category === selectedCategory.value;
    return matchesSearch && matchesCategory;
  });
});
</script>

<template>
  <DefaultNavbar transparent />

  <div class="bg-gray-200 min-vh-100 pb-5">
    <header class="position-relative">
      <div
        class="page-header min-height-300 border-radius-xl mt-4 mx-4"
        style="
          background-image: url('https://images.unsplash.com/photo-1557683316-973673baf926?auto=format&fit=crop&w=1920&q=80');
        "
      >
        <span class="mask bg-gradient-dark opacity-6"></span>
      </div>
    </header>

    <div class="container-fluid mt-n8">
      <div class="row">
        <div class="col-lg-11 mx-auto">
          <div class="card card-body blur shadow-blur mb-4 border-0">
            <div class="row align-items-center">
              <div class="col-auto">
                <img
                  :src="user.avatar"
                  class="avatar avatar-xl border-radius-lg shadow"
                />
              </div>
              <div class="col-auto my-auto">
                <h5 class="mb-1 font-weight-bolder">{{ user.name }}</h5>
                <p class="mb-0 text-sm font-weight-bold text-secondary">
                  UserID: {{ user.id }}
                </p>
              </div>
              <div class="col-lg-4 ms-auto">
                <div class="d-flex justify-content-between mb-1">
                  <span class="text-xs font-weight-bold"
                    >等級 {{ user.level }} ({{ user.levelName }})</span
                  >
                  <span class="text-xs font-weight-bold">{{ user.exp }}%</span>
                </div>
                <div class="progress progress-xs">
                  <div
                    class="progress-bar bg-gradient-primary"
                    :style="{ width: user.exp + '%' }"
                  ></div>
                </div>
              </div>
            </div>
          </div>

          <div class="row">
            <div class="col-lg-3">
              <div class="card bg-gradient-dark mb-4 border-0 shadow-dark">
                <div class="card-body p-3 text-center">
                  <p class="text-sm mb-1 text-white opacity-8">當前可用點數</p>
                  <h2 class="text-white mb-0 font-weight-bolder">
                    {{ currentBalance.toLocaleString() }}
                  </h2>
                </div>
              </div>

              <div class="card mb-4 border-0 shadow-sm">
                <div class="card-body p-3">
                  <h6 class="font-weight-bolder mb-3">本月收支</h6>
                  <div class="d-flex justify-content-between mb-2">
                    <span class="text-sm text-secondary">累計獲得</span>
                    <span class="text-sm text-success font-weight-bold"
                      >+{{ stats.monthlyEarned }}</span
                    >
                  </div>
                  <div class="d-flex justify-content-between">
                    <span class="text-sm text-secondary">累計消耗</span>
                    <span class="text-sm text-danger font-weight-bold"
                      >-{{ stats.monthlySpent }}</span
                    >
                  </div>
                </div>
              </div>
            </div>

            <div class="col-lg-9">
              <div class="card border-0 shadow-sm">
                <div class="card-header pb-0 p-3 bg-white">
                  <div
                    class="d-md-flex align-items-center justify-content-between"
                  >
                    <h6 class="mb-0">
                      點數收支明細表 (User {{ currentUserId }})
                    </h6>
                    <div class="d-flex gap-2 mt-2 mt-md-0">
                      <input
                        v-model="searchQuery"
                        type="text"
                        class="form-control form-control-sm"
                        placeholder="搜尋項目..."
                        style="width: 150px"
                      />
                      <select
                        v-model="selectedCategory"
                        class="form-select form-select-sm"
                        style="width: 100px"
                      >
                        <option
                          v-for="cat in categories"
                          :key="cat"
                          :value="cat"
                        >
                          {{ cat }}
                        </option>
                      </select>
                    </div>
                  </div>
                </div>

                <div class="card-body px-0 pt-0 pb-2">
                  <div class="table-responsive">
                    <table class="table align-items-center mb-0">
                      <thead class="bg-gray-100">
                        <tr>
                          <th
                            class="text-uppercase text-secondary text-xxs font-weight-bolder opacity-7 ps-4"
                          >
                            時間 / 項目
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            類別
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            異動
                          </th>
                          <th
                            class="text-center text-uppercase text-dark text-xxs font-weight-bolder opacity-9"
                          >
                            餘額
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-if="isLoading">
                          <td colspan="4" class="text-center py-4">
                            讀取中...
                          </td>
                        </tr>
                        <tr v-for="log in filteredLogs" :key="log.id">
                          <td class="ps-4">
                            <p class="text-xs text-secondary mb-1">
                              {{ log.date }}
                            </p>
                            <h6 class="text-sm mb-0 font-weight-bold">
                              {{ log.action }}
                            </h6>
                            <span
                              class="badge badge-xxs"
                              :class="getStatusClass(log.status)"
                              >{{ log.status }}</span
                            >
                          </td>
                          <td class="text-center">
                            <span class="text-xs font-weight-bold">{{
                              log.category
                            }}</span>
                          </td>
                          <td class="text-center">
                            <span
                              :class="[
                                'text-sm font-weight-bolder',
                                log.type === 'plus'
                                  ? 'text-success'
                                  : 'text-danger',
                              ]"
                            >
                              {{ log.type === "plus" ? "+" : ""
                              }}{{ log.amount }}
                            </span>
                          </td>
                          <td class="text-center bg-gray-100-soft">
                            <span
                              class="text-sm font-weight-bolder text-dark"
                              >{{ log.afterBalance.toLocaleString() }}</span
                            >
                          </td>
                        </tr>
                        <tr v-if="!isLoading && filteredLogs.length === 0">
                          <td
                            colspan="4"
                            class="text-center py-5 text-secondary text-sm"
                          >
                            查無點數異動紀錄
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    <DefaultFooter class="mt-5" />
  </div>
</template>

<style scoped>
.avatar-xl {
  width: 74px;
  height: 74px;
}
.progress-xs {
  height: 6px;
}
.badge-xxs {
  font-size: 0.65rem;
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
}
.bg-gray-100-soft {
  background-color: rgba(248, 249, 250, 0.8);
}
.shadow-blur {
  backdrop-filter: saturate(200%) blur(30px);
  background-color: rgba(255, 255, 255, 0.8) !important;
}
</style>
