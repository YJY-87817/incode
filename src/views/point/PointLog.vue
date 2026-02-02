<script setup>
import { onMounted } from "vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import FooterCentered from "@/examples/footers/FooterCentered.vue";
import { usePointStore } from "@/stores/point/UsePointStore.js";
import PointSummary from "@/components/point/PointSummary.vue";
import PointFilter from "@/components/point/PointFilter.vue";
import PointTable from "@/components/point/PointTable.vue";

const store = usePointStore();
onMounted(() => store.fetchLogs());
</script>

<template>
<DefaultNavbar light />  <div class="container py-5">
    <div class="row g-4">
      <div class="col-lg-3">
        <PointSummary />
      </div>
      <div class="col-lg-9">
        <div class="card shadow-sm border-0 rounded-4 overflow-hidden">
          <PointFilter />
          <PointTable />
        </div>
      </div>
    </div>
  </div>
  <FooterCentered />
</template>
