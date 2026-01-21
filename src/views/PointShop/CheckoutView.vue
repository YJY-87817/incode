<script setup>
import { ref, computed } from "vue";
import axios from "axios";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
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
      <orderlist :userId="userId"></orderlist>
    </div>
  </div>
  <DefaultFooter />
</template>

<style scoped>
/* Bootstrap 預設顏色覆蓋 */
:deep(.bg-light) {
  background-color: #f8f9fa !important;
}
</style>
