<script setup>
import { ref, computed, onMounted, watch } from "vue";
import { useRoute } from "vue-router";
import { useUserStore } from "@/stores/user/user.js";
import { useAuthStore } from "@/stores/user/auth.js";
import { storeToRefs } from "pinia";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
import { useAlertStore } from "@/stores/alert";
import FollowListDialog from "@/views/user/components/FollowListDialog.vue";

const alertStore = useAlertStore();
const authStore = useAuthStore();
const userStore = useUserStore();
const route = useRoute();
const {
  userProfile,
  displayAvatar,
  followersCount,
  isFollowingCurrentUser,
  isLoading,
} = storeToRefs(userStore);

const showFollowDialog = ref(false);
const followDialogTab = ref("following");
const openFollowDialog = (tab) => {
  // 如果資料還在載入中，或是沒有 User ID，就不打開
  if (isLoading.value || !userProfile.value.id) return;

  followDialogTab.value = tab;
  showFollowDialog.value = true;
};

const isSelf = computed(() => {
  const currentUserId = authStore.user?.id;
  const profileUserId = userProfile.value?.id;
  if (!currentUserId || !profileUserId) return false;
  return String(currentUserId) === String(profileUserId);
});

const loadData = async () => {
  const targetUsername = route.params.username; // 從網址取得 username
  await Promise.all([
    userStore.fetchUserProfile(targetUsername), // 這裡會處理 API
    userStore.fetchFollowingList(),
  ]);
};

// 處理按鈕點擊
const handleFollowToggle = async () => {
  const targetId = userProfile.value.id;
  if (!targetId || targetId === 0) return;

  try {
    if (isFollowingCurrentUser.value) {
      await userStore.unfollowUser(targetId);
      alertStore.showSuccess("已取消追蹤");
    } else {
      await userStore.followUser(targetId);
      alertStore.open({
        type: "success",
        title: "追蹤成功",
        text: `你已經開始追蹤 ${userProfile.value.username} 了！`,
      });
    }
  } catch (error) {
    console.error(error);
    alertStore.showError("操作失敗，請稍後再試");
  }
};

// 模擬文章資料
const items = ref([
  {
    id: 1,
    title: "夏日午後的海岸線",
    type: "風景",
    date: "2024-06-01",
    content: "這是在墾丁拍攝的一組照片，光線非常完美，展現了大海的層次感...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
  },
  {
    id: 2,
    title: "城市角落的人像攝影",
    type: "人像",
    date: "2024-06-01",
    content: "利用自然光與建築陰影的對比，捕捉模特兒最自然的神情。",
    img: "https://picsum.photos/id/1011/500/300",
  },
  {
    id: 3,
    title: "極簡主義建築美學",
    type: "建築",
    date: "2024-06-01",
    content: "探索清水模建築的幾何線條，黑白攝影的表現手法。",
    img: "https://picsum.photos/id/1025/500/300",
  },
  {
    id: 4,
    title: "街頭貓咪日記",
    type: "動物",
    date: "2024-06-01",
    content: "紀錄巷弄裡遇見的每一隻浪浪，牠們眼中的世界。",
    img: "https://picsum.photos/id/1074/500/300",
  },
]);

watch(
  () => route.params.username,
  () => {
    loadData();
  },
  { immediate: true }, // 這一行讓它在組件掛載時立即執行一次
);
</script>

<template>
  <DefaultNavbar />
  <div class="container">
    <v-container
      class="mt-5 position-relative"
      style="min-height: 85vh; margin-top: -60px; z-index: 1"
    >
      <v-card class="rounded-xl overflow-hidden mb-5" elevation="0" border>
        <div
          class="d-flex flex-column flex-md-row align-center align-md-end pa-6"
          style="background-color: #86b0bd"
        >
          <div class="mr-md-6 mb-4 mb-md-0" style="margin-top: -10px">
            <v-avatar
              size="140"
              class="border border-4 border-white elevation-3 bg-white"
            >
              <v-img
                :src="userStore.displayAvatar"
                alt="User Avatar"
                cover
              ></v-img>
            </v-avatar>
          </div>
          <div class="text-center text-md-left flex-grow-1 mb-4 mb-md-0">
            <v-row>
              <h1 class="text-h4 font-weight-bold text-grey-darken-3 mb-1 ml-6">
                {{ userStore.userProfile.username }}
              </h1>
              <v-spacer></v-spacer>
              <div>
                <v-btn
                  v-if="isLoading"
                  disabled
                  rounded="pill"
                  variant="tonal"
                  class="px-4"
                >
                  <v-progress-circular
                    size="20"
                    width="2"
                    indeterminate
                  ></v-progress-circular>
                </v-btn>
                <v-btn
                  v-else-if="isSelf"
                  color="grey-darken-3"
                  variant="outlined"
                  rounded="pill"
                  class="px-4 text-body-1 font-weight-bold"
                  prepend-icon="mdi-pencil"
                  :to="{ name: 'UserIndex' }"
                >
                  編輯資料
                </v-btn>
                <v-btn
                  v-else-if="userStore.isFollowingCurrentUser"
                  color="grey-lighten-2"
                  variant="flat"
                  rounded="pill"
                  class="px-4 text-body-1 font-weight-bold text-grey-darken-3"
                  prepend-icon="mdi-check"
                  @click="handleFollowToggle"
                >
                  已追蹤
                </v-btn>
                <v-btn
                  v-else
                  color="#145369"
                  variant="flat"
                  rounded="pill"
                  class="px-4 text-body-1 text-white font-weight-bold"
                  prepend-icon="mdi-plus"
                  @click="handleFollowToggle"
                >
                  追蹤
                </v-btn>
              </div>
            </v-row>
            <v-row>
              <div
                class="d-flex justify-center justify-md-start gap-4 mt-3 ml-8"
              >
                <div class="text-center mr-4">
                  <span class="text-h6 font-weight-bold text-grey-darken-3">{{
                    userStore.userProfile.articleCount
                  }}</span>
                  <span class="text-caption text-white ml-1">文章</span>
                </div>
                <div class="text-center mr-4">
                  <span class="text-h6 font-weight-bold text-grey-darken-3">{{
                    userStore.userProfile.missionCount
                  }}</span>
                  <span class="text-caption text-white ml-1">任務</span>
                </div>
                <div class="text-center mr-4">
                  <span class="text-h6 font-weight-bold text-grey-darken-3">{{
                    userStore.userProfile.points
                  }}</span>
                  <span class="text-caption text-white ml-1">點數</span>
                </div>
                <div
                  class="text-center mr-4 cursor-pointer hover-opacity"
                  @click="openFollowDialog('following')"
                >
                  <span class="text-h6 font-weight-bold text-grey-darken-3">{{
                    userStore.userProfile.followers
                  }}</span>
                  <span class="text-caption text-white ml-1">關注</span>
                </div>
                <div
                  class="text-centercursor-pointer hover-opacity"
                  @click="openFollowDialog('fans')"
                >
                  <span class="text-h6 font-weight-bold text-grey-darken-3">{{
                    userStore.userProfile.following
                  }}</span>
                  <span class="text-caption text-white ml-1">粉絲</span>
                </div>
              </div>
            </v-row>
          </div>
        </div>
      </v-card>

      <div class="mb-6">
        <div class="d-flex justify-space-between align-end mb-4 px-2">
          <div>
            <h2 class="text-h5 font-weight-bold text-grey-darken-3">
              {{ userStore.userProfile.username }} 的討論區
            </h2>
            <p class="text-caption text-grey">最新發佈的文章與動態</p>
          </div>
          <v-btn
            variant="text"
            color="primary"
            size="small"
            append-icon="mdi-arrow-right"
            class="font-weight-bold"
          >
            查看全部
          </v-btn>
        </div>

        <v-row>
          <v-col
            v-for="(item, index) in items"
            :key="item.id"
            cols="12"
            sm="6"
            md="4"
          >
            <v-card
              class="h-100 d-flex flex-column rounded-lg"
              border
              flat
              hover
              elevation="0"
            >
              <v-img
                :src="item.img"
                height="200"
                cover
                class="rounded-t-lg align-end"
                gradient="to bottom, rgba(0,0,0,0) 60%, rgba(0,0,0,0.6) 100%"
              >
                <v-card-title
                  class="text-white text-subtitle-1 font-weight-bold px-4 pb-2"
                >
                  {{ item.title }}
                </v-card-title>
              </v-img>

              <v-card-item class="pt-3 pb-2 flex-grow-1">
                <div class="d-flex align-center justify-space-between mb-2">
                  <v-chip
                    size="x-small"
                    color="primary"
                    variant="tonal"
                    class="font-weight-bold"
                  >
                    {{ item.type }}
                  </v-chip>
                  <span class="text-caption text-grey">
                    <v-icon
                      icon="mdi-calendar-blank"
                      size="small"
                      start
                      class="mr-1"
                    ></v-icon>
                    {{ item.date }}
                  </span>
                </div>

                <div class="text-body-2 text-medium-emphasis text-truncate-2">
                  {{ item.content }}
                </div>
              </v-card-item>

              <v-divider></v-divider>

              <v-card-actions class="px-4 py-3">
                <v-btn
                  variant="text"
                  color="grey-darken-1"
                  class="px-0 text-capitalize"
                  size="small"
                >
                  閱讀更多
                </v-btn>
                <v-spacer></v-spacer>
                <v-btn
                  icon="mdi-heart-outline"
                  size="small"
                  variant="text"
                  color="grey"
                ></v-btn>
                <v-btn
                  icon="mdi-bookmark-outline"
                  size="small"
                  variant="text"
                  color="grey"
                ></v-btn>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </div>

      <v-container class="d-flex flex-column align-center mt-8">
        <v-btn
          variant="text"
          color="primary"
          rounded="pill"
          class="text-body-2"
        >
          載入更多文章
          <v-icon end icon="mdi-chevron-down"></v-icon>
        </v-btn>
      </v-container>
    </v-container>
  </div>
  <FollowListDialog
    v-if="userProfile.id"
    v-model="showFollowDialog"
    :user-id="userProfile.id"
    :initial-tab="followDialogTab"
  />
  <DefaultFooter />
</template>

<style scoped>
/* [新增] 讓滑鼠移過去有互動感 */
.cursor-pointer {
  cursor: pointer;
}
.hover-opacity:hover {
  opacity: 0.7;
  transition: opacity 0.2s;
}
</style>
