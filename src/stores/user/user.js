import { defineStore } from "pinia";
import { ref, computed } from "vue";
import axios from "axios";
import defaultprofilePic from "@/assets/img/bruce-mars.jpg"; // 預設大頭貼

export const useUserStore = defineStore("user", () => {
  const isLoading = ref(false);

  // --- 1. 個人檔案資料 (目前瀏覽的對象) ---
  const userProfile = ref({
    id: 0,
    name: "載入中...",
    username: "",
    imageUrl: "",
    articleCount: 0,
    missionCount: 0,
    points: 0,
    followers: 0, // 統計數字 (顯示在卡片上)
    following: 0, // 統計數字 (顯示在卡片上)
  });

  const displayAvatar = computed(() => {
    return userProfile.value.imageUrl || defaultprofilePic;
  });

  // 目前瀏覽對象的關注/粉絲清單
  const profileFollowingList = ref([]);
  const profileFollowersList = ref([]);

  // 登入者(我)的關注清單
  const myFollowingList = ref([]);

  // 檢查「目前顯示的這位用戶」是否在「我的關注清單」中
  // 用來控制按鈕顯示 "已追蹤" 還是 "追蹤"
  const isFollowingCurrentUser = computed(() => {
    if (!userProfile.value.id) return false;
    // 比對 myFollowingList 中的 followeeId 是否等於目前頁面用戶 ID
    return myFollowingList.value.some(u => u.followeeId === userProfile.value.id);
  });

  // 取得特定用戶的個人檔案
  const fetchUserProfile = async (username) => {
    isLoading.value = true;
    try {
      let url = '/User/profile';
      const timestamp = new Date().getTime();

      if (username) {
        url += `?username=${username}&t=${timestamp}`;
      } else {
        url += `?t=${timestamp}`;
      }

      const response = await axios.get(url);
      const data = response.data;
      const currentUserId = data.userId || data.UserId || data.id || 0;

      userProfile.value = {
        ...userProfile.value,
        id: currentUserId,
        username: data.username || data.Username || "載入中...",
        email: data.email || data.Email || "",
        phone: data.phone || data.Phone || "",
        address: data.address || data.Address || "",
        imageUrl: data.imageUrl || data.ImageUrl || "",
        birthday: data.birthday || data.Birthday
          ? (data.birthday || data.Birthday).split("T")[0]
          : "",
      };

      // 取得統計數據 (文章數、關注數、粉絲數...)
      if (currentUserId) {
        try {
          const statsRes = await axios.get(`/User/${currentUserId}/stats`);
          userProfile.value.articleCount = statsRes.data.articleCount;
          userProfile.value.missionCount = statsRes.data.missionCount;
          userProfile.value.points = statsRes.data.pointBalance;
          userProfile.value.followers = statsRes.data.followerCount;
          userProfile.value.following = statsRes.data.followingCount;
        } catch (statsErr) {
          console.warn("統計數據載入失敗", statsErr);
          // 失敗時歸零
          userProfile.value.articleCount = 0;
          userProfile.value.missionCount = 0;
          userProfile.value.points = 0;
          userProfile.value.followers = 0;
          userProfile.value.following = 0;
        }
      }
    } catch (error) {
      console.error(error);
      if (error.response && error.response.status === 404) {
        userProfile.value = { ...userProfile.value, name: "查無此使用者", id: 0 };
      }
    } finally {
      isLoading.value = false;
    }
  };

  // 取得「我 (登入者)」的關注清單 -> 用於按鈕狀態判斷
  const fetchMyFollowingList = async () => {
    try {
      const response = await axios.get("/Follow/following");
      myFollowingList.value = response.data || [];
    } catch (error) {
      console.error("取得我的關注清單失敗", error);
    }
  };

  // 取得「目前查看對象」的關注清單 -> 用於 Dialog 顯示
  const fetchUserFollowingList = async (targetUserId) => {
    if (!targetUserId) return;
    try {
      // 假設後端支援 /Follow/following/{userId} 或是 query param
      // 如果後端路徑不同，請依據 Swagger 修改，例如 `/Follow/${targetUserId}/following`
      const response = await axios.get(`/Follow/following/${targetUserId}`);
      profileFollowingList.value = response.data || [];
    } catch (error) {
      console.error("取得該用戶關注清單失敗", error);
      profileFollowingList.value = [];
    }
  };

  // 取得「目前查看對象」的粉絲清單 -> 用於 Dialog 顯示
  const fetchUserFollowersList = async (targetUserId) => {
    if (!targetUserId) return;
    try {
      const response = await axios.get(`/Follow/followers/${targetUserId}`);
      profileFollowersList.value = response.data || [];
    } catch (error) {
      console.error("取得該用戶粉絲清單失敗", error);
      profileFollowersList.value = [];
    }
  };

  // 追蹤用戶 (Action)
  const followUser = async (targetId) => {
    if (!targetId) return;
    isLoading.value = true;
    try {
      await axios.post(`/Follow/${targetId}`);

      // 追蹤成功後更新「我的關注清單」，按鈕變 "已追蹤"
      await fetchMyFollowingList();

      // 卡片上的粉絲數字 +1
      userProfile.value.followers += 1;

    } catch (error) {
      console.error("追蹤失敗", error);
      // alert(error.response?.data || "追蹤失敗");
    } finally {
      isLoading.value = false;
    }
  };

  // 取消追蹤 (Action)
  const unfollowUser = async (targetId) => {
    if (!targetId) return;
    isLoading.value = true;
    try {
      await axios.delete(`/Follow/${targetId}`);

      // 成功後，從「我的關注清單」移除該 ID，讓按鈕變回 "追蹤"
      myFollowingList.value = myFollowingList.value.filter(u => u.followeeId !== targetId);

      // 選項：手動調整卡片數字
      if (userProfile.value.followers > 0) {
        userProfile.value.followers -= 1;
      }

    } catch (error) {
      console.error("取消關注失敗", error);
    } finally {
      isLoading.value = false;
    }
  };

  return {
    // State
    userProfile,
    isLoading,
    displayAvatar,

    // Lists
    myFollowingList,      // 按鈕狀態用
    profileFollowingList, // Dialog 顯示用 (關注)
    profileFollowersList, // Dialog 顯示用 (粉絲)

    // Getters
    isFollowingCurrentUser,

    // Actions
    fetchUserProfile,
    fetchMyFollowingList,   // 載入我的清單
    fetchUserFollowingList, // 載入對方的關注清單
    fetchUserFollowersList, // 載入對方的粉絲清單
    followUser,
    unfollowUser,
  };
});