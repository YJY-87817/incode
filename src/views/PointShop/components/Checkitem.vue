<script setup>
import { useCartStore } from "@/views/PointShop/services/cartStore";
import { productImageUrl } from "@/views/PointShop/services/pointShopUtils";

// 取得購物車狀態
const CartNow = useCartStore();
// 增加、減少、移除商品
const increase = (item) => {
  CartNow.updateQuantity(item.productId, item.quantity + 1);
};

const decrease = (item) => {
  if (item.quantity > 1) CartNow.updateQuantity(item.productId, item.quantity - 1);
};

</script>

<template>
  <div class="col-12">
    <div class="card shadow-sm border-0 rounded-4">
      <div class="card-header border-bottom-0 pt-4 px-4" style="background-color: #86B0BD;">
        <h4 class="fw-bold mb-0">
          <i class="bi bi-cart3 me-2"></i>購物清單
        </h4>
      </div>

      <div class="card-body px-4">
        <div v-if="CartNow.cartItems.length > 0" class="d-flex flex-column gap-3">
          <!-- 購物車清單範例 -->
          <div v-for="item in CartNow.cartItems" :key="item.productId"
            class="d-flex align-items-center p-3 border rounded-3 bg-light-hover transition-all">

            <div class="d-flex align-items-center flex-grow-1">
              <img :src="productImageUrl(item.imageName)"
                class="rounded flex-shrink-0 object-fit-cover border" style="width: 65px; height: 65px;" />
              <div class="ms-3">
                <h6 class="fw-bold mb-0 text-dark">{{ item.name }}</h6>
              </div>
            </div>

            <div class="d-flex align-items-center gap-4 px-3">
              <div class="text-center">
                <small class="text-secondary d-block">數量</small>
                <span class="fw-medium">{{ item.quantity }}</span>
              </div>
              <div class="text-end" style="min-width: 80px;">
                <small class="text-secondary d-block">小計</small>
                <span class="fw-bold" style="color: #86B0BD;">{{ item.point * item.quantity }} p</span>
              </div>
            </div>

            <div class="ms-2 ms-md-3 border-start ps-2 ps-md-3 py-1">
              <div class="d-flex flex-column flex-md-row align-items-center gap-1 gap-md-2">
                <button class="btn btn-sm btn-custom-primary" style="font-size: 0.75rem;"
                  :disabled="item.quantity >= item.stock" @click="increase(item)">+</button>
                <button class="btn btn-sm btn-custom-primary" style="font-size: 0.75rem;"
                  @click="decrease(item)">−</button>
                <button class="btn btn-sm btn-custom-primary" style="font-size: 0.75rem;"
                  @click="CartNow.removeFromCart(item.productId)">
                  <i class="bi bi-trash" style="font-size: 0.75rem;"></i>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div v-else class="text-center text-muted my-5">
          目前沒有任何商品在購物車
          <div class="mt-1">
            <router-link :to="{ name: 'PointShop' }" class="btn btn-custom-primary">去逛逛</router-link>
          </div>
        </div>
      </div>
      <div class="card-footer bg-white border-top-0 pb-4 px-4 text-end">
        <hr class="text-muted opacity-25">
        <div class="d-flex justify-content-end align-items-baseline gap-2">
          <span class="text-secondary small">共 {{ CartNow.totalItems }} 項商品，總計：</span>
          <span class="fs-4 fw-bold" style="color: #86B0BD;">{{ CartNow.totalPoints }} P</span>
        </div>
      </div>

    </div>
  </div>
</template>

<style scoped>
/* 簡單的微互動效果 */
.transition-all {
  transition: all 0.2s ease-in-out;
}

.bg-light-hover:hover {
  background-color: #f8f9fa;
  border-color: #dee2e6 !important;
  transform: translateY(-2px);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.object-fit-cover {
  object-fit: cover;
}

</style>