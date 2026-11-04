import axios from "axios";

// ===== 商品 =====
export const getProducts = () => axios.get("/ProductApi");

export const getProductSearch = (keyword) =>
  axios.get("/ProductApi/search", { params: { keyword } });

// ===== 訂單 (需登入) =====
export const getOrdersByUser = () => axios.get("/OrderApi/order");

export const getOrderDetails = (orderId) =>
  axios.get(`/OrderApi/orderdetail/${orderId}`);

export const getShippedDetails = (orderId) =>
  axios.get(`/OrderApi/shippeddetail/${orderId}`);

export const postCreateOrder = (newOrder) =>
  axios.post("/OrderApi/OrderPost", newOrder);

export const putCancelOrder = (orderId) =>
  axios.put(`/OrderApi/CancelOrder/${orderId}`);

export const putShippedOrder = (shippedDetail, shippedId) =>
  axios.put(`/OrderApi/ShippedPut/${shippedId}`, shippedDetail);

// 取得會員曾填寫過的寄送資料
export const getShipAddresses = () => axios.get("/OrderApi/shipaddress");
