import { defineStore } from "pinia";
import { ref, computed } from "vue";
import axios from "axios";
import defaultprofilePic from "@/assets/img/bruce-mars.jpg"; // 預設大頭貼

export const useUserStore = defineStore("user", () => {
  const isLoading = ref(false);
  const userProfile = ref({
    id: 0,
    name: "載入中...",
    imageUrl: ""
    //  預設統計數據，避免畫面報錯
    // articleCount: 0,
    // missionCount: 0,
    // points: 0,
    // followers: 0,
    // following: 0
  });
  const followingList = ref([]); //關注 追蹤
  const followersList = ref([]); //粉絲
  const followingCount = computed(() => followingList.value.length);
  const followersCount = computed(() => followersList.value.length);

  const displayAvatar = computed(() => {
    return userProfile.value.imageUrl || defaultprofilePic;
  });

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

      userProfile.value = {
        id: data.userId || data.UserId || data.id || 0,
        username: data.username || data.Username || "載入中...",
        email: data.email || data.Email || "",
        phone: data.phone || data.Phone || "",
        address: data.address || data.Address || "",
        imageUrl: data.imageUrl || data.ImageUrl || "",
        birthday:
          data.birthday || data.Birthday
            ? (data.birthday || data.Birthday).split("T")[0]
            : "",

        // 統計數據 (若後端有回傳)
        // articleCount: data.articleCount || data.ArticleCount || 0,
        // missionCount: data.missionCount || data.MissionCount || 0,
        // points:       data.points       || data.Points       || 0,
        // followers:    data.followers    || data.Followers    || 0,
        // following:    data.following    || data.Following    || 0,
      };

    } catch (error) {
      console.error(error);
      if (error.response && error.response.status === 404) {
        userProfile.value = {
          ...userProfile.value,
          name: "查無此使用者",
          id: 0
        };
      }
    } finally {
      isLoading.value = false;
    }
  };

  // 檢查「目前顯示的這位用戶」是否在「我的關注清單」控制按鈕顯示 "追蹤" 還是 "取消追蹤"
  const isFollowingCurrentUser = computed(() => {
    if (!userProfile.value.id) return false;// API GetMyFollowing 回傳的是FolloweeId    
    return followingList.value.some(u => u.followeeId === userProfile.value.id);
  });

  // 2. 取得「我」的關注清單 (GetMyFollowing)
  const fetchFollowingList = async () => {
    try {
      const response = await axios.get("/Follow/following");//  回傳 List<object>，通常 Axios 會自動解析為陣列      
      followingList.value = response.data || [];
    } catch (error) {
      console.error("取得關注清單失敗", error);
    }
  };

  // 3. 取得「我」的粉絲清單 (GetMyFollowers) 
  const fetchFollowersList = async () => {
    try {
      const response = await axios.get("/Follow/followers");
      followersList.value = response.data || [];
    } catch (error) {
      console.error("取得粉絲清單失敗", error);
    }
  };

  // 4. 追蹤用戶 (POST)
  const followUser = async (targetId) => {
    if (!targetId) return;
    isLoading.value = true;
    try {
      await axios.post(`/Follow/${targetId}`);// 成功後，重新抓取一次關注清單     

      await fetchFollowingList();
    } catch (error) {
      console.error("追蹤失敗", error);
      alert(error.response?.data || "追蹤失敗");
    } finally {
      isLoading.value = false;
    }
  };

  // 5. 取消追蹤 (DELETE)
  const unfollowUser = async (targetId) => {
    if (!targetId) return;
    isLoading.value = true;
    try {
      await axios.delete(`/Follow/${targetId}`);
      // 成功後，從前端陣列移除該用戶 API GetMyFollowing 回傳的欄位是 FolloweeId
      followingList.value = followingList.value.filter(u => u.followeeId !== targetId);
    } catch (error) {
      console.error("取消關注失敗", error);
    } finally {
      isLoading.value = false;
    }
  };

  return {
    userProfile,
    followingList,
    followersList,
    isLoading,
    displayAvatar,
    followingCount,
    followersCount,
    isFollowingCurrentUser, // 匯出這個 computed 給頁面用
    fetchUserProfile,
    fetchFollowingList,
    fetchFollowersList,
    followUser,
    unfollowUser
  };
});