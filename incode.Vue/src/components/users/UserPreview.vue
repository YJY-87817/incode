<script setup>
import { ref, computed, watch } from "vue";
import { useUserStore } from "@/stores/users/user.js";
import { useAuthStore } from "@/stores/users/auth.js";
import { storeToRefs } from "pinia";
import axios from "axios";

// 接收外部傳入的參數
const props = defineProps({
  userId: { type: [Number, String], default: null },
  avatarUrl: { type: String, default: "" },
  username: { type: String, default: "User" },
  size: { type: [Number, String], default: 40 }, // 頭像大小
});

const userStore = useUserStore();
const authStore = useAuthStore();
const { myFollowingList } = storeToRefs(userStore);

const menuOpen = ref(false);
const isLoading = ref(false);
const userInfo = ref({
  articleCount: 0,
  followers: 0,
  following: 0,
});

// 計算顯示用戶名稱
const displayName = computed(() => props.username || "User");

// 計算數值大小
const numericSize = computed(() => {
  const parsed = Number(props.size);
  return Number.isNaN(parsed) || parsed <= 0 ? 24 : parsed;
});

// 檢查是否是自己 (如果是自己，不顯示追蹤按鈕)
const isSelf = computed(() => {
  return String(authStore.user?.id) === String(props.userId);
});

// 檢查是否已追蹤
const isFollowing = computed(() => {
  if (isSelf.value) return false;
  // 檢查我的關注清單中是否有這個人
  return myFollowingList.value.some(
    (u) => String(u.id || u.followeeId || u.Id) === String(props.userId),
  );
});

// 當 Menu 打開時，去後端抓取該用戶的最新統計數據
const fetchUserInfo = async () => {
  if (isLoading.value) return;
  if (!props.userId) return;
  isLoading.value = true;
  try {
    const timestamp = new Date().getTime();
    const response = await axios.get(
      `/User/${props.userId}/stats?t=${timestamp}`,
    );
    const data = response.data;

    userInfo.value = {
      articleCount: data.articleCount || data.ArticleCount || 0,
      followers: data.followers || data.FollowerCount || 0, // 粉絲數
      following: data.following || data.FollowingCount || 0, // 關注中
    };
  } catch (error) {
    console.error("無法取得用戶預覽資料", error);
  } finally {
    isLoading.value = false;
  }
};

// 處理追蹤/取消
const handleFollowAction = async () => {
  if (isFollowing.value) {
    await userStore.unfollowUser(props.userId);
    // 更新本地顯示數字 (立刻 -1)
    if (userInfo.value.followers > 0) userInfo.value.followers--;
  } else {
    await userStore.followUser(props.userId);
    // 更新本地顯示數字 (立刻 +1)
    userInfo.value.followers++;
  }
};

// 監聽 Menu 開啟狀態
watch(menuOpen, (val) => {
  if (val && !props.userId) {
    // 如果沒有 userId，不要打開菜單
    menuOpen.value = false;
    return;
  }
  if (val) {
    // 每次打開都重新抓一次數據，確保準確，或者你可以加判斷只抓一次
    fetchUserInfo();
  }
});
watch(
  () => props.userId,
  (newId) => {
    if (newId) {
      // userId 改變時，可以選擇重置數據或重新抓取
      // 這裡選擇先重置，等滑鼠移上去再抓(節省流量)，或直接抓取看需求
      userInfo.value = { articleCount: 0, followers: 0, following: 0 };
    }
  },
);
</script>

<template>
  <v-menu
    v-model="menuOpen"
    open-on-hover
    :close-on-content-click="false"
    location="bottom"
    open-delay="200"
    close-delay="300"
  >
    <template v-slot:activator="{ props }">
      <span v-bind="props" class="user-hover-proxy" @click.stop>
        <img
          class="user-avatar-img"
          :src="avatarUrl || 'https://cdn.vuetifyjs.com/images/john.jpg'"
          :style="{ width: `${numericSize}px`, height: `${numericSize}px` }"
          alt=""
        />
        <span>{{ displayName }}</span>
      </span>
    </template>

    <v-card width="300" class="rounded-xl pa-4" elevation="4">
      <div class="d-flex align-center mb-3">
        <v-avatar size="60" class="mr-4 border border-2">
          <v-img
            :src="avatarUrl || 'https://cdn.vuetifyjs.com/images/john.jpg'"
            cover
          ></v-img>
        </v-avatar>
        <div>
          <h3 class="font-weight-bold text-grey-darken-3">
            {{ username }}
          </h3>
        </div>
      </div>

      <v-row no-gutters class="text-center mb-3">
        <v-col cols="4">
          <div class="text-h6 font-weight-bold text-grey-darken-3">
            {{ userInfo.articleCount }}
          </div>
          <div class="text-caption text-grey-darken-1">文章</div>
        </v-col>
        <v-col cols="4">
          <div class="text-h6 font-weight-bold text-grey-darken-3">
            {{ userInfo.followers }}
          </div>
          <div class="text-caption text-grey-darken-1">粉絲</div>
        </v-col>
        <v-col cols="4">
          <div class="text-h6 font-weight-bold text-grey-darken-3">
            {{ userInfo.following }}
          </div>
          <div class="text-caption text-grey-darken-1">關注</div>
        </v-col>
      </v-row>

      <div v-if="!isSelf">
        <v-btn
          block
          rounded="pill"
          size="small"
          :loading="isLoading"
          :variant="isFollowing ? 'tonal' : 'flat'"
          :color="isFollowing ? 'grey-darken-1' : '#145369'"
          :class="isFollowing ? '' : 'text-white'"
          @click="handleFollowAction"
        >
          {{ isFollowing ? "已追蹤" : "追蹤" }}
        </v-btn>
      </div>
      <div v-else class="text-center">
        <v-chip size="small" color="grey" variant="tonal">你自己</v-chip>
      </div>
    </v-card>
  </v-menu>
</template>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}

.user-hover-proxy {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.user-avatar-img {
  border-radius: 999px;
  object-fit: cover;
}

.user-hover-proxy :deep(.v-avatar) {
  position: absolute;
  inset: 0;
  width: 100% !important;
  height: 100% !important;
  opacity: 0;
  cursor: pointer;
}
</style>
