<template>
  <div class="product-card">
    <div class="badge">{{ product.type }}</div>
    <img
      :src="`https://localhost:7190/img/product/${product.imageName}`"
      :alt="product.name"
      class="product-image"
      style="height: 300px"
    />

    <div class="product-info">
      <h4>{{ product.name }}</h4>
      <p class="description">{{ product.description }}</p>

      <div class="footer">
        <span class="point">{{ product.point }} P</span>
        <span class="stock">庫存: {{ product.stock }}</span>
      </div>

      <button :disabled="product.stock <= 0" class="buy-btn">
        {{ product.stock > 0 ? "立即兌換" : "已售完" }}
      </button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  product: {
    type: Object,
    required: true,
  },
});
</script>

<style scoped>
.product-card {
  border: 1px solid #eee;
  border-radius: 12px;
  overflow: hidden;
  transition: transform 0.3s;
  background: white;
  position: relative;
}
.product-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
}
.badge {
  position: absolute;
  top: 10px;
  left: 10px;
  background: #ff9800;
  color: white;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
}
.product-image {
  width: 100%;
  height: 180px;
  object-fit: cover;
}
.product-info {
  padding: 15px;
}
.description {
  font-size: 14px;
  color: #666;
  height: 40px;
  overflow: hidden;
}
.footer {
  display: flex;
  justify-content: space-between;
  margin: 15px 0;
}
.point {
  color: #e91e63;
  font-weight: bold;
  font-size: 18px;
}
.stock {
  color: #999;
  font-size: 13px;
}
.buy-btn {
  width: 100%;
  padding: 10px;
  border: none;
  border-radius: 6px;
  background: #4caf50;
  color: white;
  cursor: pointer;
}
.buy-btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}
</style>
