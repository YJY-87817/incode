<script setup>
import { onMounted, onUnmounted, ref } from "vue";

//example components
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

import ProductCard from "@/Views/PointShop/ProductCard.vue";

// 1. 模擬使用者初始點數
const userPoints = ref(2000);

// 2. 模擬商品清單資料
const products = ref([
  {
    id: 1,
    name: "《奧本海默》收藏膠捲",
    price: 800,
    image: "https://picsum.photos/300/200?random=1",
    category: "電影週邊",
    stock: 3,
  },
  {
    id: 2,
    name: "雙人電影票兌換券",
    price: 1200,
    image: "https://picsum.photos/300/200?random=2",
    category: "票券",
    stock: 10,
  },
  {
    id: 3,
    name: "爆米花(大) + 可樂組合",
    price: 300,
    image: "https://picsum.photos/300/200?random=3",
    category: "餐飲",
    stock: 0,
  },
  {
    id: 4,
    name: "論壇專屬頭像框 (永久)",
    price: 500,
    image: "https://picsum.photos/300/200?random=4",
    category: "數位內容",
    stock: 99,
  },
]);

// 3. 處理兌換邏輯
const handleExchange = (product) => {
  if (userPoints.value < product.price) {
    alert(`點數不足！你還差 ${product.price - userPoints.value} 點。`);
    return;
  }

  // 執行扣款與減庫存
  if (confirm(`確定要消耗 ${product.price} 點兌換 ${product.name} 嗎？`)) {
    userPoints.value -= product.price; // 扣除點數
    product.stock -= 1; // 減少該商品的庫存
    alert("兌換成功！");
  }
};
</script>

<template>
  <NavbarDefault dark transparent />
  <div class="shop-container" style="margin-top: 100px">
    <header class="shop-header">
      <h2>電影點數商城</h2>
      <div class="user-points">
        我的點數: <span>🟡{{ userPoints }}</span>
      </div>
    </header>

    <div class="product-grid">
      <ProductCard
        v-for="item in products"
        :key="item.id"
        :product="item"
        @exchange="handleExchange"
      />
    </div>
  </div>
  <background></background>
  <DefaultFooter />
</template>

<style scoped>
.shop-container {
  max-width: 1000px;
  margin: 0 auto;
  padding: 20px;
}

.shop-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
  padding-bottom: 10px;
  border-bottom: 2px solid #eee;
}

.user-points {
  font-size: 1.2rem;
  font-weight: bold;
}

.user-points span {
  color: #d69e2e;
}

.product-grid {
  display: grid;
  /* 響應式網格：自動填充，最小 250px */
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
}
</style>
