<script setup>
import { onMounted, onUnmounted } from "vue";
import axios from "axios";
import { useRouter } from "vue-router";
import { useUserStore } from "@/stores/user/userStore";
import Posts from "@/views/user/Sections/userPosts.vue";
import Profile from "@/views/user/Sections/UserProfile.vue";
import UserHeader from "@/views/user/components/UserHeader.vue";

const userStore = useUserStore();
const router = useRouter();

onMounted(() => {
  userStore.fetchUserProfile();
});
</script>

<template>
  <Profile />
  <br />
  <h3>{{ userStore.userProfile.name }}</h3>

  <UserHeader :image="userStore.displayAvatar" />

  <span>粉絲數: userStore.userProfile.followers </span>

  <!-- 關注清單 -->
  <div class="row mt-4" v-if="followingList && followingList.length > 0">
    <div class="col-md-8 col-12 mx-auto">
      <h5>關注列表詳情：({{ followingCount }}人)</h5>
      <ul class="list-group">
        <li
          v-for="user in followingList"
          :key="user.userId"
          class="list-group-item d-flex justify-content-between align-items-center"
        >
          <button
            class="btn btn-sm btn-outline-danger mb-0"
            @click="unfollow(user.userId)"
          >
            取消關注
          </button>
        </li>
      </ul>
    </div>
  </div>

  <!-- 讀取中 
  <div class="row mt-4" v-else-if="isLoading">
    <div class="col-12 text-center">
      <p>載入中...</p>
    </div>
  </div>
  <div class="row mt-4" v-else-if="!isLoading">
    <div class="col-12 text-center text-muted">
      <p>目前沒有關注任何人</p>
    </div>
  </div>-->
  <div class="card card-body blur shadow-blur mx-3 mx-md-4 mt-n6 mb-4">
    <Posts />
  </div>
</template>
