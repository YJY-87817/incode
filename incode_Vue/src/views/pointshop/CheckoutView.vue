<script setup>
import { onMounted } from "vue";
import CheckoutItem from "@/components/pointshop/CheckoutCart.vue";
import CheckoutPoint from "@/components/pointshop/CheckoutForm.vue";
import DefaultNavbar from "@/components/layout/NavbarDefault.vue";
import DefaultFooter from "@/components/layout/FooterCentered.vue";
import { getProducts } from "@/api/pointShopApi";
import { useCartStore } from "@/stores/pointshop/cartStore";
import { shopSwal } from "@/utils/pointShopSwal";
import { escapeHtml } from "@/utils/pointShopUtils";
import "@/assets/css/pointshop.css";

const cartStore = useCartStore();

// 結帳前以最新商品資料同步購物車，有異動 (改價、下架、庫存不足) 時提示使用者
const syncCart = async () => {
  if (cartStore.cartItems.length === 0) return;
  try {
    const res = await getProducts();
    if (!res.data.ok) return;
    const changes = cartStore.syncWithProducts(res.data.data || []);
    if (changes.length) {
      shopSwal.fire({
        title: "購物車內容已更新",
        icon: "info",
        html: `<ul class="text-start">${changes
          .map((c) => `<li>${escapeHtml(c)}</li>`)
          .join("")}</ul>`,
      });
    }
  } catch (error) {
    console.error("同步購物車失敗:", error);
  }
};

onMounted(() => {
  document.body.classList.add("bg-grey-lighten-5"); // 全局背景色
  syncCart();
});
</script>

<template>
  <DefaultNavbar light />
  <div class="min-vh-100 pb-5">
    <div class="container pt-5 mb-5">
      <div class="row">
        <div class="col-12 mb-4 col-md-8">
          <CheckoutItem></CheckoutItem>
        </div>
        <div class="col-12 mb-4 col-md-4">
          <CheckoutPoint></CheckoutPoint>
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>

