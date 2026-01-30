<!-- src/components/mission-hall/TaskTabs.vue -->
<script setup>
defineProps({
  activeTab: {
    type: String,
    required: true,
    default: "mission",
  },
});

const emit = defineEmits(["change"]);
</script>

<template>
  <div class="row mb-4">
    <div class="col-md-12">
      <div class="card shadow-blur blur border-0 mb-4">
        <div class="card-body p-3">
          <div class="row align-items-center">
            <!-- Tab 導航 -->
            <!-- <div class="col-lg-5"> -->
            <div class="nav-wrapper position-relative">
              <ul
                class="nav nav-pills nav-fill p-1 bg-gray-100 border-radius-lg"
              >
                <li class="nav-item">
                  <button
                    @click="emit('change', 'mission')"
                    :class="[
                      'nav-link mb-0 px-0 py-2 border-0 w-100 transition-all',
                      activeTab === 'mission'
                        ? 'active bg-white shadow font-weight-bold text-info'
                        : 'bg-transparent text-dark',
                    ]"
                  >
                    <i class="fas fa-certificate me-2 text-xs"></i>
                    官方挑戰
                  </button>
                </li>
                <li class="nav-item">
                  <button
                    @click="emit('change', 'entrustMission')"
                    :class="[
                      'nav-link mb-0 px-0 py-2 border-0 w-100 transition-all',
                      activeTab === 'entrustMission'
                        ? 'active bg-white shadow font-weight-bold text-info'
                        : 'bg-transparent text-dark',
                    ]"
                  >
                    <i class="fas fa-users me-2 text-xs"></i>
                    影友委託
                  </button>
                </li>
              </ul>
            </div>
            <!-- </div> -->

            <!-- 右側插槽：搜尋、發布按鈕等（由父組件決定放什麼） -->
            <!-- <div class="col-lg-7 mt-3 mt-lg-0">
              <slot name="right-content"></slot>
            </div> -->
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.transition-all {
  transition: all 0.25s ease-in-out;
}
</style>
