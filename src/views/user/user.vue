<script setup>
  import { onMounted, onUnmounted } from "vue";

  //example components
  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterDefault.vue";

  //image
  import bg0 from "@/assets/img/bg9.jpg";

  //dep
  import Typed from "typed.js";

  //sections
  import Information from "@/views/user/Sections/userInformation.vue";
  import AboutTeam from "@/views/user/Sections/usermission.vue";

  import Profile from "@/views/user/Sections/userProfile.vue";
  import Posts from "@/views/user/Sections/userPosts.vue";

  const body = document.getElementsByTagName("body")[0];
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
  });
  import Login from "@/views/user/login.vue";
  import { useRouter } from "vue-router";
  const router = useRouter();
  const goToBasic = () => {
    router.push("/login");
  };

  onUnmounted(() => {
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
            <button type="submit" class="btn bg-white text-dark" @click="$router.push('/login')">
              <router-link to="/login" class="text-success text-gradient font-weight-bold">
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
  <div class="card card-body blur shadow-blur mx-3 mx-md-4 mt-n6 mb-4">
    <Profile />
    <Posts />
  </div>
  <div class="card card-body shadow-xl mx-3 mx-md-4 mt-n6">
    <Information />
    <AboutTeam />
  </div>
  <DefaultFooter />
</template>