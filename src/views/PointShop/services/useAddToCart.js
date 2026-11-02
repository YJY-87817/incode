// 加入購物車 + 提示訊息 (ProductCard / ProductDetail 共用)
import { useCartStore } from "@/views/PointShop/services/cartStore";
import { alertSuccess, alertWarning } from "@/views/PointShop/services/pointShopSwal";

export function useAddToCart() {
  const cartStore = useCartStore();

  const addToCartWithAlert = (product, quantity = 1) => {
    const added = cartStore.addToCart(product, quantity);

    if (added === 0) {
      alertWarning("無法加入", `${product.name}已達庫存上限`);
    } else if (added < quantity) {
      alertSuccess("已加入部分數量", `已達庫存上限，${added}件${product.name}已加入購物車`);
    } else {
      alertSuccess("成功！", `${added}件${product.name}已成功加入購物車`);
    }
    return added;
  };

  return { addToCartWithAlert };
}
