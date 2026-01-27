import { defineStore } from "pinia";
import { ref } from "vue";
import axios from "axios";

export const useAuthStore = defineStore("auth", () => {
  // --- State (資料狀態) ---
  const API_BASE_URL = "https://localhost:7190/api";
  const user = ref(null);
  const isAuthenticated = ref(false);
  const isLoading = ref(false);

async function login(payload) {
    isLoading.value = true;
    try {      
      const response = await axios.post(`${API_BASE_URL}/AuthApi/Login`, payload);
      if (response.status === 200) {     
        console.log("後端回傳的完整資料:", response.data);
        console.log("我嘗試抓取的 User:", response.data.user);   
        user.value = response.data.user;
        isAuthenticated.value = true;
        return true; 
      }
    } catch (error) {
      console.error("登入失敗", error);
      throw error; 
    } finally {
      isLoading.value = false;
    }
  }

  async function register(payload) {
    isLoading.value = true;
    try {
      const response = await axios.post(`${API_BASE_URL}/AuthApi/Register`, payload);
      return response.status === 200;
    } catch (error) {
      console.error("註冊失敗", error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  }

  async function logout() {
    try {
      await axios.post(`${API_BASE_URL}/AuthApi/Logout`);
    } catch (e) {
      console.error("登出 API 錯誤", e);
    } finally {      
      user.value = null;
      isAuthenticated.value = false;
      console.log("正在執行跳轉...");
      window.location.href = '/'; //跳轉回首頁
    }
  }

  async function checkAuth() {
    try {      
      const res = await axios.get(`${API_BASE_URL}/AuthApi/check`); 
      if (res.status === 200 && res.data.isLoggedIn) {       
      user.value = {
        id: res.data.userId,       
        username: res.data.username,
        role: res.data.role
      };      
      isAuthenticated.value = true;
    } else {
      throw new Error("未登入");
    }
  } catch (error) {
    user.value = null;
    isAuthenticated.value = false;
  }
}

  return {
    user,
    isAuthenticated,
    isLoading,
    login,
    register,
    logout,
    checkAuth
  };
}, {
    persist: true // 保持登入
});