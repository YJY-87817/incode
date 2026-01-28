import axios from "axios";

export const getOrdersByUser = () => axios.get(`/OrderApi/order`);

export const getOrderDetails = (orderId) =>
  axios.get(`/OrderApi/orderdetail/${orderId}`);

export const getShippedDetails = (orderDetailId) =>
  axios.get(`/OrderApi/shippeddetail/${orderDetailId}`);

export const putCancelOrder = (orderId) =>
  axios.put(`/OrderApi/CancelOrder/${orderId}`);

export const getProducts = () => axios.get("/ProductApi");

export const postCreateOrder = (neworder) =>
  axios.post(`/OrderApi/createOrder`, neworder);