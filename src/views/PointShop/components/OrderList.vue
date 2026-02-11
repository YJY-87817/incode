<script setup>
import { ref, onMounted, computed } from "vue";
import { getOrdersByUser } from "@/views/PointShop/services/PointShopApi";
import OrderItem from "@/views/PointShop/components/OrderItem.vue";
import { useAuthStore } from "@/stores/user/auth.js";

const authStore = useAuthStore();
const Islogin = authStore.isAuthenticated;
const orders = ref([]);
const loading = ref(true);

const props = defineProps({
  status: {
    type: String,
    default: "",
  },
});

// 篩選訂單
const filteredOrders = computed(() => {
  if (!props.status) {
    return orders.value; // 空字串表示顯示全部
  }
  return orders.value.filter((order) => order.status === props.status);
});

// 取得使用者訂單資料
const getordernew = async () => {
  if (!Islogin) {
    orders.value = [];
    loading.value = false;
    return;
  }
  await getOrdersByUser()
    .then((res) => {
      orders.value = res.data.data;
      loading.value = false;
    })
    .catch((error) => {
      loading.value = false;
      if (error.response && error.response.status === 401) {
        orders.value = [];
      } else {
        // console.log("取得訂單資料失敗:", error);
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
      <div
        class="card-header border-bottom-0 pt-4 px-4 mb-3"
        style="background-color: #86b0bd"
      >
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

      <!-- Not Logged In -->
      <div v-else-if="!Islogin" class="text-center bg-light card-body px-4">
        <i class="bi bi-person-lines-fill fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">您尚未登入</h4>
        <div class="text-muted">請先登入會員以查看您的訂單紀錄。</div>
        <router-link
          :to="{ name: 'Login' }"
          class="btn btn-custom-primary mt-2"
        >
          前往登入
        </router-link>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="orders.length === 0"
        class="text-center bg-light card-body px-4"
      >
        <i class="bi bi-cart-x fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">目前沒有任何訂單</h4>
        <p class="text-muted">您還沒有兌換任何商品，快去商店逛逛吧！</p>
        <router-link
          :to="{ name: 'PointShop' }"
          class="btn btn-custom-primary mt-2"
        >
          前往點數商城
        </router-link>
      </div>

      <!-- Filtered Empty State -->
      <div
        v-else-if="filteredOrders.length === 0"
        class="text-center bg-light card-body px-4"
      >
        <i class="bi bi-search fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">沒有符合條件的訂單</h4>
        <p class="text-muted">查詢條件下沒有找到相應的訂單</p>
      </div>

      <!-- Orders List -->
      <div v-else class="vstack gap-4">
        <OrderItem
          v-for="order in filteredOrders"
          :key="order.orderId"
          :order="order"
          @refresh="getordernew"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.btn-custom-primary {
  color: #145369;
  background-color: transparent;
  border: 1px solid #145369 !important;
}

.btn-custom-primary:hover {
  color: #fff;
  background-color: #145369;
  border-color: #145369;
}
</style>
