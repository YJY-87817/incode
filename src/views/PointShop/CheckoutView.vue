<script setup>
import { ref, computed } from "vue";
import axios from "axios";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import CheckoutItem from "@/components/PointShop/Checkitem.vue";
import CheckoutPoint from "@/components/PointShop/CheckPoint.vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

const CartNow = useCartStore();
</script>

<template>
  <DefaultNavbar darkText />

  <div class="min-vh-100 pb-5 bg">
    <div class="container pt-5 mb-5">
      <div class="col-12 mb-4">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>

      <div class="row">
        <div class="col-md-8">
          <CheckoutItem></CheckoutItem>
        </div>
        <div class="col-md-4">
          <CheckoutPoint></CheckoutPoint>
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped>
.bg {
background-color: #bfc6c4;
background-image: url("data:image/svg+xml,%3Csvg width='84' height='16' viewBox='0 0 84 16' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M78 7V4h-2v3h-3v2h3v3h2V9h3V7h-3zM30 7V4h-2v3h-3v2h3v3h2V9h3V7h-3zM10 0h2v16h-2V0zm6 0h4v16h-4V0zM2 0h4v16H2V0zm50 0h2v16h-2V0zM38 0h2v16h-2V0zm28 0h2v16h-2V0zm-8 0h6v16h-6V0zM42 0h6v16h-6V0z' fill='%23e8e2d8' fill-opacity='0.4' fill-rule='evenodd'/%3E%3C/svg%3E");
}
</style>
