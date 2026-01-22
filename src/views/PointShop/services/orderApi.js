import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7190/api/OrderApi",
});

export const getOrdersByUser = (userId) => api.get(`/order/${userId}`);

export const getOrderDetails = (orderId) => api.get(`/orderdetail/${orderId}`);

export const getShippedDetails = (orderDetailId) =>
  api.get(`/shippeddetail/${orderDetailId}`);

export const putCancelOrder = (orderId) => api.put(`/CancelOrder/${orderId}`);
