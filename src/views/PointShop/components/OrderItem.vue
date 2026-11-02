<script setup>
import { computed, ref } from "vue";
import {
  getOrderDetails,
  getShippedDetails,
  putCancelOrder,
  putShippedOrder,
} from "@/views/PointShop/services/PointShopApi";
import {
  shopSwal,
  alertError,
  confirmDialog,
} from "@/views/PointShop/services/pointShopSwal";
import {
  ORDER_STATUS,
  ensureOk,
  isBusinessError,
  errorText,
  escapeHtml,
  formatDate,
  formatDateTime,
  normalizePhone,
  productImageUrl,
  validateShipping,
} from "@/views/PointShop/services/pointShopUtils";

const emit = defineEmits(["refresh"]);
const props = defineProps({
  order: { type: Object, required: true },
});

// ============ 狀態 ============
const expanded = ref(false); // 是否展開
const loaded = ref(false); // 明細是否已載入 (展開時才載入，避免列表一次打大量 API)
const details = ref([]); // 訂單明細
const loading = ref(false); // 明細載入中
const shipped = ref([]); // 出貨資料
const shippedLoading = ref(false);
const shippedError = ref(null);

const shippedId = computed(() => shipped.value[0]?.shippedDetailId ?? 0);
const isPending = computed(() => props.order.status === ORDER_STATUS.PENDING);
const canEditShipping = computed(
  () =>
    isPending.value &&
    shippedId.value > 0 &&
    details.value.some((detail) => detail.requiresShipping),
);

// 非待處理訂單的按鈕文字
const statusButtonText = computed(() => {
  switch (props.order.status) {
    case ORDER_STATUS.CANCELED:
      return "訂單已取消";
    case ORDER_STATUS.SHIPPED:
      return "訂單已出貨";
    case ORDER_STATUS.COMPLETED:
      return "訂單已完成";
    default:
      return `訂單${props.order.status}`;
  }
});

// ============ 載入資料 ============
const loadDetails = async () => {
  loading.value = true;
  try {
    const res = await getOrderDetails(props.order.orderId);
    details.value = res.data.ok ? res.data.data || [] : [];
  } catch (err) {
    details.value = [];
  } finally {
    loading.value = false;
  }
};

const loadShippedDetails = async () => {
  shippedLoading.value = true;
  shippedError.value = null;
  try {
    const res = await getShippedDetails(props.order.orderId);
    shipped.value = res.data.ok ? res.data.data || [] : [];
  } catch (err) {
    shippedError.value = "載入出貨資料失敗，請稍後重試";
    shipped.value = [];
  } finally {
    shippedLoading.value = false;
  }
};

// 展開 / 收合，第一次展開時才載入明細與出貨資料
const toggle = async () => {
  expanded.value = !expanded.value;
  if (expanded.value && !loaded.value) {
    loaded.value = true;
    await Promise.all([loadDetails(), loadShippedDetails()]);
  }
};

// ============ 狀態標籤樣式 ============
const statusBadgeClass = computed(() => {
  switch (props.order.status) {
    case ORDER_STATUS.COMPLETED:
      return "bg-success-subtle text-success border border-success-subtle";
    case ORDER_STATUS.PENDING:
      return "bg-warning-subtle text-warning-emphasis border border-warning-subtle";
    case ORDER_STATUS.SHIPPED:
      return "bg-info-subtle text-info-emphasis border border-info-subtle";
    case ORDER_STATUS.CANCELED:
      return "bg-danger-subtle text-danger border border-danger-subtle";
    default:
      return "bg-secondary-subtle text-secondary border border-secondary-subtle";
  }
});

// ============ 取消訂單 ============
const cancelOrder = async () => {
  const confirmed = await confirmDialog({
    title: "確定要取消嗎?",
    text: "取消後商品須重新以點數兌換!",
    confirmText: "是, 我要取消!",
    cancelText: "返回",
  });
  if (!confirmed) return;

  try {
    ensureOk(await putCancelOrder(props.order.orderId));
    await shopSwal.fire({ title: "取消成功!", text: "您的訂單已取消", icon: "success" });
    emit("refresh");
  } catch (error) {
    console.error("取消失敗:", error);
    await alertError("取消失敗!", errorText(error));
    // 業務失敗多半是訂單狀態已變更 (例如已出貨)，重新整理顯示最新狀態
    if (isBusinessError(error)) emit("refresh");
  }
};

// ============ 修改寄送資料 ============
const editShipping = async () => {
  let businessFailed = false;

  const result = await shopSwal.fire({
    title: "修改寄送資料",
    html: `
      <div class="mb-3 text-start">
        <label for="swal-name" class="form-label">收件人姓名</label>
        <input type="text" id="swal-name" class="form-control" placeholder="請輸入收件人姓名">
      </div>
      <div class="mb-3 text-start">
        <label for="swal-phone" class="form-label">收件人電話</label>
        <input type="tel" id="swal-phone" class="form-control" placeholder="09xxxxxxxx">
      </div>
      <div class="mb-3 text-start">
        <label for="swal-address" class="form-label">收件地址</label>
        <input type="text" id="swal-address" class="form-control" placeholder="請輸入收件地址">
      </div>
      <div class="mb-3 text-start">
        <label for="swal-remark" class="form-label">備註</label>
        <input type="text" id="swal-remark" class="form-control" placeholder="請輸入備註（可選）">
      </div>
    `,
    showCancelButton: true,
    confirmButtonText: "確認",
    cancelButtonText: "取消",
    showLoaderOnConfirm: true,
    focusConfirm: false,
    allowOutsideClick: () => !shopSwal.isLoading(),
    // 驗證後直接呼叫 API，失敗時把訊息顯示在對話框內，使用者不必重填
    preConfirm: async () => {
      const value = (id) => document.getElementById(id).value.trim();
      const form = {
        name: value("swal-name"),
        phone: value("swal-phone"),
        address: value("swal-address"),
        remark: value("swal-remark"),
      };

      const invalid = validateShipping(form);
      if (invalid) {
        shopSwal.showValidationMessage(invalid);
        return false;
      }

      form.phone = normalizePhone(form.phone);
      try {
        ensureOk(
          await putShippedOrder(
            {
              RecipientName: form.name,
              RecipientAddress: form.address,
              RecipientPhone: form.phone,
              Remark: form.remark,
            },
            shippedId.value,
          ),
        );
        return form;
      } catch (error) {
        console.error("修改失敗:", error);
        businessFailed = isBusinessError(error);
        shopSwal.showValidationMessage(`修改失敗：${errorText(error)}`);
        return false;
      }
    },
  });

  if (result.isConfirmed) {
    const form = result.value;
    await shopSwal.fire({
      title: "修改成功!",
      icon: "success",
      html: `
        <div class="text-start">
          <p><strong>姓名：</strong>${escapeHtml(form.name)}</p>
          <p><strong>電話：</strong>${escapeHtml(form.phone)}</p>
          <p><strong>地址：</strong>${escapeHtml(form.address)}</p>
          ${form.remark ? `<p><strong>備註：</strong>${escapeHtml(form.remark)}</p>` : ""}
        </div>
      `,
    });
    await loadShippedDetails();
  } else if (businessFailed) {
    // 曾因訂單狀態變更被拒絕，關閉對話框後重新整理顯示最新狀態
    emit("refresh");
  }
};
</script>

<template>
  <div class="card mb-3">
    <!-- ========== 標題列：訂單基本資訊，點擊展開 ========== -->
    <div
      class="card-header bg-white py-3 d-flex flex-wrap justify-content-between align-items-center"
      role="button"
      :aria-expanded="expanded"
      :aria-controls="'collapseOrder-' + order.orderId"
      @click="toggle"
    >
      <div class="d-flex align-items-center gap-4">
        <div class="d-flex align-items-center gap-2">
          <i class="bi bi-chevron-right text-secondary transition-icon"></i>
          <span class="fw-bold fs-5 text-dark">訂單編號#{{ order.orderId }}</span>
        </div>
        <!-- 訂單日期（平板以上尺寸顯示） -->
        <span class="text-secondary small d-none d-md-inline">
          <i class="bi bi-calendar3 me-1"></i>{{ formatDate(order.orderDate) }}
        </span>
      </div>
      <div class="d-flex align-items-center gap-3">
        <span class="fw-bold text-dark">
          {{ order.totalPoint }}
          <span class="small text-muted fw-normal">點</span>
        </span>
        <span class="badge rounded-pill" :class="statusBadgeClass">
          {{ order.status }}
        </span>
      </div>
    </div>

    <!-- ========== 展開內容 (第一次展開才載入) ========== -->
    <div
      v-if="loaded"
      v-show="expanded"
      :id="'collapseOrder-' + order.orderId"
    >
      <div class="card-body bg-light-subtle border-top">
        <div v-if="loading" class="text-center py-4 text-muted">
          <div class="spinner-border spinner-border-sm me-2" role="status"></div>
          <span>載入訂單明細...</span>
        </div>

        <div v-else class="row g-4">
          <!-- ========== 左側：訂購商品 ========== -->
          <div class="col-lg-7 border-end-lg">
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
                  <tr
                    v-for="detail in details"
                    :key="detail.orderdetailId"
                    class="border-bottom"
                  >
                    <td>
                      <div class="d-flex align-items-center">
                        <img
                          :src="productImageUrl(detail.imageName)"
                          class="border rounded me-3"
                          style="width: 50px; height: 50px; object-fit: cover"
                          :alt="detail.productName"
                        />
                        <div>
                          <div class="fw-medium text-dark">{{ detail.productName }}</div>
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
              v-if="order.remark"
              class="alert alert-light mt-3 mb-0 border d-flex align-items-start small"
            >
              <i class="bi bi-chat-text-fill me-2 text-secondary"></i>
              <div>
                <strong class="text-dark">備註：</strong>
                <span class="text-muted">{{ order.remark }}</span>
              </div>
            </div>
          </div>

          <!-- ========== 右側：配送狀態與操作 ========== -->
          <div class="col-lg-5 d-flex flex-column">
            <div class="mb-4 flex-grow-1">
              <h6 class="card-title fw-bold mb-3 text-secondary">
                <i class="bi bi-truck me-2"></i>配送狀態
              </h6>

              <div v-if="shippedLoading" class="text-center py-3 text-muted">
                <div class="spinner-border spinner-border-sm me-2" role="status">
                  <span class="visually-hidden">Loading...</span>
                </div>
                <span>載入出貨資訊...</span>
              </div>

              <div
                v-else-if="shippedError"
                class="alert alert-warning d-flex align-items-center mb-0"
                role="alert"
              >
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                <div>{{ shippedError }}</div>
              </div>

              <div v-else-if="shipped.length === 0" class="my-5 text-center">
                <i class="bi bi-box-seam me-2"></i>尚無出貨資料
              </div>

              <div v-else class="list-group">
                <div
                  v-for="ship in shipped"
                  :key="ship.shippedDetailId"
                  class="list-group-item"
                >
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
                        {{ formatDateTime(ship.shippedDate) || "尚未出貨" }}
                      </span>
                    </div>
                    <div class="col-6">
                      <span class="d-block text-muted">預計到貨</span>
                      <span class="text-dark">
                        {{ formatDateTime(ship.arrivalDate) || "未定" }}
                      </span>
                    </div>
                  </div>

                  <div class="alert alert-light mt-2 mb-0 p-2 small text-muted border">
                    <i class="bi bi-geo-alt me-1"></i>收件人姓名：{{ ship.maskedName }}<br />
                    <i class="bi bi-telephone me-1"></i>收件人電話：{{ ship.maskedPhone }}<br />
                    <i class="bi bi-house-door me-1"></i>收件地址：{{ ship.maskedAddress }}<br />
                    <i class="bi bi-info-circle me-1"></i>備註：{{ ship.remark }}
                  </div>
                </div>
              </div>
            </div>

            <!-- ========== 操作按鈕 ========== -->
            <div class="mt-auto pt-3 border-top">
              <div class="d-flex gap-2">
                <!-- 只有待處理且含實體商品的訂單可修改寄送資料 -->
                <button
                  class="btn flex-fill"
                  :class="canEditShipping ? 'btn-outline-danger' : 'btn-secondary'"
                  :disabled="!canEditShipping"
                  @click="editShipping"
                >
                  修改寄送資料
                </button>

                <button
                  v-if="isPending"
                  class="btn btn-outline-danger flex-fill"
                  @click="cancelOrder"
                >
                  取消訂單
                </button>
                <button
                  v-else
                  class="btn flex-fill"
                  :class="order.status === ORDER_STATUS.COMPLETED ? 'btn-success' : 'btn-secondary'"
                  disabled
                >
                  {{ statusButtonText }}
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

/* 展開時箭頭旋轉 90 度 */
[aria-expanded="true"] .transition-icon {
  transform: rotate(90deg);
}

/* 桌面尺寸才顯示右邊框 */
.border-end-lg {
  border-right: 0;
}

@media (min-width: 992px) {
  .border-end-lg {
    border-right: 1px solid #dee2e6;
  }
}
</style>
