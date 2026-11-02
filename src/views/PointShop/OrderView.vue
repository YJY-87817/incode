<script setup>
import { onMounted, ref } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import OrderList from "@/views/PointShop/components/OrderList.vue";
import FilterOrder from "@/views/PointShop/components/filterorder.vue";
import "@/views/PointShop/styles/pointshop.css";

const selectedStatus = ref(""); // 預設顯示全部訂單
onMounted(() => {
  document.body.classList.add("bg-grey-lighten-5"); // 全局背景色
});
</script>

<template>
  <DefaultNavbar light />
  <div class="min-vh-100 pb-5">
    <div class="container pt-5 mb-5">
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

