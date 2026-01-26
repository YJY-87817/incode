<script setup>
import { ref, computed } from "vue";
import axios from "axios";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import CheckoutItem from "@/views/PointShop/components/Checkitem.vue";
import CheckoutPoint from "@/views/PointShop/components/CheckPoint.vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";

const CartNow = useCartStore();
</script>

<template>
  <DefaultNavbar light />

  <div class="min-vh-100 pb-5 ">
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

<style scoped></style>
