<script setup>
import { useCartStore } from "@/stores/pointshop/cartStore";
import { confirmDialog } from "@/utils/pointShopSwal";
import "@/assets/css/pointshop.css";

// 取得購物車狀態
const CartNow = useCartStore();
// 增加、減少、移除商品
const increase = (item) => {
  CartNow.updateQuantity(item.productId, item.quantity + 1);
};

const decrease = (item) => {
  if (item.quantity > 1) CartNow.updateQuantity(item.productId, item.quantity - 1);
};

// 清空前先確認，避免誤按
const clearCart = async () => {
  if (CartNow.cartItems.length === 0) return;
  const confirmed = await confirmDialog({
    title: "確定要清空購物車嗎?",
    confirmText: "清空",
  });
  if (confirmed) CartNow.clearCart();
};
</script>

<template>
  <!-- 購物車按鈕 -->
  <div>
    <button
      class="cart-side-btn d-lg-none"
      data-bs-toggle="offcanvas"
      data-bs-target="#cartOffcanvas"
    >
      <div class="position-relative d-inline-block d-flex align-items-center">
        <i class="bi bi-cart-dash-fill h4 mb-0 text-white"></i>
        <span
          v-if="CartNow.totalItems > 0"
          class="badge rounded-pill bg-danger position-absolute top-0 start-100 translate-middle"
          style="font-size: 0.6rem; padding: 0.2rem 0.4rem"
        >
          {{ CartNow.totalItems > 99 ? "99+" : CartNow.totalItems }}
        </span>
      </div>
    </button>

    <!-- 左側購物車 -->
    <div
      class="offcanvas offcanvas-start"
      data-bs-scroll="true"
      tabindex="-1"
      id="cartOffcanvas"
    >
      <div class="offcanvas-header">
        <h5 class="offcanvas-title">
          <i class="bi bi-cart-dash-fill h1"></i> 購物車
        </h5>
        <button
          type="button"
          class="btn btn-custom-primary"
          :disabled="CartNow.cartItems.length === 0"
          @click="clearCart"
        >
          清空購物車
        </button>
      </div>

      <div class="offcanvas-body d-flex flex-column">
        <!-- 購物車清單 -->
        <div v-if="CartNow.cartItems.length > 0">
          <div
            class="d-flex justify-content-between align-items-center border-bottom py-2"
            v-for="item in CartNow.cartItems"
            :key="item.productId"
          >
            <div>
              <div class="fw-bold">{{ item.name }}</div>
              <small class="text-muted">
                {{ item.point }} p × {{ item.quantity }}
              </small>
            </div>

            <div class="d-flex align-items-center gap-2">
              <button
                class="btn btn-sm btn-custom-primary"
                style="font-size: 0.875rem"
                :disabled="item.quantity >= item.stock"
                @click="increase(item)"
              >
                +
              </button>
              <button
                class="btn btn-sm btn-custom-primary"
                style="font-size: 0.875rem"
                @click="decrease(item)"
              >
                −
              </button>
              <button
                class="btn btn-sm btn-custom-primary"
                style="font-size: 0.875rem"
                @click="CartNow.removeFromCart(item.productId)"
              >
                <i class="bi bi-trash" style="font-size: 0.875rem"></i>
              </button>
            </div>
          </div>
        </div>

        <div
          v-else
          class="flex-grow-1 d-flex flex-column justify-content-center align-items-center text-muted"
        >
          目前沒有任何商品在購物車
          <br />
          <router-link
            :to="{ name: 'PointShop' }"
            class="btn btn-custom-primary mt-3"
            data-bs-dismiss="offcanvas"
          >
            繼續逛逛
          </router-link>
        </div>

        <!-- 底部總金額 -->
        <div class="mt-auto pt-3 border-top">
          <div class="d-flex justify-content-between fw-bold">
            <span>總點數</span>
            <span>{{ CartNow.totalPoints }} p</span>
          </div>
          <div class="d-flex justify-content-between fw-bold">
            <span>數量</span>
            <span>{{ CartNow.totalItems }} 件</span>
          </div>
          <router-link
            :to="{ name: 'Checkout' }"
            class="btn btn-custom-primary w-100 mt-3"
            :class="{ disabled: CartNow.cartItems.length === 0 }"
            data-bs-dismiss="offcanvas"
          >
            前往結帳
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>

/* 手機版側邊購物車按鈕樣式 */
.cart-side-btn {
  position: fixed;
  top: 15%;
  right: -10px;
  /* transform: translateX(75px); 移除位移，保持固定顯示 */
  background: linear-gradient(135deg, #145369 0%, #0c3645 100%);
  color: white;
  border: none;
  width: auto;
  height: 45px;
  border-radius: 30px 0 0 30px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 15px 0 20px;
  gap: 8px;
  /* box-shadow: -4px 0 15px rgba(20, 83, 105, 0.4); */
  z-index: 9990;
  /* transition: transform 0.3s ease-out, box-shadow 0.3s ease; 移除動畫 */
}

.cart-side-btn:hover,
.cart-side-btn:active {
  /* transform: translateX(0); 移除 Hover 動畫 */
  box-shadow: -6px 0 20px rgba(20, 83, 105, 0.6);
}

</style>
