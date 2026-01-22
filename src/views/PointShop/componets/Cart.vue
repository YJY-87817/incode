<script setup>
import { computed, ref } from "vue";
import { useCartStore } from "@/views/PointShop/services/cartStore";

const cart = ref([]);

// 取得購物車狀態
const CartNow = useCartStore();
// 增加、減少、移除商品
const increase = (item) => {
  item.quantity += 1;
  CartNow.updateQuantity(item.productId, item.quantity);
};

const decrease = (item) => {
  if (item.quantity > 1) item.quantity -= 1;
  CartNow.updateQuantity(item.productId, item.quantity);
};
</script>

<template>
  <!-- 購物車按鈕 -->

  <button
    class="btn position-fixed start-0 top-50 translate-middle-y me-3"
    data-bs-toggle="offcanvas"
    data-bs-target="#cartOffcanvas"
  >
    <i class="bi bi-cart-dash-fill h1"></i>
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
      <button type="button" class="btn btn-danger" @click="CartNow.clearCart()">
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
              class="btn btn-sm btn-outline-secondary"
              @click="decrease(item)"
            >
              −
            </button>
            <button
              class="btn btn-sm btn-outline-secondary"
              @click="increase(item)"
            >
              +
            </button>
            <button
              class="btn btn-sm btn-outline-danger"
              @click="CartNow.removeFromCart(item.productId)"
            >
              ✕
            </button>
          </div>
        </div>
      </div>

      <div v-else class="text-center text-muted mt-5">
        目前沒有任何商品在購物車
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
        <RouterLink
          :to="{ name: 'Checkout' }"
          class="dropdown-item border-radius-md"
        >
          <button
            class="btn btn-primary w-100 mt-3"
            data-bs-dismiss="offcanvas"
          >
            前往結帳
          </button>
        </RouterLink>
      </div>
    </div>
  </div>
</template>
