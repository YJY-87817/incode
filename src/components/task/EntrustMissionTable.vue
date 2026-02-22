<script setup>
import { computed, ref } from "vue";
import { useTaskStore } from "@/stores/point/UseTaskStore";
import { useRouter } from "vue-router";
import Swal from "sweetalert2";

const store = useTaskStore();
const router = useRouter();
const emit = defineEmits(["open-detail", "claim"]);

const isClaiming = ref(false);

const list = computed(() => store.filteredList);

const handleClaim = async ({ type, commission }) => {
  // 檢查 ID，相容後端不同的命名慣例
  const taskId = commission.id || commission.entrustMissionId;
  if (!taskId || isClaiming.value) return;

  // 1. 確認視窗
  const confirmResult = await Swal.fire({
    title: "確定接取？",
    text: `您即將接取委託：「${commission.title || "未命名委託"}」`,
    icon: "question",
    showCancelButton: true,
    confirmButtonColor: "#145369",
    cancelButtonColor: "#86B0BD",
    confirmButtonText: "確定接取",
    cancelButtonText: "取消",
    reverseButtons: true,
    customClass: {
      confirmButton: "text-white",
      cancelButton: "text-white",
    },
  });

  if (!confirmResult.isConfirmed) return;

  isClaiming.value = true;
  const originalStatus = commission.claimed;

  try {
    // 呼叫 Pinia action
    await store.claimTask(type, taskId);

    // 前端樂觀更新
    commission.claimed = true;

    // 2. 成功提示並引導跳轉
    const nextStep = await Swal.fire({
      title: "接取成功！",
      text: "任務已加入您的執行清單。",
      icon: "success",
      showCancelButton: true,
      confirmButtonColor: "#145369",
      cancelButtonColor: "#86B0BD",
      confirmButtonText: "前往我的任務",
      cancelButtonText: "留在本頁",
      reverseButtons: true,
      customClass: {
        confirmButton: "text-white",
        cancelButton: "text-white",
      },
    });

    // 點擊「前往我的任務」跳轉至 /mytask
    if (nextStep.isConfirmed) {
      router.push("/mytask");
    } else if (type === "entrust" && commission.contentId) {
      // 如果留在本頁且是委託，按原邏輯視需求決定是否跳討論區
      // 若想完全手動控制，此處可保留空白
    }
  } catch (err) {
    // 失敗時回滾狀態
    commission.claimed = originalStatus;
    Swal.fire({
      icon: "error",
      title: "接取失敗",
      text: err.message || "接取失敗，請稍後再試",
      confirmButtonColor: "#145369",
      customClass: { confirmButton: "text-white" },
    });
  } finally {
    isClaiming.value = false;
  }
};
</script>

<template>
  <div v-if="store.isLoading" class="text-center py-8">
    <div class="spinner-border custom-text-main" role="status"></div>
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
      目前沒有符合條件的任務，請嘗試調整篩選條件。
    </p>
    <button
      @click="store.clearFilters"
      class="btn btn-sm custom-btn-primary d-block mx-auto px-4"
    >
      清除所有篩選
    </button>
  </div>

  <div v-else class="col-12">
    <div class="card border-0 shadow-sm">
      <div class="table-responsive">
        <table class="table align-items-center mb-0">
          <thead class="custom-thead">
            <tr>
              <th
                class="text-uppercase text-xxs font-weight-bolder opacity-9 ps-4"
              >
                委託標題與發布者
              </th>
              <th
                class="text-center text-uppercase text-xxs font-weight-bolder opacity-9"
              >
                類別
              </th>
              <th
                class="text-center text-uppercase text-xxs font-weight-bolder opacity-9"
              >
                賞金
              </th>
              <th
                class="text-end pe-4 text-uppercase text-xxs font-weight-bolder opacity-9"
              >
                動作
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="c in list"
              :key="c.entrustMissionId || c.id"
              class="commission-row"
            >
              <td class="ps-4">
                <div
                  class="d-flex align-items-center cursor-pointer"
                  @click="emit('open-detail', c)"
                >
                  <img
                    v-if="c.imageUrl"
                    :src="c.imageUrl"
                    class="avatar avatar-sm me-3 border-radius-md"
                  />
                  <div
                    v-else
                    class="avatar avatar-sm custom-bg-main me-3 border-radius-md font-weight-bold d-flex align-items-center justify-content-center text-white"
                  >
                    {{ (c.username || "匿").substring(0, 1) }}
                  </div>
                  <div>
                    <h6 class="mb-0 text-sm font-weight-bold text-dark">
                      {{ c.title || "未命名委託" }}
                    </h6>
                    <span class="text-xxs" style="color: #145369">
                      <i class="fas fa-user me-1"></i>
                      {{ c.username || "匿名影友" }} ·
                      <i class="fas fa-clock me-1"></i>
                      {{ c.deadline || "無期限" }}
                    </span>
                  </div>
                  <span
                    v-if="c.urgent"
                    class="badge badge-xxs bg-gradient-danger ms-2"
                    >急</span
                  >
                </div>
              </td>
              <td class="text-center">
                <span class="text-xs font-weight-bold">{{
                  c.category || "其他"
                }}</span>
              </td>
              <td class="text-center">
                <span class="text-sm font-weight-bolder custom-text-main">
                  {{ c.price || c.rewardPoints || 0 }} <small>Pts</small>
                </span>
              </td>
              <td class="text-end pe-4">
                <button
                  @click="emit('open-detail', c)"
                  class="btn btn-link custom-text-main text-xs mb-0 px-2"
                >
                  詳情
                </button>

                <button
                  v-if="!c.claimed"
                  :disabled="isClaiming"
                  @click="handleClaim({ type: 'entrust', commission: c })"
                  class="btn btn-sm custom-btn-primary mb-0"
                >
                  {{ isClaiming ? "處理中..." : "接取" }}
                </button>

                <span
                  v-else
                  class="text-success text-xs font-weight-bolder px-3"
                >
                  <i class="fas fa-check-circle me-1"></i>已接取
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 確保 Swal 按鈕文字為白色 */
:global(.swal2-confirm),
:global(.swal2-cancel) {
  color: #ffffff !important;
}

.custom-text-main {
  color: #145369 !important;
}
.custom-bg-main {
  background-color: #86b0bd !important;
}

.custom-btn-primary {
  background-color: #fff !important;
  color: #145369 !important;
  border: 1px solid #145369 !important;
  transition: all 0.3s ease;
}
.custom-btn-primary:hover {
  background-color: #145369 !important;
  color: #fff !important;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.custom-thead {
  background-color: #f8f9fa;
  color: #145369;
}

.commission-row:hover {
  background-color: rgba(134, 176, 189, 0.1);
}

.avatar-sm {
  width: 36px;
  height: 36px;
  object-fit: cover;
}
</style>
