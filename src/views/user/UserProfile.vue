<script setup>
  import { onMounted, onUnmounted } from "vue";
  import axios from "axios";
  import { useRouter } from "vue-router";
  import { useUserStore } from "@/stores/user/userStore";
  import Posts from "@/views/user/Sections/userPosts.vue";
  import Profile from "@/views/user/Sections/UserProfile.vue";
  import UserHeader from "@/views/user/components/UserHeader.vue";
  //登入的彈出視窗
  import { shallowRef } from 'vue'
  const dialog = shallowRef(false)
  const dialog2 = shallowRef(false)
  const dialog3 = shallowRef(false)

  const userStore = useUserStore();
  const router = useRouter();

  onMounted(() => {
    userStore.fetchUserProfile();
  });
</script>

<template>
  <h2>個人首頁</h2>
  <h2>登入畫面</h2>

  <div class="pa-4 text-center">
    <v-btn text="登入" @click="dialog = true"></v-btn>

    <v-dialog v-model="dialog" max-width="480">
      <v-card title="歡迎回來">
        <template v-slot:text>
          <v-btn class="my-2" text="還不是會員" @click="dialog2 = true"></v-btn>
        </template>

        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn text="Close" variant="text" @click="dialog = false"></v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialog2" max-width="240">
      <v-card title="註冊">
        <template v-slot:text>
          <v-btn class="my-2" text="Open Dialog 3" @click="dialog3 = !dialog3"></v-btn>
        </template>

        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn text="Close" variant="text" @click="dialog2 = false"></v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="dialog3" width="auto">
      <v-card title="註冊">
        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn text="註冊" variant="text" @click="dialog3 = false"></v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>