<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import {
  postCreateOrder,
  getShipAddresses,
} from "@/views/PointShop/services/PointShopApi";
import {
  shopSwal,
  alertError,
  alertWarning,
} from "@/views/PointShop/services/pointShopSwal";
import {
  ensureOk,
  errorText,
  normalizePhone,
  validateShipping,
} from "@/views/PointShop/services/pointShopUtils";
import { useAuthStore } from "@/stores/user/auth.js";

const authStore = useAuthStore();
// 用 computed 保持響應式，登入狀態改變 (例如 checkAuth 完成) 時才會跟著更新
const Islogin = computed(() => authStore.isAuthenticated);
const router = useRouter();
const CartNow = useCartStore();

// 寄送資訊欄位
const name = ref("");
const phone = ref("");
const address = ref("");
const remark = ref("");
// 送出中，避免重複點擊建立多張訂單
const submitting = ref(false);
// 過去填寫的寄送資訊 (null 表示尚未載入)
const userInfo = ref(null);
const selectedShippingRecord = ref(null);

// 是否有實體商品（需要寄送資訊）
const hasPhysicalProducts = computed(() =>
  CartNow.cartItems.some((item) => item.isPhysical === true),
);

// 取得過去填過的寄送資訊 (只載入一次)
const loadShipAddresses = async () => {
  if (!Islogin.value || userInfo.value !== null) return;
  try {
    const res = await getShipAddresses();
    userInfo.value = res.data.ok ? res.data.data || [] : [];
  } catch (error) {
    console.error("取得寄送資訊失敗:", error);
  }
};

// 選擇過去的寄送資訊後自動帶入
const handleFastFill = () => {
  const data = selectedShippingRecord.value;
  if (!data) return;
  name.value = data.recipientName;
  phone.value = data.recipientPhone;
  address.value = data.recipientAddress;
};

// 建立訂單失敗：點數不足時提供前往點數明細
const showCreateFailed = async (message) => {
  // 後端訊息可能是「您的點數不足」或「會員點數不足」
  if (message?.includes("點數不足")) {
    const result = await shopSwal.fire({
      title: "訂單新增失敗!",
      text: message,
      icon: "error",
      showDenyButton: true,
      confirmButtonText: "前往點數明細",
      denyButtonText: "返回",
    });
    if (result.isConfirmed) router.push("/point-log");
    return;
  }
  await alertError("訂單新增失敗!", message);
};

// 結帳流程
const checkout = async () => {
  if (submitting.value) return;

  if (!Islogin.value) {
    alertWarning("請先登入會員!", "結帳前請先登入會員以建立訂單!");
    return;
  }

  if (CartNow.cartItems.length === 0) {
    alertWarning("購物車為空!", "請先加入商品至購物車!");
    return;
  }

  if (hasPhysicalProducts.value) {
    const invalid = validateShipping({
      name: name.value,
      phone: phone.value,
      address: address.value,
    });
    if (invalid) {
      alertWarning("寄送資訊有誤", invalid);
      return;
    }
  }

  // userId 由後端從登入 Cookie 取得，不需由前端傳送
  const newOrder = {
    remark: remark.value,
    items: CartNow.cartItems.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
    })),
    recipient: {
      orderId: 0,
      recipientName: name.value.trim(),
      recipientAddress: address.value.trim(),
      recipientPhone: normalizePhone(phone.value),
      remark: remark.value,
    },
  };

  submitting.value = true;
  try {
    ensureOk(await postCreateOrder(newOrder));
    CartNow.clearCart();
    await shopSwal.fire({
      title: "訂單新增成功!",
      text: "您的訂單已成立將盡快為您處理!",
      icon: "success",
    });
    router.push("/PointShop/Order");
  } catch (error) {
    console.error("訂單建立失敗:", error);
    await showCreateFailed(errorText(error, "訂單建立失敗，請稍後再試"));
  } finally {
    submitting.value = false;
  }
};
</script>

<template>
  <div class="card shadow-sm border-0 rounded-4">
    <div class="card-header border-bottom-0 pt-4 px-4" style="background-color: #86b0bd">
      <h5 class="fw-bold mb-0">
        <i class="bi bi-truck me-2 text-primary"></i>寄送資訊
      </h5>
    </div>

    <div class="card-body px-4 pb-0">
      <div class="mb-3">
        <label class="form-label small fw-bold">
          收件人姓名
          <span v-if="hasPhysicalProducts" class="text-danger">*</span>
        </label>
        <input type="text" class="form-control border p-2" placeholder="請輸入收件人姓名" v-model="name" />
      </div>
      <div class="mb-3">
        <label class="form-label small fw-bold">
          聯絡電話
          <span v-if="hasPhysicalProducts" class="text-danger">*</span>
        </label>
        <input type="tel" class="form-control border p-2" placeholder="09xx-xxx-xxx" v-model="phone" />
      </div>
      <div class="mb-3">
        <label class="form-label small fw-bold">
          寄送地址
          <span v-if="hasPhysicalProducts" class="text-danger">*</span>
        </label>
        <input type="text" class="form-control border p-2" placeholder="請輸入收件地址" v-model="address" />
      </div>
      <div class="mb-2">
        <label class="form-label small fw-bold">備註事項</label>
        <textarea v-model="remark" class="form-control border p-2" rows="3" placeholder="如有特殊需求請填寫"></textarea>
      </div>
      <small v-if="hasPhysicalProducts" class="text-muted">標示 * 為必填欄位</small>
    </div>

    <div class="card-footer bg-white border-top-0 pb-4 px-4">
      <button class="btn btn-custom-primary w-100 py-2 fw-bold shadow-sm mb-2" @click="loadShipAddresses"
        data-bs-toggle="collapse" data-bs-target="#shipAddressCollapse">
        取得過去填過的寄送資訊
      </button>
      <div class="collapse" id="shipAddressCollapse">
        <div>
          <select class="form-select border" v-model="selectedShippingRecord" @change="handleFastFill">
            <option :value="null">
              {{ userInfo && userInfo.length === 0 ? "尚無寄送紀錄" : "請選擇過去的寄送資訊" }}
            </option>
            <option v-for="(info, index) in userInfo" :key="index" :value="info">
              {{ info.recipientName }}/{{ info.recipientPhone }}/
              {{ info.recipientAddress }}
            </option>
          </select>
        </div>
      </div>
      <button class="btn btn-custom-primary w-100 py-2 fw-bold shadow-sm mt-2" :disabled="submitting" @click="checkout">
        {{ submitting ? "處理中..." : "確認結帳" }}
      </button>
    </div>
  </div>
</template>
