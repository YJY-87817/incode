import { defineStore } from "pinia";
import { ref, computed } from "vue";

export const useCartStore = defineStore(
  "cart",
  () => {
    // 購物車商品列表
    const cartItems = ref([]);

    // 新增商品到購物車
    const addToCart = (product) => {
      const existingItem = cartItems.value.find(
        (item) => item.productId === product.productId,
      );

      if (existingItem) {
        // 如果商品已存在，增加數量
        existingItem.quantity += 1;
      } else {
        // 新增商品
        cartItems.value.push({
          ...product,
          quantity: 1,
        });
      }
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
          item.quantity = quantity;
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
