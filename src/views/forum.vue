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
  論壇
</template>
