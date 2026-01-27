<script setup>
import { computed } from 'vue';
import { useCartStore } from "@/views/PointShop/services/cartStore";
// 直接接收路由參數作為 props
const props = defineProps({
    id: String,
    products: {
        type: Array,
        required: true,
    },
});

const product = computed(() => {
    return props.products.find(p => String(p.productId) === String(props.id));
});


const cartStore = useCartStore();
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
                        <button class="btn btn-link text-info p-0" aria-label="Close" @click="$router.back()">
                            <i class="bi bi-x-lg fs-5"></i>
                        </button>
                    </div>

                    <div>
                        <span class="badge rounded-pill bg-primary bg-opacity-10 mb-2">{{
                            product.type
                            }}</span>
                        <h2 class="card-title fw-bold text-body-highlight mb-3" :title="product.name">{{ product.name }}
                        </h2>
                    </div>

                    <div class="mt-auto">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <span class="text-primary fw-bold fs-4">
                                {{ product.point }} P
                            </span>
                            <span class="badge rounded-pill bg-light text-dark border">
                                庫存: {{ product.stock }}
                            </span>
                        </div>

                        <div class="input-group mb-3 shadow-sm rounded-pill overflow-hidden border">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button">
                                <i class=" bi bi-dash-lg"></i>
                            </button>
                            <input type="number" class="form-control border-0 text-center bg-white fw-bold" value="1"
                                min="1">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button">
                                <i class="bi bi-plus-lg"></i>
                            </button>
                        </div>

                        <button class="btn btn-lg btn-outline-primary w-100 fw-bold transition rounded-pill shadow-sm">
                            <i class="fas fa-shopping-cart me-2"></i>加入購物車
                        </button>
                    </div>
                </div>
            </div>

            <hr class="opacity-10">

            <div class="row">
                <div class="col-12">
                    <h5 class="mb-3 fw-bold d-flex align-items-center">
                        <span class="far fa-file-alt me-2 text-primary"></span>商品詳情
                    </h5>
                    <div class="text-body-secondary lh-lg">
                        <p>{{ product.description }}</p>
                    </div>
                </div>
            </div>
        </div>
        <div v-else class="text-center p-5">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="mt-2">正在讀取商品資訊，請稍候...</p>
        </div>
    </div>
</template>