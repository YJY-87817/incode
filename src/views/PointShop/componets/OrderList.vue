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
  <div class="order-list-container p-4">
    <div class="mb-4">
      <h1 class="display-6 text-primary">
        <i class="bi bi-bag-check"></i> 我的訂單
      </h1>
      <p class="text-muted">查看和管理您的所有訂單</p>
    </div>

    <div
      v-if="loading"
      class="alert alert-info alert-dismissible fade show"
      role="alert"
    >
      <div class="spinner-border spinner-border-sm me-2" role="status">
        <span class="visually-hidden">載入中...</span>
      </div>
      <strong>載入中...</strong> 正在取得您的訂單資訊
    </div>

    <div v-else-if="orders.length === 0" class="alert alert-warning">
      <i class="bi bi-info-circle"></i> 您目前沒有任何訂單
    </div>

    <div v-else>
      <p class="text-muted mb-3">
        共 <strong>{{ orders.length }}</strong> 筆訂單
      </p>
      <OrderItem v-for="order in orders" :key="order.orderId" :order="order" />
    </div>
  </div>
</template>

<style scoped>
.order-list-container {
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  border-radius: 8px;
  min-height: 500px;
}
</style>
