// 點數商城共用工具

const baseUrl = import.meta.env.VITE_BASE_URL;

// 商品圖片網址，沒有圖片時顯示預設圖
export const productImageUrl = (imageName) =>
  `${baseUrl}/img/product/${imageName || "NoImage.png"}`;

// 訂單狀態 (需與後端 / 後台一致)
export const ORDER_STATUS = Object.freeze({
  PENDING: "待處理",
  SHIPPED: "已出貨",
  COMPLETED: "已完成",
  CANCELED: "已取消",
  OTHER: "其他",
});

// 將使用者輸入插入 html 前先跳脫，避免 HTML 注入
export const escapeHtml = (str = "") =>
  String(str).replace(
    /[&<>"']/g,
    (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c],
  );

// 手機號碼去除 - 與空白
export const normalizePhone = (phone = "") => phone.trim().replace(/[- ]/g, "");

// 台灣手機號碼：09 開頭共 10 碼
export const isValidMobile = (phone) => /^09\d{8}$/.test(normalizePhone(phone));

// 日期時間格式 (yyyy/MM/dd HH:mm)
export const formatDateTime = (value) =>
  value
    ? new Date(value).toLocaleString("zh-TW", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      })
    : "";

export const formatDate = (value) =>
  value ? new Date(value).toLocaleDateString("zh-TW") : "";

// 後端業務失敗時回傳 HTTP 200 + ok:false，需要自行判斷
export class ApiFailError extends Error {}

export const ensureOk = (res) => {
  if (!res.data.ok) throw new ApiFailError(res.data.message || "操作失敗");
  return res.data;
};

// 業務失敗顯示後端訊息，其他錯誤 (網路 / 500) 顯示通用訊息
export const errorText = (error, fallback = "請稍後再試") =>
  error instanceof ApiFailError ? error.message : fallback;

// 寄送資料驗證 (結帳、修改寄送資料共用)，回傳錯誤訊息，通過則回傳 null
export const validateShipping = ({ name = "", phone = "", address = "" }) => {
  if (!name.trim()) return "請填寫收件人姓名";
  if (!phone.trim()) return "請填寫聯絡電話";
  if (!address.trim()) return "請填寫寄送地址";
  if (!/^[一-龥a-zA-Z\s]{2,10}$/.test(name.trim()))
    return "姓名請輸入2-10個中文或英文字元";
  if (!isValidMobile(phone)) return "請輸入正確的手機號碼格式（09xxxxxxxx）";
  if (address.trim().length < 5) return "地址至少需要5個字元";
  return null;
};
