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
import { useUserStore } from "@/stores/user/user.js";

const props = defineProps({
  userId: { type: [Number, String], required: true },
  initialTab: { type: String, default: "following" },
});

const isOpen = defineModel("modelValue");
const userStore = useUserStore();
const { followingList, followersList, isLoading } = storeToRefs(userStore);
const { fetchFollowingList, fetchFollowersList, unfollowUser } = userStore;
const currentTab = ref(props.initialTab);

const displayList = computed(() => {
  if (currentTab.value === "following") {
    return followingList.value;
  } else {
    return followersList.value;
  }
});

watch([isOpen, currentTab], async ([newOpen, newTab]) => {
  if (newOpen) {
    if (newTab !== currentTab.value) currentTab.value = newTab;
    await fetchList();
  }
});

const fetchList = async () => {
  // 這裡假設 props.userId 是「自己」。
  // 如果 Store 的 API 支援查別人的，可以把 props.userId 傳進去
  if (currentTab.value === "following") {
    await fetchFollowingList();
  } else {
    await fetchFollowersList();
  }
};

const handleUnfollow = async (user) => {
  //if (!confirm(`確定要取消關注 ${user.username || "這位用戶"} 嗎？`)) return;
  // 根據您的 Store 寫法，這裡需要傳入 targetId
  // 請確認 API 回傳的欄位名稱，通常是 user.id 或 user.followeeId
  const targetId = user.id || user.followeeId;
  if (targetId) {
    await unfollowUser(targetId);
  }
};
</script>
