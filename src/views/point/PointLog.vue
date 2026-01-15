<script setup>
import { ref, computed, onMounted } from "vue";
import axios from "axios";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

// --- 1. 模擬登入使用者資訊 ---
// 在實際專案中，這裡的 ID 11 會從 Cookie、LocalStorage 或 Pinia/Vuex 取得
const currentUserId = ref(11);

const user = ref({
  name: "Gemini User",
  id: "UID: " + currentUserId.value, // 顯示當前 ID
  level: 12,
  levelName: "白銀領主",
  avatar:
    "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?ixlib=rb-1.2.1&auto=format&fit=crop&w=200&q=80",
  exp: 65,
});

// --- 2. 狀態定義 ---
const currentBalance = ref(0); // 當前點數結餘
const logs = ref([]); // 存放該使用者的紀錄
const stats = ref({
  // 本月收支摘要
  monthlyEarned: 0,
  monthlySpent: 0,
});

const searchQuery = ref(""); // 搜尋框
const selectedCategory = ref("全部"); // 分類篩選
const categories = ["全部", "任務", "商店", "儲值", "活動"];
const isLoading = ref(false); // 載入狀態動畫控制

// --- 3. API 串接方法 ---

/**
 * 核心方法：抓取指定使用者的點數紀錄
 * 對應後端：[HttpGet("user/{userId}")]
 */
const fetchPointLogs = async () => {
  isLoading.value = true;
  try {
    // 【重要路徑修改】：將使用者 ID 帶入 API 路徑中
    // 這樣後端 API 就只會回傳 UserId 為 11 的資料
    const url = `https://localhost:7190/api/PointLogApi/user/${currentUserId.value}`;
    const response = await axios.get(url);

    // 將後端傳回的資料 (response.data) 進行格式轉換
    logs.value = response.data.map((item) => ({
      id: item.pointLogId,
      date: formatDate(item.createdAt), // 格式化時間
      // 將 PointTypeDefinitionId 對應到人類看得懂的文字
      action: getActionName(item.pointTypeDefinitionId),
      amount: item.changeAmount,
      // 根據正負號決定前端呈現類型
      type: item.changeAmount >= 0 ? "plus" : "minus",
      category: getCategoryName(item.pointTypeDefinitionId),
      status: "成功",
      afterBalance: item.balanceAfter, // 使用後端計算好的餘額
    }));

    // 設定畫面上方的總餘額 (取最新一筆紀錄的 BalanceAfter)
    if (logs.value.length > 0) {
      currentBalance.value = logs.value[0].afterBalance;
    }

    // 重新計算側欄的本月收支
    calculateMonthlyStats();
  } catch (error) {
    console.error("API 請求發生錯誤:", error);
    alert("無法載入點數紀錄。請確認 API 伺服器已啟動且 port 正確。");
  } finally {
    isLoading.value = false;
  }
};

/**
 * 時間格式化：將 ISO 字串轉為 yyyy-MM-dd HH:mm
 */
const formatDate = (dateStr) => {
  if (!dateStr) return "";
  const d = new Date(dateStr);
  return `${d.getFullYear()}-${(d.getMonth() + 1)
    .toString()
    .padStart(2, "0")}-${d.getDate().toString().padStart(2, "0")} ${d
    .getHours()
    .toString()
    .padStart(2, "0")}:${d.getMinutes().toString().padStart(2, "0")}`;
};

/**
 * 業務邏輯對照：根據 PointTypeDefinitionId 回傳顯示名稱
 */
const getActionName = (id) => {
  const map = { 1: "每日簽到獎勵", 2: "購買神秘寶箱", 3: "線上儲值" };
  return map[id] || "系統點數異動";
};

const getCategoryName = (id) => {
  const map = { 1: "任務", 2: "商店", 3: "儲值" };
  return map[id] || "活動";
};

/**
 * 本月收支計算
 */
const calculateMonthlyStats = () => {
  const currentMonth = new Date().getMonth();
  const thisMonthLogs = logs.value.filter(
    (l) => new Date(l.date).getMonth() === currentMonth
  );

  stats.value.monthlyEarned = thisMonthLogs
    .filter((l) => l.amount > 0)
    .reduce((sum, l) => sum + l.amount, 0);

  stats.value.monthlySpent = Math.abs(
    thisMonthLogs
      .filter((l) => l.amount < 0)
      .reduce((sum, l) => sum + l.amount, 0)
  );
};

// --- 4. 生命週期與計算屬性 ---

onMounted(() => {
  fetchPointLogs(); // 頁面載入時執行
});

// 處理前端的「即時關鍵字搜尋」與「分類過濾」
const filteredLogs = computed(() => {
  return logs.value.filter((log) => {
    const matchesSearch = log.action.includes(searchQuery.value);
    const matchesCategory =
      selectedCategory.value === "全部" ||
      log.category === selectedCategory.value;
    return matchesSearch && matchesCategory;
  });
});

const getStatusClass = (status) => {
  return status === "成功" || status === "已完成"
    ? "bg-gradient-success"
    : "bg-gradient-secondary";
};
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
                  {{ user.id }}
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
                    <span class="text-sm text-secondary">本月累計獲得</span>
                    <span class="text-sm text-success font-weight-bold"
                      >+{{ stats.monthlyEarned }}</span
                    >
                  </div>
                  <div class="d-flex justify-content-between">
                    <span class="text-sm text-secondary">本月累計消耗</span>
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
                        placeholder="搜尋項目名稱..."
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
                            異動時間 / 項目說明
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            類別
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            點數異動
                          </th>
                          <th
                            class="text-center text-uppercase text-dark text-xxs font-weight-bolder opacity-9"
                          >
                            異動後結餘
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-if="isLoading">
                          <td colspan="4" class="text-center py-4">
                            <div
                              class="spinner-border spinner-border-sm text-primary"
                            ></div>
                            資料讀取中...
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
                            <span class="text-sm font-weight-bolder text-dark">
                              {{ log.afterBalance.toLocaleString() }}
                            </span>
                          </td>
                        </tr>
                        <tr v-if="!isLoading && filteredLogs.length === 0">
                          <td
                            colspan="4"
                            class="text-center py-5 text-secondary text-sm"
                          >
                            此使用者目前沒有點數變動紀錄
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
.shadow-dark {
  box-shadow: 0 4px 20px 0 rgba(0, 0, 0, 0.14),
    0 7px 10px -5px rgba(64, 64, 64, 0.4) !important;
}
</style>
