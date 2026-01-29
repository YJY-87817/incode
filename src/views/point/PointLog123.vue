<!-- 會員點數紀錄頁面 -->
<script setup>
import { onMounted, computed } from "vue";
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";
import FooterDefault from "@/examples/footers/FooterDefault.vue";
import PointLogList from "@/components/point/PointLogList.vue";
import { usePointLogStore } from "@/stores/point/pointLogStore";

// 使用新的 PointLog Store 來管理點數紀錄資料
const pointLogStore = usePointLogStore();

// 使用 computed 來連結 Store 裡的資料
const pointLogs = computed(() => pointLogStore.pointLogs);
const isLoading = computed(() => pointLogStore.isLoading);
const errorMsg = computed(() => pointLogStore.fetchError);

// 在組件掛載時執行 API 請求
onMounted(() => {
  // 如果還沒有資料，就去抓取點數紀錄
  if (pointLogs.value.length === 0) {
    pointLogStore.fetchPointLogs();
  }
});
</script>

<template>
  <NavbarDefault light />

  <div class="container py-4">
    <div class="row">
      <div class="col-12">
        <h2 class="text-center mb-4">我的點數紀錄</h2>

        <!-- 顯示錯誤訊息 -->
        <div v-if="errorMsg" class="alert alert-danger text-center m-5 p-4">
          {{ errorMsg }}
          <button
            class="btn btn-outline-danger ms-3"
            @click="pointLogStore.fetchPointLogs"
          >
            重試
          </button>
        </div>

        <!-- 顯示點數紀錄列表 -->
        <PointLogList :point-logs="pointLogs" :is-loading="isLoading" />
      </div>
    </div>
  </div>

  <FooterDefault />
</template>
