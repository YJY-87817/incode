<script setup>
import { ref, computed } from "vue";
import axios from "axios";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

const CartNow = useCartStore();
</script>

<template>
  <DefaultNavbar darkText />
  <div class="min-vh-100 pb-5">
    <div class="container mt-5 mb-5">
      <div class="col-12 mb-4">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>
    </div>
  </div>
  <div v-if="CartNow.cartItems.length > 0"></div>
  <div v-else class="text-center text-muted mt-5">目前沒有任何商品在購物車</div>
  <DefaultFooter />
</template>

<style scoped></style>
