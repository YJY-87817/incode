<script setup>
import { ref, onMounted } from "vue";
import { getOrderDetails } from "@/Views/PointShop/services/orderapi";
import ShippedDetail from "./ShippedDetail.vue";

const props = defineProps({
  orderId: Number,
});

const details = ref([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await getOrderDetails(props.orderId);
    details.value = res.data;
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="order-detail">
    <div v-if="loading" class="alert alert-info">
      <div class="spinner-border spinner-border-sm me-2" role="status">
        <span class="visually-hidden">載入中...</span>
      </div>
      載入訂單明細中...
    </div>

    <div v-else class="row g-3">
      <div v-for="detail in details" :key="detail.orderdetailId" class="col-12">
        <div class="detail-item card border-0 shadow-sm">
          <div class="card-body">
            <div class="row align-items-center">
              <div class="col-auto">
                <img
                  :src="`https://localhost:7190/img/product/${detail.imageName}`"
                  width="100"
                  height="100"
                  class="rounded object-fit-cover"
                  :alt="detail.productName"
                />
              </div>

              <div class="col">
                <h5 class="card-title mb-2">{{ detail.productName }}</h5>
                <div class="row g-2">
                  <div class="col-auto">
                    <span class="badge bg-info">
                      <i class="bi bi-box"></i> 數量：{{ detail.quantity }}
                    </span>
                  </div>
                  <div class="col-auto">
                    <span class="badge bg-warning text-dark">
                      <i class="bi bi-star-fill"></i>
                      {{ detail.unitPointAtPurchase }} 點
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <div class="mt-3 border-top pt-3">
              <ShippedDetail :orderDetailId="detail.orderdetailId" />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.detail-item {
  transition:
    transform 0.2s,
    box-shadow 0.2s;
}

.detail-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
}

.object-fit-cover {
  object-fit: cover;
}

/* Bootstrap 預設顏色覆蓋 */
:deep(.badge.bg-info) {
  background-color: #0dcaf0 !important;
}

:deep(.badge.bg-warning) {
  background-color: #ffc107 !important;
  color: #212529 !important;
}

:deep(.alert.alert-info) {
  background-color: #cfe2ff !important;
  border-color: #b6d4fe !important;
  color: #084298 !important;
}
</style>
