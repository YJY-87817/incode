import { defineStore } from "pinia";
import { ref } from "vue";
import axios from "axios";

export const useAuthStore = defineStore("auth", () => {
  // --- State (資料狀態) ---
  const user = ref(null);
  const isAuthenticated = ref(false);
  const isLoading = ref(false);

  // 整合表單資料 (讓 Login/Register 頁面共用)
  const form = ref({
    email: "",
    username: "",
    password: "",
    confirmPassword: "",
    name: "",
    birthday: "",
    avatarUrl: ""
  });
  const errors = ref({});

  // --- Helper Actions (輔助功能) ---

  // 重置表單與錯誤
  function resetForm() {
    form.value = {
      email: "",
      username: "",
      password: "",
      confirmPassword: "",
      name: "",
      birthday: "",
      avatarUrl: ""
    };
    errors.value = {};
    isLoading.value = false;
  }
  
  async function login() {
    isLoading.value = true;
    errors.value = {};

    try {
      // 設定 withCredentials 為 true 是 Cookie 驗證的關鍵
      const response = await axios.post(
        "/api/AuthApi/Login", 
        {
          email: form.value.email,
          password: form.value.password
        }
      );

      if (response.status === 200) {
        user.value = response.data; // 儲存使用者資料
        isAuthenticated.value = true;
        return true; // 回傳成功
      }
    } catch (error) {
      console.error("登入失敗", error);
      if (error.response && error.response.data) {
        // 假設後端回傳錯誤訊息在 message 欄位
        errors.value.email = error.response.data.message || "帳號或密碼錯誤";
      } else {
        errors.value.email = "無法連接伺服器";
      }
      return false; // 回傳失敗
    } finally {
      isLoading.value = false;
    }
  }

  // 2. 註冊
  async function register() {
    isLoading.value = true;
    errors.value = {};

    // 前端簡易驗證
    if (form.value.password !== form.value.confirmPassword) {
      errors.value.confirmPassword = "兩次輸入的密碼不一致";
      isLoading.value = false;
      return false;
    }

    try {
      const response = await axios.post("/api/AuthApi/Register", form.value);

      if (response.status === 200) {
        return true;
      }
    } catch (error) {
      console.error("註冊失敗", error);
      errors.value.email = error.response?.data?.message || "註冊發生錯誤";
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  // 3. 登出
  async function logout() {
    try {
      // 呼叫後端清除 Server 端的 Cookie
      await axios.post("/api/AuthApi/Logout");
    } catch (e) {
      console.error("登出 API 錯誤", e);
    } finally {
      // 無論 API 是否成功，前端都要清除狀態
      user.value = null;
      isAuthenticated.value = false;
      resetForm();
      // 選擇性：重新載入頁面以確保乾淨的狀態
      // window.location.reload(); 
    }
  }

  // 4. 檢查登入狀態 (給 App.vue onMounted 使用)
  // 這是 Cookie 驗證最重要的部分：重新整理後確認 Cookie 是否有效
  async function checkAuth() {
    try {
      const res = await axios.get("/api/AuthApi/check"); 
      if (res.status === 200) {
        user.value = res.data;
        isAuthenticated.value = true;
      }
    } catch {
      // Cookie 過期或無效
      user.value = null;
      isAuthenticated.value = false;
    }
  }

  return {
    user,
    isAuthenticated,
    isLoading,
    form,
    errors,
    login,
    register,
    logout,
    checkAuth,
    resetForm
  };
});