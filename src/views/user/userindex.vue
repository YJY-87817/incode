<script setup>
import { onMounted, onUnmounted, ref, shallowRef } from "vue";
import { useRouter } from "vue-router";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import { useAuthStore } from "@/stores/user/auth";

// 引入 Store
const authStore = useAuthStore();
const router = useRouter();

// 狀態控制
const dialog = shallowRef(false);
const activeTab = ref(null); // 控制下方 Tab

// 表單資料
const name = ref("暱稱name");
const email = ref("email@example.com");
const formValid = ref(false);

// 驗證規則
const rules = {
  required: (value) => !!value || "此欄位必填",
  email: (value) => {
    const pattern = /^[^@]+@[^@]+\.[^@]+$/;
    return pattern.test(value) || "Email 格式不正確";
  },
};

// 模擬通知資料
const notifications = ref([
  {
    id: 1,
    title: "系統",
    subtitle: "您的密碼已成功修改",
    time: "剛剛",
    icon: "mdi-cog",
    color: "grey-darken-1",
  },
  {
    id: 2,
    title: "任務",
    subtitle: "恭喜完成新手任務！點數已發放。",
    time: "1小時前",
    icon: "mdi-check-circle",
    color: "success",
  },
  {
    id: 3,
    title: "好友",
    subtitle: "小明 回覆了您的文章",
    time: "昨天",
    icon: "mdi-account-multiple",
    color: "primary",
  },
]);

// Hooks
onMounted(() => {
  const body = document.getElementsByTagName("body")[0];
  body.classList.add("bg-grey-lighten-5"); // 全局背景色
});

onUnmounted(() => {
  const body = document.getElementsByTagName("body")[0];
  body.classList.remove("bg-grey-lighten-5");
});
</script>

<template>
  <DefaultNavbar />
  <div class="container">
    <v-container
      class="mt-5 position-relative"
      style="min-height: 85vh; z-index: 1"
    >
      <div class="text-center mb-3">
        <h1 class="text-h3 font-weight-bold text-grey-darken-4">會員中心</h1>
        <p class="text-subtitle-1 text-grey-darken-1">Welcome back, User</p>
      </div>

      <v-row>
        <v-col cols="12" md="8">
          <h3 class="text-h6 font-weight-bold text-grey-darken-3 mb-4">
            我的資料
          </h3>
          <v-card class="rounded-xl overflow-hidden" elevation="0" border>
            <div
              class="d-flex align-center pa-6"
              style="background-color: #86b0bd"
            >
              <div style="position: relative" class="mb-0 mr-6">
                <v-avatar
                  size="120"
                  class="border border-4 border-white elevation-2"
                >
                  <v-img
                    src="https://cdn.vuetifyjs.com/images/john.jpg"
                    alt="User Avatar"
                    cover
                  ></v-img>
                  <v-btn
                    icon="mdi-camera"
                    size="x-small"
                    color="grey-darken-3"
                    variant="flat"
                    style="position: absolute; bottom: 0; right: 0"
                  ></v-btn>
                </v-avatar>
              </div>
              <div class="d-flex flex-column justify-center w-100">
                <h2 class="text-h5 font-weight-bold text-grey-darken-4 mb-2">
                  暱稱 Name
                </h2>
                <div class="d-flex flex-wrap align-center gap-4 mt-2">
                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3"
                      >1,423</span
                    >
                    <span class="text-caption text-white ml-1">文章</span>
                  </div>

                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3"
                      >9</span
                    >
                    <span class="text-caption text-white ml-1">任務</span>
                  </div>

                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3"
                      >9</span
                    >
                    <span class="text-caption text-white ml-1">點數</span>
                  </div>

                  <div class="text-center mr-4">
                    <span class="text-h10 font-weight-bold text-grey-darken-3"
                      >168</span
                    >
                    <span class="text-caption text-white ml-1">關注</span>
                  </div>

                  <div class="text-center">
                    <span class="text-h10 font-weight-bold text-grey-darken-3"
                      >1,034</span
                    >
                    <span class="text-caption text-white ml-1">粉絲</span>
                  </div>
                </div>
              </div>
            </div>

            <v-row class="text-body-2 mt-2 pa-4">
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3">mdi-email-outline</v-icon>
                <span class="text-grey-darken-1">Email：</span>
                <span class="font-weight-medium ml-1">email@example.com</span>
              </v-col>
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3">mdi-phone-outline</v-icon>
                <span class="text-grey-darken-1">手機：</span>
                <span class="font-weight-medium ml-1">0912-345-678</span>
              </v-col>
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3"
                  >mdi-map-marker-outline</v-icon
                >
                <span class="text-grey-darken-1">地址：</span>
                <span class="font-weight-medium ml-1">台北市信義區</span>
              </v-col>
              <v-col cols="12" sm="6" class="d-flex align-center py-2">
                <v-icon color="grey" class="mr-3"
                  >mdi-cake-variant-outline</v-icon
                >
                <span class="text-grey-darken-1">生日：</span>
                <span class="font-weight-medium ml-1">1995-01-01</span>
              </v-col>
            </v-row>
            <div class="mt-2 mb-2 d-flex justify-content-end">
              <v-dialog v-model="dialog" max-width="600">
                <template v-slot:activator="{ props }">
                  <v-btn
                    v-bind="props"
                    prepend-icon="mdi-pencil-outline"
                    variant="flat"
                    color="#145369"
                    class="text-white"
                    rounded="pill"
                    size="small"
                  >
                    編輯資料
                  </v-btn>
                </template>

                <v-card rounded="xl" class="pa-6">
                  <div class="d-flex justify-space-between align-center mb-6">
                    <h3 class="text-h6 font-weight-bold">編輯個人資料</h3>
                    <v-btn
                      icon="mdi-close"
                      variant="text"
                      @click="dialog = false"
                    ></v-btn>
                  </div>

                  <v-form v-model="formValid">
                    <v-row dense>
                      <v-col cols="12" sm="6">
                        <v-text-field
                          v-model="name"
                          label="暱稱"
                          variant="outlined"
                          density="comfortable"
                          :rules="[rules.required]"
                        ></v-text-field>
                      </v-col>
                      <v-col cols="12" sm="6">
                        <v-text-field
                          v-model="email"
                          label="Email"
                          variant="outlined"
                          density="comfortable"
                          :rules="[rules.required, rules.email]"
                        ></v-text-field>
                      </v-col>
                      <v-col cols="12" sm="6">
                        <v-text-field
                          label="手機"
                          variant="outlined"
                          density="comfortable"
                        ></v-text-field>
                      </v-col>
                      <v-col cols="12" sm="6"
                        ><!--改生日 -->
                        <v-select
                          label="年齡"
                          :items="['0-17', '18-29', '30-54', '54+']"
                          variant="outlined"
                          density="comfortable"
                        ></v-select>
                      </v-col>
                      <v-col cols="12">
                        <v-text-field
                          label="地址"
                          variant="outlined"
                          density="comfortable"
                        ></v-text-field>
                      </v-col>
                    </v-row>
                  </v-form>

                  <v-card-actions class="pa-0 mt-4">
                    <v-spacer></v-spacer>
                    <v-btn variant="text" color="grey" @click="dialog = false"
                      >取消</v-btn
                    >
                    <v-btn
                      color="primary"
                      variant="flat"
                      :disabled="!formValid"
                      @click="dialog = false"
                      >保存變更</v-btn
                    >
                  </v-card-actions>
                </v-card>
              </v-dialog>
            </div>
          </v-card>
        </v-col>

        <v-col cols="12" md="4">
          <h3 class="text-h6 font-weight-bold text-grey-darken-3 mb-4">
            最新通知
          </h3>

          <div class="d-flex flex-column">
            <v-card
              v-for="item in notifications"
              :key="item.id"
              class="rounded-lg mb-3"
              border
              flat
              hover
            >
              <div class="d-flex align-start pa-4">
                <v-avatar
                  :color="item.color"
                  variant="tonal"
                  class="mr-3"
                  rounded
                >
                  <v-icon :icon="item.icon"></v-icon>
                </v-avatar>
                <div class="flex-grow-1">
                  <div class="d-flex justify-space-between align-center mb-1">
                    <span class="text-subtitle-2 font-weight-bold">{{
                      item.title
                    }}</span>
                    <span class="text-caption text-grey">{{ item.time }}</span>
                  </div>
                  <div class="text-body-2 text-grey-darken-1">
                    {{ item.subtitle }}
                  </div>
                </div>
              </div>
            </v-card>
          </div>
        </v-col>
      </v-row>
      <v-row>
        <v-col cols="12">
          <v-card class="rounded-xl overflow-hidden" border flat>
            <nav
              class="text-center py-3"
              style="font-size: 24px; background-color: #86b0bd"
            >
              <router-link :to="{ name: 'UserMission' }">任務</router-link> |
              <router-link :to="{ name: 'UserCollections' }"
                >討論區</router-link
              >
            </nav>

            <div class="nested-content">
              <router-view></router-view>
            </div>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
  <DefaultFooter />
</template>
