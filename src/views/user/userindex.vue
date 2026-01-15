<script setup>
import { onMounted, onUnmounted, ref, computed } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";
import Typed from "typed.js";

import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";
import bg0 from "@/assets/img/bg9.jpg";

//sections
import Information from "@/views/user/Sections/userInformation.vue";
import AboutTeam from "@/views/user/Sections/usermission.vue";
import userDefaultInfoCard from "@/views/user/Sections/userDefaultInfoCard.vue";
import Profile from "@/views/user/Sections/userProfile.vue";
import Posts from "@/views/user/Sections/userPosts.vue";
import Login from "@/views/user/login.vue";

const body = document.getElementsByTagName("body")[0];
const router = useRouter();
const API_BASE_URL = "https://localhost:7190/api";
const goToBasic = () => {
  router.push("/login");
};
// 計算關注人數
const followingCount = computed(() => followingList.value.length);

// 關鍵：允許跨域請求自動攜帶 Cookie (Session)
const getAxiosConfig = () => {
  return {
    withCredentials: true,
    headers: {
      "Content-Type": "application/json",
    },
  };
};

// 關注清單
const fetchFollowingList = async () => {
  try {
    isLoading.value = true;
    const response = await axios.get(
      `${API_BASE_URL}/Social/following`,
      getAxiosConfig()
    );
    // 假設後端回傳的是 List<User>
    followingList.value = response.data;
    console.log("關注清單取得成功:", followingList.value);
  } catch (error) {
    console.error("取得關注清單失敗:", error);
    // 401 Unauthorized 代表 Cookie 無效或過期
    if (error.response && error.response.status === 401) {
      alert("請先登入");
      router.push("/login");
    }
  } finally {
    isLoading.value = false;
  }
};

//hooks
onMounted(() => {
  body.classList.add("about-us");
  body.classList.add("bg-gray-200");

  if (document.getElementById("typed")) {
    // eslint-disable-next-line no-unused-vars
    var typed = new Typed("#typed", {
      stringsElement: "#typed-strings",
      typeSpeed: 90,
      backSpeed: 90,
      backDelay: 200,
      startDelay: 500,
      loop: true,
    });
  }
  await fetchFollowingList(); // 呼叫後端 API
});

onUnmounted(() => {
  body.classList.remove("about-us");
  body.classList.remove("bg-gray-200");
});
</script>
<template>
  <DefaultNavbar
    :action="{
      route: 'javascript:;',
      label: 'Buy Now',
      color: 'btn-white',
    }"
    transparent
  />
  <header class="bg-gradient-dark">
    <div
      class="page-header min-vh-75"
      :style="{ backgroundImage: `url(${bg0})` }"
    >
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
            <button
              type="submit"
              class="btn bg-white text-dark"
              @click="$router.push('/login')"
            >
              <router-link
                to="/login"
                class="text-success text-gradient font-weight-bold"
              >
                登入
              </router-link>
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
  <!-- <Header>
    <div
      class="page-header min-height-400"
      :style="{ backgroundImage: `url(${image})` }"
      loading="lazy"
    >
      <span class="mask bg-gradient-dark opacity-8"></span>
    </div>
  </Header> -->

  <Profile />
  <div class="card card-body shadow-xl mx-3 mx-md-4 mt-n6">
    <Information />
  </div>

  <div class="card card-body blur shadow-blur mx-3 mx-md-4 mt-n6 mb-4">
    <Posts />
    <div class="row mt-4" v-if="followingList.length > 0">
      <div class="col-6">
        <h5>關注列表詳情：</h5>
        <ul>
          <li v-for="user in followingList" :key="user.id">
            {{ user.userName || user.name }}
            <button class="btn btn-sm btn-danger" @click="unfollow(user.id)">
              取消關注
            </button>
          </li>
        </ul>
      </div>
    </div>
    <!-- <AboutTeam /> -->
  </div>
  <DefaultFooter />
</template>
