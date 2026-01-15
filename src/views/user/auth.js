import { defineStore } from "pinia";
import { reactive } from "vue";
import axios from "axios";

export const useAuthStore = defineStore("auth", () => {
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
  const validateEmail = (email) => {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
  };

const validateForm = () => {
    let isValid = true;
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
    } else if (form.password.length < 8) {
      errors.password = "密碼長度至少需 8 個字元";
      isValid = false;
    }

    // 驗證確認密碼 (只在註冊時需要，如果你是登入頁面共用此 store，要注意邏輯)
    if (form.confirmPassword && form.password !== form.confirmPassword) {
      errors.confirmPassword = "兩次輸入的密碼不一致";
      isValid = false;
    }

    return isValid;
  };

  //清除特定欄位的錯誤 
  const clearError = (field) => {
    if (errors[field]) {
      errors[field] = "";
    }
  };

const resetForm = () => {
    console.log("重置表單資料");
    form.email = "";
    form.password = "";
    form.confirmPassword = "";
    // 如果有 errors 也要清空
    Object.keys(errors).forEach(key => delete errors[key]); 
  };

  // 註冊
  const register = async () => {
    // 先執行驗證
    if (!validateForm()) {
      return false; // 驗證失敗
    }

    try {
         const response = await axios.post("https://localhost:7190/api/Auth/register", {
        email: form.email,
        password: form.password,
        confirmPassword: form.confirmPassword
      });

      console.log("註冊成功回應:", response.data);
      alert("註冊成功！請前往登入。");
      resetForm();
      return true; 

    } catch (error) {
      console.error("註冊錯誤:", error);
      const msg = error.response?.data?.message || "伺服器發生錯誤";
      alert(`註冊失敗: ${msg}`);
      return false;
    }
  };

// 登入
  const login = async () => {
    if (!form.email || !form.password) {
        alert("請輸入帳號密碼");
        return false;
    }
    try {
        // 假設登入 API 路徑
        const response = await axios.post("https://localhost:7190/api/Auth/login", {
            email: form.email,
            password: form.password
        },
        { 
                withCredentials: true // 確保 Cookie 被包含在請求中
            }
      );
        console.log("登入成功", response.data);
        alert("登入成功");
        return true;
    } catch (error) {
        alert("登入失敗: " + (error.response?.data?.message || error.message));
        return false;
    }
  };

  return {
    form,
    errors,
    register,
    login,     // 記得匯出 login
    resetForm, // 記得匯出 resetForm
    clearError,
  };

});