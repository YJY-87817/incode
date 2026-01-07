<script setup>
import { ref } from "vue";
// 這裡可以引入套版內建的 Navbar 或 Footer
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
  exp: 65, // 經驗值百分比
});

// 2. 數據統計
const stats = ref({
  balance: 2840,
  monthlyEarned: 1200,
  monthlySpent: 350,
});

// 3. 點數紀錄資料
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
</script>

<template>
  <DefaultNavbar transparent />

  <div class="bg-gray-200 min-vh-100">
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
        <div class="col-lg-10 mx-auto">
          <div class="card card-body blur shadow-blur overflow-hidden mb-4">
            <div class="row gx-4 align-items-center">
              <div class="col-auto">
                <div class="avatar avatar-xl position-relative">
                  <img
                    :src="user.avatar"
                    alt="profile_image"
                    class="w-100 border-radius-lg shadow-sm"
                  />
                </div>
              </div>
              <div class="col-auto my-auto">
                <div class="h-100">
                  <h5 class="mb-1 font-weight-bolder">{{ user.name }}</h5>
                  <p class="mb-0 font-weight-bold text-sm text-secondary">
                    {{ user.id }}
                  </p>
                </div>
              </div>
              <div
                class="col-lg-4 col-md-6 my-sm-auto ms-sm-auto me-sm-0 mx-auto mt-3"
              >
                <div class="nav-wrapper position-relative">
                  <div class="d-flex justify-content-between mb-1">
                    <span class="text-xs font-weight-bold"
                      >等級 {{ user.level }} · {{ user.levelName }}</span
                    >
                    <span class="text-xs font-weight-bold"
                      >{{ user.exp }}%</span
                    >
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
          </div>

          <div class="row">
            <div class="col-lg-4">
              <div class="card bg-gradient-dark mb-4">
                <div class="card-body p-3">
                  <div class="row">
                    <div class="col-8">
                      <p
                        class="text-sm mb-0 text-capitalize font-weight-bold text-white opacity-7"
                      >
                        可用點數總額
                      </p>
                      <h3 class="text-white mb-0 font-weight-bolder">
                        {{ stats.balance.toLocaleString() }}
                        <small class="text-sm">Pts</small>
                      </h3>
                    </div>
                    <div class="col-4 text-end">
                      <div
                        class="icon icon-shape bg-gradient-primary shadow text-center border-radius-md mt-1"
                      >
                        <i class="fas fa-coins text-white opacity-10"></i>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="card mb-4 shadow-sm">
                <div class="card-body p-3">
                  <h6 class="font-weight-bolder mb-3">本月動態</h6>
                  <div class="d-flex mb-3">
                    <div
                      class="icon icon-shape icon-xs bg-light-success shadow-none text-center border-radius-sm me-3"
                    >
                      <i class="fas fa-arrow-up text-success text-xs"></i>
                    </div>
                    <p class="text-sm mb-0">
                      本月獲得：<span class="text-dark font-weight-bold"
                        >+{{ stats.monthlyEarned }}</span
                      >
                    </p>
                  </div>
                  <div class="d-flex">
                    <div
                      class="icon icon-shape icon-xs bg-light-danger shadow-none text-center border-radius-sm me-3"
                    >
                      <i class="fas fa-arrow-down text-danger text-xs"></i>
                    </div>
                    <p class="text-sm mb-0">
                      本月支出：<span class="text-dark font-weight-bold"
                        >-{{ stats.monthlySpent }}</span
                      >
                    </p>
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

            <div class="col-lg-8">
              <div class="card shadow-sm">
                <div class="card-header pb-0 p-3">
                  <div class="d-flex justify-content-between">
                    <h6 class="mb-0">收支明細清單</h6>
                    <button class="btn btn-link text-dark text-sm mb-0 px-0">
                      <i class="fas fa-file-pdf me-1"></i> 匯出帳單
                    </button>
                  </div>
                </div>
                <div class="card-body p-3">
                  <div class="table-responsive">
                    <table class="table align-items-center mb-0">
                      <thead>
                        <tr>
                          <th
                            class="text-uppercase text-secondary text-xxs font-weight-bolder opacity-7 ps-2"
                          >
                            項目
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            類別
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            日期
                          </th>
                          <th
                            class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
                          >
                            金額
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="log in logs" :key="log.id">
                          <td>
                            <div class="d-flex px-2 py-1">
                              <div
                                class="d-flex flex-column justify-content-center"
                              >
                                <h6 class="mb-0 text-sm font-weight-bold">
                                  {{ log.action }}
                                </h6>
                                <p class="text-xs text-secondary mb-0">
                                  {{ log.status }}
                                </p>
                              </div>
                            </div>
                          </td>
                          <td class="align-middle text-center text-sm">
                            <span
                              class="badge badge-sm bg-gradient-light text-dark"
                              >{{ log.category }}</span
                            >
                          </td>
                          <td class="align-middle text-center">
                            <span
                              class="text-secondary text-xs font-weight-bold"
                              >{{ log.date }}</span
                            >
                          </td>
                          <td class="align-middle text-center">
                            <span
                              :class="[
                                'text-sm font-weight-bold',
                                log.type === 'plus'
                                  ? 'text-success'
                                  : 'text-danger',
                              ]"
                            >
                              {{ log.type === "plus" ? "+" : ""
                              }}{{ log.amount }}
                            </span>
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
.bg-light-success {
  background-color: #e8f5e9;
}
.bg-light-danger {
  background-color: #ffebee;
}
.progress-xs {
  height: 6px;
}

/* 讓表格在小螢幕也能滑動 */
.table-responsive {
  overflow-x: auto;
}

/* 玻璃擬態效果 */
.shadow-blur {
  box-shadow: inset 0 0 2px #fefefed1, 0 4px 6px -1px #0000001a,
    0 2px 4px -1px #0000000f !important;
}
.blur {
  backdrop-filter: saturate(200%) blur(30px);
  background-color: rgba(255, 255, 255, 0.8) !important;
}
</style>
