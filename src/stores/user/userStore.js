import { defineStore } from "pinia";
import { ref, computed } from "vue";
import axios from "axios";
import { useAuthStore } from "@/stores/user/auth"; // 引用另一個 store 拿 token
import defaultprofilePic from "@/assets/img/bruce-mars.jpg"; // 預設大頭貼

export const useUserStore = defineStore("user", () => {
  const userProfile = ref({
    name: "載入中...",
    posts: 0,
    followers: 0,
    following: 0,
    description: "",
    imageUrl: ""
  });

  const isLoading = ref(false);
  const displayAvatar = computed(() => {
    return userProfile.value.imageUrl || defaultprofilePic;
  });
  //會員資料
  const authStore = useAuthStore();
  const API_BASE_URL = "https://localhost:7190/api";
  const fetchUserProfile = async () => {

    isLoading.value = true;
    try {
      const response = await axios.get(
        `${API_BASE_URL}/UserApi/profile`,
        {
          headers: { Authorization: `Bearer ${authStore.token}` }
        }
      );

      const data = response.data;
      userProfile.value = {
        name: data.username || data.UserName || "未命名",
        // posts: data.PostCount || 0,
        // followers: data.FollowerCount || 0,
        // following: data.FollowingCount || 0,
        // description: data.SelfIntro || "",
        imageUrl: data.ImageUrl || ""
      };

    } catch (error) {
      console.error("取得資料失敗", error);
    } finally {
      isLoading.value = false;
    }
  };

  return { userProfile, isLoading, displayAvatar, fetchUserProfile };

  // persist: true 資料就會自動存到 localStorage
});

// 關注清單
const followingCount = computed(() =>
  followingList.value ? followingList.value.length : 0
);
const followingList = ref([]);
const fetchFollowingList = async () => {
  try {
    isLoading.value = true;
    const response = await axios.get(url, authStore.getAxiosConfig());

    // 檢查 1: 如果回傳的是字串且包含 HTML 標籤，代表被導向去登入頁面了
    if (
      typeof response.data === "string" &&
      response.data.includes("<!DOCTYPE")
    ) {
      alert("請重新登入");
      router.push("/login");
      return;
    }

    const data = response.data;
    if (Array.isArray(data)) {
      followingList.value = data;
    } else if (data && Array.isArray(data.result)) {
      followingList.value = data.result;
    } else {
      followingList.value = [];
    }
  } catch (error) {
    console.error("取得關注清單失敗:", error);
    if (
      error.response &&
      (error.response.status === 401 || error.response.status === 403)
    ) {
      alert("請先登入");
      router.push("/login");
    }
  } finally {
    isLoading.value = false;
  }
};
// 取消關注功能
const unfollow = async (targetId) => {
  if (!confirm("確定要取消關注嗎？")) return;

  try {
    await axios.post(
      `${API_BASE_URL}/Social/unfollow/${targetId}`,
      {}, // POST 需要有 body，即使是空的
      getAxiosConfig()
    );

    // 成功後，從前端清單移除該使用者，避免重新整理頁面
    followingList.value = followingList.value.filter((u) => u.id !== targetId);
    alert("已取消關注");
  } catch (error) {
    console.error("取消關注失敗", error);
    alert("操作失敗，請稍後再試");
  }
};