<script setup>
import { ref, onMounted, computed } from "vue";
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";

// 狀態管理
const isLoading = ref(false);
const searchQuery = ref("");
const filterType = ref("全部類型");
const currentPage = ref(1);
const itemsPerPage = 10;

// 模擬資料
const mockData = ref([
  {
    id: 1,
    date: "2024/05/15 10:30",
    item: "每日登入簽到",
    category: "平台任務",
    points: 100,
    status: "成功",
    balance: 1100,
    type: "mission",
  },
  {
    id: 2,
    date: "2024/05/14 18:45",
    item: "購買咖啡兌換",
    category: "商品兌換",
    points: -200,
    status: "成功",
    balance: 900,
    type: "redeem",
  },
  {
    id: 3,
    date: "2024/05/13 09:15",
    item: "完成新手教學",
    category: "平台任務",
    points: 50,
    status: "成功",
    balance: 950,
    type: "mission",
  },
  {
    id: 4,
    date: "2024/05/12 14:20",
    item: "活動獎勵",
    category: "系統贈送",
    points: 150,
    status: "成功",
    balance: 1100,
    type: "event",
  },
  {
    id: 5,
    date: "2024/05/11 11:30",
    item: "商品退款",
    category: "退款/修正",
    points: 200,
    status: "成功",
    balance: 1300,
    type: "refund",
  },
]);

// 計算屬性
const filteredLogs = ref([]);
const totalPages = ref(1);

// 計算累計獲得和消耗
const totalGained = computed(() => {
  return filteredLogs.value
    .filter((log) => log.points > 0)
    .reduce((sum, log) => sum + log.points, 0);
});

const totalSpent = computed(() => {
  return filteredLogs.value
    .filter((log) => log.points < 0)
    .reduce((sum, log) => sum + Math.abs(log.points), 0);
});

// 模擬資料加載
const loadData = () => {
  isLoading.value = true;

  setTimeout(() => {
    // 過濾資料
    filteredLogs.value = mockData.value.filter((log) => {
      const matchesSearch =
        searchQuery.value === "" ||
        log.item.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
        log.date.includes(searchQuery.value);

      const matchesFilter =
        filterType.value === "全部類型" ||
        log.category === filterType.value ||
        log.type === filterType.value.toLowerCase();

      return matchesSearch && matchesFilter;
    });

    // 分頁計算
    totalPages.value = Math.ceil(filteredLogs.value.length / itemsPerPage);
    isLoading.value = false;
  }, 800);
};

// 刷新資料
const refreshData = () => {
  loadData();
};

// 搜尋功能
const handleSearch = () => {
  currentPage.value = 1;
  loadData();
};

// 分頁導航
const goToPage = (page) => {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page;
  }
};

// 初始化
onMounted(() => {
  loadData();
});
</script>

<template>
  <NavbarDefault light />

  <div class="container mt-5">
    <div class="card shadow-sm border-0 rounded-4 overflow-hidden">
      <!-- 統計摘要 - 整合累計獲得和消耗 -->
      <div class="card-header bg-white border-0 px-4 py-3">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="mb-0 fw-semibold">
            <i class="ni ni-credit-card me-2" style="color: #1e40af"></i>
            點數收支明細
          </h5>
          <button
            class="btn btn-outline-primary btn-sm rounded-pill"
            @click="refreshData"
            :disabled="isLoading"
          >
            <span v-if="!isLoading">
              <i class="ni ni-refresh-69 me-1"></i>
              刷新
            </span>
            <span v-else>
              <span
                class="spinner-border spinner-border-sm me-1"
                role="status"
              ></span>
              更新中
            </span>
          </button>
        </div>

        <!-- 累計統計 - 整合到明細上方 -->
        <div class="row g-3 mb-3">
          <div class="col-md-6">
            <div
              class="d-flex align-items-center p-3 bg-success-subtle rounded-3"
            >
              <i
                class="ni ni-fat-add text-success me-3"
                style="font-size: 1.5rem"
              ></i>
              <div>
                <div class="text-muted small">累計獲得</div>
                <div class="fw-bold text-success fs-5">
                  +{{ totalGained }} 點
                </div>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <div
              class="d-flex align-items-center p-3 bg-danger-subtle rounded-3"
            >
              <i
                class="ni ni-fat-remove text-danger me-3"
                style="font-size: 1.5rem"
              ></i>
              <div>
                <div class="text-muted small">累計消耗</div>
                <div class="fw-bold text-danger fs-5">-{{ totalSpent }} 點</div>
              </div>
            </div>
          </div>
        </div>

        <!-- 搜尋和過濾控制 -->
        <div class="d-flex gap-2 mb-3">
          <div class="input-group flex-grow-1">
            <span
              class="input-group-text bg-white border-end-0 rounded-start-pill"
            >
              <i class="ni ni-zoom-split-in text-muted"></i>
            </span>
            <input
              type="text"
              class="form-control form-control-sm rounded-end-pill px-3 py-2 shadow-sm border-start-0"
              placeholder="搜尋項目、日期或類別..."
              v-model="searchQuery"
              @input="handleSearch"
            />
          </div>
          <select
            class="form-select form-select-sm rounded-pill px-3 shadow-sm"
            v-model="filterType"
            @change="handleSearch"
            style="width: 150px"
          >
            <option selected>全部類型</option>
            <option value="平台任務">平台任務</option>
            <option value="商品兌換">購買商品</option>
            <option value="系統贈送">系統贈送</option>
            <option value="退款/修正">退款/修正</option>
          </select>
        </div>
      </div>

      <!-- 加載中狀態 -->
      <div v-if="isLoading" class="text-center py-5">
        <div
          class="spinner-border text-primary"
          role="status"
          style="width: 3rem; height: 3rem"
        ></div>
        <div class="mt-3 fs-5 text-muted">正在載入點數紀錄...</div>
      </div>

      <!-- 空狀態 -->
      <div v-else-if="filteredLogs.length === 0" class="text-center py-5">
        <div class="empty-state-icon mb-3">
          <i
            class="ni ni-money-coins text-muted"
            style="font-size: 3rem; opacity: 0.6"
          ></i>
        </div>
        <h4 class="text-muted">沒有找到符合條件的紀錄</h4>
        <p class="text-muted">嘗試調整搜尋條件或過濾器</p>
      </div>

      <!-- 表格內容 -->
      <div v-else class="table-responsive">
        <table class="table table-hover align-middle mb-0 table-borderless">
          <thead class="bg-light-subtle text-secondary small fw-medium">
            <tr>
              <th class="ps-4 py-3" style="width: 40%">時間 / 項目</th>
              <th style="width: 18%">類別</th>
              <th class="text-center" style="width: 14%">點數異動</th>
              <th class="text-center" style="width: 14%">狀態</th>
              <th class="text-end pe-4" style="width: 14%">餘額</th>
            </tr>
          </thead>
          <tbody class="text-muted">
            <tr
              class="align-middle"
              v-for="log in filteredLogs.slice(
                (currentPage - 1) * itemsPerPage,
                currentPage * itemsPerPage,
              )"
              :key="log.id"
            >
              <td class="ps-4 py-3">
                <div class="small mb-1 text-muted">{{ log.date }}</div>
                <div class="fw-medium text-dark d-flex align-items-center">
                  <i
                    class="ni me-2"
                    :class="{
                      'ni-check-bold text-success': log.category === '平台任務',
                      'ni-cart text-primary': log.category === '商品兌換',
                      'ni-gift-2 text-warning': log.category === '系統贈送',
                      'ni-refresh-69 text-info': log.category === '退款/修正',
                    }"
                    style="font-size: 0.9rem"
                  ></i>
                  {{ log.item }}
                </div>
              </td>
              <td class="fw-medium">
                <span
                  class="badge"
                  :class="{
                    'bg-success-subtle text-success':
                      log.category === '平台任務',
                    'bg-primary-subtle text-primary':
                      log.category === '商品兌換',
                    'bg-warning-subtle text-warning':
                      log.category === '系統贈送',
                    'bg-info-subtle text-info': log.category === '退款/修正',
                  }"
                >
                  {{ log.category }}
                </span>
              </td>
              <td
                class="fw-bold text-center fs-5"
                :class="log.points > 0 ? 'text-success' : 'text-danger'"
              >
                {{ log.points > 0 ? "+" : "" }}{{ log.points }}
              </td>
              <td class="text-center">
                <span
                  class="badge bg-success-subtle text-success px-3 py-1 fw-medium"
                >
                  {{ log.status }}
                </span>
              </td>
              <td class="text-end pe-4 fw-medium text-secondary">
                {{ log.balance }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- 分頁控制 -->
      <div
        v-if="totalPages > 1"
        class="card-footer bg-transparent border-top-0"
      >
        <nav aria-label="Page navigation">
          <ul class="pagination justify-content-center mb-0">
            <li class="page-item" :class="{ disabled: currentPage === 1 }">
              <button
                class="page-link rounded-pill mx-1"
                @click="goToPage(currentPage - 1)"
                :disabled="currentPage === 1"
              >
                <i class="ni ni-bold-left"></i>
              </button>
            </li>
            <li
              class="page-item"
              v-for="page in Math.min(totalPages, 5)"
              :key="page"
              :class="{ active: currentPage === page }"
            >
              <button
                class="page-link rounded-pill mx-1"
                @click="goToPage(page)"
              >
                {{ page }}
              </button>
            </li>
            <li
              class="page-item"
              :class="{ disabled: currentPage === totalPages }"
            >
              <button
                class="page-link rounded-pill mx-1"
                @click="goToPage(currentPage + 1)"
                :disabled="currentPage === totalPages"
              >
                <i class="ni ni-bold-right"></i>
              </button>
            </li>
          </ul>
        </nav>
        <div class="text-center mt-2">
          <small class="text-muted">
            顯示 {{ (currentPage - 1) * itemsPerPage + 1 }} -
            {{ Math.min(currentPage * itemsPerPage, filteredLogs.length) }} 筆，
            共 {{ filteredLogs.length }} 筆紀錄
          </small>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.card {
  transition: all 0.3s ease;
  border: none;
}
.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.08) !important;
}

.table-hover tbody tr:hover {
  background-color: rgba(34, 197, 94, 0.05);
  transition: background-color 0.2s ease;
}

.rounded-pill {
  border-radius: 50rem !important;
}

.fs-5 {
  font-size: 1.25rem !important;
}

/* 分頁樣式 */
.pagination .page-link {
  transition: all 0.2s ease;
  border: none;
  color: #6c757d;
  min-width: 40px;
}

.pagination .page-link:hover {
  background-color: #f8f9fa;
  color: #1e40af;
}

.pagination .page-item.active .page-link {
  background-color: #1e40af;
  border-color: #1e40af;
  color: white;
}

/* 按鈕過渡效果 */
.btn {
  transition: all 0.2s ease;
}

.btn:active {
  transform: scale(0.98);
}

/* 載入動畫 */
@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.fade-in {
  animation: fadeIn 0.3s ease-out;
}

/* 空狀態動畫 */
.empty-state-icon {
  animation: bounce 2s infinite;
}

@keyframes bounce {
  0%,
  20%,
  50%,
  80%,
  100% {
    transform: translateY(0);
  }
  40% {
    transform: translateY(-10px);
  }
  60% {
    transform: translateY(-5px);
  }
}

/* 統計卡片樣式 */
.bg-success-subtle,
.bg-danger-subtle {
  transition: transform 0.2s ease;
}
.bg-success-subtle:hover,
.bg-danger-subtle:hover {
  transform: translateY(-2px);
}
</style>
