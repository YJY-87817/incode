<template>
  <h3>收藏文章</h3>
  <v-container max-width="912" class="w-100">
    <v-row>
      <v-col v-for="(item, index) in items" :key="index" cols="12" md="4">
        <v-card class="mb-4" hover elevation="2">
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
          <v-card-text class="flex-grow-1"> {{ item.content }} </v-card-text>
          <v-card-actions>
            <v-btn color="orange" text="分享"></v-btn>
            <v-btn color="orange" text="查看"></v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref } from "vue";
// 內容掛api
const items = ref([
  {
    title: "文章標題1",
    type: "風景",
    date: "2024-06-01",
    content: "內容A",
    img: "https://cdn.vuetifyjs.com/images/cards/docks.jpg",
    show: false,
  },
  {
    title: "文章標題2",
    type: "人像",
    date: "2024-06-01",
    content: "內容B",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
    show: false,
  },
  {
    title: "文章標題3",
    type: "靜物",
    content: "內容C",
    date: "2024-06-01",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
    show: false,
  },
]);
</script>
