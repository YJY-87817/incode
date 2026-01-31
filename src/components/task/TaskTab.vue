<script setup>
import { useTaskStore } from "@/stores/point/UseTaskStore";

const props = defineProps({
  activeTab: {
    type: String,
    default: "mission",
  },
});

const emit = defineEmits(["change"]);
const store = useTaskStore();

function changeTab(tab) {
  emit("change", tab);
}
</script>

<template>
  <div class="nav-wrapper position-relative mb-3">
    <ul
      class="nav nav-pills nav-fill p-1 bg-white border-radius-lg shadow-sm flex-row justify-content-center mx-auto"
      style="max-width: 400px"
    >
      <li class="nav-item">
        <a
          class="nav-link mb-0 px-0 py-2"
          :class="{ active: store.activeTab === 'mission' }"
          @click="changeTab('mission')"
          href="javascript:;"
        >
          <i class="fas fa-flag text-sm me-2"></i>
          <span class="ms-1">官方挑戰</span>
        </a>
      </li>
      <li class="nav-item">
        <a
          class="nav-link mb-0 px-0 py-2"
          :class="{ active: store.activeTab === 'entrust' }"
          @click="changeTab('entrust')"
          href="javascript:;"
        >
          <i class="fas fa-users text-sm me-2"></i>
          <span class="ms-1">影友委託</span>
        </a>
      </li>
    </ul>

    <div class="tab-content-slot mt-2">
      <slot name="bottom-content"></slot>
    </div>
  </div>
</template>

<style scoped>
/* 膠囊式切換樣式：視覺上更緊湊且無邊框空白 */
.nav-pills .nav-link {
  transition: all 0.3s ease;
  border-radius: 0.5rem;
  color: #67748e;
  font-weight: 600;
  cursor: pointer;
}

.nav-pills .nav-link.active {
  background-image: linear-gradient(310deg, #11cdef 0%, #1171ef 100%);
  color: #fff !important;
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.1),
    0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

.nav-link:hover:not(.active) {
  background-color: #f8f9fa;
}

/* 移除 nav-tabs 預設的下劃線，避免產生不必要的視覺斷層 */
.nav-tabs {
  border-bottom: none;
}
</style>
