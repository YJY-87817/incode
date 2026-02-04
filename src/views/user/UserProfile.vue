<script setup>
  import { ref, computed, onMounted, watch } from "vue";
  import { useRoute } from "vue-router";
  import { useUserStore } from "@/stores/user/user.js";
  import { useAuthStore } from "@/stores/user/auth.js";
  import { storeToRefs } from "pinia";
  import axios from "axios";
  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterCentered.vue";
  import FollowListDialog from "@/views/user/components/FollowListDialog.vue";

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
        Swal.fire({
          title: "追蹤成功",
          text: `你已經開始追蹤 ${userProfile.value.username} 了！`,
          icon: "success",
          draggable: true
        });
      }
    } catch (error) {
      Swal.fire({
        icon: "error",
        title: "追蹤失敗",
        text: "請稍後再試一次",
      });
    }
  };

  const items = ref([]);
  const extractFirstImage = (htmlContent) => {
    if (!htmlContent) return null;
    const parser = new DOMParser();
    const doc = parser.parseFromString(htmlContent, "text/html");
    const imgTag = doc.querySelector("img");
    return imgTag ? imgTag.src : null;
  };

  const stripHtml = (html) => {
    if (!html) return "";
    const doc = new DOMParser().parseFromString(html, "text/html");
    return doc.body.textContent || "";
  };

  const fetchMyPosts = async () => {
    isLoading.value = true;
    try {
      const response = await axios.get("/contentapi/me/posts");
      const listData = response.data.items || [];
      const STATIC_BASE_URL = "https://localhost:7190";
      const processedItems = await Promise.all(
        listData.map(async (post) => {
          let rawHtml = "";
          let extractedImg = null;
          let displayText = "載入中...";
          let bodyPath = post.body;

          const defaultImage =
            "https://cdn.vuetifyjs.com/images/cards/forest.jpg";

          if (bodyPath && bodyPath.toLowerCase().endsWith(".html")) {
            try {
              const cleanPath = bodyPath.startsWith("/")
                ? bodyPath.substring(1)
                : bodyPath;

              const targetUrl = `${STATIC_BASE_URL}/${cleanPath}`;

              console.log("嘗試下載:", targetUrl);

              const htmlRes = await axios.get(targetUrl);
              const rawHtml = htmlRes.data;

              extractedImg = extractFirstImage(rawHtml);
              const textContent = stripHtml(rawHtml).trim();

              if (textContent.length > 0) {
                displayText = textContent.substring(0, 60) + "...";
              } else {
                displayText = "本篇文章僅包含圖片";
              }

              if (extractedImg && !extractedImg.startsWith("http")) {
                const cleanImgPath = extractedImg.startsWith("/")
                  ? extractedImg.substring(1)
                  : extractedImg;
                extractedImg = `${STATIC_BASE_URL}/${cleanImgPath}`;
              }
            } catch (err) {
              console.error(`下載失敗: ${targetUrl}`, err);
              displayText = "無法讀取內容";
            }
          } else {
            displayText = stripHtml(bodyPath || "");
          }

          return {
            id: post.contentId,
            userid: post.userId,
            title: post.title,
            category: post.categoryName,
            date: post.createdAt ? post.createdAt.split("T")[0] : "",
            img: extractedImg || defaultImage,
            content: displayText,
          };
        }),
      );

      items.value = processedItems;
    } catch (error) {
      console.error("列表取得失敗:", error);
    } finally {
      isLoading.value = false;
    }
  };

  onMounted(() => {
    fetchMyPosts();
  });

  watch(
    () => route.params.username,
    () => {
      loadData();
    },
    { immediate: true }, // 這一行讓它在組件掛載時立即執行一次
  );
</script>

<template>
  <DefaultNavbar light />
  <div class="container">
    <v-container class="mt-5 position-relative" style="min-height: 85vh; margin-top: -60px; z-index: 1">
      <v-card class="rounded-xl overflow-hidden mb-5" elevation="0" border>
        <div class="d-flex flex-column flex-md-row align-center align-md-end pa-6" style="background-color: #86b0bd">
          <div class="mr-md-6 mb-4 mb-md-0" style="margin-top: -10px">
            <v-avatar size="140" class="border border-4 border-white elevation-3 bg-white">
              <v-img :src="userStore.displayAvatar" alt="User Avatar" cover></v-img>
            </v-avatar>
          </div>
          <div class="text-center text-md-left flex-grow-1 mb-4 mb-md-0">
            <v-row>
              <h1 class="text-h4 font-weight-bold text-grey-darken-3 mb-1 ml-6">
                {{ userStore.userProfile.username }}
              </h1>
              <v-spacer></v-spacer>
              <div>
                <v-btn v-if="isLoading" disabled rounded="pill" variant="tonal" class="px-4">
                  <v-progress-circular size="20" width="2" indeterminate></v-progress-circular>
                </v-btn>
                <v-btn v-else-if="isSelf" color="grey-darken-3" variant="outlined" rounded="pill"
                  class="px-4 text-body-1 font-weight-bold" prepend-icon="mdi-pencil" :to="{ name: 'UserIndex' }">
                  編輯資料
                </v-btn>
                <v-btn v-else-if="userStore.isFollowingCurrentUser" color="grey-lighten-2" variant="flat" rounded="pill"
                  class="px-4 text-body-1 font-weight-bold text-grey-darken-3" prepend-icon="mdi-check"
                  @click="handleFollowToggle">
                  已追蹤
                </v-btn>
                <v-btn v-else color="#145369" variant="flat" rounded="pill"
                  class="px-4 text-body-1 text-white font-weight-bold" prepend-icon="mdi-plus"
                  @click="handleFollowToggle">
                  追蹤
                </v-btn>
              </div>
            </v-row>
            <v-row>
              <div class="d-flex justify-center justify-md-start gap-4 mt-3 ml-8">
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
                <div class="text-center mr-4 cursor-pointer hover-opacity" @click="openFollowDialog('following')">
                  <span class="text-h6 font-weight-bold text-grey-darken-3">{{
                    userStore.userProfile.followers
                    }}</span>
                  <span class="text-caption text-white ml-1">關注</span>
                </div>
                <div class="text-centercursor-pointer hover-opacity" @click="openFollowDialog('fans')">
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
        </div>

        <v-row>
          <v-col v-for="(item, index) in items" :key="item.id" cols="12" sm="6" md="4">
            <v-card class="h-100 d-flex flex-column rounded-lg" border flat hover elevation="0">
              <v-img :src="item.img" height="200" cover class="rounded-t-lg align-end"
                gradient="to bottom, rgba(0,0,0,0) 60%, rgba(0,0,0,0.6) 100%">
                <v-card-title class="text-white text-subtitle-1 font-weight-bold px-4 pb-2">
                  {{ item.title }}
                </v-card-title>
              </v-img>

              <v-card-item class="pt-3 pb-2 flex-grow-1">
                <div class="d-flex align-center justify-space-between mb-2">
                  <v-chip size="x-small" color="primary" variant="tonal" class="font-weight-bold">
                    {{ item.category }}
                  </v-chip>
                  <span class="text-caption text-grey">
                    <v-icon icon="mdi-calendar-blank" size="small" start class="mr-1"></v-icon>
                    {{ item.date }}
                  </span>
                </div>

                <div class="text-body-2 text-medium-emphasis text-truncate-2">
                  {{ item.content }}
                </div>
              </v-card-item>
              <v-spacer></v-spacer>
              <v-card-actions class="px-4 pb-4 pt-0">
                <router-link :to="`/forum/${item.id}`">
                  <v-btn variant="text" color="grey-darken-1" class="px-0" size="small">
                    繼續閱讀
                  </v-btn>
                </router-link>
                <v-spacer></v-spacer>
                <template v-if="userStore.userProfile.userId == item.userId">
                  <v-btn icon="mdi-pencil-outline" size="small" variant="text" color="grey" title="編輯文章"></v-btn>
                  <v-btn icon="mdi-delete-outline" size="small" variant="text" color="grey" title="刪除文章"></v-btn>
                </template>
                <template v-else>
                  <v-btn icon="mdi-bookmark-outline" size="small" variant="text" color="grey" title="收藏文章"></v-btn>
                  <v-btn icon="mdi-share-variant-outline" size="small" variant="text" color="grey" title="分享文章"></v-btn>
                </template>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </div>

      <v-container class="d-flex flex-column align-center mt-8">
        <!-- 換成分頁 
        <v-btn
          variant="text"
          color="primary"
          rounded="pill"
          class="text-body-2"
        >
          載入更多文章
          <v-icon end icon="mdi-chevron-down"></v-icon>
        </v-btn> -->
      </v-container>
    </v-container>
  </div>
  <FollowListDialog v-if="userProfile.id" v-model="showFollowDialog" :user-id="userProfile.id"
    :initial-tab="followDialogTab" />
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