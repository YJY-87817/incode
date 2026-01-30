<!-- src/components/mission-hall/EntrustMissionTable.vue -->
<script setup>
import { useTaskStore } from "@/stores/point/UseTaskStore";

const store = useTaskStore();

const emit = defineEmits(["open-detail", "claim"]);
</script>

<template>
  <div class="col-12">
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
            <tr
              v-for="c in store.filteredList"
              :key="c.id"
              class="commission-row"
            >
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
                  @click="emit('claim', c.title)"
                  class="btn btn-sm bg-gradient-info mb-0"
                >
                  接取
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
