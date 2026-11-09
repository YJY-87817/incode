<script setup>
import { ref, computed } from "vue";
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import axios from "axios";
import Swal from "sweetalert2";
import { RECHARGE_ORDER_KEY } from "@/utils/rechargeOrder.js";

// 後端一律以「金額 × 10」給點（無贈點），方案的點數由金額計算，與實際入帳一致
const exchangeRate = 10;
const plans = ref(
  [
    { id: 1, price: 50, popular: false },
    { id: 2, price: 100, popular: true },
    { id: 3, price: 300, popular: false },
  ].map((p) => ({ ...p, points: p.price * exchangeRate })),
);

// 狀態控制
const selectedPlan = ref(2); // 預設選中熱門方案
const customAmount = ref(null);
const isLoading = ref(false);
// 自訂金額範圍（與後端 PointRechargeDTO 的 [Range] 一致）
const MIN_AMOUNT = 10;
const MAX_AMOUNT = 50000;

// 計算屬性
const finalDetails = computed(() => {
  if (selectedPlan.value === "custom") {
    const amt = customAmount.value || 0;
    return {
      points: Math.floor(amt * exchangeRate), // 金額轉點數
      price: amt,
    };
  }
  const plan = plans.value.find((p) => p.id === selectedPlan.value);
  return plan || { points: 0, price: 0 };
});

// 自訂金額的錯誤訊息（null 表示可送出）
const customAmountError = computed(() => {
  const amt = customAmount.value;
  if (amt === null || amt === "") return null;
  if (!Number.isInteger(Number(amt))) return "金額必須是整數";
  if (amt < MIN_AMOUNT) return `金額不可低於 ${MIN_AMOUNT}`;
  if (amt > MAX_AMOUNT) return `金額不可超過 ${MAX_AMOUNT.toLocaleString()}`;
  return null;
});

const isSubmitDisabled = computed(() => {
  if (
    selectedPlan.value === "custom" &&
    (!customAmount.value || customAmountError.value)
  )
    return true;
  return false;
});

// 方法
const selectPlan = (id) => {
  selectedPlan.value = id;
  customAmount.value = null;
};

// 點確認付款 → 呼叫後端生成訂單 → 自動導到綠界
const handleCheckout = async () => {
  if (isSubmitDisabled.value) return;

  // 1. 彈出緩衝提示 (倒數 3 秒)
  let timerInterval;
  const result = await Swal.fire({
    title: "安全跳轉中...",
    html: "正在為您加密連接綠界金流系統，請稍候 <b>3</b> 秒。",
    timer: 3000, // 3000ms = 3秒
    timerProgressBar: true,
    didOpen: () => {
      Swal.showLoading();
      const timer = Swal.getHtmlContainer().querySelector("b");
      timerInterval = setInterval(() => {
        const secondsLeft = Math.ceil(Swal.getTimerLeft() / 1000);
        timer.textContent = secondsLeft;
      }, 100);
    },
    willClose: () => {
      clearInterval(timerInterval);
    },
  });

  // 如果使用者在倒數期間沒發生錯誤，則繼續執行
  if (result.dismiss === Swal.DismissReason.timer || result.isConfirmed) {
    isLoading.value = true;

    try {
      // 2. 呼叫後端生成訂單
      const payload = {
        cashAmount: finalDetails.value.price,
        paymentMethod: "credit",
      };

      const res = await axios.post(
        `${import.meta.env.VITE_API_BASE_URL}/PointRechargeApi/recharge`,
        payload,
        { withCredentials: true },
      );

      const ecpayData = res.data;

      // 付款完成頁（RechargeResult.vue）以訂單編號查詢入帳狀態；綠界導回時不會帶訂單編號，先存起來
      try {
        sessionStorage.setItem(RECHARGE_ORDER_KEY, ecpayData.MerchantTradeNo);
      } catch {
        // 無痕模式等無法使用 sessionStorage 時，結果頁會改顯示「處理中」
      }

      // 3. 建立並提交隱藏 Form
      const form = document.createElement("form");
      form.method = "POST";
      form.action = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";

      Object.entries(ecpayData).forEach(([key, value]) => {
        const input = document.createElement("input");
        input.type = "hidden";
        input.name = key;
        input.value = String(value);
        form.appendChild(input);
      });

      document.body.appendChild(form);
      form.submit();
      document.body.removeChild(form);
    } catch (err) {
      console.error("金流串接失敗:", err);
      Swal.fire({
        icon: "error",
        title: "支付失敗",
        // 後端驗證失敗（例如金額超出範圍）時顯示後端訊息
        text:
          err.response?.data?.errors?.CashAmount?.[0] ||
          err.response?.data?.message ||
          "無法啟動支付流程，請檢查網路或聯繫客服",
        confirmButtonColor: "#145369",
        customClass: { confirmButton: "text-white" },
      });
    } finally {
      isLoading.value = false;
    }
  }
};
</script>

<template>
  <DefaultNavbar light />
  <div class="bg-gray-200 min-vh-100 pb-5 mt-6">
    <div class="container-fluid mt-n6">
      <div class="row">
        <div class="col-lg-10 mx-auto">
          <div class="container mt-4">
            <h4 class="mb-3 ps-2 font-weight-bolder">選擇方案</h4>
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
                    <h2 class="font-weight-bolder mb-0">
                      NT$ {{ plan.price }}
                    </h2>
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
                        自定義儲值額度 (最低新台幣 10 元)
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
                          :min="MIN_AMOUNT"
                          :max="MAX_AMOUNT"
                          step="1"
                          placeholder="請輸入金額"
                        />
                      </div>
                      <small
                        v-if="customAmountError"
                        class="text-danger mt-1 d-block text-xs"
                      >
                        {{ customAmountError }}
                      </small>
                    </div>
                    <div class="col-md-6 text-center text-md-end mt-3 mt-md-0">
                      <p class="text-sm mb-0 text-secondary">預計獲得點數</p>
                      <h2 class="font-weight-bolder text-primary mb-0">
                        {{ finalDetails.points.toLocaleString() }} Pts
                      </h2>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <h4 class="mb-3 ps-2 font-weight-bolder">支付金額</h4>
          <div class="card shadow-sm mb-4 border-0">
            <div class="card-body p-4">
              <div class="row justify-content-center">
                <div class="col-lg-6 text-center">
                  <div class="d-flex justify-content-between mb-2">
                    <span class="text-secondary">儲值點數：</span>
                    <span class="font-weight-bold text-dark"
                      >{{ finalDetails.points }} Pts</span
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
  box-shadow: 0 0 15px rgba(233, 30, 99, 0.1);
  background-color: rgba(233, 30, 99, 0.02);
}
.cursor-pointer {
  cursor: pointer;
}
.text-transparent {
  color: transparent;
  user-select: none;
}
</style>
