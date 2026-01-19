<script setup>
import { ref } from "vue";
import { getShippedDetails } from "@/Views/PointShop/services/orderapi";

const props = defineProps({
  orderDetailId: Number,
});

const open = ref(false);
const loading = ref(false);
const shipped = ref([]);

const toggle = async () => {
  open.value = !open.value;
  if (open.value && shipped.value.length === 0) {
    loading.value = true;
    try {
      const res = await getShippedDetails(props.orderDetailId);
      shipped.value = res.data;
    } finally {
      loading.value = false;
    }
  }
};
</script>

<template>
  <div class="shipped-box">
    <button @click="toggle">{{ open ? "隱藏" : "查看" }} 出貨資訊</button>

    <div v-if="open">
      <div v-if="loading">載入出貨資料...</div>

      <div v-for="ship in shipped" :key="ship.shippedDate">
        <p>商品：{{ ship.productName }}</p>
        <p>出貨日：{{ ship.shippedDate }}</p>
        <p>到貨日：{{ ship.arrivalDate || "尚未到貨" }}</p>
        <p>
          狀態：
          <span v-if="ship.isCompleted">已完成</span>
          <span v-else-if="ship.isFailed">失敗</span>
          <span v-else>運送中</span>
        </p>
      </div>
    </div>
  </div>
</template>
