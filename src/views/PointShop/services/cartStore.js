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

    // 以最新商品資料同步購物車 (價格、庫存、名稱、圖片)
    // 已下架的商品移除、數量超過庫存的調降，回傳異動說明供畫面提示
    const syncWithProducts = (products) => {
      const latest = new Map(products.map((p) => [p.productId, p]));
      const changes = [];
      const synced = [];

      for (const item of cartItems.value) {
        const product = latest.get(item.productId);
        if (!product || product.stock <= 0) {
          changes.push(`${item.name} 已下架或售完，已從購物車移除`);
          continue;
        }
        if (product.point !== item.point) {
          changes.push(`${product.name} 點數已由 ${item.point} 調整為 ${product.point}`);
        }
        const quantity = Math.min(item.quantity, product.stock);
        if (quantity < item.quantity) {
          changes.push(`${product.name} 庫存剩 ${product.stock}，數量已調整`);
        }
        synced.push({ ...product, quantity });
      }

      cartItems.value = synced;
      return changes;
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
      syncWithProducts,
      totalItems,
      totalPoints,
    };
  },
  { persist: true },
);
