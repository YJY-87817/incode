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
  <div>
    <h2>我的訂單</h2>

    <div v-if="loading">載入中...</div>

    <OrderItem v-for="order in orders" :key="order.orderId" :order="order" />
  </div>
</template>
