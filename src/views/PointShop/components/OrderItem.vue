<script setup>
import { ref, onMounted } from "vue";
import ShippedDetail from "./ShippedDetail.vue";
import { getOrderDetails } from "@/views/PointShop/services/PointShopApi";

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
  <div class="accordion-item mb-3 border shadow-sm rounded">
    <h2 class="accordion-header">
      <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse"
        :data-bs-target="'#order-' + order.orderId">
        <div class="container-fluid">
          <div class="row w-100 align-items-center">
            <div class="col-6 col-md-3">
              <strong>#{{ order.orderId }}</strong>
            </div>
            <div class="col-6 col-md-3 text-muted small">
              {{ new Date(order.orderDate).toLocaleDateString("zh-TW") }}
            </div>
            <div class="col-6 col-md-3 text-primary fw-bold">
              <i class="bi bi-coin"></i>
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

    <div :id="'order-' + order.orderId" class="accordion-collapse collapse rounded" data-bs-parent="#orderAccordion">
      <div class="accordion-body bg-white">
        <div class="order-detail rounded">
          <div v-if="loading" class="alert alert-info">
            <div class="spinner-border spinner-border-sm me-2" role="status">
              <span class="visually-hidden">載入中...</span>
            </div>
            載入訂單明細中...
          </div>

          <div v-else class="row g-4 ">
            <div class="col-6 px-4">
              <h6 class="fw-bold border-bottom pb-2">商品明細</h6>
              <div v-for="detail in details" :key="detail.orderdetailId" class="col-12">
                <div class="d-flex align-items-start gap-3">
                  <img :src="`https://localhost:7190/img/product/${detail.imageName}`"
                    class="rounded flex-shrink-0 product-img" :alt="detail.productName" />
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
              <div class="text-muted px-4">備註: {{ order.remark }}</div>
            </div>
            <div class="col-6 px-4">
              <h6 class="fw-bold border-bottom pb-2">配送資訊</h6>
              <div v-for="detail in details" :key="detail.orderdetailId" class="col-12">
                <ShippedDetail :orderDetailId="detail.orderdetailId" />
              </div>
            </div>
          </div>
          <div class="mt-3 text-end">
            <button class="btn btn-outline-danger btn-sm me-2">取消訂單</button>
          </div>
        </div>
      </div>
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
