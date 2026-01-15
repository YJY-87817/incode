<script setup>
import { ref } from "vue";
import OrderDetail from "./OrderDetail.vue";

defineProps({
  order: Object,
});

const open = ref(false);
const toggle = () => (open.value = !open.value);
</script>

<template>
  <div class="order-card">
    <div class="order-header" @click="toggle">
      <strong>訂單編號：{{ order.orderId }}</strong>
      <span>狀態：{{ order.status }}</span>
      <span>總點數：{{ order.totalPoint }}</span>
    </div>

    <OrderDetail v-if="open" :orderId="order.orderId" />
  </div>
</template>

<style scoped>
.order-card {
  border: 1px solid #ccc;
  margin-bottom: 12px;
  padding: 10px;
}
.order-header {
  cursor: pointer;
  display: flex;
  justify-content: space-between;
}
</style>
