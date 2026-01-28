<script setup>
import { ref, onMounted } from "vue";
import ShippedDetail from "@/views/PointShop/components/ShippedDetail.vue";
import { getOrderDetails } from "@/views/PointShop/services/PointShopApi";

// 引入 SweetAlert2 的套件
import CartSwal from 'sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';

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
      return "bg-completed";
    case "待出貨":
      return "bg-warning text-dark";
    case "已出貨":
      return "text-white" + " " + "" + " style=\"background-color: #86B0BD;\""
    case "已取消":
      return "bg-danger";
    default:
      return "bg-secondary";
  }
};

const canerlOrder = () => {

  CartSwal.fire({
    title: "確定要取消嗎?",
    text: "取消後商品須重新以點數兌換!",
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#3085d6",
    cancelButtonColor: "#d33",
    confirmButtonText: "是, 我要取消!",
    cancelButtonText: "返回",
  }).then((result) => {
    if (result.isConfirmed) {
      CartSwal.fire({
        title: "取消成功!",
        text: "您的訂單已取消",
        icon: "success"
      });
    }
  });
};


</script>

<template>
  <div class="order-item-card">
    <h2 class="accordion-header">
      <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse"
        :data-bs-target="'#order-' + order.orderId">
        <div class="order-header-content">
          <div class="order-id-section">
            <i class="bi bi-receipt-cutoff order-icon"></i>
            <span class="order-id">訂單 #{{ order.orderId }}</span>
          </div>
          <div class="order-info-section">
            <div class="order-date">
              <i class="bi bi-calendar3 me-1"></i>
              {{ new Date(order.orderDate).toLocaleDateString("zh-TW") }}
            </div>
            <div class="order-points">
              <i class="bi bi-coin"></i>
              <span class="points-value">{{ order.totalPoint }}</span>
              <span class="points-label">點</span>
            </div>
            <span :class="['status-badge', getStatusBadgeClass(order.status)]">
              {{ order.status }}
            </span>
          </div>
        </div>
      </button>
    </h2>

    <div :id="'order-' + order.orderId" class="accordion-collapse collapse" data-bs-parent="#orderAccordion">
      <div class="accordion-body">
        <div class="order-detail-container">
          <div v-if="loading" class="loading-detail">
            <div class="spinner-border spinner-border-sm me-2" style="color: #86B0BD" role="status">
              <span class="visually-hidden">載入中...</span>
            </div>
            <span>載入訂單明細中...</span>
          </div>

          <div v-else class="order-detail-content">
            <div class="detail-sections">
              <div class="detail-section products-section">
                <div class="section-header">
                  <i class="bi bi-box-seam me-2"></i>
                  <h6 class="section-title">商品明細</h6>
                </div>
                <div class="products-list">
                  <div v-for="detail in details" :key="detail.orderdetailId" class="product-item">
                    <div class="product-image-wrapper">
                      <img :src="`https://localhost:7190/img/product/${detail.imageName}`" class="product-image"
                        :alt="detail.productName" />
                    </div>
                    <div class="product-info">
                      <h6 class="product-name">{{ detail.productName }}</h6>
                      <div class="product-meta">
                        <span class="meta-item">
                          <i class="bi bi-box"></i>
                          數量：{{ detail.quantity }}
                        </span>
                        <span class="meta-item">
                          <i class="bi bi-coin"></i>
                          {{ detail.unitPointAtPurchase }} 點
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
                <div v-if="order.remark" class="order-remark">
                  <i class="bi bi-chat-left-text me-2"></i>
                  <span class="remark-label">備註：</span>
                  <span class="remark-text">{{ order.remark }}</span>
                </div>
              </div>

              <div class="detail-section shipping-section">
                <div class="section-header">
                  <i class="bi bi-truck me-2"></i>
                  <h6 class="section-title">配送資訊</h6>
                </div>
                <div class="shipping-list">
                  <div v-for="detail in details" :key="detail.orderdetailId" class="shipping-item">
                    <ShippedDetail :orderDetailId="detail.orderdetailId" />
                  </div>
                </div>
              </div>
            </div>

            <div class="order-actions">
              <button class="btn btn-outline-danger btn-sm action-btn" @click="canerlOrder">
                <i class="bi bi-x-circle me-1"></i>取消訂單
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.order-item-card {
  background: #ffffff;
  border: 1px solid #e9ecef;
  border-radius: 0.75rem;
  margin-bottom: 1.25rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  transition: all 0.3s ease;
  overflow: hidden;
}

.order-item-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.accordion-header {
  margin: 0;
}

.accordion-button {
  background: #ffffff;
  border: none;
  padding: 1.25rem 1.5rem;
  font-weight: 500;
  box-shadow: none;
}

.accordion-button:not(.collapsed) {
  background: #f8f9fa;
  color: inherit;
  box-shadow: none;
}

.accordion-button:focus {
  box-shadow: none;
  border-color: transparent;
}

.order-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  flex-wrap: wrap;
  gap: 1rem;
}

.order-id-section {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.order-icon {
  font-size: 1.25rem;
  color: #86B0BD;
}

.order-id {
  font-size: 1.1rem;
  font-weight: 600;
  color: #212529;
}

.order-info-section {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.order-date {
  display: flex;
  align-items: center;
  color: #6c757d;
  font-size: 0.9rem;
}

.order-points {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  color: #86B0BD;
  font-weight: 600;
}

.order-points i {
  font-size: 1.1rem;
}

.points-value {
  font-size: 1.1rem;
}

.points-label {
  font-size: 0.9rem;
  font-weight: 500;
}

.status-badge {
  padding: 0.4rem 0.9rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 500;
  white-space: nowrap;
}

.bg-completed {
  background-color: #86B0BD;
  color: white;
}

.accordion-body {
  padding: 0;
  background: #f8f9fa;
}

.order-detail-container {
  padding: 1.5rem;
}

.loading-detail {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  color: #6c757d;
  font-size: 0.95rem;
}

.order-detail-content {
  animation: fadeIn 0.3s ease-in;
}

.detail-sections {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  margin-bottom: 1.5rem;
}

.detail-section {
  background: #ffffff;
  border-radius: 0.75rem;
  padding: 1.5rem;
  border: 1px solid #e9ecef;
}

.section-header {
  display: flex;
  align-items: center;
  margin-bottom: 1.25rem;
  padding-bottom: 0.75rem;
  border-bottom: 2px solid #e9ecef;
}

.section-header i {
  color: #86B0BD;
  font-size: 1.1rem;
}

.section-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: #212529;
}

.products-list {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.product-item {
  display: flex;
  gap: 1rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid #f1f3f5;
}

.product-item:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.product-image-wrapper {
  flex-shrink: 0;
}

.product-image {
  width: 80px;
  height: 80px;
  object-fit: cover;
  border-radius: 0.5rem;
  border: 1px solid #e9ecef;
}

.product-info {
  flex: 1;
}

.product-name {
  font-size: 0.95rem;
  font-weight: 600;
  color: #212529;
  margin-bottom: 0.5rem;
  line-height: 1.4;
}

.product-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  color: #6c757d;
  font-size: 0.85rem;
}

.meta-item i {
  color: #86B0BD;
}

.order-remark {
  margin-top: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px solid #f1f3f5;
  color: #6c757d;
  font-size: 0.9rem;
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
}

.order-remark i {
  color: #86B0BD;
  margin-top: 0.15rem;
}

.remark-label {
  font-weight: 500;
}

.remark-text {
  flex: 1;
}

.shipping-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.shipping-item {
  width: 100%;
}

.order-actions {
  display: flex;
  justify-content: flex-end;
  padding-top: 1.5rem;
  border-top: 1px solid #e9ecef;
  margin-top: 1.5rem;
}

.action-btn {
  border-radius: 0.5rem;
  padding: 0.5rem 1.25rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

.action-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(220, 53, 69, 0.2);
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(5px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 768px) {
  .order-header-content {
    flex-direction: column;
    align-items: flex-start;
  }

  .order-info-section {
    width: 100%;
    justify-content: space-between;
  }

  .detail-sections {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .order-detail-container {
    padding: 1rem;
  }

  .detail-section {
    padding: 1.25rem;
  }

  .product-image {
    width: 60px;
    height: 60px;
  }
}
</style>
