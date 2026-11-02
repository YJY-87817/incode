<script setup>
import { computed, ref, onMounted, watch } from 'vue';
import { useAddToCart } from "@/views/PointShop/services/useAddToCart";
import { productImageUrl } from "@/views/PointShop/services/pointShopUtils";

// ProductView 透過 router-view 傳入的其他屬性 (filteredProducts 等) 不要落到根元素上
defineOptions({ inheritAttrs: false });

// 直接接收路由參數作為 props
const props = defineProps({
    id: String,
    products: {
        type: Array,
        required: true,
    },
    loading: {
        type: Boolean,
        default: false,
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

// 手動輸入數量時限制在 1 ~ 庫存 之間的整數
const normalizeQty = () => {
    const max = Math.max(product.value?.stock ?? 1, 1);
    const qty = Math.floor(Number(quantity.value));
    quantity.value = Number.isFinite(qty) ? Math.min(Math.max(qty, 1), max) : 1;
};

const { addToCartWithAlert } = useAddToCart();

const handleAddToCart = () => {
    normalizeQty();
    addToCartWithAlert(product.value, quantity.value);
};

// 切換到其他商品時數量重設為 1
watch(() => props.id, () => {
    quantity.value = 1;
});

// 頁面掛載時滾動到上方
onMounted(() => {
    window.scrollTo(0, 0);
});

</script>

<template>
    <div class="card shadow-sm border border-0 rounded-3 overflow-hidden">
        <div v-if="product" class="card-body p-4 p-lg-5">
            <div class="row g-4 g-lg-5">
                <div class="col-12 col-md-6">
                    <div class="d-flex align-items-center justify-content-center border border-translucent rounded-3 mb-3 bg-light"
                        style="aspect-ratio: 1/1;">
                        <img :src="productImageUrl(product.imageName)" :alt="product.name"
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
                                v-model.number="quantity" min="1" :max="product?.stock" @change="normalizeQty">
                            <button class="btn btn-light border-0 px-3 transition m-0" type="button"
                                @click="changeQty(1)">
                                <i class="bi bi-plus-lg"></i>
                            </button>
                        </div>

                        <button @click="handleAddToCart" :disabled="product.stock <= 0"
                            class="btn btn-lg w-100 fw-bold transition rounded-pill shadow-sm"
                            :class="product.stock > 0 ? 'btn-custom-primary' : 'btn-secondary'">
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
        <div v-else-if="loading" class="text-center p-5">
            <div class="spinner-border" style="color: #86B0BD;" role="status"></div>
            <p class="mt-2">正在讀取商品資訊，請稍候...</p>
        </div>
        <div v-else class="text-center p-5">
            <h4 class="fw-bold">找不到此商品</h4>
            <p class="text-muted">商品可能已下架</p>
            <router-link :to="{ name: 'PointShop' }" class="btn btn-custom-primary">返回商城</router-link>
        </div>
    </div>
</template>

<style scoped>
.product-image {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
}

</style>