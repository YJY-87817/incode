import { defineStore } from "pinia";
import { ref, computed } from "vue";
import axios from "axios";
import defaultprofilePic from "@/assets/img/bruce-mars.jpg"; // 預設大頭貼

export const useUserStore = defineStore("user", () => {
  const API_BASE_URL = "https://localhost:7190/api";
  const userProfile = ref({
    name: "載入中...",
    posts: 0,
    followers: 0,
    following: 0,
    description: "",
    imageUrl: ""
  });

  const isLoading = ref(false);
  const followingList = ref([]);
const followingCount = computed(() =>
  followingList.value ? followingList.value.length : 0
);
  const displayAvatar = computed(() => {
    return userProfile.value.imageUrl || defaultprofilePic;
  });  
  
  const fetchUserProfile = async () => {
    isLoading.value = true;
    try {
      const response = await axios.get(
        `${API_BASE_URL}/UserApi/profile`,       
      );
      const data = response.data;
      userProfile.value = {
        name: data.username || data.UserName || "未命名",
        // posts: data.PostCount || 0,
        // followers: data.FollowerCount || 0,
        // following: data.FollowingCount || 0,       
        imageUrl: data.ImageUrl || ""
      };
    } catch (error) {
      console.error("取得資料失敗", error);
    } finally {
      isLoading.value = false;
    }
  };

const fetchFollowingList = async () => {
  isLoading.value = true;
  try {    
    const response = await axios.get(`${API_BASE_URL}/Follow/following`);
const data = response.data;
   if (Array.isArray(data)) {
        followingList.value = data;
      } else if (data && Array.isArray(data.result)) {
        followingList.value = data.result;
      } else {
        followingList.value = [];
      }
    } catch (error) {
        console.error("取得關注清單失敗", error);
    } finally {
      isLoading.value = false;
    }
  };

const unfollow = async (targetId) => {
  if (!confirm("確定要取消關注嗎？")) return;
  try {
    await axios.post(
      `${API_BASE_URL}/Follow/${targetId}`);
     followingList.value = followingList.value.filter((u) => u.id !== targetId);
      alert("已取消關注");
    } catch (error) {
      console.error("取消關注失敗", error);
    }
  };
  return { 
    userProfile, 
    followingList, 
    isLoading, 
    displayAvatar, 
    followingCount, 
    fetchUserProfile, 
    fetchFollowingList, 
    unfollow 
  };
});