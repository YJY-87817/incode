<script setup>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import OrderList from "@/views/PointShop/components/OrderList.vue";
import NavbarsCard from "@/components/NavbarCard.vue";


// const userId = 3; // 通常從登入資訊或 Pinia / Vuex 取得
</script>

<template>
  <DefaultNavbar light />

  <div class="min-vh-100 pb-5">
    <div class="container pt-5 mb-5">
      <div class="col-12 mb-4">
        <div class="card border-0 shadow-sm" style="background-color: #86B0BD;">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div>
      <div class="row">

        <div class="col-lg-3">
          <NavbarsCard></NavbarsCard>
        </div>
        <div class="col-lg-9">

          <OrderList></OrderList>

        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped></style>
