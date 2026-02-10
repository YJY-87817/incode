<script setup>
import { onMounted, onUnmounted, ref } from "vue";
import { useRouter } from "vue-router"; // 修正 1: 必須匯入 useRouter
import { usePointStore } from "@/stores/point/UsePointStore.js";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";

// 修正 2: 初始化 router 與 store
const router = useRouter();
const pointStore = usePointStore();

const countdown = ref(5);
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
  <DefaultNavbar light />
  <div class="container mt-9 text-center">
    <div class="card shadow-lg p-5 border-radius-xl">
      <div
        class="icon-shape icon-lg bg-gradient-success shadow-success text-center border-radius-xl mt-n4 mx-auto"
      >
        <i class="fas fa-check text-white opacity-10"></i>
      </div>
      <h2 class="mt-4 font-weight-bolder">結帳手續已完成</h2>
      <p class="text-secondary">
        感謝您的購買！由於銀行與金流端作業時間不同，<br />
        點數若未即時入帳，請稍候 1-3 分鐘並重新整理頁面。
      </p>

      <div class="mt-4">
        <button @click="router.push('/')" class="btn btn-outline-dark me-2">
          回首頁
        </button>
        <button
          @click="router.push('/point-log')"
          class="btn bg-gradient-primary"
        >
          查看點數餘額
        </button>
      </div>

      <p class="text-xs text-secondary mt-4">
        <span class="badge badge-secondary text-dark">{{ countdown }}</span>
        秒後自動返回首頁...
      </p>
    </div>
  </div>
</template>

<style scoped>
.mt-9 {
  margin-top: 9rem !important;
}
</style>
