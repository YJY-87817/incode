<script setup>
import { ref, computed, onMounted } from 'vue';

const emit = defineEmits(['update:modelValue']);
const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  }
});

const selectedStatus = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
});

const statuses = [
  { value: '', label: '全部訂單' },
  { value: '待處理', label: '待處理' },
  { value: '已出貨', label: '已出貨' },
  { value: '已完成', label: '已完成' },
  { value: '已取消', label: '已取消' },
  { value: '其他', label: '其他' }
];

const filterCollapseRef = ref(null);
const filterHeaderRef = ref(null);

onMounted(() => {
  const mdBreakpoint = 768;

  if (window.innerWidth >= mdBreakpoint) {
    if (filterCollapseRef.value && filterHeaderRef.value) {
      filterCollapseRef.value.classList.add("show");
      filterHeaderRef.value.classList.remove("collapsed");
      filterHeaderRef.value.setAttribute("aria-expanded", "true");
    }
  }
});
</script>

<template>
  <div class="card shadow-sm border-0 mb-5 rounded sticky-top" style="top: 80px">
    <div
      ref="filterHeaderRef"
      class="card-header text-white d-flex justify-content-between align-items-center collapsed"
      data-bs-toggle="collapse"
      data-bs-target="#filterCollapse"
      aria-expanded="false"
      aria-controls="filterCollapse"
      style="cursor: pointer; background-color: #86B0BD"
    >
      <h5 class="fw-bold mb-0">
        <i class="bi bi-funnel me-2"></i>訂單篩選
      </h5>
      <i class="bi bi-chevron-down"></i>
    </div>
    <div class="collapse" id="filterCollapse" ref="filterCollapseRef">
      <div class="card-body p-0">
        <div class="list-group list-group-flush">
          <button
            v-for="status in statuses"
            :key="status.value"
            @click="selectedStatus = status.value"
            :class="[
              'list-group-item list-group-item-action fw-bold',
              selectedStatus === status.value
                ? 'active'
                : ''
            ]"
            style="--bs-list-group-active-bg: #145369; --bs-list-group-active-border-color: #145369;"
          >
            {{ status.label }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sticky-top {
  z-index: 10;
}

/* 當 header 沒有 .collapsed class 時 (代表展開中)，箭頭旋轉 180度 */
.card-header:not(.collapsed) .bi-chevron-down {
  transform: rotate(180deg);
}

/* 加上過渡動畫效果 */
.bi-chevron-down {
  transition: transform 0.3s ease;
}

/* 修正收合時底部圓角消失的問題 */
.card-header.collapsed {
  border-bottom-left-radius: 0.375rem;
  border-bottom-right-radius: 0.375rem;
}

.list-group-item-action:hover {
  background-color: #f0f0f0;
}
</style>