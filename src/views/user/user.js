import { defineStore } from "pinia";
import { reactive } from "vue";
import axios from "axios";

export const useAuthStore = defineStore("auth", () => {
  // 1. 定義狀態 (State)
  const form = reactive({
    email: "",
    password: "",
    confirmPassword: "",
  });

  const errors = reactive({
    email: "",
    password: "",
    confirmPassword: "",
  });

  // 2. 驗證邏輯 (Actions)
  const validateEmail = (email) => {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
  };

  const validateForm = () => {
    let isValid = true;
    
    // 重置錯誤
    errors.email = "";
    errors.password = "";
    errors.confirmPassword = "";

    // 驗證 Email
    if (!form.email) {
      errors.email = "請輸入 Email";
      isValid = false;
    } else if (!validateEmail(form.email)) {
      errors.email = "Email 格式不正確";
      isValid = false;
    }

    // 驗證密碼
    if (!form.password) {
      errors.password = "請輸入密碼";
      isValid = false;
    } else if (form.password.length < 6) {
      errors.password = "密碼長度至少需 6 個字元";
      isValid = false;
    }

    // 驗證確認密碼
    if (form.password !== form.confirmPassword) {
      errors.confirmPassword = "兩次輸入的密碼不一致";
      isValid = false;
    }

    return isValid;
  };

  // 3. 註冊動作 (Actions)
  const register = async () => {
    // 先執行驗證
    if (!validateForm()) {
      return false; // 驗證失敗
    }

    try {
      // 發送 API (請依實際後端網址修改)
      const response = await axios.post("http://localhost:3000/api/auth/register", {
        email: form.email,
        password: form.password,
      });
      
      console.log("註冊成功回應:", response.data);
      alert("註冊成功！準備跳轉...");
      
      // 清空表單
      form.email = "";
      form.password = "";
      form.confirmPassword = "";
      
      return true; // 註冊成功

    } catch (error) {
      console.error("註冊錯誤:", error);
      const msg = error.response?.data?.message || "伺服器發生錯誤";
      alert(`註冊失敗: ${msg}`);
      return false;
    }
  };

  return {
    form,
    errors,
    register,
  };
  
});