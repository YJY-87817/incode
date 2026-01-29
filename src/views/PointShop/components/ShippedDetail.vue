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
  <div v-if="loading" class="d-flex justify-content-center align-items-center py-4 bg-light rounded text-muted">
    <div class="spinner-border spinner-border-sm text-primary me-2" role="status">
      <span class="visually-hidden">載入中...</span>
    </div>
    <span class="small">載入出貨資料...</span>
  </div>

  <div v-else-if="error" class="alert alert-warning d-flex align-items-center mb-0" role="alert">
    <i class="bi bi-exclamation-triangle-fill me-2 fs-5"></i>
    <div>{{ error }}</div>
  </div>

  <div v-else-if="shipped.length === 0"
    class="d-flex flex-column align-items-center justify-content-center py-4 bg-light rounded text-muted border border-dashed">
    <i class="bi bi-inbox fs-1 mb-2 text-secondary opacity-50"></i>
    <span class="small">尚無出貨資料</span>
  </div>

  <div v-else class="vstack gap-3">
    <div v-for="ship in shipped" :key="ship.shippedDate" class="card border border-light-subtle shadow-sm h-100">

      <div class="card-body">
        <div class="row g-3 mb-3">
          <div class="col-6">
            <div class="d-flex flex-column">
              <span class="text-secondary small d-flex align-items-center mb-1">
                <i class="bi bi-calendar-event me-1 text-primary"></i>
                出貨日期
              </span>
              <span class="fw-medium" :class="ship.shippedDate ? 'text-dark' : 'text-muted fst-italic'">
                {{ ship.shippedDate ? new Date(ship.shippedDate).toLocaleDateString("zh-TW") : "尚未出貨" }}
              </span>
            </div>
          </div>

          <div class="col-6">
            <div class="d-flex flex-column">
              <span class="text-secondary small d-flex align-items-center mb-1">
                <i class="bi bi-calendar-check me-1 text-primary"></i>
                預計到貨
              </span>
              <span class="fw-medium" :class="ship.arrivalDate ? 'text-dark' : 'text-muted fst-italic'">
                {{ ship.arrivalDate ? new Date(ship.arrivalDate).toLocaleDateString("zh-TW") : "尚未到貨" }}
              </span>
            </div>
          </div>
        </div>

        <div class="border-top pt-3">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-secondary small fw-medium">運送狀態</span>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <span v-if="ship.isCompleted"
              class="badge rounded-pill bg-success-subtle text-success border border-success-subtle px-3 py-2 d-flex align-items-center">
              <i class="bi bi-check-circle-fill me-1"></i>
              已完成
            </span>
            <span v-else-if="ship.isFailed"
              class="badge rounded-pill bg-danger-subtle text-danger border border-danger-subtle px-3 py-2 d-flex align-items-center">
              <i class="bi bi-x-circle-fill me-1"></i>
              失敗
            </span>
            <span v-else
              class="badge rounded-pill bg-warning-subtle text-warning-emphasis border border-warning-subtle px-3 py-2 d-flex align-items-center">
              <i class="bi bi-truck me-1"></i>
              運送中
            </span>
          </div>
        </div>

        <div v-if="ship.remark"
          class="mt-3 p-3 bg-light-subtle rounded border border-light d-flex align-items-start small text-secondary">
          <i class="bi bi-info-circle-fill me-2 text-info mt-1"></i>
          <span>{{ ship.remark }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.border-dashed {
  border-style: dashed !important;
}

/* Card hover effect */
.card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 .5rem 1rem rgba(0, 0, 0, .15) !important;
}
</style>
