<script setup>
import { ref, computed, onMounted } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import axios from "axios";
import Swal from "sweetalert2";

// 2. 儲值方案設定
const plans = ref([
  { id: 1, points: 500, price: 50, popular: false, bonus: 0 },
  { id: 2, points: 1000, price: 95, popular: true, bonus: 50 },
  { id: 3, points: 3000, price: 270, popular: false, bonus: 300 },
]);

// 3. 狀態控制
const selectedPlan = ref(2); // 預設選中熱門方案
// const selectedPayment = ref("credit-card"); // 預設支付方式
const customAmount = ref(null);
const isLoading = ref(false);
const exchangeRate = 10;

// 5. 計算屬性
const finalDetails = computed(() => {
  if (selectedPlan.value === "custom") {
    const amt = customAmount.value || 0;
    return {
      points: Math.floor(amt * exchangeRate), // 金額轉點數
      price: amt,
      bonus: 0,
    };
  }
  const plan = plans.value.find((p) => p.id === selectedPlan.value);
  return plan || { points: 0, price: 0, bonus: 0 };
});

const isSubmitDisabled = computed(() => {
  if (
    selectedPlan.value === "custom" &&
    (!customAmount.value || customAmount.value < 10)
  )
    return true;
  return false;
});

// 6. 方法
const selectPlan = (id) => {
  selectedPlan.value = id;
  customAmount.value = null;
};

// 點確認付款 → 呼叫後端生成訂單 → 自動導到綠界
// 6. 方法 (優化後的 handleCheckout)
// 修改後的 handleCheckout 方法
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
        text: "無法啟動支付流程，請檢查網路或聯繫客服",
        confirmButtonColor: "#145369",
        customClass: { confirmButton: "text-white" },
      });
    } finally {
      isLoading.value = false;
    }
  }
};

onMounted(async () => {
  try {
    // 這裡很重要！因為 ReturnURL 是背景執行的，
    // 使用者跳回來時，可能 ReturnURL 還沒跑完。
    // 你可以延遲 1-2 秒再重新獲取餘額，或者提供一個「重新整理」按鈕。
    await userStore.fetchUserInfo();
    statusMessage.value = "感謝您的儲值！點數處理中，請稍候。";
  } catch (err) {
    console.error(err);
  }
});
</script>

<template>
  <DefaultNavbar light />
  <div class="bg-gray-200 min-vh-100 pb-5 mt-6">
    <div class="container-fluid mt-n6">
      <div class="row">
        <div class="col-lg-10 mx-auto">
          <!-- 選擇方案區 -->
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
                    <small
                      v-if="plan.bonus > 0"
                      class="text-success font-weight-bold"
                    >
                      額外贈送 {{ plan.bonus }} 點
                    </small>
                    <small v-else class="text-transparent">N/A</small>
                  </div>
                </div>
              </div>
              <!-- 自定義方案 -->
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
                          placeholder="請輸入金額"
                        />
                      </div>
                      <small
                        v-if="customAmount && customAmount < 10"
                        class="text-danger mt-1 d-block text-xs"
                      >
                        金額不可低於 10
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

          <!-- 支付方式 & 總金額 -->
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

  <!-- <form
    method="post"
    action="https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5"
  >
    <input type="hidden" name="MerchantID" value="3002599" />
    <input type="hidden" name="MerchantTradeNo" value="PR639063232747464280" />
    <input type="hidden" name="MerchantTradeDate" value="2026/02/10 12:27:56" />
    <input type="hidden" name="PaymentType" value="aio" />
    <input type="hidden" name="TotalAmount" value="122" />
    <input type="hidden" name="TradeDesc" value="PointRecharge" />
    <input type="hidden" name="ItemName" value="Point 1220" />

     ✅ 新增 ClientBackURL -->
  <!-- <input
      type="hidden"
      name="ClientBackURL"
      value="https://你的前端網址/point/recharge/result"
    /> -->

  <!-- 後端用的回呼 -->
  <!-- <input
      type="hidden"
      name="ReturnURL"
      value="https://separative-learnable-immanuel.ngrok-free.dev/api/PointRechargeApi/ecpay/return"
    /> -->

  <!-- <input type="hidden" name="ChoosePayment" value="ALL" />
    <input type="hidden" name="EncryptType" value="1" />
    <input
      type="hidden"
      name="CheckMacValue"
      value="5E8B1F4320B1559A808E49E728777AACAD991AB7C3B8DA3803E630DC81561C91"
    />
    <button type="submit">前往付款</button>
  </form>  -->
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
  box-shadow:
    inset 0 0 2px #fefefe,
    0 4px 6px -1px rgba(0, 0, 0, 0.1) !important;
}
</style>
