<script setup>
import { onMounted, onUnmounted, ref } from "vue";
import { useRouter } from "vue-router"; // 修正 1: 必須匯入 useRouter
import { usePointStore } from "@/stores/point/UsePointStore.js";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";

// 修正 2: 初始化 router 與 store
const router = useRouter();
const pointStore = usePointStore();

const countdown = ref(5); // 改回 5 秒
let timer = null; // 宣告 timer 變數

onMounted(async () => {
  // 修正 3: 正確的執行順序
  try {
    // 呼叫 API 更新資料
    if (pointStore.fetchLogs) {
      await pointStore.fetchLogs();
    }
  } catch (error) {
    console.error("更新日誌失敗:", error);
  }

  // 修正 4: 倒數計時器邏輯
  timer = setInterval(() => {
    countdown.value--;
    if (countdown.value <= 0) {
      clearInterval(timer);
      router.push("/");
    }
  }, 1000);
});

// 好的習慣：組件銷毀時清除計時器，防止記憶體洩漏
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
        <div
          class="icon-shape icon-lg bg-gradient-success shadow-success text-center border-radius-xl mt-n4 mx-auto"
        >
          <i class="fas fa-check text-white opacity-10"></i>
        </div>
        <h2 class="mt-4 font-weight-bolder">交易已完成！點數準備入帳中...</h2>
        <p class="text-secondary">
          由於金流端處理時間不一，若未即時看到點數更新，請先別擔心<br />
          請於 <strong>1-3 分鐘後</strong> 重新整理，祝您在 incode 玩得愉快！
        </p>

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
/* 確保背景是純白 */
.page-container {
  background-color: #ffffff;
  min-height: 100vh;
}

.mt-9 {
  margin-top: 9rem !important;
}

/* 限制卡片寬度，讓視覺比例集中 */
.custom-card {
  max-width: 600px; /* 根據截圖，這個寬度最為適中 */
  width: 100%;
}

/* 針對純白背景加強陰影深度與柔和度 */
.shadow-dark-deep {
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15) !important;
}

/* 確保卡片也是純白 */
.card {
  background-color: #ffffff !important;
}
</style>
