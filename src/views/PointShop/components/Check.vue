<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import { postCreateOrder } from "@/views/PointShop/services/PointShopApi";
import checkoutSwal from "sweetalert2";
import { useAuthStore } from "@/stores/user/auth.js";

const authStore = useAuthStore();
const Islogin = authStore.isAuthenticated;
const router = useRouter();
const CartNow = useCartStore();
const remark = ref("");
const name = ref("");
const phone = ref("");
const address = ref("");
const userid = ref(0);

// 檢查是否有實體商品（isPhysical: true）
const hasPhysicalProducts = computed(() => {
  return CartNow.cartItems.some((item) => item.isPhysical === true);
});

// 驗證寄送資訊
const validateShippingInfo = () => {
  if (!hasPhysicalProducts.value) {
    return true; // 沒有實體商品，不需要驗證
  }

  // 檢查必填欄位
  if (!name.value.trim()) {
    checkoutSwal.fire({
      title: "必填欄位未完成",
      text: "請填寫收件人姓名",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return false;
  }

  if (!phone.value.trim()) {
    checkoutSwal.fire({
      title: "必填欄位未完成",
      text: "請填寫聯絡電話",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return false;
  }

  if (!address.value.trim()) {
    checkoutSwal.fire({
      title: "必填欄位未完成",
      text: "請填寫寄送地址",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return false;
  }

  // 驗證姓名格式（2-10個中文或英文字符）
  const nameRegex = /^[\u4e00-\u9fa5a-zA-Z\s]{2,10}$/;
  if (!nameRegex.test(name.value.trim())) {
    checkoutSwal.fire({
      title: "姓名格式錯誤",
      text: "姓名請輸入2-10個中文或英文字元",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return false;
  }

  // 驗證電話格式（台灣手機號碼：09開頭共10碼）
  const phoneRegex = /^09\d{8}$/;
  if (!phoneRegex.test(phone.value.trim())) {
    checkoutSwal.fire({
      title: "電話格式錯誤",
      text: "請輸入正確的手機號碼格式（09xxxxxxxx）",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return false;
  }

  // 驗證地址格式（至少5個字符）
  if (address.value.trim().length < 5) {
    checkoutSwal.fire({
      title: "地址格式錯誤",
      text: "地址至少需要5個字元",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return false;
  }

  return true;
};

const checkout = async () => {
  // 檢查是否登入
  if (Islogin === false) {
    checkoutSwal.fire({
      title: "請先登入會員!",
      text: "結帳前請先登入會員以建立訂單!",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return;
  }

  // 檢查購物車是否為空
  if (CartNow.cartItems.length == 0) {
    checkoutSwal.fire({
      title: "購物車為空!",
      text: "請先加入商品至購物車!",
      icon: "warning",
      didOpen: (modal) => {
        modal.querySelector(".swal2-confirm").style.color = "#fff";
      },
    });
    return;
  }

  const Neworder = {
    userId: userid.value,
    remark: remark.value,
    items: [],
    recipient: {
      orderId: 0,
      recipientName: name.value,
      recipientAddress: address.value,
      recipientPhone: phone.value,
      remark: remark.value,
    },
  };

  // 將購物車中的商品加入訂單
  for (const item of CartNow.cartItems) {
    Neworder.items.push({
      productId: item.productId,
      quantity: item.quantity,
    });
  }
  // 驗證寄送資訊
  if (!validateShippingInfo()) {
    return;
  }

  // 呼叫api建立訂單
  await postCreateOrder(Neworder)
    .then((result) => {
      if (result.data.ok) {
        CartNow.clearCart();
        checkoutSwal
          .fire({
            title: "訂單新增成功!",
            text: "您的訂單已成立將盡快為您處理!",
            icon: "success",
            didOpen: (modal) => {
              modal.querySelector(".swal2-confirm").style.color = "#fff";
            },
          })
          .then(() => {
            router.push("/PointShop/Order");
          });
      } else {
        if (result.data.message === "您的點數不足") {
          checkoutSwal
            .fire({
              title: "訂單新增失敗!",
              text:
                result.data.message || "您的點數不足，請重新選擇商品或增加點數",
              icon: "error",
              didOpen: (modal) => {
                modal.querySelector(".swal2-confirm").style.color = "#fff";
              },
              showDenyButton: true,
              confirmButtonText: "前往點數明細",
              denyButtonText: "返回",
            })
            .then((result) => {
              if (result.isConfirmed) {
                router.push("/point-log"); // 根據你的儲值頁面路由調整
              }
            });
          return;
        }
        checkoutSwal.fire({
          title: "訂單新增失敗!",
          text: result.data.message || "訂單建立失敗，請稍後再試",
          icon: "error",
          didOpen: (modal) => {
            modal.querySelector(".swal2-confirm").style.color = "#fff";
          },
        });
        return;
      }
    })
    .catch((error) => {
      console.error("訂單建立失敗:", error);
      checkoutSwal.fire({
        title: "訂單建立失敗!",
        text: "訂單建立失敗，請稍後再試",
        icon: "error",
        didOpen: (modal) => {
          modal.querySelector(".swal2-confirm").style.color = "#fff";
        },
      });
    })
    .finally();
};
</script>

<template>
  <div class="card shadow-sm border-0 rounded-4">
    <div
      class="card-header border-bottom-0 pt-4 px-4"
      style="background-color: #86b0bd"
    >
      <h5 class="fw-bold mb-0">
        <i class="bi bi-truck me-2 text-primary"></i>寄送資訊
      </h5>
    </div>

    <div class="card-body px-4">
      <div class="mb-3">
        <label class="form-label small fw-bold">
          收件人姓名
          <span v-if="hasPhysicalProducts" class="text-danger">*</span>
        </label>
        <input
          type="text"
          class="form-control border p-2"
          placeholder="請輸入收件人姓名"
          v-model="name"
        />
      </div>
      <div class="mb-3">
        <label class="form-label small fw-bold">
          聯絡電話
          <span v-if="hasPhysicalProducts" class="text-danger">*</span>
        </label>
        <input
          type="tel"
          class="form-control border p-2"
          placeholder="09xxxxxxxx"
          v-model="phone"
        />
      </div>
      <div class="mb-3">
        <label class="form-label small fw-bold">
          寄送地址
          <span v-if="hasPhysicalProducts" class="text-danger">*</span>
        </label>
        <input
          type="text"
          class="form-control border p-2"
          placeholder="請輸入收件地址"
          v-model="address"
        />
      </div>
      <div class="mb-2">
        <label class="form-label small fw-bold">備註事項</label>
        <textarea
          v-model="remark"
          class="form-control border p-2"
          rows="3"
          placeholder="如有特殊需求請填寫"
        ></textarea>
      </div>
      <small v-if="hasPhysicalProducts" class="text-muted"
        >標示 * 為必填欄位</small
      >
    </div>
    <div class="card-footer bg-white border-top-0 pb-4 px-4">
      <button
        class="btn btn-custom-primary w-100 py-2 fw-bold shadow-sm"
        @click="checkout"
      >
        確認結帳
      </button>
    </div>
  </div>
</template>

<style scoped>
.btn-custom-primary {
  color: #145369;
  background-color: transparent;
  border: 1px solid #145369 !important;
}

.btn-custom-primary:hover {
  color: #fff;
  background-color: #145369;
  border-color: #145369;
}
</style>
