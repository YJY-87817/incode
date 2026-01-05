<script setup>
import { reactive } from "vue";

//example components
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
</script>

<template>
  <DefaultNavbar
    :action="{
      route: 'javascript:;',
    }"
  />
  <div>
    <h3>論壇</h3>
  </div>
  <DefaultFooter />
</template>
