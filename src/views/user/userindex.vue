<script setup>
  import { onMounted, onUnmounted, ref, computed } from "vue";
  import { useRouter } from "vue-router";
  import axios from "axios";
  import Typed from "typed.js";
  // 引入 Store 以便取得 Token
  import { useAuthStore } from "@/views/user/auth";
  const authStore = useAuthStore();

  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterDefault.vue";
  import bg0 from "@/assets/img/bg9.jpg";
  import Information from "@/views/user/Sections/userInformation.vue";
  import Profile from "@/views/user/Sections/userProfile.vue";
  import Posts from "@/views/user/Sections/userPosts.vue";

  const router = useRouter();
  const API_BASE_URL = "https://localhost:7190/api";

  const goToBasic = () => {
    router.push("/login");
  };

  const isLoading = ref(false);
  // 計算關注人數
  const followingCount = computed(() =>
    followingList.value ? followingList.value.length : 0
  );

  const getAxiosConfig = () => {
    const config = {
      withCredentials: true, // 允許跨域請求攜帶 Cookie (Session)
      headers: {
        "Content-Type": "application/json",
      },
    };
    return config; //彈性方便改回 JWT 模式
  };

  // 關注清單
  const fetchFollowingList = async () => {
    try {
      isLoading.value = true;
      console.log(`正在請求 API: ${API_BASE_URL}/Social/following`);
      const response = await axios.get(
        `${API_BASE_URL}/Social/following`,
        getAxiosConfig()
      );
      //console.log("API 回傳原始資料:", response);

      // 檢查 1: 如果回傳的是字串且包含 HTML 標籤，代表被導向去登入頁面了
      if (
        typeof response.data === "string" &&
        response.data.trim().startsWith("<!DOCTYPE")
      ) {
        // console.error(
        //   "錯誤：API 回傳了 HTML 頁面而非 JSON，通常代表未登入或 Cookie 無效。"
        // );
        alert("請先登入會員");
        router.push("/login");
        return;
      }

      const data = response.data;
      if (Array.isArray(data)) {
        followingList.value = data;
      } else if (data && Array.isArray(data.result)) {
        followingList.value = data.result;
      } else {
        console.warn("API 回傳格式非陣列，設為空值");
        followingList.value = [];
      }
    } catch (error) {
      console.error("取得關注清單失敗:", error);
      if (
        error.response &&
        (error.response.status === 401 || error.response.status === 403)
      ) {
        alert("權限不足或未登入");
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

  //hooks
  onMounted(() => {
    const body = document.getElementsByTagName("body")[0];
    body.classList.add("about-us");
    body.classList.add("bg-gray-200");

    if (document.getElementById("typed")) {
      new Typed("#typed", {
        stringsElement: "#typed-strings",
        typeSpeed: 90,
        backSpeed: 90,
        backDelay: 200,
        startDelay: 500,
        loop: true,
      });
    }
    fetchFollowingList(); // 呼叫後端 API
  });

  onUnmounted(() => {
    const body = document.getElementsByTagName("body")[0];
    body.classList.remove("about-us");
    body.classList.remove("bg-gray-200");
  });
</script>
<template>
  <DefaultNavbar :action="{
      route: 'javascript:;',
      label: 'Buy Now',
      color: 'btn-white',
    }" transparent />
  <header class="bg-gradient-dark">
    <div class="page-header min-vh-75" :style="{ backgroundImage: `url(${bg0})` }">
      <span class="mask bg-gradient-dark opacity-6"></span>
      <div class="container">
        <div class="row justify-content-center">
          <div class="col-lg-8 text-center mx-auto my-auto">
            <h1 class="text-white">
              welcome to incode<span class="text-white" id="typed"></span>
            </h1>
            <div id="typed-strings">
              <h1>!!!</h1>
              <h1>~~~</h1>
            </div>
            <p class="lead mb-4 text-white opacity-8">會員中心</p>
            <button type="submit" class="btn bg-white text-dark" @click="goToBasic">
              <span class="text-success text-gradient font-weight-bold">
                登入
              </span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
  <Profile />

  <div class="card card-body shadow-xl mx-3 mx-md-4 mt-n6">
    <Information />

    <div class="row mt-4" v-if="followingList && followingList.length > 0">
      <div class="col-md-8 col-12 mx-auto">
        <h5>關注列表詳情：({{ followingCount }}人)</h5>
        <ul class="list-group">
          <li v-for="user in followingList" :key="user.userId"
            class="list-group-item d-flex justify-content-between align-items-center">
            <div class="d-flex align-items-center">
              <!-- 顯示頭像 (如果有) -->
              <img v-if="user.imageUrl" :src="user.imageUrl" class="avatar avatar-sm me-3 border-radius-lg"
                alt="user" />
              <div class="d-flex flex-column">
                <!-- 修正：使用 username 對應 DTO -->
                <h6 class="mb-1 text-dark text-sm">
                  {{ user.Username || "未命名使用者" }}
                </h6>
                <span class="text-xs">{{ user.Email }}</span>
              </div>
            </div>
            <button class="btn btn-sm btn-outline-danger mb-0" @click="unfollow(user.userId)">
              取消關注
            </button>
          </li>
        </ul>
      </div>
    </div>

    <!-- 讀取中 -->
    <div class="row mt-4" v-else-if="isLoading">
      <div class="col-12 text-center">
        <p>載入中...</p>
      </div>
    </div>
    <div class="row mt-4" v-else-if="!isLoading">
      <div class="col-12 text-center text-muted">
        <p>目前沒有關注任何人<!-- 無資料時的顯示 --></p>
      </div>
    </div>
  </div>
  <div class="card card-body blur shadow-blur mx-3 mx-md-4 mt-n6 mb-4">
    <Posts />

    <!-- <AboutTeam /> -->
  </div>
  <DefaultFooter />
</template>