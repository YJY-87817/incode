<script setup>
import { onMounted, ref } from "vue";
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import DefaultFooter from "@/components/layout/FooterCentered.vue";
import OrderList from "@/components/pointshop/OrderList.vue";
import FilterOrder from "@/components/pointshop/FilterOrder.vue";
import "@/assets/css/pointshop.css";

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

