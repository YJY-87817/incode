<script setup>
import { computed } from "vue";
import { useRouter } from "vue-router";
import { useTaskStore } from "@/stores/point/UseTaskStore";
import Swal from "sweetalert2";

const store = useTaskStore();
const router = useRouter();
const emit = defineEmits(["claim"]);
const list = computed(() => {
  return [...store.filteredList].sort((a, b) => a.id - b.id);
});

// 領取任務邏輯
async function handleClaim(mission) {
  const result = await Swal.fire({
    title: "確定領取？",
    text: `您即將開始任務：「${mission.title}」`,
    icon: "question",
    showCancelButton: true,
    confirmButtonColor: "#145369", // 深藍綠色
    cancelButtonColor: "#86b0bd", // 淺藍灰色
    confirmButtonText: "確定領取",
    cancelButtonText: "取消",
    reverseButtons: true,
  });

  if (result.isConfirmed) {
    try {
      // 觸發父組件的領取邏輯
      emit("claim", { type: "platform", mission });

      // 2. 領取成功後的導引彈窗
      const nextStep = await Swal.fire({
        title: "領取成功！",
        text: `任務「${mission.title}」已加入您的執行清單。`,
        icon: "success",
        showCancelButton: true,
        confirmButtonColor: "#145369",
        cancelButtonColor: "#86b0bd",
        confirmButtonText: "前往我的任務",
        cancelButtonText: "留在本頁",
        reverseButtons: true,
        footer:
          '<span style="font-size: 0.8rem; color: #6c757d;">提示：您可以在「我的任務」中查看進度</span>',
      });

      // 3. 跳轉邏輯
      if (nextStep.isConfirmed) {
        router.push("/mytask");
      }
    } catch (error) {
      Swal.fire({
        title: "領取失敗",
        text: "暫時無法領取該任務，請稍後再試。",
        icon: "error",
        confirmButtonColor: "#145369",
      });
    }
  }
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

  <div v-else class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-4 mb-4">
    <div v-for="m in list" :key="m.id" class="col d-flex align-items-stretch">
      <div class="card border-0 shadow-sm w-100 mission-card">
        <div class="card-body p-4 d-flex flex-column">
          <div class="mission-icon-header mb-3">
            <div class="category-icon-wrapper">
              <i :class="m.icon || store.getCategoryIcon(m.category)"></i>
            </div>
          </div>

          <h6 class="mission-title mb-2">{{ m.title }}</h6>
          <p class="mission-desc mb-4 flex-grow-1">
            {{ m.desc || "暫無詳細說明" }}
          </p>

          <div
            class="d-flex justify-content-between align-items-center pt-3 border-top mt-auto"
          >
            <div class="reward-box">
              <div class="reward-value">
                {{ store.activeTab === "mission" ? m.reward : m.price }}
                <small>Pts</small>
              </div>
              <div class="participants-tag text-dark">
                <i class="fas fa-user-friends me-1 text-dark"></i
                >{{
                  store.activeTab === "mission" ? m.participants : m.applicants
                }}
                人參與
              </div>
            </div>

            <button
              v-if="!m.claimed && !m.completed"
              class="btn-claim"
              @click="handleClaim(m)"
            >
              領取
            </button>
            <span v-else-if="m.claimed" class="status-label text-success">
              <i class="fas fa-check-circle me-1"></i>已領取
            </span>
            <span v-else class="status-label text-muted">已完成</span>
          </div>
        </div>
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

.mission-card {
  border-radius: 20px; /* 稍微加大圓角，呼應你的圖片感 */
  transition: all 0.3s ease;
  background: #ffffff;
}

/* 標題加強，因為少了標籤，標題要撐起視覺重量 */
.mission-title {
  font-size: 1.05rem;
  font-weight: 700;
  color: black;
  letter-spacing: 0.5px;
}

.mission-desc {
  font-size: 0.85rem;
  color: #3c3d3d;
  line-height: 1.6;
}

/* 按鈕微調，更接近你圖片中的那種質感 */
.btn-claim {
  background: #86b0bd; /* 使用你圖片中的深藍綠色 */
  color: black;
  border: none;
  padding: 8px 24px;
  border-radius: 12px;
  font-size: 0.9rem;
  font-weight: 500;
  transition: all 0.2s;
  box-shadow: 0 4px 6px rgba(85, 128, 139, 0.2);
}

.btn-claim:hover {
  background: #145369;
  transform: translateY(-2px);
  box-shadow: 0 6px 12px rgba(20, 83, 105, 0.2);
}

/* 圖示外層容器：增加呼吸感 */
.mission-icon-header {
  display: flex;
  align-items: center;
}

/* 圖示包裹器：還原圖片中的莫蘭迪藍綠色系 */
.category-icon-wrapper {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;

  /* 使用與按鈕同色系但更淺的背景，製造層次 */
  background-color: #f0f5f6;
  color: #145369; /* 與你圖片中的按鈕色一致 */

  border-radius: 10px; /* 呼應按鈕的圓角 */
  font-size: 1.1rem;

  /* 增加一個極細的邊框讓輪廓更清晰 */
  border: 1px solid rgba(85, 128, 139, 0.1);
  transition: all 0.3s ease;
}

/* 卡片 Hover 時，Icon 產生微幅震動或放大效果 */
.mission-card:hover .category-icon-wrapper {
  background-color: #86b0bd;
  color: #ffffff;
  transform: scale(1.05) rotate(-5deg);
}

/* 獎勵數值與 Pts 單位 */
.reward-value {
  color: #145369; /* 設定為主色調 */
  font-weight: 800;
  font-size: 1.1rem;
}

.reward-value small {
  color: #145369;
  opacity: 0.8; /* 單位 Pts 稍微透明，增加質感 */
  font-size: 0.75rem;
  margin-left: 2px;
}

/* 參與人數區塊 */
.participants-tag {
  font-size: 0.75rem;
  color: #145369; /* 設定為主色調 */
  font-weight: 600;
  margin-top: 2px;
  display: flex;
  align-items: center;
}

/* 圖示也同步顏色 */
.participants-tag i {
  color: #145369;
  opacity: 0.9;
}
</style>
