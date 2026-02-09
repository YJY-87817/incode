<script setup>
import { ref, onMounted, watch } from "vue";
import ShippedDetail from "@/views/PointShop/components/ShippedDetail.vue";
import {
  getOrderDetails,
  putCancelOrder,
  putShippedOrder,
} from "@/views/PointShop/services/PointShopApi";

// 引入 SweetAlert2 的套件
import CartSwal from "sweetalert2";
import "sweetalert2/dist/sweetalert2.min.css";

const emit = defineEmits(["refresh"]);
const props = defineProps({
  order: { type: Object, required: true },
});

const details = ref([]);
const loading = ref(true);
const shippedId = ref(0);
const shippedDetailRef = ref(null);

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
    didOpen: (modal) => {
      modal.querySelector(".swal2-confirm").style.color = "#fff";
      modal.querySelector(".swal2-cancel").style.color = "#fff";
    },
  }).then(async (result) => {
    // 修正：只有在點擊「確認」後才呼叫 API
    if (result.isConfirmed) {
      try {
        await putCancelOrder(orderId); // 執行 API

        await CartSwal.fire({
          title: "取消成功!",
          text: "您的訂單已取消",
          icon: "success",
          didOpen: (modal) => {
            modal.querySelector(".swal2-confirm").style.color = "#fff";
          },
        });

        emit("refresh"); // 通知父元件重整
      } catch (error) {
        console.error("取消失敗:", error);
        // 可以額外加一個失敗的 Swal 提醒
        await CartSwal.fire({
          title: "取消失敗!",
          text: "請稍後再試",
          icon: "error",
          didOpen: (modal) => {
            modal.querySelector(".swal2-confirm").style.color = "#fff";
          },
        });
      }
    }
  });
};

const shippedchange = (shippedid) => {
  console.log("shippedId:", shippedId.value);
  CartSwal.fire({
    title: "修改寄送資料",
    html: `
      <div class="mb-3">
        <label for="swal-name" class="form-label">收件人姓名</label>
        <input type="text" id="swal-name" class="form-control" placeholder="請輸入收件人姓名">
      </div>
      <div class="mb-3">
        <label for="swal-phone" class="form-label">收件人電話</label>
        <input type="tel" id="swal-phone" class="form-control" placeholder="請輸入收件人電話">
      </div>
      <div class="mb-3">
        <label for="swal-address" class="form-label">收件地址</label>
        <input type="text" id="swal-address" class="form-control" placeholder="請輸入收件地址">
      </div>
      <div class="mb-3">
        <label for="swal-remark" class="form-label">備註</label>
        <input type="text" id="swal-remark" class="form-control" placeholder="請輸入備註（可選）">
      </div>
    `,
    showCancelButton: true,
    confirmButtonText: "確認",
    cancelButtonText: "取消",
    showLoaderOnConfirm: true,
    focusConfirm: false,
    preConfirm: () => {
      const name = document.getElementById("swal-name").value;
      const phone = document.getElementById("swal-phone").value;
      const address = document.getElementById("swal-address").value;
      const remark = document.getElementById("swal-remark").value;

      if (!name || !phone || !address) {
        CartSwal.showValidationMessage("請填寫姓名、電話和地址");
        return false;
      }

      // 驗證電話格式（台灣手機號碼）
      const phoneRegex = /^09\d{8}$/;
      if (!phoneRegex.test(phone)) {
        CartSwal.showValidationMessage(
          "請輸入正確的手機號碼格式（09xxxxxxxx）",
        );
        return false;
      }

      return { name, phone, address, remark };
    },
    allowOutsideClick: () => !CartSwal.isLoading(),
    didOpen: (modal) => {
      modal.querySelector(".swal2-confirm").style.color = "#fff";
      modal.querySelector(".swal2-cancel").style.color = "#fff";
    },
  }).then(async (result) => {
    if (result.isConfirmed) {
      try {
        const shippedDetail = {
          RecipientName: result.value.name,
          RecipientAddress: result.value.address,
          RecipientPhone: result.value.phone,
          Remark: result.value.remark || "",
        };

        await putShippedOrder(shippedDetail, shippedId.value);

        await CartSwal.fire({
          title: "修改成功!",
          html: `
            <div class="text-start">
              <p><strong>姓名：</strong>${result.value.name}</p>
              <p><strong>電話：</strong>${result.value.phone}</p>
              <p><strong>地址：</strong>${result.value.address}</p>
              ${result.value.remark ? `<p><strong>備註：</strong>${result.value.remark}</p>` : ""}
            </div>
          `,
          icon: "success",
          didOpen: (modal) => {
            modal.querySelector(".swal2-confirm").style.color = "#fff";
          },
        });

        // 重新載入出貨資料
        if (shippedDetailRef.value) {
          await shippedDetailRef.value.loadShippedDetails();
        }

        emit("refresh"); // 通知父元件重整
      } catch (error) {
        console.error("修改失敗:", error);
        await CartSwal.fire({
          title: "修改失敗!",
          text: "請稍後再試",
          icon: "error",
          didOpen: (modal) => {
            modal.querySelector(".swal2-confirm").style.color = "#fff";
          },
        });
      }
    }
  });
};
</script>

<template>
  <div class="card mb-3">
    <!-- Card Header: Order Info - Click to expand -->
    <div
      class="card-header bg-white py-3 d-flex flex-wrap justify-content-between align-items-center"
      data-bs-toggle="collapse"
      :data-bs-target="'#collapseOrder-' + order.orderId"
      aria-expanded="false"
      :aria-controls="'collapseOrder-' + order.orderId"
      style="cursor: pointer"
    >
      <div class="d-flex align-items-center gap-4">
        <div class="d-flex align-items-center gap-2">
          <i class="bi bi-chevron-right text-secondary transition-icon"></i>
          <span class="fw-bold fs-5 text-dark"
            >訂單編號#{{ order.orderId }}</span
          >
        </div>
        <span class="text-secondary small d-none d-md-inline">
          <i class="bi bi-calendar3 me-1"></i>
          {{ new Date(order.orderDate).toLocaleDateString("zh-TW") }}
        </span>
      </div>
      <div class="d-flex align-items-center gap-3">
        <span class="fw-bold text-dark">
          {{ order.totalPoint }}
          <span class="small text-muted fw-normal">點</span>
        </span>
        <span
          class="badge rounded-pill"
          :class="getStatusBadgeClass(order.status)"
        >
          {{ order.status }}
        </span>
      </div>
    </div>

    <!-- Collapsible Body -->
    <div :id="'collapseOrder-' + order.orderId" class="collapse">
      <div class="card-body bg-light-subtle border-top">
        <div v-if="loading" class="text-center py-4 text-muted">
          <div
            class="spinner-border spinner-border-sm me-2"
            role="status"
          ></div>
          <span>載入訂單明細...</span>
        </div>

        <div v-else class="row g-4">
          <!-- Main: Product List -->
          <div class="col-lg-7 border-end-lg">
            <h6 class="card-title fw-bold mb-3 text-secondary">
              <i class="bi bi-box-seam me-2"></i>訂購商品
            </h6>
            <div class="table-responsive">
              <table class="table table-borderless align-middle mb-0">
                <thead class="table-light small text-secondary">
                  <tr>
                    <th scope="col">商品資訊</th>
                    <th scope="col" class="text-center" style="width: 80px">
                      數量
                    </th>
                    <th scope="col" class="text-end" style="width: 100px">
                      小計
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="detail in details"
                    :key="detail.orderdetailId"
                    class="border-bottom"
                  >
                    <td>
                      <div class="d-flex align-items-center">
                        <img
                          :src="`https://localhost:7190/img/product/${detail.imageName}`"
                          class="border rounded me-3"
                          style="width: 50px; height: 50px; object-fit: cover"
                          :alt="detail.productName"
                        />
                        <div>
                          <div class="fw-medium text-dark">
                            {{ detail.productName }}
                          </div>
                          <div class="small text-muted">
                            單價: {{ detail.unitPointAtPurchase }} 點
                          </div>
                        </div>
                      </div>
                    </td>
                    <td class="text-center">{{ detail.quantity }}</td>
                    <td class="text-end fw-bold">
                      {{ detail.unitPointAtPurchase * detail.quantity }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div
              class="alert alert-light mt-3 mb-0 border d-flex align-items-start small"
            >
              <i class="bi bi-chat-text-fill me-2 text-secondary"></i>
              <div>
                <strong class="text-dark">備註：</strong>
                <span class="text-muted">{{ order.remark }}</span>
              </div>
            </div>
          </div>

          <!-- Sidebar: Shipping & Actions -->
          <div class="col-lg-5 d-flex flex-column">
            <!-- Shipping Section -->
            <div class="mb-4 flex-grow-1">
              <h6 class="card-title fw-bold mb-3 text-secondary">
                <i class="bi bi-truck me-2"></i>配送狀態
              </h6>
              <ShippedDetail
                ref="shippedDetailRef"
                :orderId="order.orderId"
                v-model:shipped-id="shippedId"
              />
            </div>

            <!-- Actions Footer (In column) -->
            <div class="mt-auto pt-3 border-top">
              <div class="d-flex gap-2">
                <button
                  v-if="
                    order.status == '待處理' &&
                    details.some((detail) => detail.requiresShipping)
                  "
                  class="btn btn-outline-danger flex-fill"
                  @click="shippedchange(shippedId)"
                >
                  訂單修改
                </button>
                <button
                  v-else
                  class="btn btn-secondary disabled flex-fill"
                  disabled
                >
                  訂單修改
                </button>

                <button
                  v-if="order.status == '已取消'"
                  class="btn btn-secondary disabled flex-fill"
                  disabled
                >
                  訂單已取消
                </button>
                <button
                  v-else-if="order.status == '待處理'"
                  class="btn btn-outline-danger flex-fill"
                  @click="cancelOrder(order.orderId)"
                >
                  取消訂單
                </button>
                <button
                  v-else-if="order.status == '已出貨'"
                  class="btn btn-secondary disabled flex-fill"
                  disabled
                >
                  訂單已出貨
                </button>
                <button
                  v-else
                  class="btn btn-success disabled flex-fill"
                  disabled
                >
                  訂單已完成
                </button>
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
