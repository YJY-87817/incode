<script setup>
import { onMounted, onUnmounted, ref } from "vue";
import { useRouter, useRoute } from "vue-router";
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import axios from "axios";
import { RECHARGE_ORDER_KEY } from "@/utils/rechargeOrder.js";

const router = useRouter();
const route = useRoute();

const countdown = ref(5);
let timer = null;
let cancelled = false;
const isProcessing = ref(true);
// success：已入帳、failed：付款失敗 / 取消、pending：尚未收到綠界付款通知（或查不到訂單）
const status = ref("checking");
const pointsGranted = ref(null);

// 綠界的付款通知（ReturnURL）可能比使用者導回本頁晚到，最多輪詢約 20 秒
const POLL_INTERVAL_MS = 2000;
const POLL_TIMES = 10;
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

const readOrderNo = () => {
  if (route.query.orderNo) return String(route.query.orderNo);
  try {
    return sessionStorage.getItem(RECHARGE_ORDER_KEY);
  } catch {
    return null;
  }
};

// 查詢自己的儲值訂單（GET api/PointRechargeApi/order/{orderNo}），直到有結果或逾時
const checkOrder = async (orderNo) => {
  for (let i = 0; i < POLL_TIMES && !cancelled; i++) {
    try {
      const { data } = await axios.get(
        `/PointRechargeApi/order/${encodeURIComponent(orderNo)}`,
      );
      if (data.status === "success" || data.status === "failed") {
        pointsGranted.value = data.pointsGranted;
        return data.status;
      }
    } catch (error) {
      // 404（不存在 / 不是自己的訂單）、401（未登入）不再重試；其他錯誤（網路）繼續等
      const code = error.response?.status;
      if (code === 404 || code === 401) return "pending";
    }
    await sleep(POLL_INTERVAL_MS);
  }
  return "pending";
};

onMounted(async () => {
  const orderNo = readOrderNo();
  const result = orderNo ? await checkOrder(orderNo) : "pending";
  if (cancelled) return;
  status.value = result;
  isProcessing.value = false;

  // 已有結果時清除，避免下次進入本頁顯示舊訂單
  if (result !== "pending") {
    try {
      sessionStorage.removeItem(RECHARGE_ORDER_KEY);
    } catch {
      // 忽略
    }
  }

  // 已入帳才自動返回首頁；失敗 / 處理中留在本頁讓使用者看清楚
  if (result !== "success") return;
  timer = setInterval(() => {
    countdown.value--;
    if (countdown.value <= 0) {
      clearInterval(timer);
      router.push("/");
    }
  }, 1000);
});

onUnmounted(() => {
  cancelled = true;
  if (timer) clearInterval(timer);
});
</script>

<template>
  <div class="page-container">
    <DefaultNavbar light />
    <div class="container mt-9 text-center">
      <div
        class="card shadow-dark-deep p-5 border-radius-xl border-0 mx-auto custom-card"
      >
        <div v-if="isProcessing">
          <div class="spinner-border text-primary mb-3" role="status"></div>
          <h2 class="font-weight-bolder">正在確認付款結果...</h2>
          <p class="text-secondary">請稍候片刻，正在為您更新餘額。</p>
        </div>

        <div v-else-if="status === 'success'">
          <div
            class="icon-shape icon-lg bg-gradient-success shadow-success text-center border-radius-xl mt-n4 mx-auto"
          >
            <i class="fas fa-check text-white opacity-10"></i>
          </div>
          <h2 class="mt-4 font-weight-bolder">儲值成功！</h2>
          <p class="text-secondary">
            <template v-if="pointsGranted">
              {{ pointsGranted.toLocaleString() }} 點已入帳。<br />
            </template>
            <template v-else>點數已入帳。<br /></template>
            祝您在 incode 玩得愉快！
          </p>
        </div>

        <div v-else-if="status === 'failed'">
          <div
            class="icon-shape icon-lg bg-gradient-danger shadow-danger text-center border-radius-xl mt-n4 mx-auto"
          >
            <i class="fas fa-times text-white opacity-10"></i>
          </div>
          <h2 class="mt-4 font-weight-bolder text-danger">付款未完成</h2>
          <p class="text-secondary">
            本次付款失敗或已取消，點數不會入帳。<br />
            可以回到儲值頁重新付款。
          </p>
        </div>

        <div v-else>
          <div
            class="icon-shape icon-lg bg-gradient-warning shadow-warning text-center border-radius-xl mt-n4 mx-auto"
          >
            <i class="fas fa-hourglass-half text-white opacity-10"></i>
          </div>
          <h2 class="mt-4 font-weight-bolder">系統處理中</h2>
          <p class="text-secondary">
            尚未收到付款結果，入帳可能需要幾分鐘。<br />
            請稍後到點數明細確認，若 5 分鐘後仍未入帳，請聯繫客服。
          </p>
        </div>

        <div class="mt-4">
          <button @click="router.push('/')" class="btn btn-outline-dark me-2">
            回首頁
          </button>
          <button
            @click="router.push('/point-log')"
            class="btn bg-gradient-primary"
          >
            查看點數明細
          </button>
        </div>

        <p v-if="status === 'success'" class="text-xs text-secondary mt-4">
          <span class="badge badge-secondary text-dark">{{ countdown }}</span>
          秒後自動返回首頁...
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-container {
  background-color: #f0f2f5;
  min-height: 100vh;
}
.mt-9 {
  margin-top: 9rem !important;
}
.custom-card {
  max-width: 600px;
  width: 100%;
}
.shadow-dark-deep {
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15) !important;
}
.card {
  background-color: #ffffff !important;
}
</style>
