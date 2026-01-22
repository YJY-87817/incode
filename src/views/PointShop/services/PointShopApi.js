import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7190/api",
});

export const getOrdersByUser = (userId) => api.get(`/OrderApi/order/${userId}`);

export const getOrderDetails = (orderId) =>
  api.get(`/OrderApi/orderdetail/${orderId}`);

export const getShippedDetails = (orderDetailId) =>
  api.get(`/OrderApi/shippeddetail/${orderDetailId}`);

export const putCancelOrder = (orderId) =>
  api.put(`/OrderApi/CancelOrder/${orderId}`);

export const getProducts = () => api.get("/ProductApi");
