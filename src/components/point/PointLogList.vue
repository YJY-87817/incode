<!-- 使用者點數紀錄列表組件 -->
<script setup>
import { defineProps } from 'vue';

const props = defineProps({
  pointLogs: {
    type: Array,
    required: true,
    default: () => []
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

// 格式化時間顯示
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  try {
    const date = new Date(dateString);
    return date.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  } catch (e) {
    return dateString; // 如果格式化失敗，回傳原始字串
  }
};

// 格式化點數變化（正數為綠色，負數為紅色）
const formatPointChange = (amount) => {
  if (amount === undefined || amount === null) return '0';
  return amount > 0 ? `+${amount}` : amount.toString();
};

const getPointChangeClass = (amount) => {
  if (amount === undefined || amount === null) return '';
  return amount > 0 ? 'text-success' : 'text-danger';
};

// 來源類型中文映射
const getSourceTypeName = (sourceType) => {
  const sourceMap = {
    'mission': '任務獎勵',
    'purchase': '購買',
    'redeem': '兌換',
    'admin': '管理員調整',
    'event': '活動獎勵',
    'system': '系統調整'
  };
  return sourceMap[sourceType] || sourceType || '未知來源';
};

// 輔助方法：計算總點數變化
const getTotalPoints = () => {
  return props.pointLogs.reduce((total, log) => total + (log.changeAmount || 0), 0);
};
</script>

<template>
  <div class="point-log-container">
    <!-- 顯示總點數概覽 -->
    <div v-if="pointLogs.length > 0" class="summary-card mb-4">
      <div class="card">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <h5 class="mb-0">點數紀錄總覽</h5>
              <p class="text-muted mb-0">共 {{ pointLogs.length }} 筆紀錄</p>
            </div>
            <div class="text-end">
              <h4 class="mb-0">
                總變化:
                <span :class="getPointChangeClass(getTotalPoints())">
                  {{ formatPointChange(getTotalPoints()) }} 點
                </span>
              </h4>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 顯示加載中狀態 -->
    <div v-if="isLoading" class="text-center py-5">
      <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;"></div>
      <div class="mt-3 fs-5">正在載入點數紀錄...</div>
    </div>

    <!-- 顯示空狀態 -->
    <div v-else-if="pointLogs.length === 0" class="text-center py-5">
      <div class="empty-state-icon mb-3">
        <i class="ni ni-money-coins text-muted" style="font-size: 3rem;"></i>
      </div>
      <h4 class="text-muted">目前沒有點數紀錄</h4>
      <p class="text-muted">完成任務或活動後，您的點數變化將會顯示在這裡</p>
    </div>

    <!-- 顯示點數紀錄列表 -->
    <div v-else class="point-log-list">
      <div class="card">
        <div class="card-header bg-transparent">
          <h5 class="mb-0">點數變化明細</h5>
        </div>
        <div class="table-responsive">
          <table class="table align-items-center mb-0">
            <thead class="thead-light">
              <tr>
                <th class="text-uppercase text-secondary text-xxs font-weight-bolder">時間</th>
                <th class="text-uppercase text-secondary text-xxs font-weight-bolder">來源</th>
                <th class="text-uppercase text-secondary text-xxs font-weight-bolder text-center">變化</th>
                <th class="text-uppercase text-secondary text-xxs font-weight-bolder">狀態</th>
                <th class="text-uppercase text-secondary text-xxs font-weight-bolder">備註</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(log, index) in pointLogs" :key="log.pointLogId || index">
                <td>
                  <div class="d-flex flex-column">
                    <span class="text-xs">{{ formatDateTime(log.createdAt) }}</span>
                  </div>
                </td>
                <td>
                  <div class="d-flex align-items-center">
                    <span class="badge badge-sm bg-gradient-info me-2">
                      {{ getSourceTypeName(log.sourceType) }}
                    </span>
                    <span class="text-xs">{{ log.sourceId || '-' }}</span>
                  </div>
                </td>
                <td class="text-center">
                  <span class="text-sm font-weight-bold" :class="getPointChangeClass(log.changeAmount)">
                    {{ formatPointChange(log.changeAmount) }}
                  </span>
                </td>
                <td>
                  <span class="badge badge-sm" :class="{
                    'bg-gradient-success': log.status === 'success',
                    'bg-gradient-warning': log.status === 'pending',
                    'bg-gradient-danger': log.status === 'failed',
                    'bg-gradient-secondary': !log.status || log.status === 'unknown'
                  }">
                    {{ log.status === 'success' ? '成功' :
                       log.status === 'pending' ? '處理中' :
                       log.status === 'failed' ? '失敗' : '未知' }}
                  </span>
                </td>
                <td>
                  <span class="text-xs">{{ log.displayName || log.sourceType || '-' }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="card-footer text-end">
          <small class="text-muted">最後更新: {{ formatDateTime(pointLogs[0]?.createdAt) }}</small>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.point-log-container {
  max-width: 1200px;
  margin: 0 auto;
}

.summary-card {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 12px;
}

.summary-card .card {
  background: transparent;
  border: none;
  color: white;
}

.empty-state-icon {
  opacity: 0.6;
}

.point-log-list .table {
  min-width: 600px;
}

.point-log-list .table th {
  padding: 1rem;
  border-bottom: 1px solid #dee2e6;
}

.point-log-list .table td {
  padding: 1rem;
  border-bottom: 1px solid #dee2e6;
}

.point-log-list .table tr:last-child td {
  border-bottom: none;
}

.text-success {
  color: #2dce89 !important;
}

.text-danger {
  color: #f5365c !important;
}
</style>