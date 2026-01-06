<script setup>
import { ref, computed } from "vue";
// 引入套版組件
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";

// 1. 使用者資料 (與點數頁面同步)
const user = ref({
  name: "Gemini User",
  balance: 2840,
  level: 12,
  avatar:
    "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=200&q=80",
});

// 2. 儲值方案設定
const plans = ref([
  { id: 1, points: 500, price: 500, popular: false, bonus: 0 },
  { id: 2, points: 1000, price: 950, popular: true, bonus: 50 },
  { id: 3, points: 3000, price: 2700, popular: false, bonus: 300 },
]);

// 3. 狀態控制
const selectedPlan = ref(null); // 紀錄選中的方案 ID 或 'custom'
const customAmount = ref(null); // 自定義點數輸入
const exchangeRate = 1; // 1 點 = 1 元

// 4. 計算屬性：計算自定義金額
const customPrice = computed(() => {
  return customAmount.value ? customAmount.value * exchangeRate : 0;
});

// 5. 方法：選擇方案
const selectPlan = (id) => {
  selectedPlan.value = id;
  customAmount.value = null; // 選擇固定方案時清空自定義輸入
};

const selectCustom = () => {
  selectedPlan.value = "custom";
};
</script>

<template>
  <DefaultNavbar transparent />

  <div class="bg-gray-200 min-vh-100 pb-5">
    <header class="position-relative">
      <div
        class="page-header min-height-200 border-radius-xl mt-4 mx-4"
        style="
          background-image: url('https://images.unsplash.com/photo-1557683316-973673baf926?auto=format&fit=crop&w=1920&q=80');
        "
      >
        <span class="mask bg-gradient-dark opacity-6"></span>
      </div>
    </header>

    <div class="container-fluid mt-n6">
      <div class="row">
        <div class="col-lg-10 mx-auto">
          <div class="card card-body blur shadow-blur mb-4">
            <div class="d-flex align-items-center">
              <img
                :src="user.avatar"
                class="avatar avatar-md border-radius-lg me-3 shadow"
              />
              <div>
                <h6 class="mb-0 font-weight-bolder">{{ user.name }}</h6>
                <p class="text-xs mb-0 text-secondary">
                  帳戶餘額：<span class="text-primary font-weight-bold"
                    >{{ user.balance.toLocaleString() }} Pts</span
                  >
                </p>
              </div>
            </div>
          </div>

          <h4 class="mb-3 ps-2">選擇儲值方案</h4>
          <div class="row">
            <div v-for="plan in plans" :key="plan.id" class="col-md-4 mb-4">
              <div
                @click="selectPlan(plan.id)"
                :class="[
                  'card shadow-lg border-radius-lg text-center p-3 cursor-pointer transition-all',
                  selectedPlan === plan.id
                    ? 'border border-primary border-2 scale-105'
                    : '',
                ]"
              >
                <div
                  v-if="plan.popular"
                  class="badge bg-gradient-primary position-absolute top-0 start-50 translate-middle"
                >
                  熱門推薦
                </div>
                <div class="card-body">
                  <h6
                    class="text-uppercase font-weight-bold text-sm text-secondary"
                  >
                    {{ plan.points }} 點數
                  </h6>
                  <h2 class="font-weight-bolder">NT$ {{ plan.price }}</h2>
                  <p
                    v-if="plan.bonus > 0"
                    class="text-success text-sm font-weight-bold mb-0"
                  >
                    贈送 {{ plan.bonus }} 點
                  </p>
                  <p v-else class="text-sm mb-0 opacity-0">-</p>
                </div>
              </div>
            </div>

            <div class="col-12 mb-4">
              <div
                @click="selectCustom"
                :class="[
                  'card shadow-lg border-radius-lg p-4 cursor-pointer',
                  selectedPlan === 'custom'
                    ? 'border border-primary border-2'
                    : '',
                ]"
              >
                <div class="row align-items-center">
                  <div class="col-md-6">
                    <h6 class="font-weight-bold text-sm text-secondary mb-3">
                      自定義儲值額度
                    </h6>
                    <div
                      class="input-group input-group-outline"
                      :class="customAmount ? 'is-filled' : ''"
                    >
                      <label class="form-label"
                        >請輸入欲儲值的點數 (最低 100)</label
                      >
                      <input
                        type="number"
                        class="form-control"
                        v-model="customAmount"
                        @focus="selectCustom"
                      />
                    </div>
                  </div>
                  <div class="col-md-6 text-center text-md-end mt-3 mt-md-0">
                    <p class="text-sm mb-0 text-secondary">預計應付金額</p>
                    <h2 class="font-weight-bolder text-primary mb-0">
                      NT$ {{ customPrice.toLocaleString() }}
                    </h2>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="card shadow-sm mt-2">
            <div class="card-header pb-0 p-3">
              <h6 class="mb-0">選擇支付方式</h6>
            </div>
            <div class="card-body p-4">
              <div class="row text-center">
                <div class="col-6 col-md-3 mb-3">
                  <button
                    class="btn btn-outline-primary w-100 py-3 mb-0 active"
                  >
                    <i class="fas fa-credit-card d-block mb-2 text-lg"></i>
                    <span class="text-xs font-weight-bold">信用卡</span>
                  </button>
                </div>
                <div class="col-6 col-md-3 mb-3">
                  <button class="btn btn-outline-secondary w-100 py-3 mb-0">
                    <i
                      class="fab fa-line d-block mb-2 text-lg text-success"
                    ></i>
                    <span class="text-xs font-weight-bold text-dark"
                      >Line Pay</span
                    >
                  </button>
                </div>
                <div class="col-6 col-md-3 mb-3">
                  <button class="btn btn-outline-secondary w-100 py-3 mb-0">
                    <i class="fas fa-wallet d-block mb-2 text-lg text-info"></i>
                    <span class="text-xs font-weight-bold text-dark"
                      >街口支付</span
                    >
                  </button>
                </div>
                <div class="col-6 col-md-3 mb-3">
                  <button class="btn btn-outline-secondary w-100 py-3 mb-0">
                    <i class="fas fa-university d-block mb-2 text-lg"></i>
                    <span class="text-xs font-weight-bold text-dark"
                      >銀行轉帳</span
                    >
                  </button>
                </div>
              </div>
              <hr class="horizontal dark my-4" />
              <div class="text-center">
                <button class="btn bg-gradient-dark btn-lg px-6 mb-0">
                  <i class="fas fa-lock me-2 text-sm"></i> 安全結帳
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
