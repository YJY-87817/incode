<template>
  <h3>我的文章</h3>
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
  <!-- 如果文章被刪除
   <v-card
  class="mx-auto"
  max-width="344"
  subtitle="The card stays disabled"
  title="Disabled card"
  disabled
  link
></v-card> -->
</template>

<script setup>
import { ref } from "vue";

// 內容掛api
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
</script>
