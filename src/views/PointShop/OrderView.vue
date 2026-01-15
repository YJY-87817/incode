<script setup>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import { ref, onMounted } from "vue";
import axios from "axios";

// 配置 API 基礎路徑 (建議放在環境變數)
const API_BASE = "https://localhost:7190/api/OrderApi";
const userId = 1; // 假設目前登入者的 ID

const orders = ref([]);
const loading = ref(false);
const selectedOrderId = ref(null);
const orderDetails = ref([]);
const shippingInfo = ref({}); // 用 orderdetailId 當 key 存出貨資訊

// 取得訂單列表
const fetchOrders = async () => {
  loading.value = true;
  try {
    const res = await axios.get(`${API_BASE}/order/${userId}`);
    orders.value = res.data;
  } catch (err) {
    console.error("獲取訂單失敗", err);
  } finally {
    loading.value = false;
  }
};

// 取得明細與出貨資訊
const viewDetail = async (orderId) => {
  selectedOrderId.value = orderId;
  try {
    const res = await axios.get(`${API_BASE}/orderdetail/${orderId}`);
    orderDetails.value = res.data;

    // 針對需要物流的商品，抓取物流資料
    orderDetails.value.forEach(async (item) => {
      if (item.requiresShipping) {
        try {
          const shipRes = await axios.get(
            `${API_BASE}/shippeddetail/${item.orderdetailId}`
          );
          // 假設回傳的是陣列，取第一筆
          if (shipRes.data.length > 0) {
            shippingInfo.value[item.orderdetailId] = shipRes.data[0];
          }
        } catch (e) {
          console.warn(`明細 ${item.orderdetailId} 無物流資料`);
        }
      }
    });
  } catch (err) {
    console.error("獲取明細失敗", err);
  }
};

// 狀態標籤顏色
const statusClass = (status) => {
  switch (status) {
    case "pending":
      return "bg-yellow-100 text-yellow-700";
    case "completed":
      return "bg-green-100 text-green-700";
    default:
      return "bg-gray-100 text-gray-700";
  }
};

const formatDate = (dateStr) => {
  if (!dateStr) return "尚未更新";
  return new Date(dateStr).toLocaleDateString();
};

onMounted(fetchOrders);
</script>

<template>
  <DefaultNavbar :action="{ route: 'javascript:;' }" />
  <div class="container mt-5 mb-5">
    <div class="row mb-4">
      <div class="col-12">
        <div class="card border-0 bg-light shadow-sm">
          <div class="card-body p-4">
            <h2 class="fw-bold mb-1">我的訂單</h2>
          </div>
        </div>
      </div>
    </div>
    <div class="max-w-4xl mx-auto p-4">
      <div v-if="!selectedOrderId" class="space-y-4">
        <div v-if="loading" class="text-center py-10">載入中...</div>

        <div
          v-for="order in orders"
          :key="order.orderId"
          class="border rounded-lg p-4 shadow-sm hover:shadow-md transition bg-white"
        >
          <div class="flex justify-between items-center">
            <div>
              <p class="text-sm text-gray-500">
                訂單編號：#{{ order.orderId }}
              </p>
              <p class="font-medium">
                總消耗點數：<span class="text-orange-600"
                  >{{ order.totalPoint }} P</span
                >
              </p>
              <p class="text-sm text-gray-600">
                備註：{{ order.remark || "無" }}
              </p>
            </div>
            <div class="text-right">
              <span
                :class="statusClass(order.status)"
                class="px-3 py-1 rounded-full text-xs"
              >
                {{ order.status }}
              </span>
              <button
                @click="viewDetail(order.orderId)"
                class="block mt-2 text-blue-500 hover:text-blue-700 text-sm"
              >
                查看明細 >
              </button>
            </div>
          </div>
        </div>
      </div>

      <div v-else>
        <button
          @click="selectedOrderId = null"
          class="mb-4 text-gray-600 hover:text-black"
        >
          &larr; 返回訂單列表
        </button>

        <div class="bg-white border rounded-lg overflow-hidden">
          <div class="bg-gray-50 p-4 border-b">
            <h2 class="font-bold text-lg">訂單詳情 #{{ selectedOrderId }}</h2>
          </div>

          <div
            v-for="item in orderDetails"
            :key="item.orderdetailId"
            class="p-4 border-b last:border-0"
          >
            <div class="flex gap-4">
              <img
                :src="`https://localhost:7190/img/product/${item.imageName}`"
                class="w-20 h-20 object-cover rounded"
              />
              <div class="flex-1">
                <h3 class="font-bold">{{ item.productName }}</h3>
                <p class="text-sm text-gray-500">
                  單價: {{ item.unitPointAtPurchase }} P
                </p>
                <p class="text-sm text-gray-500">數量: {{ item.quantity }}</p>

                <div
                  v-if="item.requiresShipping"
                  class="mt-2 p-2 bg-blue-50 rounded text-xs"
                >
                  <p class="font-semibold text-blue-700">物流狀態：</p>
                  <div v-if="shippingInfo[item.orderdetailId]">
                    <p>
                      出貨日期：{{
                        formatDate(shippingInfo[item.orderdetailId].shippedDate)
                      }}
                    </p>
                    <p>
                      預計到達：{{
                        formatDate(shippingInfo[item.orderdetailId].arrivalDate)
                      }}
                    </p>
                    <p>
                      狀態：{{
                        shippingInfo[item.orderdetailId].isCompleted
                          ? "已送達"
                          : "運送中"
                      }}
                    </p>
                  </div>
                  <p v-else class="text-gray-400">處理中，尚未出貨</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>
