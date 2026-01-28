<script setup>
import { onMounted } from "vue";
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";
import FooterCentered from "@/examples/footers/FooterCentered.vue";
import { useUserPointStore } from "@/stores/point/UserPoint.js";
import PointSummary from "@/components/point/PointSummary.vue";
import PointFilter from "@/components/point/PointFilter.vue";
import PointTable from "@/components/point/PointTable.vue";

const store = useUserPointStore();
onMounted(() => store.fetchLogs());
</script>

<template>
  <NavbarDefault class="custom-navbar" />
  <div class="container py-5">
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

<style scoped>
.custom-navbar {
  background-color: #86b0bd; /* 通常導覽列改背景色比較多 */
  color: black;
}
</style>
