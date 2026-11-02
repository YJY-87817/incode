<script setup>
import { onMounted } from "vue";
import CheckoutItem from "@/views/PointShop/components/Checkitem.vue";
import CheckoutPoint from "@/views/PointShop/components/Check.vue";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import { getProducts } from "@/views/PointShop/services/PointShopApi";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import { shopSwal } from "@/views/PointShop/services/pointShopSwal";
import { escapeHtml } from "@/views/PointShop/services/pointShopUtils";
import "@/views/PointShop/styles/pointshop.css";

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

