<script setup>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import OrderList from "@/views/PointShop/components/OrderList.vue";

const userId = 3; // 通常從登入資訊或 Pinia / Vuex 取得
</script>

<template>
  <DefaultNavbar light />

  <div class="min-vh-100 pb-5 ">
    <div class="container py-5">
      <div class="col-12 mb-4">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>
      <OrderList :userId="userId"></OrderList>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped></style>
