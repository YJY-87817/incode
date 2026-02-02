<script setup>
import { ref } from "vue";

// 模擬 API 資料
const items = ref([
  {
    id: 1,
    title: "夏日午後的海岸線",
    type: "風景",
    date: "2024-06-01",
    content: "這是在墾丁拍攝的一組照片，光線非常完美，展現了大海的層次感...",
    img: "https://cdn.vuetifyjs.com/images/cards/sunshine.jpg",
  },
  {
    id: 2,
    title: "城市角落的人像攝影",
    type: "人像",
    date: "2024-05-28",
    content: "利用自然光與建築陰影的對比，捕捉模特兒最自然的神情。",
    img: "https://picsum.photos/id/1011/500/300",
  },
  {
    id: 3,
    title: "極簡主義建築美學",
    type: "建築",
    date: "2024-05-20",
    content: "探索清水模建築的幾何線條，黑白攝影的表現手法。",
    img: "https://picsum.photos/id/1025/500/300",
  },
  {
    id: 4,
    title: "街頭貓咪日記",
    type: "動物",
    date: "2024-05-15",
    content: "紀錄巷弄裡遇見的每一隻浪浪，牠們眼中的世界。",
    img: "https://picsum.photos/id/1074/500/300",
  },
]);
</script>

<template>
  <div class="d-flex justify-space-between align-center mb-4 mt-4 px-4">
    <h3 class="text-h6 font-weight-bold text-grey-darken-3 mb-0">我的文章</h3>
    <v-btn
      variant="text"
      color="primary"
      size="small"
      append-icon="mdi-arrow-right"
      >查看全部</v-btn
    >
  </div>

  <v-container class="pa-0 px-4 mb-4" fluid>
    <v-row>
      <v-col
        v-for="(item, index) in items"
        :key="item.id"
        cols="12"
        sm="6"
        md="4"
      >
        <v-card
          class="h-100 d-flex flex-column rounded-lg"
          border
          flat
          hover
          elevation="0"
        >
          <v-img
            :src="item.img"
            height="200"
            cover
            class="rounded-t-lg align-end"
            gradient="to bottom, rgba(0,0,0,0) 60%, rgba(0,0,0,0.6) 100%"
          >
            <v-card-title
              class="text-white text-subtitle-1 font-weight-bold px-4 pb-2"
            >
              {{ item.title }}
            </v-card-title>
          </v-img>

          <v-card-item class="pt-3 pb-2">
            <div class="d-flex align-center justify-space-between mb-2">
              <v-chip
                size="x-small"
                color="primary"
                variant="tonal"
                class="font-weight-bold"
              >
                {{ item.type }}
              </v-chip>
              <span class="text-caption text-grey">
                <v-icon
                  icon="mdi-calendar-blank"
                  size="small"
                  start
                  class="mr-1"
                ></v-icon>
                {{ item.date }}
              </span>
            </div>

            <div class="text-body-2 text-medium-emphasis text-truncate-2">
              {{ item.content }}
            </div>
          </v-card-item>

          <v-spacer></v-spacer>

          <v-card-actions class="px-4 pb-4 pt-0">
            <v-btn
              variant="text"
              color="grey-darken-1"
              class="px-0"
              size="small"
            >
              繼續閱讀
            </v-btn>
            <v-spacer></v-spacer>
            <v-btn
              icon="mdi-pencil-outline"
              size="small"
              variant="text"
              color="grey"
            ></v-btn>
            <v-btn
              icon="mdi-delete-outline"
              size="small"
              variant="text"
              color="grey"
            ></v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<style scoped>
/* 限制文字行數 CSS */
.text-truncate-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
