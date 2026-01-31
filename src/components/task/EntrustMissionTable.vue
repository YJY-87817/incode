<script setup>
import { computed } from "vue";
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();
const emit = defineEmits(["open-detail", "claim"]);

const list = computed(() => store.filteredList);
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
      目前沒有符合條件的委託任務，請嘗試調整篩選條件。
    </p>

    <button
      @click="store.clearFilters"
      class="btn btn-sm btn-outline-info border-radius-pill px-4 d-block mx-auto"
    >
      清除所有篩選
    </button>
  </div>

  <div v-else class="col-12">
    <div class="card border-0 shadow-sm">
      <div class="table-responsive">
        <table class="table align-items-center mb-0">
          <thead class="bg-gray-100">
            <tr>
              <th
                class="text-uppercase text-secondary text-xxs font-weight-bolder opacity-7 ps-4"
              >
                委託標題與發布者
              </th>
              <th
                class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
              >
                類別
              </th>
              <th
                class="text-center text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
              >
                賞金
              </th>
              <th
                class="text-end pe-4 text-uppercase text-secondary text-xxs font-weight-bolder opacity-7"
              >
                動作
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in list" :key="c.id" class="commission-row">
              <td class="ps-4">
                <div
                  class="d-flex align-items-center cursor-pointer"
                  @click="emit('open-detail', c)"
                >
                  <div
                    class="avatar avatar-sm bg-gradient-dark me-3 border-radius-md font-weight-bold"
                  >
                    {{ (c.requester || "匿名").substring(0, 1) }}
                  </div>
                  <div>
                    <h6 class="mb-0 text-sm font-weight-bold">
                      {{ c.title || "未命名委託" }}
                    </h6>
                    <span class="text-xxs text-secondary">
                      <i class="fas fa-user me-1"></i>
                      {{ c.requester || c.requesterName || "匿名影友" }}
                      ·
                      <i class="fas fa-clock me-1"></i>
                      {{ c.deadline || "無期限" }}
                    </span>
                  </div>
                  <span
                    v-if="c.urgent"
                    class="badge badge-xxs bg-gradient-danger ms-2"
                  >
                    急
                  </span>
                </div>
              </td>
              <td class="text-center">
                <span class="text-xs font-weight-bold">
                  {{ c.category || "其他" }}
                </span>
              </td>
              <td class="text-center">
                <span class="text-sm font-weight-bolder text-info">
                  {{ c.price || c.rewardPoints || 0 }}
                  <small>Pts</small>
                </span>
              </td>
              <td class="text-end pe-4">
                <button
                  @click="emit('open-detail', c)"
                  class="btn btn-link text-info text-xs mb-0 px-2"
                >
                  詳情
                </button>
                <button
                  v-if="!c.claimed && !c.completed"
                  @click="emit('claim', { type: 'entrust', commission: c })"
                  class="btn btn-sm bg-gradient-info mb-0"
                >
                  接取
                </button>
                <button
                  v-else-if="c.claimed"
                  class="btn btn-sm btn-outline-success shadow-none px-3 mb-0"
                  disabled
                >
                  已接取
                </button>
                <button
                  v-else
                  class="btn btn-sm btn-outline-secondary shadow-none px-3 mb-0"
                  disabled
                >
                  已完成
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.commission-row {
  transition: background-color 0.2s ease;
}

.commission-row:hover {
  background-color: rgba(17, 205, 239, 0.04);
}

.cursor-pointer {
  cursor: pointer;
}
</style>
