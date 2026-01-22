<script setup>
import { ref, onMounted } from "vue";
import { getOrderDetails } from "@/Views/PointShop/services/orderapi";
import ShippedDetail from "./ShippedDetail.vue";

const props = defineProps({
  orderId: Number,
  remark: String,
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

    <div v-else class="row g-4">
      <div class="col-7 px-4">
        <h6 class="fw-bold border-bottom pb-2">商品明細</h6>
        <div
          v-for="detail in details"
          :key="detail.orderdetailId"
          class="col-12"
        >
          <div class="d-flex align-items-start gap-3">
            <img
              :src="`https://localhost:7190/img/product/${detail.imageName}`"
              class="rounded flex-shrink-0 product-img"
              :alt="detail.productName"
            />
            <div class="flex-grow-1">
              <h6 class="fw-bold mb-2">{{ detail.productName }}</h6>
              <div class="d-flex flex-wrap gap-2 mb-3 text-dark">
                <small class="text-muted">
                  <i class="bi bi-box"></i> 數量：{{ detail.quantity }}
                </small>
                <small class="text-muted">
                  <i class="bi bi-coin"></i>
                  {{ detail.unitPointAtPurchase }} 點
                </small>
              </div>
            </div>
          </div>
        </div>
        <div class="text-muted px-4">備註: {{ props.remark }}</div>
      </div>
      <div class="col-5">
        <h6 class="fw-bold border-bottom pb-2">配送資訊</h6>
        <div
          v-for="detail in details"
          :key="detail.orderdetailId"
          class="col-12"
        >
          <ShippedDetail :orderDetailId="detail.orderdetailId" />
        </div>
      </div>
    </div>
    <div class="mt-3 text-end">
      <button class="btn btn-outline-danger btn-sm me-2">取消訂單</button>
    </div>
  </div>
</template>

<style scoped>
.product-img {
  width: 60px;
  height: 60px;
  object-fit: cover;
  border-radius: 5px;
  object-fit: cover;
}
</style>
