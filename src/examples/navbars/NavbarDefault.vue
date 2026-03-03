<script setup>
import { RouterLink, useRoute, useRouter } from "vue-router";
import { ref, computed, watch } from "vue";
import { useWindowsWidth } from "@/assets/js/useWindowsWidth";
import { useCartStore } from "@/views/PointShop/services/cartStore";
import { useAuthStore } from "@/stores/user/auth";
import ArrDark from "@/assets/img/down-arrow-dark.svg";
import DownArrWhite from "@/assets/img/down-arrow-white.svg";
import axio from "axios";
import mysweet from "sweetalert2";

// 取得購物車狀態
const CartNow = useCartStore();
const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();
//導覽列會員
const BACKEND_BASE_URL = import.meta.env.VITE_BASE_URL;
const userAvatarUrl = computed(() => {
  const path = authStore.user?.avatar;
  if (!path) return null; // 如果沒圖，回傳 null
  if (path.startsWith("http")) return path;
  const cleanPath = path.startsWith("/") ? path.substring(1) : path;
  return `${BACKEND_BASE_URL}/${cleanPath}`;
});
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
    title: "點數儲值",
    icon: "mdi-cash-plus",
    to: { name: "recharge" },
  },
  {
    title: "我的訂單",
    icon: "mdi-cart-outline",
    to: { name: "Order" },
  },
]);

const handleLogout = async () => {
  await authStore.logout();

  const isProtectedPage = route.matched.some(
    (record) => record.meta?.requiresAuth,
  );

  if (isProtectedPage) {
    await router.push({
      name: "Login",
      query: { redirect: route.fullPath },
    });
  }
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
  return "text-white";
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
  <nav
    class="navbar navbar-expand-lg top-0"
    :style="props.light ? 'background-color: #5e8a96 !important' : ''"
    :class="{
      'z-index-3 w-100 shadow-none navbar-transparent position-absolute my-3':
        props.transparent,
      'my-3 blur border-radius-lg z-index-3 py-2 shadow py-2 start-0 end-0 mx-4 position-absolute mt-4':
        props.sticky,
      'navbar-light py-3': props.light,
      ' navbar-dark bg-gradient-dark z-index-3 py-3': props.dark,
    }"
  >
    <div
      :class="
        props.transparent || props.light || props.dark
          ? 'container'
          : 'container-fluid px-0'
      "
    >
      <router-link
        class="navbar-brand d-none d-md-block"
        :class="['text-white font-weight-bolder ms-sm-3']"
        :to="{ name: 'firstview' }"
        rel="tooltip"
        title="Designed and Coded by Creative Tim"
        data-placement="bottom"
      >
        <img
          src="@/assets/img/okapi.svg"
          alt="logo"
          style="
            height: 40px;
            width: auto;
            margin-right: 8px;
            vertical-align: middle;
          "
        />
        incode影庫
      </router-link>
      <button
        class="navbar-toggler shadow-none"
        type="button"
        data-bs-toggle="collapse"
        data-bs-target="#navigation"
        aria-controls="navigation"
        aria-expanded="false"
        aria-label="Toggle navigation"
      >
        <i class="material-icons text-white">menu</i>
      </button>
      <div
        class="collapse navbar-collapse w-100 pt-3 pb-2 py-lg-0"
        id="navigation"
      >
        <ul class="navbar-nav navbar-nav-hover">
          <!-- 討論區 -->
          <li class="nav-item mx-2">
            <router-link
              :to="{ name: 'forum' }"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center"
              :class="getTextColor()"
            >
              <i
                class="material-icons opacity-6 me-2 text-md"
                :class="getTextColor()"
              >
                comment</i
              >
              討論區
            </router-link>
          </li>
          <!-- 任務區 -->
          <li class="nav-item mx-2">
            <router-link
              :to="{ name: 'task' }"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center"
              :class="getTextColor()"
            >
              <i
                class="material-icons opacity-6 me-2 text-md"
                :class="getTextColor()"
              >
                assignment</i
              >
              任務大廳
            </router-link>
          </li>

          <!-- 點數商城區 -->
          <li class="nav-item mx-2">
            <router-link
              :to="{ name: 'PointShop' }"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center"
              :class="getTextColor()"
            >
              <i
                class="material-icons opacity-6 me-2 text-md"
                :class="getTextColor()"
              >
                shopping_bag</i
              >
              點數商城
            </router-link>
          </li>
          <!-- owen -->
          <li class="nav-item mx-2">
            <RouterLink
              :to="{ name: 'UserIndex' }"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center"
              :class="getTextColor()"
            >
              <i
                class="material-icons opacity-6 me-2 text-md"
                :class="getTextColor()"
              >
                person</i
              >會員中心
            </RouterLink>
          </li>
          <div
            class="dropdown-menu dropdown-menu-end dropdown-menu-animation dropdown-md dropdown-md-responsive p-3 border-radius-lg mt-0 mt-lg-3"
            aria-labelledby="dropdownMenuBlocks"
          >
            <div class="d-none d-lg-block">
              <ul class="navbar-nav d-lg-block d-none">
                <li class="nav-item dropdown dropdown-hover">
                  <a
                    role="button"
                    class="nav-link ps-2 d-flex cursor-pointer align-items-center"
                    :class="getTextColor()"
                    id="dropdownMenuUser"
                    data-bs-toggle="dropdown"
                    aria-expanded="false"
                  >
                    <img
                      v-if="userAvatarUrl"
                      :src="userAvatarUrl"
                      class="avatar-img rounded-circle me-2"
                      alt="user"
                    />

                    <i
                      v-else
                      class="material-icons opacity-6 me-2 text-md"
                      :class="getTextColor()"
                    >
                      account_circle
                    </i>
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
        <div
          class="mx-auto d-none d-lg-flex align-items-center"
          style="max-width: 350px; flex: 1"
        >
          <div
            class="input-group d-flex align-items-center"
            style="
              background: #f8f9fa;
              border-radius: 50px;
              padding: 0.25rem 0.5rem;
            "
          >
            <input
              id="searchInput"
              type="text"
              class="form-control border-0 bg-transparent shadow-none"
              placeholder="搜尋"
              style="
                padding: 0.5rem 1rem;
                color: #344767;
                outline: none;
                box-shadow: none !important;
                background-image: none !important;
              "
            />
            <button
              type="button"
              class="btn border-0 bg-transparent d-flex align-items-center justify-content-center"
              style="
                outline: none;
                padding: 0.5rem 0.75rem;
                margin: 0;
                line-height: 1;
              "
            >
              <i
                class="material-icons text-muted"
                style="font-size: 1.75rem; display: block; line-height: 1"
                >search</i
              >
            </button>
          </div>
        </div>
        <ul class="navbar-nav ms-0">
          <!-- 購物車 -->
          <li class="nav-item d-none d-lg-block">
            <a
              role="button"
              class="nav-link ps-2 d-flex cursor-pointer align-items-center position-relative"
              :class="getTextColor()"
              data-bs-toggle="offcanvas"
              data-bs-target="#cartOffcanvas"
              aria-controls="cartOffcanvas"
            >
              <div class="position-relative d-inline-block">
                <i
                  class="bi bi-cart-dash"
                  style="font-size: 1.2rem; line-height: 1"
                  :class="getTextColor()"
                ></i>
                <span
                  v-if="CartNow.totalItems > 0"
                  class="badge rounded-pill bg-danger position-absolute top-0 start-100 translate-middle"
                  style="font-size: 0.6rem; padding: 0.2rem 0.4rem"
                >
                  {{ CartNow.totalItems > 99 ? "99+" : CartNow.totalItems }}
                </span>
              </div>
            </a>
          </li>
        </ul>

        <ul class="navbar-nav ms-0">
          <!--手機版登入 -->
          <!-- owen -->
          <li class="nav-item" v-if="!authStore.isAuthenticated">
            <RouterLink
              :to="{ name: 'Auth' }"
              class="btn btn-sm mb-0 nav-login-btn"
            >
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
                v-if="userAvatarUrl"
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
                  :src="userAvatarUrl"
                  alt="Avatar"
                  style="
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                    display: block;
                  "
                />
              </div>
              <i
                v-else
                class="material-icons opacity-6 text-md"
                :class="getTextColor()"
                style="font-size: 24px"
              >
                account_circle
              </i>
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
/* 導航連結 hover 效果 */
.nav-link {
  transition: color 0.3s ease;
}

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

.navbar-avatar {
  width: 24px;
  /* 寬度設定與文字 icon 差不多大 */
  height: 24px;
  /* 高度固定 */
  object-fit: cover;
  /* 確保圖片縮放後不變形 */
  border: 1px solid #e0e0e0;
  /* 選用：加個細邊框增加辨識度 */
}
</style>
