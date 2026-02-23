<script setup>
import { useCartStore } from "@/views/PointShop/services/cartStore";
// 引入 SweetAlert2 的套件
import CartSwal from "sweetalert2";
import "sweetalert2/dist/sweetalert2.min.css";

const baseUrl = import.meta.env.VITE_BASE_URL;
const props = defineProps({
  product: {
    type: Object,
    required: true,
  },
});

const cartStore = useCartStore();

const handleAddToCart = () => {
  cartStore.addToCart(props.product);

  CartSwal.fire({
    title: "成功！",
    text: `${props.product.name}已成功加入購物車`,
    icon: "success",
    confirmButtonText: "確定",
    confirmButtonColor: "#145369",
    didOpen: (modal) => {
      modal.querySelector(".swal2-confirm").style.color = "#fff";
    },
  });
};
</script>

<template>
  <div class="card h-auto shadow-sm border-0 product-hover-effect">
    <div class="row g-0">
      <!-- 圖片區域 -->
      <div class="col-md-4">
        <router-link
          :to="{ name: 'ProductDetail', params: { id: product.productId } }"
        >
          <div
            class="product-image-container position-relative bg-light overflow-hidden"
          >
            <img
              :src="`${baseUrl}/img/product/${product.imageName}`"
              :alt="product.name"
              class="img-fluid product-image"
            />
            <div class="position-absolute top-0 start-0 m-2">
              <h5
                class="badge opacity-75 fw-normal"
                style="background-color: #145369"
              >
                {{ product.type }}
              </h5>
            </div>
          </div>
        </router-link>
      </div>

      <!-- 內容區域 -->
      <div class="col-md-8">
        <div class="card-body d-flex flex-column h-100">
          <router-link
            :to="{ name: 'ProductDetail', params: { id: product.productId } }"
          >
            <h5 class="card-title fw-bold text-truncate" :title="product.name">
              {{ product.name }}
            </h5>
            <p
              class="card-text text-muted small flex-grow-1 text-multi-line-truncate"
            >
              {{ product.description }}
            </p>

            <div class="d-flex justify-content-between align-items-center mb-3">
              <span class="fw-bold fs-5" style="color: #86b0bd"
                >{{ product.point }} <small>P</small></span
              >
              <span class="badge rounded-pill bg-light text-dark border">
                庫存: {{ product.stock }}
              </span>
            </div>
          </router-link>

          <button
            @click="handleAddToCart"
            :disabled="product.stock <= 0"
            class="btn w-100 fw-bold transition"
            :class="product.stock > 0 ? 'btn-custom-primary' : 'btn-secondary'"
          >
            {{ product.stock > 0 ? "加入購物車" : "已售完" }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.btn-custom-primary {
  color: #145369;
  background-color: transparent;
  border: 1px solid #145369 !important;
}

.btn-custom-primary:hover {
  color: #fff;
  background-color: #145369;
  border-color: #145369;
}

/* 讓卡片滑過時有微小的浮起效果 */
.product-hover-effect {
  transition:
    transform 0.2s ease-in-out,
    box-shadow 0.2s ease-in-out;
  overflow: hidden;
}

.product-hover-effect:hover {
  transform: translateY(-3px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.12) !important;
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

/* 圖片容器固定高度 */
.product-image-container {
  width: 100%;
  height: 220px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 圖片完整顯示 */
.product-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  transition: transform 0.3s ease;
}

.product-hover-effect:hover .product-image {
  transform: scale(1.05);
}

/* 響應式調整 */
@media (max-width: 768px) {
  .card-body {
    padding: 1rem !important;
  }
}
</style>
