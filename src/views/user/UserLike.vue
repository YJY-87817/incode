<template>
  <h3>點讚內容</h3>
  <v-container max-width="912">
    <v-row>
      <v-col v-for="(item, index) in items" :key="index" cols="12" md="4">
        <v-card class="mx-auto" max-width="344">
          <v-img height="200px" :src="item.img" cover></v-img>
          <v-card-title> {{ item.title }} </v-card-title>
          <v-card-subtitle> {{ item.content }} </v-card-subtitle>
          <v-card-actions>
            <v-btn color="orange-lighten-2" text="看更多"></v-btn>
            <v-spacer></v-spacer>
            <v-btn
              :icon="item.show ? 'mdi-chevron-up' : 'mdi-chevron-down'"
              @click="item.show = !item.show"
            ></v-btn>
          </v-card-actions>
          <v-expand-transition>
            <div v-show="item.show">
              <v-divider></v-divider>
              <v-card-text> {{ item.details }} </v-card-text>
            </div>
          </v-expand-transition>
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
    content: "內容A",
    details: "第一段細節...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
    show: false,
  },
  {
    title: "文章標題2",
    content: "內容B",
    details: "第二段細節...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
    show: false,
  },
  {
    title: "文章標題3",
    content: "內容C",
    details: "第三段細節...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
    show: false,
  },
]);
</script>
