<script setup>
import { computed } from "vue";
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();
const emit = defineEmits(["open-detail", "claim"]);

const list = computed(() => store.filteredList);

function handleClaim(mission) {
  emit("claim", { type: "platform", mission });
}
</script>

<template>
  <div v-if="store.isLoading" class="text-center py-8">
    <div class="spinner-border text-info" role="status"></div>
    <p class="mt-3 text-secondary">載入任務中...</p>
  </div>

  <div v-else-if="store.error" class="alert alert-danger text-center py-5">
    {{ store.error }}
    <button
      @click="store.fetchTasks"
      class="btn btn-sm btn-outline-danger mt-2"
    >
      重試
    </button>
  </div>

  <div v-else-if="list.length === 0" class="empty-state-wrapper py-5">
    <h5 class="text-dark font-weight-bold mb-1 text-center">找不到相關任務</h5>
    <p class="text-secondary text-sm mb-4 text-center">
      目前沒有符合條件的參與任務，請嘗試調整篩選條件。
    </p>

    <button
      @click="store.clearFilters"
      class="btn btn-sm btn-outline-info border-radius-pill px-4 d-block mx-auto"
    >
      清除所有篩選
    </button>
  </div>

  <div v-else class="row row-cols-2 row-cols-lg-3 g-3 mb-0">
    <div v-for="m in list" :key="m.id" class="col d-flex align-items-stretch">
      <div class="card border-0 shadow-sm w-100 mission-card mb-0">
        <div class="card-body p-3 d-flex flex-column">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div
              class="icon icon-shape shadow-sm border-radius-md text-white p-2"
              :class="m.color || 'bg-gradient-info'"
              style="width: 36px; height: 36px"
            >
              <i :class="m.icon || store.getCategoryIcon(m.category)"></i>
            </div>

            <span
              v-if="m.featured"
              class="badge badge-xxs bg-gradient-warning shadow-none"
              >特色</span
            >
          </div>

          <h6 class="font-weight-bolder text-dark mb-1 text-truncate-2">
            {{ m.title }}
          </h6>
          <p class="text-xs text-secondary mb-3 flex-grow-1 text-truncate-3">
            {{ m.desc }}
          </p>

          <div
            class="d-flex justify-content-between align-items-end pt-2 border-top mt-auto"
          >
            <div class="reward-info">
              <span class="text-sm font-weight-bolder text-dark d-block">
                +{{ m.reward }} Pts
              </span>
              <span class="text-xxs text-secondary">
                <i class="fas fa-users me-1"></i>{{ m.participants }} 人
              </span>
            </div>

            <button
              v-if="!m.claimed && !m.completed"
              class="btn btn-sm btn-info shadow-none px-3 mb-0"
              @click="handleClaim(m)"
            >
              領取
            </button>

            <button
              v-else-if="m.claimed"
              class="btn btn-sm btn-outline-success shadow-none px-3 mb-0"
              disabled
            >
              已領取
            </button>

            <button
              v-else
              class="btn btn-sm btn-outline-secondary shadow-none px-3 mb-0"
              disabled
            >
              已完成
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mission-card {
  transition: all 0.3s ease;
  border-top: 4px solid transparent;
}

.mission-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 15px 30px -5px rgba(0, 0, 0, 0.1) !important;
  border-top-color: #11cdef;
}

@media (max-width: 576px) {
  .card-body {
    padding: 1rem !important;
  }
}
</style>
