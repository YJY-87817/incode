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
    <div v-if="loading">載入訂單明細中...</div>

    <div
      v-for="detail in details"
      :key="detail.orderdetailId"
      class="detail-item"
    >
      <img
        :src="`https://localhost:7190/img/product/${detail.imageName}`"
        width="80"
      />

      <div>
        <p>{{ detail.productName }}</p>
        <p>數量：{{ detail.quantity }}</p>
        <p>點數：{{ detail.unitPointAtPurchase }}</p>
      </div>

      <ShippedDetail :orderDetailId="detail.orderdetailId" />
    </div>
  </div>
</template>
