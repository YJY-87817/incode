<script setup>
import { ref } from "vue";
import OrderDetail from "./OrderDetail.vue";

defineProps({
  order: Object,
});

const open = ref(false);
const toggle = () => (open.value = !open.value);

const getStatusBadgeClass = (status) => {
  switch (status) {
    case "已完成":
      return "bg-success";
    case "待出貨":
      return "bg-warning text-dark";
    case "已出貨":
      return "bg-info";
    case "已取消":
      return "bg-danger";
    default:
      return "bg-secondary";
  }
};
</script>

<template>
  <div class="order-card card shadow-sm mb-3 border-0">
    <div class="order-header card-header bg-primary text-white" @click="toggle">
      <div class="row w-100 align-items-center">
        <div class="col-md-4">
          <i class="bi bi-receipt"></i>
          <strong> 訂單 #{{ order.orderId }}</strong>
        </div>
        <div class="col-md-4 text-center">
          <span class="badge" :class="getStatusBadgeClass(order.status)">
            {{ order.status }}
          </span>
        </div>
        <div class="col-md-4 text-end">
          <i class="bi bi-star"></i> {{ order.totalPoint }} 點
        </div>
      </div>
    </div>

    <transition name="expand">
      <div v-if="open" class="card-body">
        <OrderDetail :orderId="order.orderId" />
      </div>
    </transition>
  </div>
</template>

<style scoped>
.order-card {
  transition: transform 0.2s, box-shadow 0.2s;
  cursor: pointer;
}

.order-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
}

.order-header {
  cursor: pointer;
  padding: 1rem !important;
  user-select: none;
}

.order-header:hover {
  background-color: #0b5ed7 !important;
}

.expand-enter-active,
.expand-leave-active {
  transition: all 0.3s ease;
}

.expand-enter-from {
  opacity: 0;
  max-height: 0;
}

.expand-leave-to {
  opacity: 0;
  max-height: 0;
}
</style>
