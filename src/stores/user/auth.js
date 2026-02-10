import { defineStore } from "pinia";
import { ref, computed } from "vue";
import axios from "axios";

export const useAuthStore = defineStore("auth", () => {
  // --- State (資料狀態) --- 
  const user = ref(null);
  const isAuthenticated = ref(false);
  const isLoading = ref(false);
  const updateUserInfo = (newData) => {
    if (user.value) {
      // 保留原本的 id, role 等不變，只覆蓋傳進來的新值 (例如 username, avatar)
      user.value = {
        ...user.value,
        ...newData
      };
    }
  };
  const userAvatar = computed(() => {
    return user.value?.avatar || 'https://cube.elemecdn.com/3/7c/3ea6beec64369c2642b92c6726f1epng.png';
  });

  async function forgotPassword(email) {
    isLoading.value = true;
    try {
      // 注意：後端 Controller 目前寫死了 frontendUrl，所以這裡只要傳 email
      const response = await axios.post("/AuthApi/forgot-password", { email });
      return response.status === 200;
    } catch (error) {
      console.error("發送重設信失敗", error);
      throw error; // 將錯誤拋出給前端組件處理
    } finally {
      isLoading.value = false;
    }
  }

  async function resetPassword(payload) {
    // payload 格式: { email, token, newPassword }
    isLoading.value = true;
    try {
      const response = await axios.post("/AuthApi/reset-password", payload);
      return response.status === 200;
    } catch (error) {
      console.error("重設密碼失敗", error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  }

  async function login(payload) {
    isLoading.value = true;
    try {
      const response = await axios.post("/AuthApi/Login", payload);
      if (response.status === 200) {
        const userData = response.data.user;
        user.value = {
          id: userData.userid || userData.UserId,
          username: userData.username,
          role: userData.role,
          avatar: userData.avatarUrl || userData.ImageUrl
        };
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
      if (response.status === 200) {
        if (response.data.user) {
          const userData = response.data.user;
          user.value = {
            id: userData.userid,
            username: userData.username,
            email: userData.email,
            role: userData.role,
            avatar: userData.avatarUrl || 'https://cube.elemecdn.com/3/7c/3ea6beec64369c2642b92c6726f1epng.png'
          };
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
      window.location.href = '/'; //router.push('/');
    }
  }

  async function checkAuth() {
    try {
      const res = await axios.get("/AuthApi/check");
      if (res.status === 200 && res.data.isLoggedIn) {
        const profileRes = await axios.get(`/User/profile?t=${new Date().getTime()}`);
        const profileData = profileRes.data;
        user.value = {
          // 使用 profile API 回傳的最新資料
          id: profileData.userId || profileData.UserId,
          username: profileData.username || profileData.Username,
          role: res.data.role,
          avatar: profileData.imageUrl || profileData.ImageUrl
        };
        isAuthenticated.value = true;
        return true;
      } else {
        throw new Error("Not logged in")
      }
    } catch (error) {
      user.value = null;
      isAuthenticated.value = false;
      return false;
    }
  }

  return {
    user,
    isAuthenticated,
    isLoading,
    userAvatar,
    login,
    register,
    logout,
    checkAuth,
    updateUserInfo,
    forgotPassword,
    resetPassword
  };
}, {
  persist: true // 保持登入
});