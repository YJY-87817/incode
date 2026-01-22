<script setup>
import { ref, onMounted } from "vue";
import { getOrdersByUser } from "@/Views/PointShop/services/PointShopApi";
import OrderItem from "./OrderItem.vue";

const props = defineProps({
  userId: Number,
});

const orders = ref([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await getOrdersByUser(props.userId);
    orders.value = res.data.data;
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="col-12">
    <h2 class="mb-4 fw-bold"><i class="bi bi-bag-check me-2"></i>訂單查詢</h2>

    <div v-if="loading" class="text-center py-5">
      <div
        class="spinner-border text-primary mb-3"
        role="status"
        style="width: 3rem; height: 3rem"
      >
        <span class="visually-hidden">載入中...</span>
      </div>
      <h5 class="text-muted fw-normal">正在取得您的訂單資訊...</h5>
    </div>

    <div v-else-if="orders.length === 0" class="text-center py-5">
      <i class="bi bi-inbox text-muted" style="font-size: 3rem"></i>
      <p class="mt-3 text-muted">目前沒有任何訂單</p>
      <button class="btn btn-primary">前往購物</button>
    </div>

    <div v-else>
      <div class="accordion" id="orderAccordion">
        <OrderItem
          v-for="order in orders"
          :key="order.orderId"
          :order="order"
        />
      </div>
    </div>
  </div>
</template>

<style scoped></style>
