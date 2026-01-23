<script setup>
  import { ref } from "vue";

  const items = ref([
    {
      id: 1,
      title: "2024 攝影器材評測大賞",
      type: "器材",
      date: "2024-06-01",
      content: "針對今年度發布的無反光鏡相機進行深度解析與實拍對比...",
      img: "https://cdn.vuetifyjs.com/images/cards/docks.jpg",
    },
    {
      id: 2,
      title: "人像修圖技巧分享：Lightroom",
      type: "教學",
      date: "2024-05-29",
      content: "如何調出日系清新的色調？五個步驟教你快速上手。",
      img: "https://picsum.photos/id/106/500/300",
    },
    {
      id: 3,
      title: "週末去哪拍？台北秘境清單",
      type: "旅遊",
      date: "2024-05-25",
      content: "避開人潮，探索城市裡的廢墟與老街，適合底片攝影。",
      img: "https://picsum.photos/id/111/500/300",
    },
  ]);
</script>

<template>
  <div class="mb-4">
    <h3 class="text-h6 font-weight-bold text-grey-darken-3">收藏文章</h3>
  </div>

  <v-container class="pa-0" fluid>
    <v-row>
      <v-col v-for="(item, index) in items" :key="item.id" cols="12" sm="6" md="4">
        <v-card class="h-100 d-flex flex-column rounded-lg" border flat hover elevation="0">
          <v-img :src="item.img" height="200" cover class="rounded-t-lg">
            <div class="d-flex justify-end pa-2">
              <v-btn icon="mdi-bookmark" color="white" variant="text" class="bg-black-transparent"></v-btn>
            </div>
          </v-img>

          <v-card-item class="pt-3">
            <div class="text-overline text-primary mb-1">{{ item.type }}</div>
            <div class="text-h6 font-weight-bold mb-1" style="line-height: 1.4;">
              {{ item.title }}
            </div>
            <div class="text-caption text-grey mb-3">{{ item.date }}</div>
            <div class="text-body-2 text-medium-emphasis text-truncate-2">
              {{ item.content }}
            </div>
          </v-card-item>

          <v-spacer></v-spacer>

          <v-divider class="mx-4 mb-2"></v-divider>

          <v-card-actions class="px-2">
            <v-btn prepend-icon="mdi-share-variant-outline" variant="text" color="grey-darken-1" size="small">
              分享
            </v-btn>
            <v-spacer></v-spacer>
            <v-btn variant="flat" color="primary" size="small" class="px-4" rounded="pill">
              查看
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<style scoped>
  .bg-black-transparent {
    background-color: rgba(0, 0, 0, 0.3);
    border-radius: 50%;
  }

  .text-truncate-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    text-overflow: ellipsis;
  }
</style>