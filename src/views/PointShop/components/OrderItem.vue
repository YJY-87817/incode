<script setup>
import { ref, onMounted } from "vue";
import ShippedDetail from "@/views/PointShop/components/ShippedDetail.vue";
import { getOrderDetails, putCancelOrder } from "@/views/PointShop/services/PointShopApi";

// 引入 SweetAlert2 的套件
import CartSwal from 'sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';

const props = defineProps({
  order: { type: Object, required: true },
});

const details = ref([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await getOrderDetails(props.order.orderId);
    details.value = res.data.data;
  } finally {
    loading.value = false;
  }
});

const getStatusBadgeClass = (status) => {
  switch (status) {
    case "已完成":
      return "bg-success-subtle text-success border border-success-subtle";
    case "待出貨":
      return "bg-warning-subtle text-warning-emphasis border border-warning-subtle";
    case "已出貨":
      return "bg-info-subtle text-info-emphasis border border-info-subtle";
    case "已取消":
      return "bg-danger-subtle text-danger border border-danger-subtle";
    default:
      return "bg-secondary-subtle text-secondary border border-secondary-subtle";
  }
};

const cancelOrder = (orderId) => {
  CartSwal.fire({
    title: "確定要取消嗎?",
    text: "取消後商品須重新以點數兌換!",
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#3085d6",
    cancelButtonColor: "#d33",
    confirmButtonText: "是, 我要取消!",
    cancelButtonText: "返回",
  }).then(async (result) => {
    if (result.isConfirmed) {
      CartSwal.fire({
        title: "取消成功!",
        text: "您的訂單已取消",
        icon: "success"
      });
      await putCancelOrder(orderId);
    }
  });
};
</script>

<template>
  <div class="card mb-3 border-0 shadow-sm rounded-4 overflow-hidden">
    <div class="accordion-header" :id="'heading-' + order.orderId">
      <button class="accordion-button collapsed p-4 bg-white shadow-none" type="button" data-bs-toggle="collapse"
        :data-bs-target="'#order-' + order.orderId" aria-expanded="false" :aria-controls="'order-' + order.orderId">
        <div class="w-100 me-3">
          <div class="row align-items-center g-3">
            <div class="col-md-4">
              <div class="d-flex align-items-center">
                <div class="rounded-circle bg-light p-2 me-3 text-secondary">
                  <i class="bi bi-receipt-cutoff fs-5"></i>
                </div>
                <div>
                  <h6 class="mb-0 fw-bold text-dark">訂單 #{{ order.orderId }}</h6>
                  <span class="text-muted small">
                    <i class="bi bi-calendar3 me-1"></i>
                    {{ new Date(order.orderDate).toLocaleDateString("zh-TW") }}
                  </span>
                </div>
              </div>
            </div>

            <div class="col-6 col-md-4 text-md-center">
              <div class="d-inline-flex align-items-center bg-light px-3 py-1 rounded-pill">
                <span class="text-muted small me-2">總計</span>
                <span class="fw-bold text-dark fs-5">{{ order.totalPoint }}</span>
                <span class="ms-1 small text-secondary">點</span>
              </div>
            </div>

            <div class="col-6 col-md-4 text-end">
              <span class="badge rounded-pill px-3 py-2 fw-medium" :class="getStatusBadgeClass(order.status)">
                {{ order.status }}
              </span>
            </div>
          </div>
        </div>
      </button>
    </div>

    <div :id="'order-' + order.orderId" class="accordion-collapse collapse" data-bs-parent="#orderAccordion">
      <div class="accordion-body bg-light-subtle p-0">

        <div v-if="loading" class="text-center py-5">
          <div class="spinner-border text-secondary mb-2" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p class="text-muted small mb-0">載入訂單明細中...</p>
        </div>

        <div v-else class="p-4">
          <div class="row g-4">
            <!-- Products Section -->
            <div class="col-lg-7">
              <div class="card h-100 border-0 shadow-sm">
                <div class="card-header bg-white border-bottom-0 py-3">
                  <h6 class="mb-0 fw-bold d-flex align-items-center">
                    <i class="bi bi-box-seam text-primary me-2"></i>商品明細
                  </h6>
                </div>
                <div class="card-body pt-0">
                  <div class="vstack gap-3">
                    <div v-for="detail in details" :key="detail.orderdetailId"
                      class="d-flex align-items-center border-bottom pb-3 last-no-border">
                      <div class="flex-shrink-0">
                        <img :src="`https://localhost:7190/img/product/${detail.imageName}`"
                          class="rounded border object-fit-cover" style="width: 70px; height: 70px;"
                          :alt="detail.productName" />
                      </div>
                      <div class="flex-grow-1 ms-3">
                        <h6 class="mb-1 text-dark fw-medium">{{ detail.productName }}</h6>
                        <div class="d-flex flex-wrap gap-3 text-secondary small">
                          <span><i class="bi bi-x me-1"></i>{{ detail.quantity }}</span>
                          <span class="text-primary-emphasis"><i class="bi bi-coin me-1"></i>{{
                            detail.unitPointAtPurchase }} 點</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div v-if="order.remark" class="mt-3 p-3 bg-light rounded d-flex align-items-start small text-muted">
                    <i class="bi bi-chat-left-text me-2 mt-1"></i>
                    <div>
                      <span class="fw-medium text-dark">備註：</span>{{ order.remark }}
                    </div>
                  </div>

                </div>
              </div>
            </div>

            <!-- Shipping Section -->
            <div class="col-lg-5">
              <div class="card h-100 border-0 shadow-sm">
                <div class="card-header bg-white border-bottom-0 py-3">
                  <h6 class="mb-0 fw-bold d-flex align-items-center">
                    <i class="bi bi-truck text-primary me-2"></i>配送資訊
                  </h6>
                </div>
                <div class="card-body pt-0">
                  <ShippedDetail :orderId="order.orderId" />
                </div>
              </div>
            </div>
          </div>

          <!-- Actions Footer -->
          <div class="d-flex justify-content-end mt-4 pt-3 border-top">
            <div v-if="order.status == '取消'">
              <button class="btn btn-secondary btn-sm rounded-pill px-4" disabled>
                訂單已取消
              </button>
            </div>
            <div v-else-if="order.status == '待處理'">
              <button class="btn btn-outline-danger btn-sm rounded-pill px-4" @click="cancelOrder(order.orderId)">
                取消訂單
              </button>
            </div>
            <div v-else>
              <button class="btn btn-secondary btn-sm rounded-pill px-4" disabled>
                訂單已受理
              </button>
            </div>
          </div>

        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Remove default accordion focused outline */
.accordion-button:focus {
  box-shadow: none;
  border-color: rgba(0, 0, 0, .125);
}

.accordion-button::after {
  background-size: 1rem;
  opacity: 0.5;
}

.accordion-button:not(.collapsed) {
  background-color: #fff;
  color: inherit;
}

.last-no-border:last-child {
  border-bottom: 0 !important;
  padding-bottom: 0 !important;
}

/* Transitions */
.card {
  transition: box-shadow 0.3s ease;
}

.card:hover {
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.08) !important;
}
</style>
