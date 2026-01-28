import { defineStore } from "pinia";
import { ref } from "vue";
import axios from "axios";

export const useAuthStore = defineStore("auth", () => {
  // --- State (資料狀態) --- 
  const user = ref(null);
  const isAuthenticated = ref(false);
  const isLoading = ref(false);

async function login(payload) {
    isLoading.value = true;
    try {      
      const response = await axios.post("/AuthApi/Login", payload);
      if (response.status === 200) {       
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
      const response = await axios.post("/AuthApi/Register", payload);
if (response.status === 200) {// 因為後端會回傳 user 物件，直接存起來         
          if (response.data.user) {
         user.value = response.data.user;
         isAuthenticated.value = true;
      }  
      return true;
    }
  } catch (error) {
    console.error("註冊失敗", error);
    throw error;
  } finally {
    isLoading.value = false;
  }
}

  async function logout() {
    try {
      await axios.post("/AuthApi/Logout");
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
      const res = await axios.get("/AuthApi/check"); 
      if (res.status === 200 && res.data.isLoggedIn) {       
      user.value = {
        id: res.data.userId,       
        username: res.data.username,
        role: res.data.role,
        avatar: res.data.avatar ||'https://cube.elemecdn.com/3/7c/3ea6beec64369c2642b92c6726f1epng.png' //改成替代圖片
      };      
      isAuthenticated.value = true;
    } else {
      user.value = null;
        isAuthenticated.value = false;
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