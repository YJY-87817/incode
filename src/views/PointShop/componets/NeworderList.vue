<script setup>
import { ref } from "vue";
import NewOrderItem from "./NewOrderItem.vue";

// 模擬從 API 獲取的資料
const orderData = ref([
  {
    id: "2024052088",
    date: "2024-05-20",
    total: 3780,
    status: "已出貨",
    statusClass: "bg-success",
    items: [
      {
        name: "無線降噪耳機",
        price: 2990,
        qty: 1,
        img: "https://placehold.co/60x60",
      },
      {
        name: "經典帆布鞋",
        price: 890,
        qty: 1,
        img: "https://placehold.co/60x60",
      },
    ],
    recipient: {
      name: "王小明",
      phone: "0912-345-678",
      address: "台北市信義區...",
    },
  },
  {
    id: "2024041522",
    date: "2024-04-15",
    total: 1280,
    status: "訂單完成",
    statusClass: "bg-secondary",
    items: [
      {
        name: "防水後背包",
        price: 1280,
        qty: 1,
        img: "https://placehold.co/60x60",
      },
    ],
    recipient: {
      name: "王小明",
      phone: "0912-345-678",
      address: "台北市信義區...",
    },
  },
]);
</script>

<template>
  <h2 class="mb-4 fw-bold"><i class="bi bi-bag-check me-2"></i>訂單查詢</h2>

  <div class="accordion" id="orderAccordion">
    <NewOrderItem v-for="item in orderData" :key="item.id" :order="item" />

    <div v-if="orderData.length === 0" class="text-center py-5">
      <i class="bi bi-inbox text-muted" style="font-size: 3rem"></i>
      <p class="mt-3">目前沒有任何訂單</p>
      <button class="btn btn-primary">前往購物</button>
    </div>
  </div>
</template>
