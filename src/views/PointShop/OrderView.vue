<script setup>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import { getOrdersByUser } from "@/Views/PointShop/services/orderapi";
import orderlist from "@/views/PointShop/componets/OrderList.vue";

const userId = 1; // 通常從登入資訊或 Pinia / Vuex 取得
</script>

<template>
  <DefaultNavbar :action="{ route: 'javascript:;' }" />
  <div class="container mt-5 mb-5">
    <div class="row mb-4">
      <div class="col-12">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>
    </div>
    <orderlist :userId="userId"></orderlist>
  </div>
  <DefaultFooter />
</template>

<style scoped></style>
