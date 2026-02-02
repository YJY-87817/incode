import { createRouter, createWebHistory } from "vue-router";
import PresentationView from "../views/Presentation/PresentationView.vue";
import AboutView from "../views/LandingPages/AboutUs/AboutView.vue";
import ContactView from "../views/LandingPages/ContactUs/ContactView.vue";
import AuthorView from "../views/LandingPages/Author/AuthorView.vue";

// tim 以下
import FirstView from "@/views/FirstView.vue"
// tim 以上
// owen
import Register from "@/views/user/Register.vue";
import Login from "@/views/user/Login.vue";
import UserIndex from "@/views/user/UserIndex.vue";
import UserProfile from "@/views/user/UserProfile.vue"
import UserSettings from "@/views/user/UserSettings.vue"
import UserCollections from "@/views/user/UserCollections.vue"
import UserPost from "@/views/user/UserPost.vue"
import UserLike from "@/views/user/UserLike.vue"
import UserSave from "@/views/user/UserSave.vue"
import UserMission from "@/views/user/UserMission.vue"
// owen
import PageHeaders from "../layouts/sections/page-sections/page-headers/HeadersView.vue";
import PageFeatures from "../layouts/sections/page-sections/features/FeaturesView.vue";
import NavigationNavbars from "../layouts/sections/navigation/navbars/NavbarsView.vue";
import NavigationNavTabs from "../layouts/sections/navigation/nav-tabs/NavTabsView.vue";
import NavigationPagination from "../layouts/sections/navigation/pagination/PaginationView.vue";
import InputAreasInputs from "../layouts/sections/input-areas/inputs/InputsView.vue";
import InputAreasForms from "../layouts/sections/input-areas/forms/FormsView.vue";
import ACAlerts from "../layouts/sections/attention-catchers/alerts/AlertsView.vue";
import ACModals from "../layouts/sections/attention-catchers/modals/ModalsView.vue";
import ACTooltipsPopovers from "../layouts/sections/attention-catchers/tooltips-popovers/TooltipsPopoversView.vue";
import ElAvatars from "../layouts/sections/elements/avatars/AvatarsView.vue";
import ElBadges from "../layouts/sections/elements/badges/BadgesView.vue";
import ElBreadcrumbs from "../layouts/sections/elements/breadcrumbs/BreadcrumbsView.vue";
import ElButtons from "../layouts/sections/elements/buttons/ButtonsView.vue";
import ElButtonGroups from "../layouts/sections/elements/button-groups/ButtonGroupsView.vue";
import ElDropdowns from "../layouts/sections/elements/dropdowns/DropdownsView.vue";
import ElProgressBars from "../layouts/sections/elements/progress-bars/ProgressBarsView.vue";
import ElToggles from "../layouts/sections/elements/toggles/TogglesView.vue";
import ElTypography from "../layouts/sections/elements/typography/TypographyView.vue";
import TestQiu from "../views/point/TestQiu.vue";
import Mission from "../views/point/Mission.vue";
import Mytask from "../views/point/Mytask.vue";
import Recharge from "../views/point/Recharge.vue";
import PointLog from "../views/point/PointLog.vue";
import PointShopView from "@/views/PointShop/PointShopView.vue";
import OrderView from "@/views/PointShop/OrderView.vue";
import Checkout from "@/views/PointShop/CheckoutView.vue";


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      // 首頁改成這個_tim
      path: "/",
      name: "firstview",
      component: FirstView,
    },
    {
      path: "/presentation",
      name: "presentation",
      component: PresentationView,
    },
    {
      path: "/pages/landing-pages/about-us",
      name: "about",
      component: AboutView,
    },
    {
      path: "/pages/landing-pages/contact-us",
      name: "contactus",
      component: ContactView,
    },
    {
      path: "/pages/landing-pages/author",
      name: "author",
      component: AuthorView,
    },
    {
      path: "/sections/page-sections/page-headers",
      name: "page-headers",
      component: PageHeaders,
    },
    {
      path: "/sections/page-sections/features",
      name: "page-features",
      component: PageFeatures,
    },
    {
      path: "/sections/navigation/navbars",
      name: "navigation-navbars",
      component: NavigationNavbars,
    },
    {
      path: "/sections/navigation/nav-tabs",
      name: "navigation-navtabs",
      component: NavigationNavTabs,
    },
    {
      path: "/sections/navigation/pagination",
      name: "navigation-pagination",
      component: NavigationPagination,
    },
    {
      path: "/sections/input-areas/inputs",
      name: "inputareas-inputs",
      component: InputAreasInputs,
    },
    {
      path: "/sections/input-areas/forms",
      name: "inputareas-forms",
      component: InputAreasForms,
    },
    {
      path: "/sections/attention-catchers/alerts",
      name: "ac-alerts",
      component: ACAlerts,
    },
    {
      path: "/sections/attention-catchers/modals",
      name: "ac-modals",
      component: ACModals,
    },
    {
      path: "/sections/attention-catchers/tooltips-popovers",
      name: "ac-tooltips-popovers",
      component: ACTooltipsPopovers,
    },
    {
      path: "/sections/elements/avatars",
      name: "el-avatars",
      component: ElAvatars,
    },
    {
      path: "/sections/elements/badges",
      name: "el-badges",
      component: ElBadges,
    },
    {
      path: "/sections/elements/breadcrumbs",
      name: "el-breadcrumbs",
      component: ElBreadcrumbs,
    },
    {
      path: "/sections/elements/buttons",
      name: "el-buttons",
      component: ElButtons,
    },
    {
      path: "/sections/elements/button-groups",
      name: "el-button-groups",
      component: ElButtonGroups,
    },
    {
      path: "/sections/elements/dropdowns",
      name: "el-dropdowns",
      component: ElDropdowns,
    },
    {
      path: "/sections/elements/progress-bars",
      name: "el-progress-bars",
      component: ElProgressBars,
    },
    {
      path: "/sections/elements/toggles",
      name: "el-toggles",
      component: ElToggles,
    },
    {
      path: "/sections/elements/typography",
      name: "el-typography",
      component: ElTypography,
    },

    // 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由// 以下是大家的路由
    // 以下是tim的路由
    {
      path: "/forum",
      name: "forum",
      component: () => import("@/views/Contents/forum.vue"),
    },
    {
      path: "/forum/new-topic",
      name: "new-topic",
      component: () => import("@/views/Contents/NewTopicPage.vue"),
    },
    {
      path: "/test",
      name: "test",
      component: () => import("@/views/Contents/test.vue"),
    },
    {
      path: "/tmdb",
      name: "tmdb",
      component: () => import("@/views/Contents/tmdb.vue"),
    },
    {
      path: "/imagetest",
      name: "imagetest",
      component: () => import("@/components/Contents/ImageTest.vue"),
    },
    // 以上是tim的路由

    // owen
    {
      path: "/user",
      name: "user",
      component: () => import("@/views/user/UserIndex.vue"),
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
            }
          ]
        }]
    },
    // owen

    // qiu以下
    {
      path: "/test-qiu",
      name: "test-qiu",
      component: TestQiu, // 我的任務
    },
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
      path: "/mission",
      name: "mission",
      component: Mission, //views的檔案名稱
    },
    {
      path: "/recharge",
      name: "recharge",
      component: Recharge, //views的檔案名稱
    },
    // qiu以上

    // yjy 以下
    {
      path: "/PointShop/PointShop",
      name: "PointShop",
      component: PointShopView,
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
  ],
});

export default router;