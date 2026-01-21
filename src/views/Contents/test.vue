<script setup>
import axios from "axios";
import { reactive, ref } from "vue";

import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

const responseData = ref("");
function btnGet() {
  axios
    .get("https://localhost:7190/api/ContentApi")
    .then((resp) => {
      console.log(resp);
      responseData.value = resp.data;
    })
    .catch((error) => {
      console.error(error);
    });
}
</script>

<template>
  <DefaultNavbar light />

  <div class="container mt-5 mb-5">
    <button @click="btnGet()">GET</button>
    <div>
      回傳結果:
      <br />
      {{ responseData }}
    </div>
  </div>
  <DefaultFooter />
</template>
