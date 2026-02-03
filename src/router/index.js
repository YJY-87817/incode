import { createRouter, createWebHistory } from "vue-router";
// tim 以下
import FirstView from "@/views/FirstView.vue";
// tim 以上
// owen
import Register from "@/views/user/register.vue";
import Login from "@/views/user/login.vue";
import UserIndex from "@/views/user/userindex.vue";
import UserSettings from "@/views/user/UserSettings.vue"
import UserCollections from "@/views/user/UserCollections.vue"
import UserPost from "@/views/user/UserPost.vue"
import UserLike from "@/views/user/UserLike.vue"
import UserSave from "@/views/user/UserSave.vue"
import UserMission from "@/views/user/UserMission.vue"
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

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  /**
   * 控制路由切換時的滾動行為
   * @param {Object} to - 目標路由對象
   * @param {Object} from - 來源路由對象
   * @param {Object|null} savedPosition - 儲存的滾動位置（僅在使用瀏覽器前進/後退按鈕時存在）
   * @returns {Object} 滾動位置對象，包含 left 和 top 屬性
   * @description 如果存在儲存的位置則返回該位置，否則滾動到頁面頂部
   */
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition;
    } else {
      return { left: 0, top: 0 };
    }
  },
  routes: [
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
      path: "/user",
      name: "user",
      component: () => import("@/views/user/userindex.vue"),
    },
    {
      path: "/register",
      name: "Register",
      component: Register,
    },
    {
      path: "/login",
      name: "Login",
      component: Login,
    },
    {
      path: "/usersettings",
      name: "UserSettings",
      component: UserSettings,
    },
    {
      path: '/profile/:username',
      name: 'UserProfile',
      // 建議使用動態載入 (Lazy loading)
      component: () => import('@/views/user/UserProfile.vue'),
      props: true
    },
    {
      path: "/userindex",
      name: "UserIndex",
      component: UserIndex,
      children: [
        {
          path: "/usermission",
          name: "UserMission",
          component: UserMission,
        },
        {
          path: "/usercollections",
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
    // 沒設定的路由導向NotFound頁面、放最下面
    {
      path: '/:pathMatch(.*)*',
      name: 'notFound',
      component: () => import('@/views/error404.vue'),
    },
  ],
});

export default router;
