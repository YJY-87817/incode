<script setup>
import { ref, onMounted } from "vue";
import { getOrdersByUser } from "@/views/PointShop/services/PointShopApi";
import OrderItem from "@/views/PointShop/components/OrderItem.vue";


const orders = ref([]);
const loading = ref(true);

onMounted(async () => {
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
});
</script>

<template>
  <div class="row justify-content-center">
    <div class="col-12">
      <div class="card shadow-lg border-0 rounded-4">

        <!-- Card Header with Gradient -->
        <div class="card-header p-4"
          style="background: linear-gradient(87deg, #86B0BD 0%, #6da1b3 100%); border-radius: 1rem 1rem 0 0;">
          <div class="d-flex justify-content-between align-items-center">
            <div class="text-white">
              <h5 class="mb-0 font-weight-bold d-flex align-items-center">
                <i class="bi bi-bag-check-fill me-2 fs-5"></i>
                訂單查詢
              </h5>
              <small class=" ms-1">查看您的歷史購物記錄</small>
            </div>
            <div v-if="!loading && orders.length > 0">
              <span class="badge bg-white text-dark rounded-pill px-3 py-2 shadow-sm d-flex align-items-center">
                <i class="bi bi-receipt me-1"></i> 共 {{ orders.length }} 筆
              </span>
            </div>
          </div>
        </div>

        <div class="card-body p-4 bg-light-subtle rounded-bottom-4">

          <!-- Loading State -->
          <div v-if="loading" class="text-center py-5">
            <div class="spinner-border text-info mb-3" style="width: 3rem; height: 3rem;" role="status">
              <span class="visually-hidden">載入中...</span>
            </div>
            <h5 class="text-secondary fw-normal">正在取得您的訂單資訊...</h5>
          </div>

          <!-- Empty State -->
          <div v-else-if="orders.length === 0" class="text-center py-5">
            <div class="mb-4">
              <div class="rounded-circle bg-light d-inline-flex align-items-center justify-content-center shadow-inner"
                style="width: 100px; height: 100px;">
                <i class="bi bi-cart-x text-secondary" style="font-size: 3rem;"></i>
              </div>
            </div>
            <h4 class="fw-bold text-dark mb-2">目前沒有任何訂單</h4>
            <p class="text-muted mb-4">您還沒有兌換任何商品，快去商店逛逛吧！</p>
            <router-link :to="{ name: 'PointShop' }" class="btn btn-custom-primary  btn-lg rounded-pill px-5 shadow-md">
              <i class="bi bi-cart-plus me-2"></i>前往購物
            </router-link>
          </div>

          <!-- <div v-else-if="login == false" class="text-center py-5">
            <h4 class="fw-bold text-dark mb-2">目前還沒有登入</h4>
            <p class="text-muted mb-4">您還沒有登入系統，前往登入！</p>
            <router-link :to="{ name: 'Login' }" class="btn btn-custom-primary  btn-lg rounded-pill px-5 shadow-md">
              <i class="bi bi-cart-plus me-2"></i>前往登入
            </router-link>
          </div> -->

          <!-- Orders List -->
          <div v-else class="orders-wrapper">
            <div class="accordion d-flex flex-column gap-3" id="orderAccordion">
              <OrderItem v-for="order in orders" :key="order.orderId" :order="order" />
            </div>
          </div>

        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Custom animations & refinements */
.card {
  transition: all 0.3s ease;
}

/* Empty state styling enhancement */
.shadow-inner {
  box-shadow: inset 0 2px 4px 0 rgba(0, 0, 0, 0.06);
}

.orders-wrapper {
  animation: fadeIn 0.5s ease-in-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Ensure accordion items (OrderItem) have spacing */
.accordion {
  padding: 0.5rem 0;
}

/* Loading spinner size adjustment if needed override */
.spinner-border {
  width: 3rem;
  height: 3rem;
}

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
