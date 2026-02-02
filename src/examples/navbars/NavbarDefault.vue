<script setup>
import { RouterLink } from "vue-router";
import { ref, computed, watch } from "vue";
import { useWindowsWidth } from "@/assets/js/useWindowsWidth";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import { useAuthStore } from "@/stores/user/auth";
import ArrDark from "@/assets/img/down-arrow-dark.svg";
import DownArrWhite from "@/assets/img/down-arrow-white.svg";

// 取得購物車狀態
const CartNow = useCartStore();
const authStore = useAuthStore();
//導覽列會員
const menuItems = computed(() => [
  {
    title: "會員中心",
    icon: "mdi-account-circle-outline",
    to: { name: "UserIndex" },
  },
  {
    title: "我的首頁",
    icon: "mdi-face-man-profile",
    to: authStore.user?.username
      ? {
          name: "UserProfile",
          params: { username: authStore.user.username },
        }
      : { name: "UserIndex" }, // 安全的替代路徑
  },
  {
    title: "我的任務",
    icon: "mdi-flag-variant-outline",
    to: { name: "mytask" },
  },
  {
    title: "我的點數",
    icon: "mdi-star-outline",
    to: { name: "point-log" },
  },
  {
    title: "我的訂單",
    icon: "mdi-cart-outline",
    to: { name: "Order" },
  },
]);

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
  },
);
</script>

<template>
  <nav class="navbar navbar-expand-lg top-0" :style="props.light ? 'background-color: #FFF0DD !important' : ''" :class="{
    'z-index-3 w-100 shadow-none navbar-transparent position-absolute my-3':
      props.transparent,
    'my-3 blur border-radius-lg z-index-3 py-2 shadow py-2 start-0 end-0 mx-4 position-absolute mt-4':
      props.sticky,
    'navbar-light py-3': props.light,
    ' navbar-dark bg-gradient-dark z-index-3 py-3': props.dark,
  }">
    <div :class="props.transparent || props.light || props.dark
      ? 'container'
      : 'container-fluid px-0'
      ">
      <router-link class="navbar-brand d-none d-md-block" :class="[
        (props.transparent && textDark.value) || !props.transparent
          ? 'text-dark font-weight-bolder ms-sm-3'
          : 'text-white font-weight-bolder ms-sm-3',
      ]" :to="{ name: 'firstview' }" rel="tooltip" title="Designed and Coded by Creative Tim" data-placement="bottom">
        <i class="fas fa-hat-cowboy-side"></i>
        incode影庫
      </router-link>
      <router-link class="navbar-brand d-block d-md-none" :class="props.transparent || props.dark
        ? 'text-white'
        : 'font-weight-bolder ms-sm-3'
        " to="/" rel="tooltip" title="Designed and Coded by Creative Tim" data-placement="bottom">
        Material Design
      </router-link>
      <button class="navbar-toggler shadow-none ms-2" type="button" data-bs-toggle="collapse"
        data-bs-target="#navigation" aria-controls="navigation" aria-expanded="false" aria-label="Toggle navigation">
        <span class="navbar-toggler-icon mt-2">
          <span class="navbar-toggler-bar bar1"></span>
          <span class="navbar-toggler-bar bar2"></span>
          <span class="navbar-toggler-bar bar3"></span>
        </span>
      </button>
      <div class="collapse navbar-collapse w-100 pt-3 pb-2 py-lg-0" id="navigation">
        <ul class="navbar-nav navbar-nav-hover">
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
                      <router-link :to="{ name: 'forum' }" class="dropdown-item border-radius-md">
                        <span>討論區</span>
                      </router-link>
                    </div>
                  </div>
                </div>
              </div>
              <!-- 響應式討論區 -->
              <div class="d-lg-none">
                <router-link :to="{ name: 'forum' }" class="dropdown-item border-radius-md">
                  <span>討論區</span>
                </router-link>
              </div>
            </div>
          </li>
          <!-- 任務區 -->
          <li class="nav-item dropdown dropdown-hover mx-2">
            <a
              role="button"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center"
              :class="getTextColor()"
              id="dropdownMenuPages"
              data-bs-toggle="dropdown"
              aria-expanded="false"
            >
              <i
                class="material-icons opacity-6 me-2 text-md"
                :class="getTextColor()"
              >
                dashboard</i
              >
              任務大廳
              <img
                :src="getArrowColor()"
                alt="down-arrow"
                class="arrow ms-2 d-lg-block d-none"
              />
              <img
                :src="getArrowColor()"
                alt="down-arrow"
                class="arrow ms-1 d-lg-none d-block ms-auto"
              />
            </a>
            <div
              class="dropdown-menu dropdown-menu-animation ms-n3 dropdown-md p-3 border-radius-xl mt-0 mt-lg-3"
              style="overflow: hidden"
              aria-labelledby="dropdownMenuPages"
            >
              <div class="row d-none d-lg-block">
                <!-- 任務 -->
                <div class="col-12 px-4 py-2">
                  <div class="row">
                    <div class="position-relative">
                      <!-- <router-link :to="{ name: 'test-qiu' }" class="dropdown-item border-radius-md">
                        <span>TestQiu</span>
                      </router-link> -->
                      <router-link :to="{ name: 'point-log' }" class="dropdown-item border-radius-md">
                        <span>我的點數明細</span>
                      </router-link>
                      <router-link :to="{ name: 'mytask' }" class="dropdown-item border-radius-md">
                        <span>我的任務進度</span>
                      </router-link>
                      <router-link :to="{ name: 'task' }" class="dropdown-item border-radius-md">
                        <span>任務大廳</span>
                      </router-link>
                      <router-link :to="{ name: 'recharge' }" class="dropdown-item border-radius-md">
                        <span>點數儲值</span>
                      </router-link>
                    </div>
                  </div>
                </div>
              </div>
              <!-- 響應式任務 -->
              <div class="d-lg-none">
                <!-- <router-link :to="{ name: 'test-qiu' }" class="dropdown-item border-radius-md">
                  <span>TestQiu</span>
                </router-link> -->
                <router-link :to="{ name: 'point-log' }" class="dropdown-item border-radius-md">
                  <span>我的點數明細</span>
                </router-link>
                <router-link :to="{ name: 'mytask' }" class="dropdown-item border-radius-md">
                  <span>我的任務進度</span>
                </router-link>
                <router-link :to="{ name: 'task' }" class="dropdown-item border-radius-md">
                  <span>任務大廳</span>
                </router-link>
                <router-link :to="{ name: 'recharge' }" class="dropdown-item border-radius-md">
                  <span>點數儲值</span>
                </router-link>
              </div>
            </div>
          </li>

          <!-- 點數商城區 -->
          <li class="nav-item dropdown dropdown-hover mx-2">
            <a
              role="button"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center"
              :class="getTextColor()"
              id="dropdownMenuPages"
              data-bs-toggle="dropdown"
              aria-expanded="false"
            >
              <i
                class="material-icons opacity-6 me-2 text-md"
                :class="getTextColor()"
              >
                dashboard</i
              >
              點數商城
              <img
                :src="getArrowColor()"
                alt="down-arrow"
                class="arrow ms-2 d-lg-block d-none"
              />
              <img
                :src="getArrowColor()"
                alt="down-arrow"
                class="arrow ms-1 d-lg-none d-block ms-auto"
              />
            </a>
            <div
              class="dropdown-menu dropdown-menu-animation ms-n3 dropdown-md p-3 border-radius-xl mt-0 mt-lg-3"
              style="overflow: hidden"
              aria-labelledby="dropdownMenuPages"
            >
              <div class="row d-none d-lg-block">
                <!-- 點數商城 -->
                <div class="col-12 px-4 py-2">
                  <div class="row">
                    <div class="position-relative">
                      <router-link :to="{ name: 'PointShop' }" class="dropdown-item border-radius-md">
                        <span>點數商城</span>
                      </router-link>
                      <router-link :to="{ name: 'Order' }" class="dropdown-item border-radius-md">
                        <span>我的訂單</span>
                      </router-link>
                      <router-link :to="{ name: 'Checkout' }" class="dropdown-item border-radius-md">
                        <span>前往結帳</span>
                      </router-link>

                    </div>
                  </div>
                </div>
              </div>
              <!-- 響應式點數商城 -->
              <div class="d-lg-none">
                <button class="dropdown-item border-radius-md d-flex align-items-center justify-content-between"
                  data-bs-toggle="offcanvas" data-bs-target="#cartOffcanvas"
                  style="background: none; border: none; cursor: pointer; width: 100%; text-align: left; padding: 0.5rem 1rem;">
                  <span>
                    購物車
                  </span>
                  <span v-if="CartNow.totalItems > 0" class="badge rounded-pill bg-danger"
                    style="font-size: 0.65rem; padding: 0.25em 0.5em; min-width: 1.25rem;">
                    {{ CartNow.totalItems > 99 ? '99+' : CartNow.totalItems }}
                  </span>
                </button>
                <router-link :to="{ name: 'PointShop' }" class="dropdown-item border-radius-md">
                  <span>點數商城</span>
                </router-link>
                <router-link :to="{ name: 'Order' }" class="dropdown-item border-radius-md">
                  <span>我的訂單</span>
                </router-link>
                <router-link :to="{ name: 'Checkout' }" class="dropdown-item border-radius-md">
                  <span>前往結帳</span>
                </router-link>
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
              <ul class="navbar-nav d-lg-block d-none">

            <li class="nav-item dropdown dropdown-hover">
              <a role="button" class="nav-link ps-2 d-flex cursor-pointer align-items-center" :class="getTextColor()"
                id="dropdownMenuUser" data-bs-toggle="dropdown" aria-expanded="false">
                <i class="material-icons opacity-6 me-2 text-md" :class="getTextColor()">account_circle</i>
                {{ authStore.user?.username || "會員" }}
              </a>

            <div
              class="dropdown-menu dropdown-menu-end dropdown-menu-animation dropdown-md p-3 border-radius-lg mt-0 mt-lg-3"
              aria-labelledby="dropdownMenuUser"
            >
              <div class="d-flex align-items-center px-2 py-2 mb-2">
                <div class="d-flex flex-column">
                  <h6 class="text-dark font-weight-bolder mb-0">
                    {{ authStore.user?.username || "親愛的會員" }}
                  </h6>
                  <span class="text-xs text-muted">歡迎回來</span>
                </div>
              </div>

              <hr class="horizontal dark my-1" />

              <ul class="list-group">
                <li
                  v-for="(item, index) in menuItems"
                  :key="index"
                  class="nav-item list-group-item border-0 p-0"
                >
                  <RouterLink
                    :to="item.to"
                    class="dropdown-item py-2 ps-3 border-radius-md d-flex align-items-center"
                  >
                    <i
                      v-if="item.icon"
                      :class="`mdi ${item.icon} me-2 text-primary`"
                    ></i>
                    <span>{{ item.title }}</span>
                  </RouterLink>
                </li>

                <hr class="horizontal dark my-2" />

                <li class="nav-item list-group-item border-0 p-0">
                  <a
                    href="javascript:;"
                    class="dropdown-item py-2 ps-3 border-radius-md text-danger d-flex align-items-center"
                    @click="handleLogout"
                  >
                    <i class="mdi mdi-logout me-2"></i>
                    <span>登出</span>
                  </a>
                </li>
                </ul>
              </div>
              </li>
            </ul>
              
            </div>
          </div>
        </ul>
        <!-- 搜尋框 -->
        <div class="mx-auto d-none d-lg-flex align-items-center" style="max-width: 350px; flex: 1;">
          <div class="input-group d-flex align-items-center"
            style="background: #f8f9fa; border-radius: 50px; padding: 0.25rem 0.5rem;">
            <input type="text" class="form-control border-0 bg-transparent shadow-none" placeholder="搜尋"
              style="padding: 0.5rem 1rem; color: #344767; outline: none; box-shadow: none !important; background-image: none !important;" />
            <button type="button" class="btn border-0 bg-transparent d-flex align-items-center justify-content-center"
              style="outline: none; padding: 0.5rem 0.75rem; margin: 0; line-height: 1;">
              <i class="material-icons text-muted"
                style="font-size: 1.75rem; display: block; line-height: 1;">search</i>
            </button>
          </div>
        </div>
        <ul class="navbar-nav ms-auto ms-2">
          <!--手機版登入 -->
          <!-- 購物車 -->
          <li class="nav-item dropdown dropdown-hover d-none d-lg-block">
            <button class="d-flex align-items-center position-relative" data-bs-toggle="offcanvas"
              data-bs-target="#cartOffcanvas" style="background: none; border: none; cursor: pointer; padding: 0.5rem;">
              <div class="position-relative d-inline-block">
                <i class="bi bi-cart-dash" style="font-size: 1.2rem; line-height: 1;"></i>
                <span v-if="CartNow.totalItems > 0"
                  class="badge rounded-pill bg-danger position-absolute top-0 start-100 translate-middle"
                  style="font-size: 0.6rem; padding: 0.2rem 0.4rem;">
                  {{ CartNow.totalItems > 99 ? '99+' : CartNow.totalItems }}
                </span>
              </div>
            </button>
          </li>

          <!-- owen -->
          <li class="nav-item" v-if="!authStore.isAuthenticated">
            <RouterLink to="/login" class="btn btn-sm mb-0 nav-login-btn">
              登入
            </RouterLink>
          </li>

          <li class="nav-item dropdown dropdown-hover mx-2" v-else>
            <a
              role="button"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center justify-content-center"
              id="dropdownMenuUser"
              data-bs-toggle="dropdown"
              aria-expanded="false"
              style="height: 100%; min-height: 40px"
            >
              <div
                class="d-flex align-items-center justify-content-center bg-white shadow-sm"
                style="
                  width: 40px;
                  height: 40px;
                  border-radius: 50%;
                  border: 2px solid #fff;
                  overflow: hidden;
                "
              >
                <img
                  :src="authStore.userAvatar"
                  alt="Avatar"
                  style="
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                    display: block;
                  "
                />
              </div>
            </a>

            <div
              class="dropdown-menu dropdown-menu-end dropdown-menu-animation dropdown-md p-3 border-radius-lg mt-0 mt-lg-3"
              aria-labelledby="dropdownMenuUser"
            >
              <div class="d-flex align-items-center px-2 py-2 mb-2">
                <div class="d-flex flex-column">
                  <h6 class="text-dark font-weight-bolder mb-0">
                    {{ authStore.user?.username || "親愛的會員" }}
                  </h6>
                  <span class="text-xs text-muted">歡迎回來</span>
                </div>
              </div>

              <hr class="horizontal dark my-1" />

              <ul class="list-group">
                <li
                  v-for="(item, index) in menuItems"
                  :key="index"
                  class="nav-item list-group-item border-0 p-0"
                >
                  <RouterLink
                    :to="item.to"
                    class="dropdown-item py-2 ps-3 border-radius-md d-flex align-items-center"
                  >
                    <i
                      v-if="item.icon"
                      :class="`mdi ${item.icon} me-2 text-primary`"
                    ></i>
                    <span>{{ item.title }}</span>
                  </RouterLink>
                </li>

                <hr class="horizontal dark my-2" />

                <li class="nav-item list-group-item border-0 p-0">
                  <a
                    href="javascript:;"
                    class="dropdown-item py-2 ps-3 border-radius-md text-danger d-flex align-items-center"
                    @click="handleLogout"
                  >
                    <i class="mdi mdi-logout me-2"></i>
                    <span>登出</span>
                  </a>
                </li>
              </ul>
            </div>
          </li>
        </ul>
      </div>
    </div>
  </nav>
  <!-- End Navbar -->
</template>

<style scoped>
/* 登入按鈕 */
.nav-login-btn {
  /* 預設狀態 */
  background-color: #145369 !important;
  /* 指定顏色 */
  color: #ffffff !important;
  /* 文字白色 */
  border: 1px solid #145369 !important;
  /* 邊框同色 */
  margin-left: 10px;
  /* 左側間距 */
  transition: all 0.3s ease;
  /* 平滑過渡效果 */
}

/* 滑鼠懸停 (Hover) 效果 */
.nav-login-btn:hover {
  background-color: #ffffff !important;
  /* 背景變白 */
  color: #145369 !important;
  /* 文字變回深色 */
  border: 1px solid #145369 !important;
  /* 保留邊框 */
}
</style>
