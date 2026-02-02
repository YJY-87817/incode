<script setup>
import { ref, onMounted } from "vue";
import ShippedDetail from "@/views/PointShop/components/ShippedDetail.vue";
import { getOrderDetails, putCancelOrder } from "@/views/PointShop/services/PointShopApi";

// 引入 SweetAlert2 的套件
import CartSwal from 'sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';

const emit = defineEmits(['refresh']);
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
      return "bg-success-subtle text-success border border-success-subtle";
    case "待出貨":
      return "bg-warning-subtle text-warning-emphasis border border-warning-subtle";
    case "已出貨":
      return "bg-info-subtle text-info-emphasis border border-info-subtle";
    case "已取消":
      return "bg-danger-subtle text-danger border border-danger-subtle";
    default:
      return "bg-secondary-subtle text-secondary border border-secondary-subtle";
  }
};

const cancelOrder = (orderId) => {
  CartSwal.fire({
    title: "確定要取消嗎?",
    text: "取消後商品須重新以點數兌換!",
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#3085d6",
    cancelButtonColor: "#d33",
    confirmButtonText: "是, 我要取消!",
    cancelButtonText: "返回",
  }).then(async (result) => {
    // 修正：只有在點擊「確認」後才呼叫 API
    if (result.isConfirmed) {
      try {
        await putCancelOrder(orderId); // 執行 API

        await CartSwal.fire({
          title: "取消成功!",
          text: "您的訂單已取消",
          icon: "success"
        });

        emit('refresh'); // 通知父元件重整
      } catch (error) {
        console.error("取消失敗:", error);
        // 可以額外加一個失敗的 Swal 提醒
        await CartSwal.fire({
          title: "取消失敗!",
          text: "請稍後再試",
          icon: "error"
        });
      }
    }
  });
};
</script>


<template>
  <div class="card mb-3">
    <!-- Card Header: Order Info - Click to expand -->
    <div class="card-header bg-white py-3 d-flex flex-wrap justify-content-between align-items-center"
      data-bs-toggle="collapse" :data-bs-target="'#collapseOrder-' + order.orderId" aria-expanded="false"
      :aria-controls="'collapseOrder-' + order.orderId" style="cursor: pointer;">
      <div class="d-flex align-items-center gap-4">
        <div class="d-flex align-items-center gap-2">
          <i class="bi bi-chevron-right text-secondary transition-icon"></i>
          <span class="fw-bold fs-5 text-dark">#{{ order.orderId }}</span>
        </div>
        <span class="text-secondary small d-none d-md-inline">
          <i class="bi bi-calendar3 me-1"></i>
          {{ new Date(order.orderDate).toLocaleDateString("zh-TW") }}
        </span>
      </div>
      <div class="d-flex align-items-center gap-3">
        <span class="fw-bold text-dark">
          {{ order.totalPoint }} <span class="small text-muted fw-normal">點</span>
        </span>
        <span class="badge rounded-pill" :class="getStatusBadgeClass(order.status)">
          {{ order.status }}
        </span>
      </div>
    </div>

    <!-- Collapsible Body -->
    <div :id="'collapseOrder-' + order.orderId" class="collapse">
      <div class="card-body bg-light-subtle border-top">
        <div v-if="loading" class="text-center py-4 text-muted">
          <div class="spinner-border spinner-border-sm me-2" role="status"></div>
          <span>載入訂單明細...</span>
        </div>

        <div v-else class="row g-4">
          <!-- Main: Product List -->
          <div class="col-lg-8 border-end-lg">
            <h6 class="card-title fw-bold mb-3 text-secondary">
              <i class="bi bi-box-seam me-2"></i>訂購商品
            </h6>
            <div class="table-responsive">
              <table class="table table-borderless align-middle mb-0">
                <thead class="table-light small text-secondary">
                  <tr>
                    <th scope="col">商品資訊</th>
                    <th scope="col" class="text-center" style="width: 80px">數量</th>
                    <th scope="col" class="text-end" style="width: 100px">小計</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="detail in details" :key="detail.orderdetailId" class="border-bottom">
                    <td>
                      <div class="d-flex align-items-center">
                        <img :src="`https://localhost:7190/img/product/${detail.imageName}`" class="border rounded me-3"
                          style="width: 50px; height: 50px; object-fit: cover;" :alt="detail.productName" />
                        <div>
                          <div class="fw-medium text-dark">{{ detail.productName }}</div>
                          <div class="small text-muted">單價: {{ detail.unitPointAtPurchase }} 點</div>
                        </div>
                      </div>
                    </td>
                    <td class="text-center">{{ detail.quantity }}</td>
                    <td class="text-end fw-bold">{{ detail.unitPointAtPurchase * detail.quantity }}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="alert alert-light mt-3 mb-0 border d-flex align-items-start small">
              <i class="bi bi-chat-text-fill me-2 mt-1 text-secondary"></i>
              <div>
                <strong class="text-dark">備註：</strong>
                <span class="text-muted">{{ order.remark }}</span>
              </div>
            </div>
          </div>

          <!-- Sidebar: Shipping & Actions -->
          <div class="col-lg-4 d-flex flex-column">
            <!-- Shipping Section -->
            <div class="mb-4 flex-grow-1">
              <h6 class="card-title fw-bold mb-3 text-secondary">
                <i class="bi bi-truck me-2"></i>配送狀態
              </h6>
              <ShippedDetail :orderId="order.orderId" />
            </div>

            <!-- Actions Footer (In column) -->
            <div class="mt-auto pt-3 border-top">
              <div v-if="order.status == '取消'" class="d-flex gap-2">
                <button class="btn btn-secondary disabled flex-fill" disabled>訂單修改</button>
                <button class="btn btn-secondary disabled flex-fill" disabled>訂單已取消</button>
              </div>
              <div v-else-if="order.status == '待處理'" class="d-flex gap-2">
                <button class="btn btn-outline-danger flex-fill">
                  訂單修改
                </button>
                <button class="btn btn-outline-danger flex-fill" @click="cancelOrder(order.orderId)">
                  取消訂單
                </button>
              </div>
              <div v-else class="d-flex gap-2">
                <!-- Placeholder for other statuses, usually no action needed -->
                <button class="btn btn-success disabled flex-fill" disabled>訂單修改</button>
                <button class="btn btn-success disabled flex-fill" disabled>訂單已完成</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.transition-icon {
  transition: transform 0.3s ease;
}

/* Rotate icon when not collapsed (expanded) */
[aria-expanded="true"] .transition-icon {
  transform: rotate(90deg);
}

/* Standard Bootstrap Overrides if necessary, essentially clean */
.border-end-lg {
  border-right: 0;
}

@media (min-width: 992px) {
  .border-end-lg {
    border-right: 1px solid #dee2e6;
  }
}
</style>
