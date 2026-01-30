<script setup>
import { ref, onMounted } from "vue";
import { getOrdersByUser } from "@/views/PointShop/services/PointShopApi";
import OrderItem from "@/views/PointShop/components/OrderItem.vue";


const orders = ref([]);
const loading = ref(true);
// 取得使用者訂單資料
const getordernew = async () => {
  await getOrdersByUser().then(res => {
    orders.value = res.data.data
    loading.value = false
  }).catch(error => {
    loading.value = false
    if (error.response && error.response.status === 401) {
      orders.value = []
    } else {
      console.log("取得訂單資料失敗:", error);
    }
  });
};

onMounted(() => {
  getordernew();
});
</script>


<template>
  <div class="col-12">
    <div class="card shadow-sm border-0 rounded-4">
      <!-- Header -->
      <div class="card-header border-bottom-0 pt-4 px-4 mb-3 " style="background-color: #86B0BD;">
        <h2 class="fw-bold mb-1">
          <i class="bi bi-person-lines-fill me-2"></i>訂單查詢
        </h2>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="text-center card-body px-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">正在取得您的訂單...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="orders.length === 0" class="text-center bg-light card-body px-4">
        <i class="bi bi-cart-x fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">目前沒有任何訂單</h4>
        <p class="text-muted">您還沒有兌換任何商品，快去商店逛逛吧！</p>
        <router-link :to="{ name: 'PointShop' }" class="btn btn-primary mt-2">
          前往購物
        </router-link>
      </div>

      <!-- Orders List -->
      <div v-else class="vstack gap-4">
        <OrderItem v-for="order in orders" :key="order.orderId" :order="order" @refresh="getordernew" />
      </div>
    </div>
  </div>
</template>

<style scoped></style>
