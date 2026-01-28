<script setup>
import { computed, ref } from 'vue';
import { useCartStore } from "@/views/PointShop/services/cartStore";
import CartSwal from 'sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';

// 直接接收路由參數作為 props
const props = defineProps({
    id: String,
    products: {
        type: Object,
        required: true,
    },
});

const product = computed(() => {
    return props.products.find(p => String(p.productId) === String(props.id));
});

const quantity = ref(1);
const changeQty = (num) => {
    const newQty = quantity.value + num;
    // 檢查：不能小於 1，且不能超過商品庫存 (如果 product 存在)
    if (newQty >= 1 && (!product.value || newQty <= product.value.stock)) {
        quantity.value = newQty;
    }
};

const cartStore = useCartStore();

const handleAddToCart = () => {
    var count = 0
    for (let i = 0; i < quantity.value; i++) {
        cartStore.addToCart(product.value);
        count++;
    }
    // 可以在這裡添加提示訊息
    CartSwal.fire({
        title: '成功！',
        text: `${count}件${product.value.name}已成功加入購物車`,
        icon: 'success',
        confirmButtonText: '確定',
        confirmButtonColor: '#145369'
    });
};

</script>

<template>
    <div class="card shadow-sm border border-0 rounded-3 overflow-hidden">
        <div v-if="product" class="card-body p-4 p-lg-5">
            <div class="row g-4 g-lg-5">
                <div class="col-12 col-md-6">
                    <div class="d-flex align-items-center justify-content-center border border-translucent rounded-3 mb-3 bg-light"
                        style="aspect-ratio: 1/1;">
                        <img :src="`https://localhost:7190/img/product/${product.imageName}`" :alt="product.name"
                            class="img-fluid product-image" />
                    </div>
                </div>

                <div class="col-12 col-md-6 d-flex flex-column h-100">
                    <div class="d-flex justify-content-end mb-2">
                        <button class="btn p-0" aria-label="Close" @click="$router.back()">
                            <h6>
                                <i class="bi bi-caret-left-fill"></i>返回
                            </h6>
                        </button>
                    </div>

                    <div>
                        <span class="badge rounded-pill mb-2"
                            style="background-color: rgba(134, 176, 189, 0.1); color: #86B0BD;">{{ product.type
                            }}</span>
                        <h2 class="card-title fw-bold text-body-highlight mb-3" :title="product.name">{{ product.name }}
                        </h2>
                    </div>

                    <div class="mt-auto">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <span class="fw-bold fs-4" style="color: #86B0BD;">{{ product.point }} P</span>
                            <span class="badge rounded-pill bg-light text-dark border">
                                庫存: {{ product.stock }}
                            </span>
                        </div>

                        <div class="input-group mb-3 shadow-sm rounded-pill overflow-hidden border">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button"
                                @click="changeQty(-1)">
                                <i class=" bi bi-dash-lg"></i>
                            </button>
                            <input type="number" class="form-control border-0 text-center bg-white fw-bold"
                                v-model.number="quantity" min="1" :max="product?.stock">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button"
                                @click="changeQty(1)">
                                <i class="bi bi-plus-lg"></i>
                            </button>
                        </div>

                        <button @click="handleAddToCart" :disabled="product.stock <= 0"
                            class="btn btn-lg w-100 fw-bold transition rounded-pill shadow-sm"
                            :class="product.stock > 0 ? 'btn-custom-solid' : 'btn-secondary'">
                            <i class="fas fa-shopping-cart me-2"></i>{{ product.stock > 0 ? "加入購物車" : "已售完" }}
                        </button>
                    </div>
                </div>
            </div>

            <hr class="opacity-10">

            <div class="row">
                <div class="col-12">
                    <h5 class="mb-3 fw-bold d-flex align-items-center">
                        <span class="far fa-file-alt me-2" style="color: #86B0BD;"></span>商品詳情
                    </h5>
                    <div class="lh-lg">
                        <p>{{ product.description }}</p>
                    </div>
                </div>
            </div>
        </div>
        <div v-else class="text-center p-5">
            <div class="spinner-border" style="color: #86B0BD;" role="status"></div>
            <p class="mt-2">正在讀取商品資訊，請稍候...</p>
        </div>
    </div>
</template>

<style scoped>
.product-image {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
}

.btn-custom-solid {
    background-color: #145369;
    border-color: #145369;
    color: white;
}

.btn-custom-solid:hover {
    background-color: #104255;
    border-color: #104255;
}
</style>