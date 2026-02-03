<script setup>
import { ref, onMounted } from "vue";
import { getShippedDetails } from "@/views/PointShop/services/PointShopApi";

const props = defineProps({
  orderId: Number,
  shippedId: Number
});

const emit = defineEmits(['update:shippedId']);

const loading = ref(false);
const shipped = ref([]);
const error = ref(null);

const loadShippedDetails = async () => {
  loading.value = true;
  error.value = null;
  try {
    const res = await getShippedDetails(props.orderId);
    if (res.data.ok) {
      shipped.value = res.data.data || [];
      const newShippedId = shipped.value.length > 0 ? shipped.value[0].shippedDetailId : 0;
      emit('update:shippedId', newShippedId);
    }
  } catch (err) {
    error.value = "載入出貨資料失敗，請稍後重試";
    shipped.value = [];
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  await loadShippedDetails();
});

defineExpose({
  loadShippedDetails
});
</script>


<template>
  <div>
    <!-- Loading State -->
    <div v-if="loading" class="text-center py-3 text-muted">
      <div class="spinner-border spinner-border-sm me-2" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <span>載入出貨資訊...</span>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="alert alert-warning d-flex align-items-center mb-0" role="alert">
      <i class="bi bi-exclamation-triangle-fill me-2"></i>
      <div>{{ error }}</div>
    </div>

    <!-- Empty State -->
    <div v-else-if="shipped.length === 0" class="my-5 text-center">
      <i class="bi bi-box-seam me-2"></i>
      尚無出貨資料
    </div>

    <!-- Data List -->
    <div v-else class="list-group">
      <div v-for="ship in shipped" :key="ship.shippedDate" class="list-group-item">
        <div class="d-flex justify-content-between align-items-center mb-2">
          <h6 class="mb-0 fw-bold">出貨單</h6>
          <span v-if="ship.isCompleted" class="badge bg-success">
            <i class="bi bi-check-circle me-1"></i>已完成
          </span>
          <span v-else-if="ship.isFailed" class="badge bg-danger">
            <i class="bi bi-x-circle me-1"></i>失敗
          </span>
          <span v-else class="badge bg-warning text-dark">
            <i class="bi bi-truck me-1"></i>處理中
          </span>
        </div>

        <div class="row g-2 small text-secondary">
          <div class="col-6">
            <span class="d-block text-muted">出貨日期</span>
            <span class="text-dark">
              {{ ship.shippedDate ? new Date(ship.shippedDate).toLocaleString("zh-TW", {
                year: 'numeric',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                hour12: false,
                month: '2-digit',
              }) : "尚未出貨" }}
            </span>
          </div>
          <div class="col-6">
            <span class="d-block text-muted">預計到貨</span>
            <span class="text-dark">
              {{ ship.arrivalDate ? new Date(ship.arrivalDate).toLocaleString("zh-TW", {
                year: 'numeric',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                hour12: false,
                month: '2-digit',
              }) : "未定" }}
            </span>
          </div>
        </div>
        <div>
          <div class="alert alert-light mt-2 mb-0 p-2 small text-muted border">
            <i class="bi bi-geo-alt me-1"></i>收件人姓名：
            {{ ship.maskedName }} <br>
            <i class="bi bi-telephone me-1"></i>收件人電話：
            {{ ship.maskedPhone }} <br>
            <i class="bi bi-house-door me-1"></i>收件地址：
            {{ ship.maskedAddress }} <br>
            <i class="bi bi-info-circle me-1"></i>備註：
            {{ ship.remark }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped></style>
