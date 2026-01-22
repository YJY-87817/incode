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
      return "bg-info text-dark";
    case "已取消":
      return "bg-danger";
    default:
      return "bg-secondary";
  }
};
</script>

<template>
  <div class="order-card card border-0 shadow-sm overflow-hidden">
    <div
      class="card-header bg-white p-3 p-md-4 cursor-pointer border-bottom-0"
      @click="toggle"
      :class="{ 'bg-light-active': open }"
    >
      <div class="row align-items-center g-3">
        <!-- Order Info -->
        <div class="col-md-4 d-flex align-items-center">
          <div
            class="icon-shape bg-primary bg-opacity-10 text-primary rounded-circle p-3 me-3 d-flex align-items-center justify-content-center"
            style="width: 48px; height: 48px"
          >
            <i class="bi bi-box-seam fs-5"></i>
          </div>
          <div>
            <h6 class="mb-0 fw-bold text-dark">訂單 #{{ order.orderId }}</h6>
            <small class="text-muted">點擊查看明細</small>
          </div>
        </div>

        <!-- Status -->
        <div class="col-md-4 text-md-center">
          <span
            class="badge rounded-pill px-3 py-2"
            :class="getStatusBadgeClass(order.status)"
          >
            {{ order.status }}
          </span>
        </div>

        <!-- Points & Toggle -->
        <div
          class="col-md-4 text-md-end d-flex align-items-center justify-content-md-end justify-content-between"
        >
          <div class="me-3">
            <span class="fw-bold text-primary fs-5">{{
              order.totalPoint
            }}</span>
            <span class="small text-muted ms-1">點</span>
          </div>
          <i
            class="bi bi-chevron-down transition-icon text-muted"
            :class="{ 'rotate-180': open }"
          ></i>
        </div>
      </div>
    </div>

    <transition name="expand">
      <div v-if="open" class="card-body bg-light border-top">
        <div class="p-2">
          <OrderDetail :orderId="order.orderId" />
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.order-card {
  transition: all 0.3s ease;
}

.order-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.08) !important;
}

.cursor-pointer {
  cursor: pointer;
}

.bg-light-active {
  background-color: #f8f9fa !important;
}

.transition-icon {
  transition: transform 0.3s ease;
}

.rotate-180 {
  transform: rotate(180deg);
}

.expand-enter-active,
.expand-leave-active {
  transition: all 0.3s ease-in-out;
  overflow: hidden;
}

.expand-enter-from {
  opacity: 0;
  max-height: 0;
}

.expand-enter-to,
.expand-leave-from {
  opacity: 1;
  max-height: 1000px;
}
</style>
