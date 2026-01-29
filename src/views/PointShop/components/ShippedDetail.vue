<script setup>
import { ref, onMounted } from "vue";
import { getShippedDetails } from "@/views/PointShop/services/PointShopApi";

const props = defineProps({
  orderId: Number,
});

const loading = ref(false);
const shipped = ref([]);
const error = ref(null);

onMounted(async () => {
  try {
    const res = await getShippedDetails(props.orderId);
    if (res.data.ok) {
      shipped.value = res.data.data || [];
    }
  } catch (err) {
    error.value = "載入出貨資料失敗，請稍後重試";
    shipped.value = [];
  } finally {
    loading.value = false;
  }
}
);
</script>

<template>
  <div v-if="loading" class="loading-state">
    <div class="spinner-border spinner-border-sm text-primary me-2" role="status">
      <span class="visually-hidden">載入中...</span>
    </div>
    <span>載入出貨資料...</span>
  </div>

  <div v-else-if="error" class="error-state">
    <i class="bi bi-exclamation-triangle-fill"></i>
    <span>{{ error }}</span>
  </div>

  <div v-else-if="shipped.length === 0" class="empty-state">
    <i class="bi bi-inbox"></i>
    <span>尚無出貨資料</span>
  </div>

  <div v-else class="shipped-cards">
    <div v-for="ship in shipped" :key="ship.shippedDate" class="shipped-card">

      <div class="card-body-section">
        <div class="info-grid">
          <div class="info-item">
            <div class="info-label">
              <i class="bi bi-calendar-event"></i>
              <span>出貨日期</span>
            </div>
            <div class="info-value" :class="{ 'text-muted': !ship.shippedDate }">
              {{
                ship.shippedDate
                  ? new Date(ship.shippedDate).toLocaleDateString("zh-TW")
                  : "尚未出貨"
              }}
            </div>
          </div>

          <div class="info-item">
            <div class="info-label">
              <i class="bi bi-calendar-check"></i>
              <span>預計到貨</span>
            </div>
            <div class="info-value" :class="{ 'text-muted': !ship.arrivalDate }">
              {{
                ship.arrivalDate
                  ? new Date(ship.arrivalDate).toLocaleDateString("zh-TW")
                  : "尚未到貨"
              }}
            </div>
          </div>
        </div>

        <div class="status-section">
          <div class="status-label">運送狀態</div>
          <div class="status-badges">
            <span v-if="ship.isCompleted" class="status-badge status-completed">
              <i class="bi bi-check-circle-fill"></i>
              <span>已完成</span>
            </span>
            <span v-else-if="ship.isFailed" class="status-badge status-failed">
              <i class="bi bi-x-circle-fill"></i>
              <span>失敗</span>
            </span>
            <span v-else class="status-badge status-shipping">
              <i class="bi bi-truck"></i>
              <span>運送中</span>
            </span>
          </div>
        </div>

        <div v-if="ship.remark" class="remark-section">
          <i class="bi bi-info-circle-fill"></i>
          <span>{{ ship.remark }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.shipped-detail-wrapper {
  width: 100%;
}

.toggle-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.75rem 1rem;
  background: #f8f9fa;
  border: 1px solid #dee2e6;
  border-radius: 0.5rem;
  color: #495057;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.toggle-btn:hover {
  background: #e9ecef;
  border-color: #adb5bd;
  color: #212529;
}

.toggle-btn.active {
  background: #e7f3ff;
  border-color: #0d6efd;
  color: #0d6efd;
}

.toggle-btn i {
  font-size: 0.85rem;
  transition: transform 0.2s ease;
}

.toggle-btn.active i {
  transform: rotate(180deg);
}

.shipped-content {
  margin-top: 1rem;
  animation: fadeIn 0.3s ease-in;
}

.loading-state,
.error-state,
.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 1.5rem;
  background: #ffffff;
  border: 1px solid #e9ecef;
  border-radius: 0.5rem;
  color: #6c757d;
  font-size: 0.9rem;
}

.error-state {
  background: #fff3cd;
  border-color: #ffc107;
  color: #856404;
}

.error-state i {
  color: #ffc107;
  font-size: 1.1rem;
}

.empty-state {
  background: #f8f9fa;
  color: #adb5bd;
}

.empty-state i {
  font-size: 1.5rem;
}

.shipped-cards {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.shipped-card {
  background: #ffffff;
  border: 1px solid #e9ecef;
  border-radius: 0.75rem;
  overflow: hidden;
  transition: all 0.3s ease;
}

.shipped-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  transform: translateY(-2px);
  border-color: #0d6efd;
}

.card-header-section {
  padding: 1rem 1.25rem;
  background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
  border-bottom: 1px solid #e9ecef;
}

.product-name-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.4rem 0.75rem;
  background: #e7f3ff;
  border-radius: 0.5rem;
  color: #0d6efd;
  font-size: 0.85rem;
  font-weight: 500;
}

.product-name-badge i {
  font-size: 0.9rem;
}

.card-body-section {
  padding: 1.25rem;
}

.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
  margin-bottom: 1.25rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.info-label {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.8rem;
  color: #6c757d;
  font-weight: 500;
}

.info-label i {
  color: #0d6efd;
  font-size: 0.9rem;
}

.info-value {
  font-size: 0.95rem;
  font-weight: 600;
  color: #212529;
}

.status-section {
  padding-top: 1.25rem;
  border-top: 1px solid #f1f3f5;
}

.status-label {
  font-size: 0.8rem;
  color: #6c757d;
  font-weight: 500;
  margin-bottom: 0.75rem;
}

.status-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 0.9rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 500;
  white-space: nowrap;
}

.status-badge i {
  font-size: 0.9rem;
}

.status-completed {
  background: #d1e7dd;
  color: #0f5132;
}

.status-completed i {
  color: #198754;
}

.status-failed {
  background: #f8d7da;
  color: #842029;
}

.status-failed i {
  color: #dc3545;
}

.status-shipping {
  background: #fff3cd;
  color: #856404;
}

.status-shipping i {
  color: #ffc107;
}

.remark-section {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin-top: 1rem;
  padding: 0.75rem;
  background: #f8f9fa;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  color: #495057;
  line-height: 1.5;
}

.remark-section i {
  color: #0d6efd;
  margin-top: 0.15rem;
  flex-shrink: 0;
}

.slide-fade-enter-active {
  transition: all 0.3s ease-out;
}

.slide-fade-leave-active {
  transition: all 0.2s ease-in;
}

.slide-fade-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.slide-fade-leave-to {
  opacity: 0;
  transform: translateY(-5px);
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@media (max-width: 768px) {
  .info-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }

  .card-body-section {
    padding: 1rem;
  }

  .card-header-section {
    padding: 0.875rem 1rem;
  }
}
</style>
