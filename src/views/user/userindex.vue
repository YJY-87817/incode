<script setup>
  import { onMounted, onUnmounted, ref, computed } from "vue";
  import { useRouter } from "vue-router";
  import axios from "axios";
  import Typed from "typed.js";
  // 引入 Store 以便取得 Token
  import { useAuthStore } from "@/stores/user/auth";
  const authStore = useAuthStore();

  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterDefault.vue";
  import bg0 from "@/assets/img/bg9.jpg";

  const router = useRouter();
  const API_BASE_URL = "https://localhost:7190/api";

  // const goToBasic = () => {
  //   router.push("/login");
  // };
  // const goToindex = () => {
  //   router.push("/");
  // };

  const isLoading = ref(false);

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
  <header class="bg-gradient-dark"></header>

  <div class="container user-index-page mt-5 pt-5" style="min-height: 80vh">
    <div class="row">
      <div class="col-12">
        <h1 class="mb-4 display-4 font-weight-bold">會員中心</h1>

        <nav>
          <router-link :to="{ name: 'UserProfile' }">個人資料</router-link> |
          <router-link :to="{ name: 'UserSettings' }">設定</router-link> |
          <router-link :to="{ name: 'UserCollections' }">收藏</router-link> |
          <router-link :to="{ name: 'usertest' }">測試</router-link>
        </nav>

        <hr class="horizontal dark my-4" />

        <div class="nested-content">
          <router-view></router-view>
        </div>
      </div>
    </div>
  </div>
  <DefaultFooter />
</template>
<style scoped>
  /* 讓被選中的路由連結變色 (Bootstrap 預設 router-link-active 可能需要微調) */
  .router-link-active {
    background-color: #fff;
    /* 或您想要的主題色 */
    color: #000;
    font-weight: bold;
    box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
    border-radius: 5px;
  }

  .nav-link {
    color: #344767;
    font-weight: 600;
    margin-right: 1rem;
    /* 按鈕之間的間距 */
  }
</style>