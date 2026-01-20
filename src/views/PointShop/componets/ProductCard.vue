<script setup>
import { useCartStore } from "@/views/PointShop/services/cartStore";

const props = defineProps({
  product: {
    type: Object,
    required: true,
  },
});

const cartStore = useCartStore();

const handleAddToCart = () => {
  cartStore.addToCart(props.product);
  // 可以在這裡添加提示訊息
  alert(`已將 ${props.product.name} 加入購物車！`);
};
</script>

<template>
  <div class="card h-100 shadow-sm border-0 product-hover-effect">
    <div class="position-absolute top-0 start-0 m-2">
      <span class="badge bg-primary opacity-75 fw-normal">{{
        product.type
      }}</span>
    </div>

    <div class="ratio ratio-1x1 bg-light card-img-top overflow-hidden">
      <img
        :src="`https://localhost:7190/img/product/${product.imageName}`"
        :alt="product.name"
        class="img-fluid object-fit-cover w-100"
      />
    </div>

    <div class="card-body d-flex flex-column">
      <h5 class="card-title fw-bold text-truncate" :title="product.name">
        {{ product.name }}
      </h5>
      <p
        class="card-text text-muted small flex-grow-1 text-multi-line-truncate"
      >
        {{ product.description }}
      </p>

      <div class="d-flex justify-content-between align-items-center mb-3">
        <span class="text-primary fw-bold fs-5"
          >{{ product.point }} <small>P</small></span
        >
        <span class="badge rounded-pill bg-light text-dark border">
          庫存: {{ product.stock }}
        </span>
      </div>

      <button
        @click="handleAddToCart"
        :disabled="product.stock <= 0"
        class="btn w-100 fw-bold transition"
        :class="product.stock > 0 ? 'btn-outline-primary' : 'btn-secondary'"
      >
        {{ product.stock > 0 ? "加入購物車" : "已售完" }}
      </button>
    </div>
  </div>
</template>

<style scoped>
/* 讓卡片滑過時有微小的浮起效果 */
.product-hover-effect {
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}
.product-hover-effect:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1) !important;
}

/* 限制描述文字顯示兩行，超過顯示點點點 */
.text-multi-line-truncate {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 3em;
}

/* 確保圖片填滿容器 */
.object-fit-cover {
  object-fit: cover;
  transition: transform 0.3s ease;
}
.product-hover-effect:hover .object-fit-cover {
  transform: scale(1.05);
}
</style>
