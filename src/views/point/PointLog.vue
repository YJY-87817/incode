<script setup>
import { ref, computed } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

// 1. 會員個人資料
const user = ref({
  name: "Gemini User",
  id: "ID: 9527123",
  level: 12,
  levelName: "白銀領主",
  avatar:
    "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?ixlib=rb-1.2.1&auto=format&fit=crop&w=200&q=80",
  exp: 65,
});

// 2. 目前總餘額 (以此為基準計算每行餘額)
const currentBalance = ref(2840);

const stats = ref({
  monthlyEarned: 1200,
  monthlySpent: 350,
});

// 3. 點數紀錄資料 (按時間倒序排列：最新的在上面)
const logs = ref([
  {
    id: 1,
    date: "2026-01-06 10:30",
    action: "每日簽到獎勵",
    amount: 50,
    type: "plus",
    category: "任務",
    status: "已完成",
  },
  {
    id: 2,
    date: "2026-01-05 18:15",
    action: "購買：神秘寶箱",
    amount: -200,
    type: "minus",
    category: "商店",
    status: "成功",
  },
  {
    id: 3,
    date: "2026-01-04 22:00",
    action: "線上儲值 - 優惠方案",
    amount: 1000,
    type: "plus",
    category: "儲值",
    status: "成功",
  },
  {
    id: 4,
    date: "2026-01-04 12:00",
    action: "週年慶活動紅包",
    amount: 150,
    type: "plus",
    category: "活動",
    status: "系統發放",
  },
  {
    id: 5,
    date: "2026-01-03 09:45",
    action: "兌換虛擬商品",
    amount: -150,
    type: "minus",
    category: "商店",
    status: "成功",
  },
]);

// 4. 計算每一項的「交易後餘額」
// 邏輯：從當前餘額開始，逐筆扣回（或加上）該筆金額來回推
const logsWithRunningBalance = computed(() => {
  let runningTotal = currentBalance.value;
  return logs.value.map((log, index) => {
    const rowBalance = runningTotal;
    // 準備下一筆（因為是倒序，所以下一行其實是「更早之前」的狀態）
    runningTotal -= log.amount;
    return { ...log, afterBalance: rowBalance };
  });
});

// 5. 搜尋與過濾
const searchQuery = ref("");
const selectedCategory = ref("全部");
const categories = ["全部", "任務", "商店", "儲值", "活動"];

const filteredLogs = computed(() => {
  return logsWithRunningBalance.value.filter((log) => {
    const matchesSearch = log.action.includes(searchQuery.value);
    const matchesCategory =
      selectedCategory.value === "全部" ||
      log.category === selectedCategory.value;
    return matchesSearch && matchesCategory;
  });
});

const getStatusClass = (status) => {
  if (status === "成功" || status === "已完成") return "bg-gradient-success";
  if (status === "系統發放") return "bg-gradient-info";
  return "bg-gradient-secondary";
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
                  <div class="mt-3">
                    <router-link
                      to="/recharge"
                      class="btn btn-sm bg-gradient-primary w-100"
                      >立即儲值</router-link
                    >
                  </div>
                </div>
              </div>

              <div class="card mb-4 border-0 shadow-sm">
                <div class="card-body p-3">
                  <h6 class="font-weight-bolder mb-3">本月摘要</h6>
                  <div class="d-flex justify-content-between mb-2">
                    <span class="text-sm text-secondary">獲得點數</span>
                    <span class="text-sm text-success font-weight-bold"
                      >+{{ stats.monthlyEarned }}</span
                    >
                  </div>
                  <div class="d-flex justify-content-between">
                    <span class="text-sm text-secondary">消耗點數</span>
                    <span class="text-sm text-danger font-weight-bold"
                      >-{{ stats.monthlySpent }}</span
                    >
                  </div>
                </div>
              </div>
              <div class="d-grid gap-2 mb-4">
                <router-link to="/recharge" class="btn bg-gradient-primary"
                  >立即儲值</router-link
                >
                <router-link to="/mission" class="btn btn-outline-primary"
                  >去做任務賺點數</router-link
                >
              </div>
            </div>

            <div class="col-lg-9">
              <div class="card border-0 shadow-sm">
                <div class="card-header pb-0 p-3 bg-white">
                  <div
                    class="d-md-flex align-items-center justify-content-between"
                  >
                    <h6 class="mb-0">收支明細與餘額對帳</h6>
                    <div class="d-flex gap-2 mt-2 mt-md-0">
                      <input
                        v-model="searchQuery"
                        type="text"
                        class="form-control form-control-sm"
                        placeholder="搜尋項目"
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
                            異動點數
                          </th>
                          <th
                            class="text-center text-uppercase text-dark text-xxs font-weight-bolder opacity-9"
                          >
                            結餘總額
                          </th>
                        </tr>
                      </thead>
                      <tbody>
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
                        <tr v-if="filteredLogs.length === 0">
                          <td
                            colspan="4"
                            class="text-center py-5 text-secondary text-sm"
                          >
                            找不到相關紀錄
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
  box-shadow: inset 0 0 2px #fefefe, 0 4px 6px -1px #0000001a !important;
}
.shadow-dark {
  box-shadow: 0 4px 20px 0 rgba(0, 0, 0, 0.14),
    0 7px 10px -5px rgba(64, 64, 64, 0.4) !important;
}
</style>
