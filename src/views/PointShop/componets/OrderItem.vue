<script setup>
import OrderDetail from "./OrderDetail.vue";

defineProps({
  order: { type: Object, required: true },
});

const getStatusBadgeClass = (status) => {
  switch (status) {
    case "已完成":
      return "bg-primary";
    case "待出貨":
      return "bg-warning text-dark";
    case "已出貨":
      return "bg-success";
    case "已取消":
      return "bg-danger";
    default:
      return "bg-secondary";
  }
};
</script>

<template>
  <div class="accordion-item mb-3 border shadow-sm">
    <h2 class="accordion-header">
      <button
        class="accordion-button collapsed"
        type="button"
        data-bs-toggle="collapse"
        :data-bs-target="'#order-' + order.orderId"
      >
        <div class="container-fluid">
          <div class="row w-100 align-items-center">
            <div class="col-6 col-md-3">
              <strong>#{{ order.orderId }}</strong>
            </div>
            <div class="col-6 col-md-3 text-muted small">
              {{ order.orderDate }}
            </div>
            <div class="col-6 col-md-3 text-primary fw-bold">
              {{ order.totalPoint }} 點
            </div>
            <div class="col-6 col-md-3">
              <span :class="['badge', getStatusBadgeClass(order.status)]">
                {{ order.status }}
              </span>
            </div>
          </div>
        </div>
      </button>
    </h2>

    <div
      :id="'order-' + order.orderId"
      class="accordion-collapse collapse"
      data-bs-parent="#orderAccordion"
    >
      <div class="accordion-body bg-white">
        <OrderDetail :orderId="order.orderId" />
      </div>
    </div>
  </div>
</template>

<style scoped></style>
