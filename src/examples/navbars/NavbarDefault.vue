<script setup>
  import { RouterLink } from "vue-router";
  import { ref, watch } from "vue";

  import { useWindowsWidth } from "@/assets/js/useWindowsWidth";
import { useCartStore } from "@/views/PointShop/services/cartStore";
  import UserIndex from "@/views/user/UserIndex.vue";
  import UserMission from "@/views/user/UserMission.vue";

  import ArrDark from "@/assets/img/down-arrow-dark.svg";
  import downArrow from "@/assets/img/down-arrow.svg";
  import DownArrWhite from "@/assets/img/down-arrow-white.svg";

// 取得購物車狀態
const CartNow = useCartStore();

  import { useAuthStore } from "@/stores/user/auth";
  const authStore = useAuthStore();

  const menuItems = [
    {
      title: '會員中心',
      icon: 'mdi-account-circle-outline',
      to: { name: 'UserIndex' }
    },
    {
      title: '我的任務',
      icon: 'mdi-flag-variant-outline',
      to: { name: 'UserMission' }
    },
    {
      title: '我的點數',
      icon: 'mdi-star-outline',
      to: { name: 'point-log' }
    },
    {
      title: '我的購物車',
      icon: 'mdi-cart-outline',
      to: { name: 'Checkout' }
    },
  ];

  const handleLogout = async () => {
    await authStore.logout();
    router.push("/login");
  };

  const props = defineProps({
    transparent: {
      type: Boolean,
      default: false,
    },
    light: {
      type: Boolean,
      default: false,
    },
    dark: {
      type: Boolean,
      default: false,
    },
    sticky: {
      type: Boolean,
      default: false,
    },
    darkText: {
      type: Boolean,
      default: false,
    },
  });

  // set arrow  color
  function getArrowColor() {
    if (props.transparent && textDark.value) {
      return ArrDark;
    } else if (props.transparent) {
      return DownArrWhite;
    } else {
      return ArrDark;
    }
  }

  // set text color
  const getTextColor = () => {
    let color;
    if (props.transparent && textDark.value) {
      color = "text-dark";
    } else if (props.transparent) {
      color = "text-white";
    } else {
      color = "text-dark";
    }

    return color;
  };

  // set nav color on mobile && desktop

  let textDark = ref(props.darkText);
  const { type } = useWindowsWidth();

  if (type.value === "mobile") {
    textDark.value = true;
  } else if (type.value === "desktop" && textDark.value == false) {
    textDark.value = false;
  }

  watch(
    () => type.value,
    (newValue) => {
      if (newValue === "mobile") {
        textDark.value = true;
      } else {
        textDark.value = false;
      }
    }
  );
</script>

<template>
  <nav class="navbar navbar-expand-lg top-0" :class="{
      'z-index-3 w-100 shadow-none navbar-transparent position-absolute my-3':
        props.transparent,
      'my-3 blur border-radius-lg z-index-3 py-2 shadow py-2 start-0 end-0 mx-4 position-absolute mt-4':
        props.sticky,
      'navbar-light py-3': props.light,
      ' navbar-dark bg-gradient-dark z-index-3 py-3': props.dark,
    }">
    <div :class="
        props.transparent || props.light || props.dark
          ? 'container'
          : 'container-fluid px-0'
      ">
      <RouterLink class="navbar-brand d-none d-md-block" :class="[
          (props.transparent && textDark.value) || !props.transparent
            ? 'text-dark font-weight-bolder ms-sm-3'
            : 'text-white font-weight-bolder ms-sm-3',
        ]"
        :to="{ name: 'firstview' }"
        rel="tooltip"
        title="Designed and Coded by Creative Tim"
        data-placement="bottom"
      >
        <i class="fas fa-hat-cowboy-side"></i>
        incode影庫
      </RouterLink>
      <RouterLink class="navbar-brand d-block d-md-none" :class="
          props.transparent || props.dark
            ? 'text-white'
            : 'font-weight-bolder ms-sm-3'
        " to="/" rel="tooltip" title="Designed and Coded by Creative Tim" data-placement="bottom">
        Material Design
      </RouterLink>
      <RouterLink to="/login" class="btn btn-sm bg-gradient-success mb-0 ms-auto d-lg-none d-block">
        登入
      </RouterLink>
      <div class="collapse navbar-collapse w-100 pt-3 pb-2 py-lg-0" id="navigation">
        <ul class="navbar-nav navbar-nav-hover ms-auto">
          <!-- 任務區 -->
          <li class="nav-item dropdown dropdown-hover mx-2">
            <a role="button" class="nav-link ps-2 d-flex cursor-pointer align-items-center" :class="getTextColor()"
              id="dropdownMenuPages" data-bs-toggle="dropdown" aria-expanded="false">
              <i class="material-icons opacity-6 me-2 text-md" :class="getTextColor()">
                dashboard</i>
              任務大廳
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-2 d-lg-block d-none" />
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-1 d-lg-none d-block ms-auto" />
            </a>
            <div class="dropdown-menu dropdown-menu-animation ms-n3 dropdown-md p-3 border-radius-xl mt-0 mt-lg-3"
              style="overflow: hidden" aria-labelledby="dropdownMenuPages">
              <div class="row d-none d-lg-block">
                <!-- 任務 -->
                <div class="col-12 px-4 py-2">
                  <div class="row">
                    <div class="position-relative">
                       <RouterLink
                        :to="{ name: 'test-qiu' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>TestQiu</span>
                      </RouterLink>
                      <RouterLink
                        :to="{ name: 'point-log' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>我的點數明細</span>
                      </RouterLink>
                      <RouterLink :to="{ name: 'mytask' }" class="dropdown-item border-radius-md">
                        <span>我的任務進度</span>
                      </RouterLink>
                      <RouterLink :to="{ name: 'mission' }" class="dropdown-item border-radius-md">
                        <span>任務大廳</span>
                      </RouterLink>
                      <RouterLink :to="{ name: 'recharge' }" class="dropdown-item border-radius-md">
                        <span>點數儲值</span>
                      </RouterLink>
                    </div>
                  </div>
                </div>
              </div>
              <!-- 響應式任務 -->
              <div class="d-lg-none">
                <RouterLink
                        :to="{ name: 'test-qiu' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>TestQiu</span>
                      </RouterLink>
                <RouterLink
                  :to="{ name: 'point-log' }"
                  class="dropdown-item border-radius-md"
                >
                  <span>我的點數明細</span>
                </RouterLink>
                <RouterLink :to="{ name: 'mytask' }" class="dropdown-item border-radius-md">
                  <span>我的任務進度</span>
                </RouterLink>
                <RouterLink :to="{ name: 'mission' }" class="dropdown-item border-radius-md">
                  <span>任務大廳</span>
                </RouterLink>
                <RouterLink :to="{ name: 'recharge' }" class="dropdown-item border-radius-md">
                  <span>點數儲值</span>
                </RouterLink>
              </div>
            </div>
          </li>
          <!-- 點數商城區 -->
          <li class="nav-item dropdown dropdown-hover mx-2">
            <a role="button" class="nav-link ps-2 d-flex cursor-pointer align-items-center" :class="getTextColor()"
              id="dropdownMenuPages" data-bs-toggle="dropdown" aria-expanded="false">
              <i class="material-icons opacity-6 me-2 text-md" :class="getTextColor()">
                dashboard</i>
              點數商城
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-2 d-lg-block d-none" />
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-1 d-lg-none d-block ms-auto" />
            </a>
            <div class="dropdown-menu dropdown-menu-animation ms-n3 dropdown-md p-3 border-radius-xl mt-0 mt-lg-3"
              style="overflow: hidden" aria-labelledby="dropdownMenuPages">
              <div class="row d-none d-lg-block">
                <!-- 點數商城 -->
                <div class="col-12 px-4 py-2">
                  <div class="row">
                    <div class="position-relative">
                      <button
                        class="dropdown-item border-radius-md d-flex align-items-center justify-content-between"
                        data-bs-toggle="offcanvas"
                        data-bs-target="#cartOffcanvas"
                        style="background: none; border: none; cursor: pointer; width: 100%; text-align: left; padding: 0.5rem 1rem;"
                      >
                        <span>
                          購物車
                        </span>
                        <span
                          v-if="CartNow.totalItems > 0"
                          class="badge rounded-pill bg-danger"
                          style="font-size: 0.65rem; padding: 0.25em 0.5em; min-width: 1.25rem;"
                        >
                          {{ CartNow.totalItems > 99 ? '99+' : CartNow.totalItems }}
                        </span>
                      </button>
                      <RouterLink
                        :to="{ name: 'PointShop' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>點數商城</span>
                      </RouterLink>
                      <RouterLink :to="{ name: 'Order' }" class="dropdown-item border-radius-md">
                        <span>我的訂單</span>
                      </RouterLink>
                      <RouterLink
                        :to="{ name: 'Checkout' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>前往結帳</span>
                      </RouterLink>
                    </div>
                  </div>
                </div>
              </div>
              <!-- 響應式點數商城 -->
              <div class="d-lg-none">
                <button
                  class="dropdown-item border-radius-md d-flex align-items-center justify-content-between"
                  data-bs-toggle="offcanvas"
                  data-bs-target="#cartOffcanvas"
                  style="background: none; border: none; cursor: pointer; width: 100%; text-align: left; padding: 0.5rem 1rem;"
                >
                  <span>
                  購物車
                  </span>
                  <span
                    v-if="CartNow.totalItems > 0"
                    class="badge rounded-pill bg-danger"
                    style="font-size: 0.65rem; padding: 0.25em 0.5em; min-width: 1.25rem;"
                  >
                    {{ CartNow.totalItems > 99 ? '99+' : CartNow.totalItems }}
                  </span>
                </button>
                <RouterLink
                  :to="{ name: 'PointShop' }"
                  class="dropdown-item border-radius-md"
                >
                  <span>點數商城</span>
                </RouterLink>
                <RouterLink :to="{ name: 'Order' }" class="dropdown-item border-radius-md">
                  <span>我的訂單</span>
                </RouterLink>
                <RouterLink
                  :to="{ name: 'Checkout' }"
                  class="dropdown-item border-radius-md"
                >
                  <span>前往結帳</span>
                </RouterLink>

              </div>
            </div>
          </li>
          <!-- 討論區 -->
          <li class="nav-item dropdown dropdown-hover mx-2">
            <a role="button" class="nav-link ps-2 d-flex cursor-pointer align-items-center" :class="getTextColor()"
              id="dropdownMenuPages" data-bs-toggle="dropdown" aria-expanded="false">
              <i class="material-icons opacity-6 me-2 text-md" :class="getTextColor()">
                dashboard</i>
              討論區
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-2 d-lg-block d-none" />
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-1 d-lg-none d-block ms-auto" />
            </a>
            <div class="dropdown-menu dropdown-menu-animation ms-n3 dropdown-md p-3 border-radius-xl mt-0 mt-lg-3"
              style="overflow: hidden" aria-labelledby="dropdownMenuPages">
              <div class="row d-none d-lg-block">
                <div class="col-12 px-4 py-2">
                  <div class="row">
                    <div class="position-relative">
                      <RouterLink :to="{ name: 'forum' }" class="dropdown-item border-radius-md">
                        <span>討論區</span>
                      </RouterLink>
                      <RouterLink
                        :to="{ name: 'presentation' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>預設首頁</span>
                      </RouterLink>
                      <RouterLink
                        :to="{ name: 'tmdb' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>下載TMDB海報</span>
                      </RouterLink>
                      <RouterLink
                        :to="{ name: 'imagetest' }"
                        class="dropdown-item border-radius-md"
                      >
                        <span>上傳圖測試</span>
                      </RouterLink>
                    </div>
                  </div>
                </div>
              </div>
              <!-- 響應式討論區 -->
              <div class="d-lg-none">
                <RouterLink :to="{ name: 'forum' }" class="dropdown-item border-radius-md">
                  <span>討論區</span>
                </RouterLink>
                <RouterLink
                  :to="{ name: 'presentation' }"
                  class="dropdown-item border-radius-md"
                >
                  <span>預設首頁</span>
                </RouterLink>
                <RouterLink
                  :to="{ name: 'tmdb' }"
                  class="dropdown-item border-radius-md"
                >
                  <span>下載TMDB海報</span>
                </RouterLink>
                <RouterLink
                  :to="{ name: 'imagetest' }"
                  class="dropdown-item border-radius-md"
                >
                  <span>上傳圖測試</span>
                </RouterLink>
              </div>
            </div>
          </li>
          <!-- owen -->
          <li class="nav-link ps-2 d-flex cursor-pointer align-items-center">
            <RouterLink :to="{ name: 'UserIndex' }" class="dropdown-item border-radius-md">
              <i class="material-icons opacity-6 me-2 text-md" :class="getTextColor()">
                person</i>會員中心
            </RouterLink>
          </li>
          <div
            class="dropdown-menu dropdown-menu-end dropdown-menu-animation dropdown-md dropdown-md-responsive p-3 border-radius-lg mt-0 mt-lg-3"
            aria-labelledby="dropdownMenuBlocks">
            <div class="d-none d-lg-block">
              <ul class="list-group">
                <li class="nav-item dropdown dropdown-hover dropdown-subitem list-group-item border-0 p-0">
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="javascript:;">
                    <div class="d-flex">
                      <div class="w-100 d-flex align-items-center justify-content-between">
                        <div>
                          <h6
                            class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                            Page Sections
                          </h6>
                          <span class="text-sm">See all sections</span>
                        </div>
                        <img :src="downArrow" alt="down-arrow" class="arrow" />
                      </div>
                    </div>
                  </a>
                  <div class="dropdown-menu mt-0 py-3 px-2 mt-3">
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'page-headers' }">
                      Page Headers
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'page-features' }">
                      Features
                    </RouterLink>
                  </div>
                </li>
                <li class="nav-item dropdown dropdown-hover dropdown-subitem list-group-item border-0 p-0">
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="javascript:;">
                    <div class="d-flex">
                      <div class="w-100 d-flex align-items-center justify-content-between">
                        <div>
                          <h6
                            class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                            Navigation
                          </h6>
                          <span class="text-sm">See all navigations</span>
                        </div>
                        <img :src="downArrow" alt="down-arrow" class="arrow" />
                      </div>
                    </div>
                  </a>
                  <div class="dropdown-menu mt-0 py-3 px-2 mt-3">
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'navigation-navbars' }">
                      Navbars
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'navigation-navtabs' }">
                      Nav Tabs
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1"
                      :to="{ name: 'navigation-pagination' }">
                      Pagination
                    </RouterLink>
                  </div>
                </li>
                <li class="nav-item dropdown dropdown-hover dropdown-subitem list-group-item border-0 p-0">
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="javascript:;">
                    <div class="d-flex">
                      <div class="w-100 d-flex align-items-center justify-content-between">
                        <div>
                          <h6
                            class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                            Input Areas
                          </h6>
                          <span class="text-sm">See all input areas</span>
                        </div>
                        <img :src="downArrow" alt="down-arrow" class="arrow" />
                      </div>
                    </div>
                  </a>
                  <div class="dropdown-menu mt-0 py-3 px-2 mt-3">
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'inputareas-inputs' }">
                      Inputs
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'inputareas-forms' }">
                      Forms
                    </RouterLink>
                  </div>
                </li>
                <li class="nav-item dropdown dropdown-hover dropdown-subitem list-group-item border-0 p-0">
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="javascript:;">
                    <div class="d-flex">
                      <div class="w-100 d-flex align-items-center justify-content-between">
                        <div>
                          <h6
                            class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                            Attention Catchers
                          </h6>
                          <span class="text-sm">See all examples</span>
                        </div>
                        <img :src="downArrow" alt="down-arrow" class="arrow" />
                      </div>
                    </div>
                  </a>
                  <div class="dropdown-menu mt-0 py-3 px-2 mt-3">
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'ac-alerts' }">
                      Alerts
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'ac-modals' }">
                      Modals
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'ac-tooltips-popovers' }">
                      Tooltips & Popovers
                    </RouterLink>
                  </div>
                </li>
                <li class="nav-item dropdown dropdown-hover dropdown-subitem list-group-item border-0 p-0">
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="javascript:;">
                    <div class="d-flex">
                      <div class="w-100 d-flex align-items-center justify-content-between">
                        <div>
                          <h6
                            class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                            Elements
                          </h6>
                          <span class="text-sm">See all elements</span>
                        </div>
                        <img :src="downArrow" alt="down-arrow" class="arrow" />
                      </div>
                    </div>
                  </a>
                  <div class="dropdown-menu mt-0 py-3 px-2 mt-3">
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-avatars' }">
                      Avatars
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-badges' }">
                      Badges
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-breadcrumbs' }">
                      Breadcrumbs
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-buttons' }">
                      Buttons
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-button-groups' }">
                      Button Groups
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-dropdowns' }">
                      Dropdowns
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-progress-bars' }">
                      Progress Bars
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-toggles' }">
                      Toggles
                    </RouterLink>
                    <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-typography' }">
                      Typography
                    </RouterLink>
                  </div>
                </li>
              </ul>
            </div>
            <div class="row d-lg-none">
              <div class="col-md-12">
                <div class="d-flex mb-2">
                  <div class="w-100 d-flex align-items-center justify-content-between">
                    <div>
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Page Sections
                      </h6>
                    </div>
                  </div>
                </div>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'page-headers' }">
                  Page Headers
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'page-features' }">
                  Features
                </RouterLink>
                <div class="d-flex mb-2 mt-3">
                  <div class="w-100 d-flex align-items-center justify-content-between">
                    <div>
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Navigation
                      </h6>
                    </div>
                  </div>
                </div>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'navigation-navbars' }">
                  Navbars
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'navigation-navtabs' }">
                  Nav Tabs
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'navigation-pagination' }">
                  Pagination
                </RouterLink>
                <div class="d-flex mb-2 mt-3">
                  <div class="w-100 d-flex align-items-center justify-content-between">
                    <div>
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Input Areas
                      </h6>
                    </div>
                  </div>
                </div>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'inputareas-inputs' }">
                  Inputs
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'inputareas-forms' }">
                  Forms
                </RouterLink>
                <div class="d-flex mb-2 mt-3">
                  <div class="w-100 d-flex align-items-center justify-content-between">
                    <div>
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Attention Catchers
                      </h6>
                    </div>
                  </div>
                </div>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'ac-alerts' }">
                  Alerts
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'ac-modals' }">
                  Modals
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'ac-tooltips-popovers' }">
                  Tooltips & Popovers
                </RouterLink>
                <div class="d-flex mb-2 mt-3">
                  <div class="w-100 d-flex align-items-center justify-content-between">
                    <div>
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Elements
                      </h6>
                    </div>
                  </div>
                </div>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-avatars' }">
                  Avatars
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-badges' }">
                  Badges
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-breadcrumbs' }">
                  Breadcrumbs
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-buttons' }">
                  Buttons
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-button-groups' }">
                  Button Groups
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-dropdowns' }">
                  Dropdowns
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-progress-bars' }">
                  Progress Bars
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-toggles' }">
                  Toggles
                </RouterLink>
                <RouterLink class="dropdown-item ps-3 border-radius-md mb-1" :to="{ name: 'el-typography' }">
                  Typography
                </RouterLink>
              </div>
            </div>
          </div>
          <li class="nav-item dropdown dropdown-hover mx-2">
            <a role="button" class="nav-link ps-2 d-flex cursor-pointer align-items-center" :class="getTextColor()"
              id="dropdownMenuDocs" data-bs-toggle="dropdown" aria-expanded="false">
              <i class="material-icons opacity-6 me-2 text-md" :class="getTextColor()">article</i>
              Docs
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-2 d-lg-block d-none" />
              <img :src="getArrowColor()" alt="down-arrow" class="arrow ms-1 d-lg-none d-block ms-auto" />
            </a>
            <div
              class="dropdown-menu dropdown-menu-end dropdown-menu-animation dropdown-md mt-0 mt-lg-3 p-3 border-radius-lg"
              aria-labelledby="dropdownMenuDocs">
              <div class="d-none d-lg-block">
                <ul class="list-group">
                  <li class="nav-item list-group-item border-0 p-0">
                    <a class="dropdown-item py-2 ps-3 border-radius-md"
                      href=" https://www.creative-tim.com/learning-lab/vue/overview/material-kit/">
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Getting Started
                      </h6>
                      <span class="text-sm">All about overview, quick start, license and
                        contents</span>
                    </a>
                  </li>
                  <li class="nav-item list-group-item border-0 p-0">
                    <a class="dropdown-item py-2 ps-3 border-radius-md"
                      href=" https://www.creative-tim.com/learning-lab/vue/colors/material-kit/">
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Foundation
                      </h6>
                      <span class="text-sm">See our colors, icons and typography</span>
                    </a>
                  </li>
                  <li class="nav-item list-group-item border-0 p-0">
                    <a class="dropdown-item py-2 ps-3 border-radius-md"
                      href=" https://www.creative-tim.com/learning-lab/vue/alerts/material-kit/">
                      <h6
                        class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                        Components
                      </h6>
                      <span class="text-sm">Explore our collection of fully designed
                        components</span>
                    </a>
                  </li>
                </ul>
              </div>
              <div class="row d-lg-none">
                <div class="col-md-12 g-0">
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="./pages/about-us.html">
                    <h6
                      class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                      Getting Started
                    </h6>
                    <span class="text-sm">All about overview, quick start, license and
                      contents</span>
                  </a>
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="./pages/about-us.html">
                    <h6
                      class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                      Foundation
                    </h6>
                    <span class="text-sm">See our colors, icons and typography</span>
                  </a>
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="./pages/about-us.html">
                    <h6
                      class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                      Components
                    </h6>
                    <span class="text-sm">Explore our collection of fully designed components</span>
                  </a>
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="./pages/about-us.html">
                    <h6
                      class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                      Plugins
                    </h6>
                    <span class="text-sm">Check how you can integrate our plugins</span>
                  </a>
                  <a class="dropdown-item py-2 ps-3 border-radius-md" href="./pages/about-us.html">
                    <h6
                      class="dropdown-header text-dark font-weight-bolder d-flex justify-content-cente align-items-center p-0">
                      Utility Classes
                    </h6>
                    <span class="text-sm">For those who want flexibility, use our utility
                      classes</span>
                  </a>
                </div>
              </div>
            </div>
          </li>
        </ul>
        <ul class="navbar-nav d-lg-block d-none">
          <!--手機版登入 -->
          <!-- 下拉選單 <v-menu v-if="authStore.isLoggedIn" min-width="200px" rounded="xl" offset="10"
            transition="slide-y-transition">
            <template v-slot:activator="{ props }">
              <v-btn icon v-bind="props" class="ml-2" size="large">
                <v-avatar size="45" class="border border-opacity-25">
                  <v-img :src="authStore.user?.avatar || 'https://cdn.vuetifyjs.com/images/john.jpg'" alt="User"
                    cover></v-img>
                </v-avatar>
              </v-btn>
            </template>

            <v-card border elevation="4" class="rounded-xl mt-2">
              <v-list density="compact" nav>

                <v-list-item class="mb-2" prepend-avatar="https://cdn.vuetifyjs.com/images/john.jpg">
                  <v-list-item-title class="font-weight-bold">Brooke</v-list-item-title>
                  <v-list-item-subtitle class="text-caption">user@example.com</v-list-item-subtitle>
                </v-list-item>

                <v-divider class="mb-2"></v-divider>

                <v-list-item v-for="(item, index) in menuItems" :key="index" :to="item.to" :prepend-icon="item.icon"
                  :title="item.title" active-class="text-primary bg-primary-lighten-5" color="primary" rounded="lg"
                  class="mb-1"></v-list-item>

                <v-divider class="my-2"></v-divider>

                <v-list-item prepend-icon="mdi-logout" title="登出" rounded="lg" class="text-error"
                  @click="handleLogout"></v-list-item>
              </v-list>
            </v-card>
          </v-menu> -->
          <li class="nav-item">
            <RouterLink v-if="!authStore.isLoggedIn" to="/login" class="btn btn-sm bg-gradient-success mb-0">
              登入會員
            </RouterLink>

            <button v-else class="btn btn-sm bg-gradient-danger mb-0" @click="authStore.logout()">
              登出
            </button>
          </li>
        </ul>
      </div>
    </div>
  </nav>
  <!-- End Navbar -->

  
</template>

<style scoped>
/* 自定義導覽列背景色 */
.navbar-light {
  background-color: #FFF0DD !important;
}
</style>