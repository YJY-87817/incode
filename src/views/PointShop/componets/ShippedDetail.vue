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
  <div class="shipped-box mt-3">
    <button
      @click="toggle"
      :class="['btn', open ? 'btn-danger' : 'btn-primary', 'w-100']"
    >
      <i :class="['bi', open ? 'bi-eye-slash' : 'bi-eye']"></i>
      {{ open ? "隱藏" : "查看" }} 出貨資訊
    </button>

    <transition name="fade">
      <div v-if="open" class="mt-3">
        <div v-if="loading" class="alert alert-info">
          <i class="bi bi-hourglass-split"></i> 載入出貨資料...
        </div>

        <div
          v-for="ship in shipped"
          :key="ship.shippedDate"
          class="card mb-3 shadow-sm"
        >
          <div class="card-body">
            <div class="row mb-2">
              <div class="col-12">
                <h6 class="card-subtitle text-muted">
                  <i class="bi bi-box"></i> {{ ship.productName }}
                </h6>
              </div>
            </div>

            <div class="row">
              <div class="col-md-6 mb-2">
                <small class="text-muted">出貨日期</small>
                <p class="mb-0">
                  <i class="bi bi-calendar-event"></i> {{ ship.shippedDate }}
                </p>
              </div>
              <div class="col-md-6 mb-2">
                <small class="text-muted">預計到貨</small>
                <p class="mb-0">
                  <i class="bi bi-calendar-check"></i>
                  {{ ship.arrivalDate || "尚未到貨" }}
                </p>
              </div>
            </div>

            <div class="mt-3">
              <small class="text-muted">運送狀態</small>
              <p>
                <span v-if="ship.isCompleted" class="badge bg-success">
                  <i class="bi bi-check-circle"></i> 已完成
                </span>
                <span v-else-if="ship.isFailed" class="badge bg-danger">
                  <i class="bi bi-x-circle"></i> 失敗
                </span>
                <span v-else class="badge bg-warning text-dark">
                  <i class="bi bi-truck"></i> 運送中
                </span>
              </p>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.shipped-box {
  padding: 0;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.card {
  border-left: 4px solid #0d6efd;
  transition:
    transform 0.2s,
    box-shadow 0.2s;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
}

.badge {
  font-size: 0.85rem;
  padding: 0.5rem 0.75rem;
}

/* Bootstrap 預設顏色覆蓋 */
:deep(.btn.btn-primary) {
  background-color: #0d6efd !important;
  border-color: #0d6efd !important;
  color: white !important;
}

:deep(.btn.btn-primary:hover) {
  background-color: #0b5ed7 !important;
  border-color: #0a58ca !important;
}

:deep(.btn.btn-danger) {
  background-color: #dc3545 !important;
  border-color: #dc3545 !important;
  color: white !important;
}

:deep(.btn.btn-danger:hover) {
  background-color: #bb2d3b !important;
  border-color: #b02a37 !important;
}

:deep(.badge.bg-success) {
  background-color: #198754 !important;
}

:deep(.badge.bg-danger) {
  background-color: #dc3545 !important;
}

:deep(.badge.bg-warning) {
  background-color: #ffc107 !important;
  color: #212529 !important;
}

:deep(.alert.alert-info) {
  background-color: #cfe2ff !important;
  border-color: #b6d4fe !important;
  color: #084298 !important;
}
</style>
