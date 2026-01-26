import { defineStore } from "pinia";
import axios from "axios";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    user: null,
    // 整合表單欄位，涵蓋登入與註冊所需
    form: {
      email: "",
      password: "",
      confirmPassword: "", // 註冊專用
    },
    errors: {
      email: "",
      password: "",
      confirmPassword: "",
    },
    isLoading: false,
  }),

  actions: {    
    clearError(field) {
      if (this.errors[field]) {
        this.errors[field] = "";
      }
    },
    resetForm() {
      this.form = { email: "", password: "", confirmPassword: "" };
      this.errors = { email: "", password: "", confirmPassword: "" };
      this.isLoading = false;
    },

    // --- 註冊邏輯 ---
    async register() {
      this.isLoading = true;
      this.errors = {}; // 清空舊錯誤

      // 1. 前端驗證 (Front-end Validation)
      let isValid = true;
      if (!this.form.email) {
        this.errors.email = "必須填寫 Email";
        isValid = false;
      }
      if (!this.form.password) {
        this.errors.password = "必須填寫密碼";
        isValid = false;
      }
      if (this.form.password !== this.form.confirmPassword) {
        this.errors.confirmPassword = "兩次輸入的密碼不一致";
        isValid = false;
      }

      if (!isValid) {
        this.isLoading = false;
        return false;
      }

      try {
       const API_BASE_URL = "https://localhost:7190/api";
        const response = await axios.post(
         `${API_BASE_URL}/AuthApi/register`,
          {
            email: this.form.email,
            password: this.form.password,
            confirmPassword: this.form.confirmPassword,
          },
          {
            headers: { "Content-Type": "application/json" }
          }
        );

        if (response.status === 200) {
          alert("註冊成功！");
          this.resetForm();
          return true;
        }
      } catch (error) {
        console.error("註冊失敗", error);
        if (error.response && error.response.data) {
          this.errors.email = error.response.data.message || "註冊失敗";
        } else {
          this.errors.email = "無法連接伺服器";
        }
        return false;
      } finally {
        this.isLoading = false;
      }
    },
async login(loginData) {
      this.isLoading = true;
      this.errors = {email: ""};
try {
    const response = await axios.post(
      "https://localhost:7190/api/AuthApi/Login", // 請確認您的後端完整網址
      loginData,
      {
        withCredentials: true, 
        headers: { "Content-Type": "application/json" },
      }
    );

    if (response.status === 200) {     
      this.user = response.data;       
      this.resetForm();
      return true;
    }
  } catch (error) {
    console.error("登入失敗", error);
    
    if (error.response && error.response.data) {      
      this.errors.email = error.response.data.message || error.response.data || "帳號或密碼錯誤";
    } else {
      this.errors.email = "無法連接伺服器";
    }
    return false;
  } finally {
    this.isLoading = false;
  }
},

    async logout() {
      // ... 您的登出邏輯 (保持不變)
      try {
        await axios.post("https://localhost:7190/api/AuthApi/logout", {}, { withCredentials: true });
        this.user = null;
      } catch (error) {
        console.error("登出失敗", error);
      }
    }
  },
});