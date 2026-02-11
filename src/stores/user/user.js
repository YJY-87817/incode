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
    userProfile.value.id = 0;
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
      const currentUserId = data.userId ||data.userid||  data.UserId || data.id || 0;

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
          articleCount: 0,
        missionCount: 0,
        points: 0,
        followers: 0,
        following: 0
      };

      // 取得統計數據 (文章數、關注數、粉絲數...)
      if (currentUserId) {
        try {
          const timestamp = new Date().getTime();
          const statsRes = await axios.get(`/User/${currentUserId}/stats?t=${timestamp}`);
          const sData = statsRes.data;
         userProfile.value.articleCount = sData.articleCount || sData.ArticleCount || 0;
        userProfile.value.missionCount = sData.missionCount || sData.MissionCount || 0;
        userProfile.value.points = sData.pointBalance || sData.PointBalance || 0;
        userProfile.value.followers = sData.followers || sData.FollowerCount || 0;
        userProfile.value.following = sData.following || sData.FollowingCount || 0;
        } catch (statsErr) {
          console.warn("統計數據載入失敗", statsErr);          
        }
      }
    } catch (error) {
      console.error(error);
      if (error.response && error.response.status === 404) {
        userProfile.value = { id: 0, 
            username: "查無此使用者", 
            imageUrl: "",
            articleCount: 0,
            followers: 0,
            following: 0};
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
      const response = await axios.get(`/Follow/following/${targetUserId}`);
      profileFollowingList.value = response.data || [];
    } catch (error) {
      console.error(`取得用戶 ${targetUserId} 的關注清單失敗`, error);
      profileFollowingList.value = [];
    }    finally {
      isLoading.value = false; 
    }
  };

  // 取得「目前查看對象」的粉絲清單 -> 用於 Dialog 顯示
  const fetchUserFollowersList = async (targetUserId) => {
    if (!targetUserId) return;
    isLoading.value = true;
    try {
      const response = await axios.get(`/Follow/followers/${targetUserId}`);
      profileFollowersList.value = response.data || [];
    } catch (error) {
      console.error(`取得用戶 ${targetUserId} 的粉絲清單失敗`, error);
      profileFollowersList.value = [];
    }finally {
      isLoading.value = false; 
    }
  };

  // 追蹤用戶 (Action)
  const followUser = async (targetId) => {
    if (!targetId) return;   
    try {
      await axios.post(`/Follow/${targetId}`);

      // 追蹤成功後更新「我的關注清單」，按鈕變 "已追蹤"
      await fetchMyFollowingList();

// 當「我追蹤的人」就是「目前頁面顯示的人」時，才更新數字
      if (userProfile.value && String(userProfile.value.id) === String(targetId)) {
         userProfile.value.followers = (userProfile.value.followers || 0) + 1;
      }
    } catch (error) {
      console.error("追蹤失敗", error);
    } 
  };

  // 取消追蹤 (Action)
  const unfollowUser = async (targetId) => {
    if (!targetId) return;
    try {
      await axios.delete(`/Follow/${targetId}`);

      // 成功後，從「我的關注清單」移除該 ID，讓按鈕變回 "追蹤"
     myFollowingList.value = myFollowingList.value.filter(u => {
          const uid = u.id || u.followeeId || u.Id; // 抓取各種可能的 ID 欄位
          return String(uid) !== String(targetId);
      });

      if (userProfile.value && String(userProfile.value.id) === String(targetId)) {
         if (userProfile.value.followers > 0) {
            userProfile.value.followers -= 1;
         }
      }
    } catch (error) {
      console.error("取消關注失敗", error);
      await fetchMyFollowingList();
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