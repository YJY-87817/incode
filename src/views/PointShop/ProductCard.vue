<template>
  <div class="product-card">
    <div class="product-image">
      <img :src="product.image" :alt="product.name" />
      <span v-if="product.stock <= 0" class="out-of-stock-badge">已售完</span>
    </div>

    <div class="product-info">
      <h3 class="product-name">{{ product.name }}</h3>
      <p class="product-category">{{ product.category }}</p>

      <div class="product-footer">
        <div class="point-tag">
          <span class="points">🟡 {{ product.price }} P</span>
        </div>

        <button
          @click="$emit('exchange', product)"
          :disabled="product.stock <= 0"
          class="exchange-btn"
        >
          {{ product.stock > 0 ? "立即兌換" : "補貨中" }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
// 定義從父元件傳進來的資料
const props = defineProps({
  product: {
    type: Object,
    required: true,
    default: () => ({
      id: 0,
      name: "商品名稱",
      price: 0,
      image: "https://via.placeholder.com/150",
      category: "未分類",
      stock: 0,
    }),
  },
});

// 定義點擊按鈕時要送出的事件
const emit = defineEmits(["exchange"]);
</script>

<style scoped>
.product-card {
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
  background: white;
  transition: transform 0.2s;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.product-card:hover {
  transform: translateY(-5px);
}

.product-image {
  position: relative;
  height: 180px;
  background-color: #f7fafc;
}

.product-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.out-of-stock-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.product-info {
  padding: 16px;
}

.product-name {
  font-size: 1.1rem;
  font-weight: bold;
  margin: 0 0 8px 0;
  color: #2d3748;
}

.product-category {
  font-size: 0.85rem;
  color: #718096;
  margin-bottom: 16px;
}

.product-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.point-tag {
  display: flex;
  align-items: center;
  color: #d69e2e; /* 金色 */
  font-weight: 800;
}

.exchange-btn {
  background-color: #4299e1;
  color: white;
  padding: 8px 16px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.exchange-btn:disabled {
  background-color: #cbd5e0;
  cursor: not-allowed;
}
</style>
