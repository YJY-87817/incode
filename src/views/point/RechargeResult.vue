<script setup>
import { onMounted, onUnmounted, ref } from "vue";
import { useRouter, useRoute } from "vue-router";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import axios from "axios";
// 如果需要更新點數餘額显示，這裡會需要一個專門查餘額的 API
// import { usePointStore } from "@/stores/point/UsePointStore.js";

const router = useRouter();
const route = useRoute();
// const pointStore = usePointStore();

const countdown = ref(5);
let timer = null;
const isProcessing = ref(true);
const status = ref("checking"); // checking, success, failed

onMounted(async () => {
  // 核心邏輯：等待 2 秒，讓後端的 ReturnURL 有時間跑完
  await new Promise((resolve) => setTimeout(resolve, 2000));

  try {
    // 這裡通常需要一個 API 來根據 URL 參數查訂單
    // 例如網址有 ?OrderNo=...
    const orderNo = route.query.OrderNo;

    // 範例：呼叫後端 API 檢查該訂單狀態
    // const res = await axios.get(`${import.meta.env.VITE_API_BASE_URL}/PointRechargeApi/check-order/${orderNo}`);

    // 假設 API 檢查結果是成功
    status.value = "success";

    // 如果有 userStore，這裡應該更新餘額，但依照需求這裡不用。
    // if (pointStore.fetchBalance) {
    //   await pointStore.fetchBalance();
    // }
  } catch (error) {
    console.error("檢查狀態失敗:", error);
    status.value = "failed";
  } finally {
    isProcessing.value = false;
  }

  // 倒數計時器
  timer = setInterval(() => {
    countdown.value--;
    if (countdown.value <= 0) {
      clearInterval(timer);
      router.push("/");
    }
  }, 1000);
});

onUnmounted(() => {
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
            點數已即時入帳。<br />
            祝您在 incode 玩得愉快！
          </p>
        </div>

        <div v-else>
          <div
            class="icon-shape icon-lg bg-gradient-danger shadow-danger text-center border-radius-xl mt-n4 mx-auto"
          >
            <i class="fas fa-times text-white opacity-10"></i>
          </div>
          <h2 class="mt-4 font-weight-bolder text-danger">系統處理中</h2>
          <p class="text-secondary">若 5 分鐘後仍未入帳，請聯繫客服。</p>
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

        <p class="text-xs text-secondary mt-4">
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
