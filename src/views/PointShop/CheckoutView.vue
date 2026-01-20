<script setup>
import { ref, computed } from "vue";
import axios from "axios";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
</script>

<template>
  <DefaultNavbar transparent />
  <div class="bg-gray-200 min-vh-100 pb-5">
    <header class="position-relative">
      <div
        class="page-header min-height-200 border-radius-xl mt-4 mx-4"
        style="
          background-image: url('https://images.unsplash.com/photo-1557683316-973673baf926?auto=format&fit=crop&w=1920&q=80');">
        <span class="mask bg-gradient-dark opacity-6"></span>
      </div>
    </header>
    <div class="container mt-5 mb-5">
    <div class="row mb-4">
      <div class="col-12">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">購物車</h2>
          </div>
        </div>
      </div>
    </div>
    </div>
    </div>
  <DefaultFooter />
</template>
