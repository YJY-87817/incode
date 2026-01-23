<!-- 會員點數紀錄頁面 -->
<script setup>
import{onMounted, computed} from "vue";
import NavbarDefault from "@/examples/navbars/NavbarDefault.vue";
import FooterDefault from "@/examples/footers/FooterDefault.vue";
import UserProfilePage from "@/components/point/UserProfilePage.vue";
import { useUserStore } from "@/stores/point/userStore";

// A.準備要傳給子元件的資料 (可以來自 API 或 Store)
// const currentUser = ref({
//   name: "Chanhao",
//   level: 3,
//   id: "Chanhao122",
//   title: "初級冒險者",
//   exp: 85,
// });

// 1. 初始化資料 (先給預設值或 null)
// const currentUser = ref({
//   name: "載入中...",
//   level: 0,
//   id: "-",
//   title: "-",
//   exp: 0,
// });

//2.建立抓取資料的函式
// const fetchUserData = async () =>{
//   try{
//       // 模擬 API 請求 (請根據實際 API 端點修改)
//       const response = await axios.get("https://api.example.com/user/profile");
//       // 將 API 回傳的資料存入ref
//       // 假設 API 回傳格式為 { name: '...', level: ... }
//       currentUser.value = response.data;
//   }catch(error){
//       console.error("取得使用者資料失敗:", error);
//   }
// }

// 4. 在組件掛載時執行 API 請求
const userStore = useUserStore();
// 使用 computed 來連結 Store 裡的資料，這樣 Store 更新時這裡才會跟著變
const currentUser = computed(() => userStore.userProfile);
const isLoading = computed(() => userStore.isLoading)
const errorMsg = computed(() => userStore.fetchError);

onMounted(() => {
  // 只在必要時載入（例如 id 還是預設值時）
  if (currentUser.value.id === '-' || currentUser.value.name === "載入中...") {
    userStore.fetchUserProfile()
  }
});
</script>

<template>
  <NavbarDefault light />

 <div v-if="errorMsg" class="alert alert-danger text-center m-5 p-4">
    {{ errorMsg }}
    <button class="btn btn-outline-danger ms-3" @click="userStore.fetchUserProfile">
      重試
    </button>
  </div>

  <div v-else-if="isLoading || currentUser.id === '-'">
    <div class="text-center p-5 my-5">
      <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;"></div>
      <div class="mt-3 fs-5">正在召喚資料...</div>
    </div>
  </div>

  <div v-else>
    <UserProfilePage :user="currentUser" />
  </div>

  <FooterDefault />
</template>
