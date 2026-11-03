<script setup>
import { ref, computed, watch } from "vue";
import { getOrdersByUser } from "@/api/pointShopApi";
import OrderItem from "@/components/pointshop/OrderItem.vue";
import { useAuthStore } from "@/stores/users/auth.js";

const props = defineProps({
  status: {
    type: String,
    default: "",
  },
});

const authStore = useAuthStore();
// 用 computed 保持響應式，登入狀態改變時才會跟著更新 (template 會自動解包)
const Islogin = computed(() => authStore.isAuthenticated);
const orders = ref([]);
const loading = ref(true);
const loadError = ref(false);

// 依狀態篩選，空字串表示全部
const filteredOrders = computed(() =>
  props.status
    ? orders.value.filter((order) => order.status === props.status)
    : orders.value,
);

// 取得使用者訂單資料
const loadOrders = async () => {
  loadError.value = false;
  if (!Islogin.value) {
    orders.value = [];
    loading.value = false;
    return;
  }
  // 只有清單為空 (首次載入) 時才顯示載入中，重新整理時保留畫面與展開狀態
  if (orders.value.length === 0) loading.value = true;
  try {
    const res = await getOrdersByUser();
    orders.value = res.data.ok ? res.data.data || [] : [];
  } catch (error) {
    orders.value = [];
    // 401 代表未登入，其餘顯示錯誤訊息
    if (error.response?.status !== 401) loadError.value = true;
  } finally {
    loading.value = false;
  }
};

// 首次載入，以及登入狀態變更 (例如 checkAuth 完成) 時重新載入
watch(Islogin, loadOrders, { immediate: true });
</script>

<template>
  <div class="col-12">
    <div class="card shadow-sm border-0 rounded-4">
      <!-- Header -->
      <div
        class="card-header border-bottom-0 pt-4 px-4 mb-3"
        style="background-color: #86b0bd"
      >
        <h2 class="fw-bold mb-1">
          <i class="bi bi-person-lines-fill me-2"></i>訂單查詢
        </h2>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="text-center card-body px-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">正在取得您的訂單...</p>
      </div>

      <!-- Not Logged In -->
      <div v-else-if="!Islogin" class="text-center bg-light card-body px-4">
        <i class="bi bi-person-lines-fill fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">您尚未登入</h4>
        <div class="text-muted">請先登入會員以查看您的訂單紀錄。</div>
        <router-link
          :to="{ name: 'Login' }"
          class="btn btn-custom-primary mt-2"
        >
          前往登入
        </router-link>
      </div>

      <!-- Load Error -->
      <div v-else-if="loadError" class="text-center bg-light card-body px-4">
        <i class="bi bi-exclamation-triangle fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">訂單載入失敗</h4>
        <button class="btn btn-custom-primary mt-2" @click="loadOrders">重新載入</button>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="orders.length === 0"
        class="text-center bg-light card-body px-4"
      >
        <i class="bi bi-cart-x fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">目前沒有任何訂單</h4>
        <p class="text-muted">您還沒有兌換任何商品，快去商店逛逛吧！</p>
        <router-link
          :to="{ name: 'PointShop' }"
          class="btn btn-custom-primary mt-2"
        >
          前往點數商城
        </router-link>
      </div>

      <!-- Filtered Empty State -->
      <div
        v-else-if="filteredOrders.length === 0"
        class="text-center bg-light card-body px-4"
      >
        <i class="bi bi-search fs-1 text-secondary mb-3 d-block"></i>
        <h4 class="fw-bold">沒有符合條件的訂單</h4>
        <p class="text-muted">查詢條件下沒有找到相應的訂單</p>
      </div>

      <!-- Orders List -->
      <div v-else class="vstack gap-4">
        <OrderItem
          v-for="order in filteredOrders"
          :key="order.orderId"
          :order="order"
          @refresh="loadOrders"
        />
      </div>
    </div>
  </div>
</template>

