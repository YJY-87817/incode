<script setup>
import { ref } from 'vue';
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import OrderList from "@/views/PointShop/components/OrderList.vue";
import FilterOrder from "@/views/PointShop/components/filterorder.vue";

const selectedStatus = ref(''); // 預設顯示全部訂單
</script>

<template>
  <DefaultNavbar light />

  <div class="min-vh-100 pb-5">
    <div class="container pt-5 mb-5">
      <!-- <div class="col-12 mb-4">
        <div class="card border-0 shadow-sm" style="background-color: #86B0BD;">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">點數商城</h2>
          </div>
        </div>
      </div> -->
      <div class="row">

        <div class="col-lg-3">
          <FilterOrder v-model="selectedStatus" />
        </div>
        <div class="col-lg-9">

          <OrderList :status="selectedStatus"></OrderList>

        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped></style>
