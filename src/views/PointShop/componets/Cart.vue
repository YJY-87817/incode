<script setup>
import { computed, ref } from "vue";

const cart = ref([
  { id: 1, name: "商品 A", price: 300, qty: 1 },
  { id: 2, name: "商品 B", price: 450, qty: 2 },
]);

const increase = (item) => item.qty++;
const decrease = (item) => {
  if (item.qty > 1) item.qty--;
};
const remove = (item) => {
  cart.value = cart.value.filter((i) => i.id !== item.id);
};

const total = computed(() =>
  cart.value.reduce((sum, i) => sum + i.price * i.qty, 0),
);
</script>

<template>
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
        class="btn-close"
        data-bs-dismiss="offcanvas"
      ></button>
    </div>

    <div class="offcanvas-body d-flex flex-column">
      <!-- 購物車清單 -->
      <div v-if="cart.length">
        <div
          class="d-flex justify-content-between align-items-center border-bottom py-2"
          v-for="item in cart"
          :key="item.id"
        >
          <div>
            <div class="fw-bold">{{ item.name }}</div>
            <small class="text-muted">
              ${{ item.price }} × {{ item.qty }}
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
            <button class="btn btn-sm btn-outline-danger" @click="remove(item)">
              ✕
            </button>
          </div>
        </div>
      </div>

      <div v-else class="text-center text-muted mt-5">購物車是空的</div>

      <!-- 底部總金額 -->
      <div class="mt-auto pt-3 border-top">
        <div class="d-flex justify-content-between fw-bold">
          <span>總金額</span>
          <span>${{ total }}</span>
        </div>
        <RouterLink
          :to="{ name: 'Checkout' }"
          class="dropdown-item border-radius-md"
        >
          <button class="btn w-100 mt-3">前往結帳</button>
        </RouterLink>
      </div>
    </div>
  </div>
</template>
