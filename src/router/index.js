import { createRouter, createWebHistory } from "vue-router";
// tim 以下
import FirstView from "@/views/FirstView.vue";
// tim 以上
// owen
import { useAuthStore } from '@/stores/user/auth';
import Swal from 'sweetalert2';
import AuthView from '@/views/user/AuthView.vue';
import ResetPasswordView from '@/views/user/ResetPasswordView.vue'
import UserIndex from "@/views/user/userindex.vue";
import UserSettings from "@/views/user/UserSettings.vue";
import UserProfile from "@/views/user/UserProfile.vue";
import UserCollections from "@/views/user/UserCollections.vue";
import UserPost from "@/views/user/UserPost.vue";
import UserLike from "@/views/user/UserLike.vue";
import UserSave from "@/views/user/UserSave.vue";
import UserMission from "@/views/user/UserMission.vue";
// owen
// qiu以下
import Task from "../views/point/Task.vue";
import Mytask from "../views/point/Mytask.vue";
import Recharge from "../views/point/Recharge.vue";
import PointLog from "../views/point/PointLog.vue";
// qiu以上
// yjy 以下
import ProductView from "@/views/PointShop/ProductView.vue";
import OrderView from "@/views/PointShop/OrderView.vue";
import Checkout from "@/views/PointShop/CheckoutView.vue";
import ProductGrid from "@/views/PointShop/components/ProductGrid.vue";
import ProductDetail from "@/views/PointShop/components/ProductDetail.vue";
// yjy 以上

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
    component: FirstView,
  },
  // 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由

  // 以下是tim的路由
  {
    path: "/forum",
    name: "forum",
    component: () => import("@/views/Contents/forum.vue"),
  },
  // 單一content的路由
  {
    path: "/forum/:id",
    name: "forum-thread",
    component: () => import("@/views/Contents/forum.vue"),
  },
  // 以上是tim的路由

  // owen    
  {
    path: '/auth',
    name: 'Auth',
    component: AuthView,
  },
  {
    path: "/register",
    redirect: to => {
      return { path: '/auth', query: { tab: 'register' } }
    }
  },
  {
    path: "/login",
    name: "Login",
    redirect: '/auth'
  },
  {
    path: '/reset-password',
    name: 'ResetPassword',
    component: ResetPasswordView
  },
  {
    path: "/usersettings",
    name: "UserSettings",
    component: UserSettings,
    meta: { requiresAuth: true }
  },
  {
    path: '/profile/:username',
    name: 'UserProfile',
    component: UserProfile,
    props: true
  },
  {
    path: "/userindex",
    name: "UserIndex",
    component: UserIndex,
    meta: { requiresAuth: true },
    children: [
      {
        path: "usermission",
        name: "UserMission",
        component: UserMission,
      },
      {
        path: "usercollections",
        name: "UserCollections",
        component: UserCollections,
        children: [
          {
            path: "userpost",
            name: "UserPost",
            component: UserPost,
          },
          {
            path: "usersave",
            name: "UserSave",
            component: UserSave,
          },
          {
            path: "UserLike",
            name: "UserLike",
            component: UserLike,
          },
        ],
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
    component: Mytask, // 我的任務
  },
  {
    path: "/point-log",
    name: "point-log",
    component: PointLog, //views的檔案名稱
  },
  {
    path: "/task",
    name: "task",
    component: Task, //views的檔案名稱
  },
  {
    path: "/recharge",
    name: "recharge",
    component: Recharge, //views的檔案名稱
  },
  // qiu以上

  // yjy 以下
  {
    path: "/PointShop/Product",
    component: ProductView,
    children: [
      {
        path: '',
        name: "PointShop",
        component: ProductGrid,
      },
      {
        path: ':id',
        name: "ProductDetail",
        component: ProductDetail,
        props: true,
      }
    ]
  },
  {
    path: "/PointShop/Order",
    name: "Order",
    component: OrderView,
  },
  {
    path: "/PointShop/Checkout",
    name: "Checkout",
    component: Checkout,
  },
  // yjy 以上
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

    // B. 定義切換 Tab 時不捲動的頁面
    const noScrollRoutes = ['UserIndex', 'UserMission', 'UserCollections', 'UserPost', 'UserLike', 'UserSave'];

    // 如果是在 Tab 之間切換，保持原位 (return false 或不回傳 top)
    if (noScrollRoutes.includes(to.name) && noScrollRoutes.includes(from.name)) {
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
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth);
  // 3. 判斷邏輯
  if (requiresAuth && !authStore.isAuthenticated) {
    // 需要權限但沒登入 -> 踢去登入頁
    Swal.fire({
      icon: "warning",
      title: "請先登入會員",
      timer: 1500,
      showConfirmButton: false
    });
    next({
      name: 'Login',
      query: { redirect: to.fullPath }
    });
  } else {
    // 其他狀況 -> 放行
    next();
  }
});

export default router;
