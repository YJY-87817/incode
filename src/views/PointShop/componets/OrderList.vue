<script setup>
import { ref, onMounted } from "vue";
import { getOrdersByUser } from "@/Views/PointShop/services/orderapi";
import OrderItem from "./OrderItem.vue";

const props = defineProps({
  userId: Number,
});

const orders = ref([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await getOrdersByUser(props.userId);
    orders.value = res.data;
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="col-12">
    <div class="d-flex align-items-center justify-content-between mb-4">
      <div>
        <h3 class="fw-bold text-dark mb-1">
          <i class="bi bi-receipt me-2 text-primary"></i>我的訂單
        </h3>
        <p class="text-muted mb-0 small">查看和管理您的所有兌換紀錄</p>
      </div>
    </div>

    <div v-if="loading" class="text-center py-5 my-3">
      <div
        class="spinner-border text-primary mb-3"
        role="status"
        style="width: 3rem; height: 3rem"
      >
        <span class="visually-hidden">載入中...</span>
      </div>
      <h5 class="text-muted fw-normal">正在取得您的訂單資訊...</h5>
    </div>

    <div
      v-else-if="orders.length === 0"
      class="card border-0 shadow-sm py-5 text-center bg-white"
    >
      <div class="card-body">
        <div class="mb-3">
          <i class="bi bi-clipboard-x display-1 text-muted opacity-25"></i>
        </div>
        <h4 class="text-muted">您目前沒有任何訂單</h4>
        <p class="text-muted mb-0">去逛逛商城，兌換您喜歡的商品吧！</p>
      </div>
    </div>

    <div v-else>
      <div class="d-flex justify-content-end mb-3">
        <span class="badge bg-light text-secondary border rounded-pill px-3">
          共 {{ orders.length }} 筆訂單
        </span>
      </div>
      <div class="d-flex flex-column gap-3">
        <OrderItem
          v-for="order in orders"
          :key="order.orderId"
          :order="order"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Bootstrap 預設顏色覆蓋 */
:deep(.text-primary) {
  color: #0d6efd !important;
}

:deep(.spinner-border.text-primary) {
  border-color: #0d6efd !important;
  border-right-color: transparent !important;
}

:deep(.badge.bg-light) {
  background-color: #f8f9fa !important;
}

:deep(.badge.text-secondary) {
  color: #6c757d !important;
}
</style>
