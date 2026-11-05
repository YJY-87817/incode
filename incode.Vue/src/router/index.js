import { createRouter, createWebHistory } from "vue-router";
// tim 以下
// 首頁直接載入，其餘頁面以 () => import() 延遲載入（各自打包成獨立檔案，進入該頁才下載）
import HomeView from "@/views/HomeView.vue";
// tim 以上
import { useAuthStore } from "@/stores/users/auth";
import Swal from "sweetalert2";

const LOGIN_SCROLL_STORAGE_KEY_PREFIX = "login-redirect-scroll:";

const waitAndRestoreScroll = (targetTop) => {
  const startTime = Date.now();
  const timeoutMs = 2000;
  const checkIntervalMs = 50;

  return new Promise((resolve) => {
    const tryResolve = () => {
      const docHeight =
        document.documentElement?.scrollHeight ||
        document.body?.scrollHeight ||
        0;
      const viewportHeight = window.innerHeight || 0;
      const canReachTarget = docHeight >= targetTop + viewportHeight;
      const isTimedOut = Date.now() - startTime >= timeoutMs;

      if (canReachTarget || isTimedOut) {
        resolve({ left: 0, top: targetTop });
        return;
      }

      window.setTimeout(tryResolve, checkIntervalMs);
    };

    tryResolve();
  });
};

// const router = createRouter({
//   history: createWebHistory(import.meta.env.BASE_URL),
//   /**
//    * 控制路由切換時的滾動行為
//    * @param {Object} to - 目標路由對象
//    * @param {Object} from - 來源路由對象
//    * @param {Object|null} savedPosition - 儲存的滾動位置（僅在使用瀏覽器前進/後退按鈕時存在）
//    * @returns {Object} 滾動位置對象，包含 left 和 top 屬性
//    * @description 如果存在儲存的位置則返回該位置，否則滾動到頁面頂部
//    */
//   scrollBehavior(to, from, savedPosition) {
//     if (savedPosition) {
//       return savedPosition;
//     } else {
//       return { left: 0, top: 0 };
//     }
//   },
const routes = [
  {
    // 首頁改成這個_tim
    path: "/",
    name: "firstview",
    component: HomeView,
  },
  // 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由

  // 以下是tim的路由
  {
    path: "/forum",
    name: "forum",
    component: () => import("@/views/contents/ForumView.vue"),
  },
  // 單一content的路由
  {
    path: "/forum/:id",
    name: "forum-thread",
    component: () => import("@/views/contents/ForumView.vue"),
  },
  // 以上是tim的路由

  // owen
  {
    path: "/auth",
    name: "Auth",
    component: () => import("@/views/users/AuthView.vue"),
  },
  {
    path: "/register",
    redirect: (to) => {
      return { path: "/auth", query: { tab: "register" } };
    },
  },
  {
    path: "/login",
    name: "Login",
    redirect: (to) => {
      return { path: "/auth", query: to.query };
    },
  },
  {
    path: "/reset-password",
    name: "ResetPassword",
    component: () => import("@/views/users/ResetPasswordView.vue"),
  },
  {
    path: "/usersettings",
    name: "UserSettings",
    component: () => import("@/views/users/UserSettings.vue"),
    meta: { requiresAuth: true },
  },
  {
    path: "/profile/:username",
    name: "UserProfile",
    component: () => import("@/views/users/UserProfile.vue"),
    props: true,
  },
  {
    path: "/userindex",
    name: "UserIndex",
    component: () => import("@/views/users/UserIndex.vue"),
    meta: { requiresAuth: true },
    redirect: { name: "UserPost" },
    children: [
      {
        path: "usercollections",
        name: "UserCollections",
        component: () => import("@/views/users/UserCollections.vue"),
        redirect: { name: "UserPost" },
        children: [
          {
            path: "userpost",
            name: "UserPost",
            component: () => import("@/views/users/UserPost.vue"),
          },
          {
            path: "usersave",
            name: "UserSave",
            component: () => import("@/views/users/UserSave.vue"),
          },
          {
            path: "UserLike",
            name: "UserLike",
            component: () => import("@/views/users/UserLike.vue"),
          },
        ],
      },
      {
        path: "usermission",
        name: "UserMission",
        component: () => import("@/views/users/UserMission.vue"),
      },
    ],
  },
  // owen

  // qiu以下
  // {
  //   path: "/test-qiu",
  //   name: "test-qiu",
  //   component: TestQiu, // 我的任務
  // },
  {
    path: "/mytask",
    name: "mytask",
    component: () => import("@/views/point/MyTask.vue"), // 我的任務
  },
  {
    path: "/point-log",
    name: "point-log",
    component: () => import("@/views/point/PointLog.vue"), //views的檔案名稱
  },
  {
    path: "/task",
    name: "task",
    component: () => import("@/views/point/Task.vue"), //views的檔案名稱
  },
  {
    path: "/recharge",
    name: "recharge",
    component: () => import("@/views/point/Recharge.vue"), //views的檔案名稱
  },
  {
    path: "/recharge-result",
    name: "recharge-result",
    component: () => import("@/views/point/RechargeResult.vue"), //views的檔案名稱
  },
  // qiu以上

  // yjy 以下
  {
    path: "/PointShop/Product",
    component: () => import("@/views/pointshop/ProductView.vue"),
    children: [
      {
        path: "",
        name: "PointShop",
        component: () => import("@/components/pointshop/ProductGrid.vue"),
      },
      {
        path: ":id",
        name: "ProductDetail",
        component: () => import("@/components/pointshop/ProductDetail.vue"),
        props: true,
      },
    ],
  },
  {
    path: "/PointShop/Order",
    name: "Order",
    component: () => import("@/views/pointshop/OrderView.vue"),
  },
  {
    path: "/PointShop/Checkout",
    name: "Checkout",
    component: () => import("@/views/pointshop/CheckoutView.vue"),
  },
  // yjy 以上
  // 沒設定的路由導向NotFound頁面、放最下面
  {
    path: "/:pathMatch(.*)*",
    name: "notFound",
    component: () => import("@/views/NotFoundView.vue"),
  },
];
//owen
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
  scrollBehavior(to, from, savedPosition) {
    // A. 按上一頁/下一頁時，保持位置
    if (savedPosition) {
      return savedPosition;
    }

    const restoreKey = `${LOGIN_SCROLL_STORAGE_KEY_PREFIX}${to.fullPath}`;
    const storedScrollY = sessionStorage.getItem(restoreKey);
    if (storedScrollY !== null) {
      sessionStorage.removeItem(restoreKey);
      const top = Number(storedScrollY);
      if (Number.isFinite(top) && top >= 0) {
        return waitAndRestoreScroll(top);
      }
    }

    // B. 定義切換 Tab 時不捲動的頁面
    const noScrollRoutes = [
      "UserIndex",
      "UserMission",
      "UserCollections",
      "UserPost",
      "UserLike",
      "UserSave",
    ];

    // 如果是在 Tab 之間切換，保持原位 (return false 或不回傳 top)
    if (
      noScrollRoutes.includes(to.name) &&
      noScrollRoutes.includes(from.name)
    ) {
      return {};
    }

    // C. 預設滾動到頂部
    return { top: 0 };
  },
});

// 2. 設定全域路由守衛
router.beforeEach(async (to, from, next) => {
  // 必須在 beforeEach 內部呼叫 useStore，確保 Pinia 已經掛載
  const authStore = useAuthStore();

  // (選用) 如果是第一次進入網頁，且狀態是不確定，可以強制檢查一次
  // 這可以防止使用者按 F5 重新整理後，Pinia 狀態遺失導致被踢出
  if (!authStore.isAuthenticated) {
    await authStore.checkAuth();
  }
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth);
  // 3. 判斷邏輯
  if (requiresAuth && !authStore.isAuthenticated) {
    // 需要權限但沒登入 -> 踢去登入頁
    Swal.fire({
      icon: "warning",
      title: "請先登入會員",
      timer: 1500,
      showConfirmButton: false,
    });
    next({
      name: "Login",
      query: { redirect: to.fullPath },
    });
  } else {
    // 其他狀況 -> 放行
    next();
  }
});

export default router;
