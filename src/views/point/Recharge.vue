<script setup>
import { ref, computed } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";

// 1. 使用者資料
const user = ref({
  name: "Gemini User",
  balance: 2840,
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
const selectedPlan = ref(2); // 預設選中熱門方案
const selectedPayment = ref("credit-card"); // 預設支付方式
const customAmount = ref(null);
const isLoading = ref(false);
const exchangeRate = 1;

// 4. 支付方式清單
const payments = [
  {
    id: "credit-card",
    name: "信用卡",
    icon: "fas fa-credit-card",
    color: "text-dark",
  },
  {
    id: "line-pay",
    name: "Line Pay",
    icon: "fab fa-line",
    color: "text-success",
  },
  { id: "jkos", name: "街口支付", icon: "fas fa-wallet", color: "text-info" },
  {
    id: "bank",
    name: "銀行轉帳",
    icon: "fas fa-university",
    color: "text-primary",
  },
];

// 5. 計算屬性
const finalDetails = computed(() => {
  if (selectedPlan.value === "custom") {
    const amt = customAmount.value || 0;
    return {
      points: amt,
      price: amt * exchangeRate,
      bonus: 0,
    };
  }
  const plan = plans.value.find((p) => p.id === selectedPlan.value);
  return plan || { points: 0, price: 0, bonus: 0 };
});

const isSubmitDisabled = computed(() => {
  if (
    selectedPlan.value === "custom" &&
    (!customAmount.value || customAmount.value < 100)
  )
    return true;
  return false;
});

// 6. 方法
const selectPlan = (id) => {
  selectedPlan.value = id;
  customAmount.value = null;
};

const handleCheckout = () => {
  isLoading.value = true;
  // 模擬 API 請求
  setTimeout(() => {
    isLoading.value = false;
    alert(
      `儲值成功！已選擇支付方式：${selectedPayment.value}，共支付 NT$ ${finalDetails.value.price}`
    );
  }, 2000);
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
          <div class="card card-body blur shadow-blur mb-4 border-0">
            <div class="row align-items-center">
              <div class="col-auto">
                <img
                  :src="user.avatar"
                  class="avatar avatar-md border-radius-lg shadow"
                />
              </div>
              <div class="col-auto my-auto">
                <h6 class="mb-0 font-weight-bolder">{{ user.name }}</h6>
                <p class="text-xs mb-0 text-secondary">
                  目前餘額:
                  <span class="text-primary font-weight-bold"
                    >{{ user.balance.toLocaleString() }} Pts</span
                  >
                </p>
              </div>
            </div>
          </div>

          <h4 class="mb-3 ps-2 font-weight-bolder">1. 選擇方案</h4>
          <div class="row">
            <div v-for="plan in plans" :key="plan.id" class="col-md-4 mb-4">
              <div
                @click="selectPlan(plan.id)"
                :class="[
                  'card shadow-sm border-radius-lg text-center p-3 cursor-pointer transition-all plan-card',
                  selectedPlan === plan.id
                    ? 'border border-primary border-2 ring-active'
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
                  <p
                    class="text-uppercase font-weight-bold text-xs text-secondary mb-1"
                  >
                    {{ plan.points }} 點數
                  </p>
                  <h2 class="font-weight-bolder mb-0">NT$ {{ plan.price }}</h2>
                  <small
                    v-if="plan.bonus > 0"
                    class="text-success font-weight-bold"
                    >額外贈送 {{ plan.bonus }} 點</small
                  >
                  <small v-else class="text-transparent">N/A</small>
                </div>
              </div>
            </div>

            <div class="col-12 mb-4">
              <div
                @click="selectedPlan = 'custom'"
                :class="[
                  'card shadow-sm border-radius-lg p-4 cursor-pointer transition-all',
                  selectedPlan === 'custom'
                    ? 'border border-primary border-2 ring-active'
                    : '',
                ]"
              >
                <div class="row align-items-center">
                  <div class="col-md-6">
                    <h6 class="font-weight-bold text-sm text-secondary mb-3">
                      自定義儲值額度 (最低 100)
                    </h6>
                    <div
                      class="input-group input-group-outline"
                      :class="{
                        'is-filled': customAmount,
                        'is-focused': selectedPlan === 'custom',
                      }"
                    >
                      <input
                        type="number"
                        class="form-control"
                        v-model="customAmount"
                        placeholder="請輸入金額"
                      />
                    </div>
                    <small
                      v-if="customAmount && customAmount < 100"
                      class="text-danger mt-1 d-block text-xs"
                      >金額不可低於 100</small
                    >
                  </div>
                  <div class="col-md-6 text-center text-md-end mt-3 mt-md-0">
                    <p class="text-sm mb-0 text-secondary">預計獲得點數</p>
                    <h2 class="font-weight-bolder text-primary mb-0">
                      {{ (customAmount || 0).toLocaleString() }} Pts
                    </h2>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <h4 class="mb-3 ps-2 font-weight-bolder">2. 支付方式</h4>
          <div class="card shadow-sm mb-4 border-0">
            <div class="card-body p-4">
              <div class="row">
                <div
                  v-for="pay in payments"
                  :key="pay.id"
                  class="col-6 col-md-3 mb-3"
                >
                  <button
                    @click="selectedPayment = pay.id"
                    :class="[
                      'btn w-100 py-3 mb-0 transition-all pay-btn',
                      selectedPayment === pay.id
                        ? 'btn-outline-primary active'
                        : 'btn-outline-secondary',
                    ]"
                  >
                    <i
                      :class="[pay.icon, pay.color, 'd-block mb-2 text-lg']"
                    ></i>
                    <span class="text-xs font-weight-bold d-block">{{
                      pay.name
                    }}</span>
                  </button>
                </div>
              </div>

              <hr class="horizontal dark my-4" />

              <div class="row justify-content-center">
                <div class="col-lg-6 text-center">
                  <div class="d-flex justify-content-between mb-2">
                    <span class="text-secondary">儲值點數：</span>
                    <span class="font-weight-bold text-dark"
                      >{{ finalDetails.points }} Pts</span
                    >
                  </div>
                  <div
                    class="d-flex justify-content-between mb-2"
                    v-if="finalDetails.bonus > 0"
                  >
                    <span class="text-secondary">贈送獎勵：</span>
                    <span class="font-weight-bold text-success"
                      >+{{ finalDetails.bonus }} Pts</span
                    >
                  </div>
                  <div class="d-flex justify-content-between mb-3">
                    <h5 class="font-weight-bolder">總應付金額：</h5>
                    <h5 class="font-weight-bolder text-primary">
                      NT$ {{ finalDetails.price.toLocaleString() }}
                    </h5>
                  </div>

                  <button
                    @click="handleCheckout"
                    :disabled="isSubmitDisabled || isLoading"
                    class="btn bg-gradient-dark btn-lg w-100 mt-2"
                  >
                    <span
                      v-if="isLoading"
                      class="spinner-border spinner-border-sm me-2"
                    ></span>
                    <i v-else class="fas fa-lock me-2"></i>
                    {{ isLoading ? "處理中..." : "確認付款" }}
                  </button>
                  <p class="text-xxs text-secondary mt-3">
                    點擊確認即代表您同意本站之
                    <a href="javascript:;">交易條款</a> 與
                    <a href="javascript:;">隱私權政策</a>
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.transition-all {
  transition: all 0.25s ease-in-out;
}

.plan-card:hover {
  transform: translateY(-5px);
}

.ring-active {
  box-shadow: 0 0 15px rgba(233, 30, 99, 0.1); /* Soft UI Primary Color Glow */
  background-color: rgba(233, 30, 99, 0.02);
}

.pay-btn.active {
  background-color: rgba(233, 30, 99, 0.05) !important;
  border-width: 2px !important;
}

.cursor-pointer {
  cursor: pointer;
}

.text-transparent {
  color: transparent;
  user-select: none;
}

.shadow-blur {
  backdrop-filter: saturate(200%) blur(30px);
  background-color: rgba(255, 255, 255, 0.8) !important;
  box-shadow: inset 0 0 2px #fefefe, 0 4px 6px -1px rgba(0, 0, 0, 0.1) !important;
}
</style>
