<script setup>
  import { ref, computed, watch } from "vue";
  import { useRoute, useRouter } from "vue-router";
  import { useUserStore } from "@/stores/user/user.js";
  import { useAuthStore } from "@/stores/user/auth.js";
  import { storeToRefs } from "pinia";
  import axios from "axios";
  import Swal from "sweetalert2";
  import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
  import DefaultFooter from "@/examples/footers/FooterCentered.vue";
  import FollowListDialog from "@/views/user/components/FollowListDialog.vue";
  import UserProfileCard from "@/views/user/components/UserProfileCard.vue";

  const authStore = useAuthStore();
  const userStore = useUserStore();
  const route = useRoute();
  const router = useRouter();

  const { userProfile, displayAvatar, isFollowingCurrentUser, isLoading } = storeToRefs(userStore);

  const showFollowDialog = ref(false);
  const followDialogTab = ref("following");
  const postsItems = ref([]);
  const isPostsLoading = ref(false);

  // --- 判斷權限與按鈕狀態 ---
  const isSelf = computed(() => {
    return String(authStore.user?.id) === String(userProfile.value?.id);
  });
  const buttonLabel = computed(() => {
    if (isLoading.value) return "載入中...";
    return isSelf.value ? "返回會員中心" : (isFollowingCurrentUser.value ? "已追蹤" : "追蹤");
  });

  // --- 互動邏輯 ---
  const openFollowDialog = async (tab) => {
    if (!userProfile.value?.id) return;
    followDialogTab.value = tab;
    showFollowDialog.value = true;
    tab === "following"
      ? await userStore.fetchUserFollowingList(userProfile.value.id)
      : await userStore.fetchUserFollowersList(userProfile.value.id);
  };

  const handleCardAction = async () => {
    if (isSelf.value) {
      router.push({ name: "UserIndex" });
    } else {
      // 執行追蹤/取消追蹤
      const targetId = userProfile.value.id;
      if (!targetId) return;
      try {
        isFollowingCurrentUser.value
          ? await userStore.unfollowUser(targetId)
          : await userStore.followUser(targetId);

        Swal.fire({
          title: isFollowingCurrentUser.value ? "追蹤成功" : "已取消追蹤",
          icon: "success",
          timer: 1500,
          showConfirmButton: false,
        });
      } catch (error) {
        Swal.fire({ icon: "error", title: "操作失敗", text: "請稍後再試" });
      }
    }
  };

  // --- 文章列表邏輯 ---
  const fetchUserPosts = async (userId) => {
    isPostsLoading.value = true;
    try {
      const { data } = await axios.get("/ContentApi");
      const listData = Array.isArray(data) ? data : data.items || [];
      const STATIC_BASE_URL = "https://localhost:7190";

      const filteredList = listData.filter(post => String(post.userId || post.UserId) === String(userId));
      const parser = new DOMParser();

      postsItems.value = await Promise.all(
        filteredList.map(async (post) => {
          let extractedImg = null;
          let displayText = "載入中...";
          const bodyPath = post.body || post.Body || "";

          if (bodyPath.toLowerCase().endsWith(".html")) {
            try {
              const { data: rawHtml } = await axios.get(`${STATIC_BASE_URL}/${bodyPath.replace(/^\//, '')}`);
              const doc = parser.parseFromString(rawHtml, "text/html");

              const imgTag = doc.querySelector("img");
              extractedImg = imgTag ? imgTag.getAttribute("src") : null;

              const text = doc.body.textContent?.trim() || "";
              displayText = text ? text.substring(0, 60) + "..." : "本篇文章僅包含圖片";

              if (extractedImg && !extractedImg.startsWith("http")) {
                extractedImg = `${STATIC_BASE_URL}/${extractedImg.replace(/^\//, '')}`;
              }
            } catch (err) {
              displayText = "無法讀取內容";
            }
          } else {
            displayText = parser.parseFromString(bodyPath, "text/html").body.textContent || "";
          }

          return {
            id: post.contentId,
            userid: post.userId,
            title: post.title,
            category: post.categoryName,
            date: post.createdAt ? post.createdAt.split("T")[0] : "",
            img: extractedImg || "https://cdn.vuetifyjs.com/images/cards/forest.jpg",
            content: displayText,
          };
        })
      );
    } catch (error) {
      console.error("列表取得失敗:", error);
    } finally {
      isPostsLoading.value = false;
    }
  };

  // --- 初始化載入資料 ---
  const loadData = async () => {
    isPostsLoading.value = true;
    await userStore.fetchUserProfile(route.params.username);
    if (authStore.user?.id) await userStore.fetchMyFollowingList();
    if (userProfile.value.id) await fetchUserPosts(userProfile.value.id);
  };

  // 監聽路由變化
  watch(() => route.params.username, loadData, { immediate: true });

</script>

<template>
  <DefaultNavbar light />
  <div class="container mt-6">
    <v-row>
      <v-col cols="12">
        <UserProfileCard :profile="userProfile" :avatar-url="displayAvatar" :loading="isLoading"
          :action-label="buttonLabel" :show-contact-info="false" @click-action="handleCardAction"
          @click-stat="openFollowDialog" />
      </v-col>
    </v-row>

    <div class="mt-6 mb-6">
      <div class="d-flex justify-space-between align-end mb-4 px-2">
        <div>
          <h2 class="text-h5 font-weight-bold text-grey-darken-3">
            {{ userStore.userProfile.username }} 的討論區
          </h2>
          <p class="text-caption text-grey">最新發佈的文章與動態</p>
        </div>
      </div>

      <v-row>
        <v-col v-if="isPostsLoading" cols="12" class="text-center my-8">
          <v-progress-circular indeterminate color="primary"></v-progress-circular>
        </v-col>

        <v-col v-else-if="postsItems.length === 0" cols="12" class="text-center text-grey my-8">
          此使用者尚未發佈文章
        </v-col>

        <v-col v-else v-for="item in postsItems" :key="item.id" cols="12" sm="6" md="4">
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

            <v-card-actions class="px-4 pb-4 pt-0">
              <router-link :to="`/forum/${item.id}`">
                <v-btn variant="text" color="grey-darken-1" class="px-0" size="small">繼續閱讀</v-btn>
              </router-link>
              <v-spacer></v-spacer>
              <template v-if="isSelf">
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
  </div>

  <FollowListDialog v-if="userProfile.id" v-model="showFollowDialog" :user-id="userProfile.id"
    :initial-tab="followDialogTab" />
  <DefaultFooter />
</template>

<style scoped>
  .cursor-pointer {
    cursor: pointer;
  }

  .hover-opacity:hover {
    opacity: 0.7;
    transition: opacity 0.2s;
  }

  .text-truncate-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
</style>