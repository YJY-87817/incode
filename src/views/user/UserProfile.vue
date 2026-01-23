<script setup>
import { ref, onMounted } from "vue";
import axios from "axios";
import { useRouter } from "vue-router";
import { useUserStore } from "@/stores/user/userStore";

import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterCentered.vue";
const userStore = useUserStore();
const router = useRouter();

const items = ref([
  {
    title: "電影 1",
    type: "風景",
    date: "2024-06-01",
    content: "這是第一張卡片的介紹...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
  },
  {
    title: "電影 2",
    type: "人像",
    date: "2024-06-01",
    content: "這是第二張卡片的介紹...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
  },
  {
    title: "電影 3",
    type: "靜物",
    date: "2024-06-01",
    content: "這是第三張卡片的介紹...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
  },
  {
    title: "電影 4",
    type: "建築",
    date: "2024-06-01",
    content: "這是第四張卡片的介紹...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
  },
]);

onMounted(() => {
  userStore.fetchUserProfile();
});
</script>

<template>
  <DefaultNavbar
    :action="{
      route: 'javascript:;',
      label: 'Buy Now',
      color: 'btn-white',
    }"
  />
  <header class="bg-gradient-dark"></header>
  <div class="container user-index-page mt-5" style="min-height: 30vh">
    <h1 class="mb-4 display-4 font-weight-bold text-center">個人首頁</h1>
    <v-row class="align-center no-gutters">
      <!-- 帶該會員名稱 -->
      <v-avatar size="100">
        <v-img
          alt="John"
          src="https://cdn.vuetifyjs.com/images/john.jpg"
        ></v-img>
      </v-avatar>
      <h3 class="ml-4">XX</h3>
    </v-row>
    <v-row class="align-end no-gutters mt-5">
      <p class="mb-0 text-h6">自我介紹</p>
      <v-spacer></v-spacer>
      <v-btn class="ml-4"> 追蹤 </v-btn>
    </v-row>
  </div>
  <div class="container user-index-page mt-3" style="min-height: 80vh">
    <hr class="horizontal dark my-4" />
    <h2 class="mt-5">XX的討論區</h2>
    <!-- 帶該會員名稱 -->
    <v-container max-width="912">
      <v-row>
        <v-col v-for="(item, index) in items" :key="index" cols="12" md="4">
          <v-card class="mb-4" hover>
            <v-img height="200px" :src="item.img" cover> </v-img>
            <v-card-item>
              <v-card-title>
                {{ item.title }}
              </v-card-title>
              <div class="d-flex justify-space-between align-center">
                <span class="text-subtitle-2 font-weight-bold mr-2">
                  {{ item.type }}
                </span>
                <span class="text-caption text-grey">
                  {{ item.date }}
                </span>
              </div>
            </v-card-item>
            <v-card-text> {{ item.content }} </v-card-text>
            <!-- 跳轉到該文章 <v-card-actions>
            <v-btn text color="primary">查看文章</v-btn>-->
          </v-card>
        </v-col>
      </v-row>
    </v-container>
    <v-container class="d-flex flex-column gr-4">
      <v-divider
        color="primary"
        content-offset="-16"
        opacity="1"
        style="color: inherit"
        thickness="1"
      >
        <v-avatar
          class="border border-primary border-opacity-100"
          icon="mdi-chevron-down"
          size="36"
        ></v-avatar>
      </v-divider>
    </v-container>
  </div>
  <DefaultFooter />
</template>
