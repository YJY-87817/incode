<template>
  <v-dialog v-model="isOpen" max-width="500" scrollable>
    <v-card class="rounded-lg">
      <v-card-title class="pa-0">
        <v-tabs v-model="currentTab" grow color="primary">
          <v-tab value="following">關注中</v-tab>
          <v-tab value="fans">粉絲</v-tab>
        </v-tabs>
      </v-card-title>

      <v-divider></v-divider>

      <v-card-text style="height: 400px; overflow-y: auto" class="pa-0">
        <div v-if="loading" class="d-flex justify-center align-center h-100">
          <v-progress-circular
            indeterminate
            color="#86b0bd"
          ></v-progress-circular>
        </div>

        <v-list v-else lines="two">
          <template
            v-for="(user, index) in displayList"
            :key="user.id || index"
          >
            <v-list-item>
              <template v-slot:prepend>
                <v-avatar
                  size="48"
                  :image="
                    user.imageUrl || 'https://cdn.vuetifyjs.com/images/john.jpg'
                  "
                ></v-avatar>
              </template>

              <v-list-item-title class="font-weight-bold">
                {{ user.username || user.name || "未知用戶" }}
              </v-list-item-title>

              <template v-slot:append>
                <v-btn
                  v-if="currentTab === 'following'"
                  color="grey-lighten-2"
                  variant="flat"
                  size="small"
                  class="text-capitalize"
                  @click="handleUnfollow(user)"
                >
                  取消關注
                </v-btn>
              </template>
            </v-list-item>

            <v-divider v-if="index < displayList.length - 1" inset></v-divider>
          </template>

          <div
            v-if="displayList.length === 0"
            class="text-center pa-5 text-grey"
          >
            {{
              currentTab === "following"
                ? "您還沒有關注任何人"
                : "目前還沒有粉絲"
            }}
          </div>
        </v-list>
      </v-card-text>

      <v-divider></v-divider>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-hover v-slot="{ isHovering, props }">
          <v-btn
            v-bind="props"
            size="large"
            block
            rounded="lg"
            variant="flat"
            class="text-body-1 font-weight-bold mb-4"
            :loading="isLoading"
            :color="isHovering ? '#FFFFFF' : '#145369'"
            :style="{
              color: isHovering ? '#145369 !important' : '#FFFFFF !important',
              border: '1px solid #145369',
            }"
            @click="isOpen = false"
            >關閉</v-btn
          >
        </v-hover>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, watch, computed } from "vue";
import { storeToRefs } from "pinia";
import { useUserStore } from "@/stores/users/user.js";
import { useAuthStore } from "@/stores/users/auth.js";

const props = defineProps({
  userId: { type: [Number, String], required: true },
  initialTab: { type: String, default: "following" },
});

const isOpen = defineModel("modelValue");
const userStore = useUserStore();
const { followingList, followersList, isLoading } = storeToRefs(userStore);
const { fetchFollowingList, fetchFollowersList, unfollowUser } = userStore;
const currentTab = ref(props.initialTab);

const authStore = useAuthStore();
const isMyProfile = computed(
  () => String(props.userId) === String(authStore.user?.id),
);

const fetchList = async () => {
  // 無論是誰，都使用「有 ID 參數」的 API 來抓取，這樣最統一
  if (currentTab.value === "following") {
    // 傳入 props.userId
    await userStore.fetchUserFollowingList(props.userId);
  } else {
    // 傳入 props.userId
    await userStore.fetchUserFollowersList(props.userId);
  }
};

const displayList = computed(() => {
  if (currentTab.value === "following") {
    return userStore.profileFollowingList;
  } else {
    return userStore.profileFollowersList;
  }
});

const handleUnfollow = async (user) => {
  // 後端統一回傳 id，所以直接取 user.id
  if (user.id) {
    await userStore.unfollowUser(user.id);
    // 取消成功後，重新撈一次清單更新畫面
    await fetchList();
  }
};

watch([isOpen, currentTab], async ([newOpen, newTab]) => {
  if (newOpen) {
    if (newTab !== currentTab.value) currentTab.value = newTab;
    await fetchList();
  }
});
</script>
