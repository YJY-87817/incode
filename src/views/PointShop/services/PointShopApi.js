import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7190/api",
  withCredentials: true,// 攜帶cookie
});

export const getOrdersByUser = () => api.get(`/OrderApi/order`);

export const getOrderDetails = (orderId) =>
  api.get(`/OrderApi/orderdetail/${orderId}`);

export const getShippedDetails = (orderDetailId) =>
  api.get(`/OrderApi/shippeddetail/${orderDetailId}`);

export const putCancelOrder = (orderId) =>
  api.put(`/OrderApi/CancelOrder/${orderId}`);

export const getProducts = () => api.get("/ProductApi");
