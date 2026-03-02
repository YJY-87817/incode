<script setup>
import { ref, onMounted } from "vue";
import {
  getOrderDetails,
  getShippedDetails,
  putCancelOrder,
  putShippedOrder,
} from "@/views/PointShop/services/PointShopApi";

// 引入 SweetAlert2 美化提示框套件
import CartSwal from "sweetalert2";
import "sweetalert2/dist/sweetalert2.min.css";

// 定義事件發射和組件傳入的屬性
const emit = defineEmits(["refresh"]);
const props = defineProps({
  order: { type: Object, required: true }, // 訂單對象
});

// ============ 狀態變數定義 ============
const details = ref([]); // 訂單明細列表
const loading = ref(true); // 訂單明細載入狀態
const shippedId = ref(0); // 出貨單 ID
const shipped = ref([]); // 出貨對象列表
const shippedLoading = ref(false); // 出貨資訊載入狀態
const shippedError = ref(null); // 出貨資訊錯誤訊息
const baseUrl = import.meta.env.VITE_BASE_URL; // 基礎 URL

// ============ 載入出貨詳情 ============
// 非同步獲取出貨資料，設置載入狀態和錯誤處理
const loadShippedDetails = async () => {
  shippedLoading.value = true;
  shippedError.value = null;
  try {
    const res = await getShippedDetails(props.order.orderId);
    if (res.data.ok) {
      shipped.value = res.data.data || [];
      // 取得第一筆出貨單 ID，如果沒有則為 0
      const newShippedId =
        shipped.value.length > 0 ? shipped.value[0].shippedDetailId : 0;
      shippedId.value = newShippedId;
    }
  } catch (err) {
    shippedError.value = "載入出貨資料失敗，請稍後重試";
    shipped.value = [];
  } finally {
    shippedLoading.value = false;
  }
};

// ============ 生命週期鉤子 ============
// 組件掛載時同時載入訂單明細和出貨資訊
onMounted(async () => {
  try {
    const res = await getOrderDetails(props.order.orderId);
    details.value = res.data.data;
  } finally {
    loading.value = false;
  }

  // 載入出貨詳情
  await loadShippedDetails();
});

// ============ 訂單狀態標籤樣式 ============
// 根據訂單狀態返回對應的 Bootstrap 樣式類名
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

// ============ 取消訂單 ============
// 顯示確認對話框，確認後調用取消 API 並更新狀態
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
      // 設置按鈕文字顏色為白色
      modal.querySelector(".swal2-confirm").style.color = "#fff";
      modal.querySelector(".swal2-cancel").style.color = "#fff";
    },
  }).then(async (result) => {
    // 只有在點擊「確認」按鈕時才呼叫 API
    if (result.isConfirmed) {
      try {
        await putCancelOrder(orderId); // 呼叫取消 API

        // 顯示成功提示訊息
        await CartSwal.fire({
          title: "取消成功!",
          text: "您的訂單已取消",
          icon: "success",
          didOpen: (modal) => {
            modal.querySelector(".swal2-confirm").style.color = "#fff";
          },
        });

        emit("refresh"); // 通知父元件重新刷新列表
      } catch (error) {
        console.error("取消失敗:", error);
        // 顯示失敗提示訊息
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

// ============ 修改出貨資料 ============
// 顯示對話框讓用戶修改收件人信息，包括驗證和 API 調用
const shippedchange = (shippedid) => {
  console.log("出貨單 ID:", shippedId.value);
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
      // 取得表單輸入的值
      const name = document.getElementById("swal-name").value;
      const phone = document.getElementById("swal-phone").value;
      const address = document.getElementById("swal-address").value;
      const remark = document.getElementById("swal-remark").value;

      // 檢查必填欄位
      if (!name || !phone || !address) {
        CartSwal.showValidationMessage("請填寫姓名、電話和地址");
        return false;
      }

      // 驗證電話格式（台灣手機號碼：09 開頭，後接 8 位數字）
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
      // 設置按鈕文字顏色為白色
      modal.querySelector(".swal2-confirm").style.color = "#fff";
      modal.querySelector(".swal2-cancel").style.color = "#fff";
    },
  }).then(async (result) => {
    // 檢查是否點擊了確認按鈕
    if (result.isConfirmed) {
      try {
        // 組織出貨詳情數據
        const shippedDetail = {
          RecipientName: result.value.name,
          RecipientAddress: result.value.address,
          RecipientPhone: result.value.phone,
          Remark: result.value.remark || "",
        };

        // 呼叫 API 更新出貨資料
        await putShippedOrder(shippedDetail, shippedId.value);

        // 顯示修改成功訊息
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
        await loadShippedDetails();

        emit("refresh"); // 通知父元件重新刷新列表
      } catch (error) {
        console.error("修改失敗:", error);
        // 顯示修改失敗訊息
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
    <!-- ========== 卡片標題欄：顯示訂單基本信息，點擊可展開 ========== -->
    <div
      class="card-header bg-white py-3 d-flex flex-wrap justify-content-between align-items-center"
      data-bs-toggle="collapse"
      :data-bs-target="'#collapseOrder-' + order.orderId"
      aria-expanded="false"
      :aria-controls="'collapseOrder-' + order.orderId"
      style="cursor: pointer"
    >
      <!-- 左側：訂單編號和日期 -->
      <div class="d-flex align-items-center gap-4">
        <div class="d-flex align-items-center gap-2">
          <!-- 可點擊展開/收合的箭頭圖標 -->
          <i class="bi bi-chevron-right text-secondary transition-icon"></i>
          <!-- 訂單編號 -->
          <span class="fw-bold fs-5 text-dark"
            >訂單編號#{{ order.orderId }}</span
          >
        </div>
        <!-- 訂單日期（平板以上尺寸顯示） -->
        <span class="text-secondary small d-none d-md-inline">
          <i class="bi bi-calendar3 me-1"></i>
          {{ new Date(order.orderDate).toLocaleDateString("zh-TW") }}
        </span>
      </div>
      <!-- 右側：訂單金額和狀態標籤 -->
      <div class="d-flex align-items-center gap-3">
        <!-- 訂單總點數 -->
        <span class="fw-bold text-dark">
          {{ order.totalPoint }}
          <span class="small text-muted fw-normal">點</span>
        </span>
        <!-- 訂單狀態標籤 -->
        <span
          class="badge rounded-pill"
          :class="getStatusBadgeClass(order.status)"
        >
          {{ order.status }}
        </span>
      </div>
    </div>

    <!-- ========== 可收合的卡片內容區 ========== -->
    <div :id="'collapseOrder-' + order.orderId" class="collapse">
      <div class="card-body bg-light-subtle border-top">
        <!-- 載入狀態：顯示加載動畫 -->
        <div v-if="loading" class="text-center py-4 text-muted">
          <div
            class="spinner-border spinner-border-sm me-2"
            role="status"
          ></div>
          <span>載入訂單明細...</span>
        </div>

        <!-- 主要內容：分為左側商品列表和右側配送信息 -->
        <div v-else class="row g-4">
          <!-- ========== 左側區：訂購商品清單 ========== -->
          <div class="col-lg-7 border-end-lg">
            <h6 class="card-title fw-bold mb-3 text-secondary">
              <i class="bi bi-box-seam me-2"></i>訂購商品
            </h6>
            <!-- 商品表格 -->
            <div class="table-responsive">
              <table class="table table-borderless align-middle mb-0">
                <!-- 表頭 -->
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
                <!-- 表格主體：迴圈顯示每個商品 -->
                <tbody>
                  <tr
                    v-for="detail in details"
                    :key="detail.orderdetailId"
                    class="border-bottom"
                  >
                    <!-- 商品列：包含圖片、名稱和單價 -->
                    <td>
                      <div class="d-flex align-items-center">
                        <!-- 商品圖片 -->
                        <img
                          :src="`${baseUrl}/img/product/${detail.imageName}`"
                          class="border rounded me-3"
                          style="width: 50px; height: 50px; object-fit: cover"
                          :alt="detail.productName"
                        />
                        <!-- 商品名稱和單價 -->
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
                    <!-- 數量列 -->
                    <td class="text-center">{{ detail.quantity }}</td>
                    <!-- 小計列：單價 × 數量 -->
                    <td class="text-end fw-bold">
                      {{ detail.unitPointAtPurchase * detail.quantity }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- 訂單備註 -->
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

          <!-- ========== 右側區：配送狀態和操作按鈕 ========== -->
          <div class="col-lg-5 d-flex flex-column">
            <!-- 配送狀態區塊 -->
            <div class="mb-4 flex-grow-1">
              <h6 class="card-title fw-bold mb-3 text-secondary">
                <i class="bi bi-truck me-2"></i>配送狀態
              </h6>

              <!-- 載入狀態：顯示加載動畫 -->
              <div v-if="shippedLoading" class="text-center py-3 text-muted">
                <div
                  class="spinner-border spinner-border-sm me-2"
                  role="status"
                >
                  <span class="visually-hidden">Loading...</span>
                </div>
                <span>載入出貨資訊...</span>
              </div>

              <!-- 錯誤狀態：顯示錯誤訊息 -->
              <div
                v-else-if="shippedError"
                class="alert alert-warning d-flex align-items-center mb-0"
                role="alert"
              >
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                <div>{{ shippedError }}</div>
              </div>

              <!-- 空狀態：沒有出貨記錄 -->
              <div v-else-if="shipped.length === 0" class="my-5 text-center">
                <i class="bi bi-box-seam me-2"></i>
                尚無出貨資料
              </div>

              <!-- 出貨資料清單 -->
              <div v-else class="list-group">
                <!-- 迴圈顯示每個出貨單 -->
                <div
                  v-for="ship in shipped"
                  :key="ship.shippedDate"
                  class="list-group-item"
                >
                  <!-- 出貨標題和狀態 -->
                  <div
                    class="d-flex justify-content-between align-items-center mb-2"
                  >
                    <h6 class="mb-0 fw-bold">出貨單</h6>
                    <!-- 根據出貨狀態顯示對應的標籤 -->
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

                  <!-- 出貨日期和預計到貨日期 -->
                  <div class="row g-2 small text-secondary">
                    <div class="col-6">
                      <span class="d-block text-muted">出貨日期</span>
                      <span class="text-dark">
                        {{
                          ship.shippedDate
                            ? new Date(ship.shippedDate).toLocaleString(
                                "zh-TW",
                                {
                                  year: "numeric",
                                  day: "2-digit",
                                  hour: "2-digit",
                                  minute: "2-digit",
                                  hour12: false,
                                  month: "2-digit",
                                },
                              )
                            : "尚未出貨"
                        }}
                      </span>
                    </div>
                    <div class="col-6">
                      <span class="d-block text-muted">預計到貨</span>
                      <span class="text-dark">
                        {{
                          ship.arrivalDate
                            ? new Date(ship.arrivalDate).toLocaleString(
                                "zh-TW",
                                {
                                  year: "numeric",
                                  day: "2-digit",
                                  hour: "2-digit",
                                  minute: "2-digit",
                                  hour12: false,
                                  month: "2-digit",
                                },
                              )
                            : "未定"
                        }}
                      </span>
                    </div>
                  </div>
                  <!-- 出貨收件人詳情 -->
                  <div>
                    <div
                      class="alert alert-light mt-2 mb-0 p-2 small text-muted border"
                    >
                      <i class="bi bi-geo-alt me-1"></i>收件人姓名：
                      {{ ship.maskedName }} <br />
                      <i class="bi bi-telephone me-1"></i>收件人電話：
                      {{ ship.maskedPhone }} <br />
                      <i class="bi bi-house-door me-1"></i>收件地址：
                      {{ ship.maskedAddress }} <br />
                      <i class="bi bi-info-circle me-1"></i>備註：
                      {{ ship.remark }}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- ========== 操作按鈕區底部 ========== -->
            <div class="mt-auto pt-3 border-top">
              <!-- 按鈕組容器 -->
              <div class="d-flex gap-2">
                <!-- 訂單修改按鈕：只有待處理且需要配送的訂單才可修改 -->
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
                <!-- 訂單修改按鈕禁用狀態 -->
                <button
                  v-else
                  class="btn btn-secondary disabled flex-fill"
                  disabled
                >
                  訂單修改
                </button>

                <!-- 已取消訂單：按鈕禁用 -->
                <button
                  v-if="order.status == '已取消'"
                  class="btn btn-secondary disabled flex-fill"
                  disabled
                >
                  訂單已取消
                </button>
                <!-- 待處理訂單：可取消 -->
                <button
                  v-else-if="order.status == '待處理'"
                  class="btn btn-outline-danger flex-fill"
                  @click="cancelOrder(order.orderId)"
                >
                  取消訂單
                </button>
                <!-- 已出貨訂單：按鈕禁用 -->
                <button
                  v-else-if="order.status == '已出貨'"
                  class="btn btn-secondary disabled flex-fill"
                  disabled
                >
                  訂單已出貨
                </button>
                <!-- 已完成訂單：按鈕禁用 -->
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

<!-- ========== 元件樣式 ========== -->
<style scoped>
/* 箭頭圖標過渡動畫 */
.transition-icon {
  transition: transform 0.3s ease;
}

/* 展開時箭頭旋轉 90 度 */
[aria-expanded="true"] .transition-icon {
  transform: rotate(90deg);
}

/* 響應式設計：桌面設備才顯示右邊框 */
.border-end-lg {
  border-right: 0;
}

/* 平板以上尺寸：顯示右邊框 */
@media (min-width: 992px) {
  .border-end-lg {
    border-right: 1px solid #dee2e6;
  }
}
</style>
