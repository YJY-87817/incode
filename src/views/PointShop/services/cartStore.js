import { defineStore } from "pinia";
import { ref, computed } from "vue";

export const useCartStore = defineStore(
  "cart",
  () => {
    // 購物車商品列表
    const cartItems = ref([]);

    // 新增商品到購物車（數量不可超過庫存），回傳實際加入的數量
    const addToCart = (product, quantity = 1) => {
      const existingItem = cartItems.value.find(
        (item) => item.productId === product.productId,
      );
      const current = existingItem ? existingItem.quantity : 0;
      const added = Math.max(0, Math.min(quantity, (product.stock ?? Infinity) - current));
      if (added === 0) return 0;

      if (existingItem) {
        // 如果商品已存在，增加數量
        existingItem.quantity += added;
      } else {
        // 新增商品
        cartItems.value.push({
          ...product,
          quantity: added,
        });
      }
      return added;
    };

    // 移除購物車商品
    const removeFromCart = (productId) => {
      cartItems.value = cartItems.value.filter(
        (item) => item.productId !== productId,
      );
    };

    // 更新商品數量
    const updateQuantity = (productId, quantity) => {
      const item = cartItems.value.find((item) => item.productId === productId);
      if (item) {
        if (quantity <= 0) {
          removeFromCart(productId);
        } else {
          item.quantity = Math.min(quantity, item.stock ?? Infinity);
        }
      }
    };

    // 清空購物車
    const clearCart = () => {
      cartItems.value = [];
    };

    // 購物車商品總數
    const totalItems = computed(() => {
      return cartItems.value.reduce((total, item) => total + item.quantity, 0);
    });

    // 購物車總點數
    const totalPoints = computed(() => {
      return cartItems.value.reduce(
        (total, item) => total + item.point * item.quantity,
        0,
      );
    });

    return {
      cartItems,
      addToCart,
      removeFromCart,
      updateQuantity,
      clearCart,
      totalItems,
      totalPoints,
    };
  },
  { persist: true },
);
