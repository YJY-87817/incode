<script setup>
import { ref, onMounted } from "vue";
import { getOrdersByUser } from "@/views/PointShop/services/PointShopApi";
import OrderItem from "./OrderItem.vue";

const props = defineProps({
  userId: Number,
});

const orders = ref([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await getOrdersByUser(props.userId);
    orders.value = res.data.data;
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="order-list-container card shadow-sm border-0 rounded-4">
    <div class="text-center">
      <h2 class="order-title">
        <i class="bi bi-bag-check me-2"></i>訂單查詢
      </h2>
    </div>

    <div v-if="loading" class="loading-container">
      <div class="spinner-wrapper">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">載入中...</span>
        </div>
      </div>
      <h5 class="loading-text">正在取得您的訂單資訊...</h5>
    </div>

    <div v-else-if="orders.length === 0" class="empty-container">
      <div class="empty-icon">
        <i class="bi bi-inbox"></i>
      </div>
      <h4 class="empty-title">目前沒有任何訂單</h4>
      <p class="empty-description">開始購物來累積您的訂單吧！</p>
      <button class="btn btn-primary btn-lg empty-action">
        <i class="bi bi-cart-plus me-2"></i>前往購物
      </button>
    </div>

    <div v-else class="orders-wrapper">
      <div class="order-stats mb-4">
        <span class="stat-badge">
          <i class="bi bi-list-ul me-1"></i>
          共 {{ orders.length }} 筆訂單
        </span>
      </div>
      <div class="accordion" id="orderAccordion">
        <OrderItem v-for="order in orders" :key="order.orderId" :order="order" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.order-list-container {
  padding: 2rem;
  margin: 0 auto;
}


.order-title {
  font-size: 2rem;
  font-weight: 700;
  color: #212529;
  margin-bottom: 0.5rem;
  display: flex;
  align-items: center;
}

.order-title i {
  color: #0d6efd;
}

.order-subtitle {
  color: #6c757d;
  font-size: 1rem;
  margin: 0;
}

.loading-container {
  text-align: center;
  padding: 4rem 2rem;
}

.spinner-wrapper {
  display: flex;
  justify-content: center;
  margin-bottom: 1.5rem;
}

.spinner-wrapper .spinner-border {
  width: 3.5rem;
  height: 3.5rem;
  border-width: 0.3rem;
}

.loading-text {
  color: #6c757d;
  font-weight: 500;
  font-size: 1.1rem;
}

.empty-container {
  text-align: center;
  padding: 4rem 2rem;
  background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
  border-radius: 1rem;
  border: 2px dashed #dee2e6;
}

.empty-icon {
  font-size: 5rem;
  color: #adb5bd;
  margin-bottom: 1.5rem;
}

.empty-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #495057;
  margin-bottom: 0.5rem;
}

.empty-description {
  color: #6c757d;
  font-size: 1rem;
  margin-bottom: 2rem;
}

.empty-action {
  padding: 0.75rem 2rem;
  font-size: 1rem;
  border-radius: 0.5rem;
  box-shadow: 0 4px 6px rgba(13, 110, 253, 0.2);
  transition: all 0.3s ease;
}

.empty-action:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 12px rgba(13, 110, 253, 0.3);
}

.orders-wrapper {
  animation: fadeIn 0.5s ease-in;
}

.order-stats {
  display: flex;
  justify-content: flex-end;
}

.stat-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.5rem 1rem;
  background: #f8f9fa;
  border-radius: 0.5rem;
  color: #495057;
  font-size: 0.9rem;
  font-weight: 500;
  border: 1px solid #dee2e6;
}

.stat-badge i {
  color: #0d6efd;
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

@media (max-width: 768px) {
  .order-list-container {
    padding: 1rem;
  }

  .order-title {
    font-size: 1.5rem;
  }

  .order-subtitle {
    font-size: 0.9rem;
  }
}
</style>
