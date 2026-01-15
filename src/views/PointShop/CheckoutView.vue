<script setup>
import { ref, computed } from "vue";
import axios from "axios";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
</script>

<template>
  <DefaultNavbar :action="{ route: 'javascript:;' }" />

  <DefaultFooter />
</template>
