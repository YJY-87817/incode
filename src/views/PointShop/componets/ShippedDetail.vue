<script setup>
import { ref } from "vue";
import { getShippedDetails } from "@/Views/PointShop/services/orderapi";

const props = defineProps({
  orderDetailId: Number,
});

const open = ref(false);
const loading = ref(false);
const shipped = ref([]);
const error = ref(null);

const toggle = async () => {
  open.value = !open.value;
  if (open.value && shipped.value.length === 0) {
    loading.value = true;
    error.value = null;
    try {
      const res = await getShippedDetails(props.orderDetailId);
      if (res.data.ok) {
        shipped.value = res.data.data || [];
      } else {
        error.value = res.data.message || "找不到出貨資料目前還沒寄送";
        shipped.value = [];
      }
    } catch (err) {
      error.value = "載入出貨資料失敗，請稍後重試";
      shipped.value = [];
    } finally {
      loading.value = false;
    }
  }
};
</script>

<template>
  <div class="shipped-box p-2">
    <button
      @click="toggle"
      :class="['btn', open ? 'btn-success' : 'btn-secondary']"
    >
      {{ open ? "隱藏" : "查看" }} 出貨資訊
    </button>

    <transition name="fade">
      <div v-if="open" class="mt-3">
        <div v-if="loading" class="alert alert-info">
          <i class="bi bi-hourglass-split"></i> 載入出貨資料...
        </div>

        <div v-if="error" class="alert alert-warning">
          <i class="bi bi-exclamation-triangle"></i> {{ error }}
        </div>

        <div
          v-for="ship in shipped"
          :key="ship.shippedDate"
          class="card border mb-3"
        >
          <div class="card-body">
            <div class="row mb-2">
              <div class="col-12">
                <h6 class="card-subtitle text-muted">
                  商品:{{ ship.productName }}
                </h6>
              </div>
            </div>

            <div class="row">
              <div class="col-md-6 mb-1">
                <small class="text-muted">出貨日期</small>
                <p class="mb-0 text-muted">
                  <i class="bi bi-calendar-event"></i>
                  {{ new Date(ship.shippedDate).toLocaleDateString("zh-TW") }}
                </p>
              </div>
              <div class="col-md-6 mb-2">
                <small class="text-muted">預計到貨</small>
                <p class="mb-0 text-muted">
                  <i class="bi bi-calendar-check"></i>
                  {{
                    ship.arrivalDate
                      ? new Date(ship.arrivalDate).toLocaleDateString("zh-TW")
                      : "尚未到貨"
                  }}
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
              <small v-if="ship.remark" class="text-muted d-block">
                <i class="bi bi-info-circle"></i> {{ ship.remark }}
              </small>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.card {
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
</style>
