<script setup>
import { ref } from "vue";

const items = ref([
  {
    id: 1,
    title: "為什麼底片相機又流行起來了？",
    content: "從色彩科學到懷舊情懷，探討底片復興的原因。",
    details:
      "隨著數位攝影的普及，底片的不可預測性與獨特的顆粒感反而成為了年輕一代追求的風格。這篇文章詳細比較了 Kodak 與 Fujifilm 的經典底片特色...",
    img: "https://picsum.photos/id/250/500/300",
    show: false,
  },
  {
    id: 2,
    title: "新手入門：構圖的三分法與引導線",
    content: "攝影基礎教學，讓你的照片瞬間升級。",
    details:
      "三分法是最基礎但也最實用的構圖法則。將畫面分割成九宮格，將主體放在交叉點上，能讓畫面更平衡...",
    img: "https://picsum.photos/id/338/500/300",
    show: false,
  },
  {
    id: 3,
    title: "推薦 5 款適合掃街的輕便相機",
    content: "Ricoh GR III, Fujifilm X100V 等實機體驗。",
    details:
      "街頭攝影講求速度與隱蔽性，這五款相機不僅體積小巧，開機速度快，更擁有優秀的畫質表現...",
    img: "https://picsum.photos/id/435/500/300",
    show: false,
  },
]);
</script>

<template>
  <div class="mt-4 mb-4 px-4">
    <h3 class="text-h6 font-weight-bold text-grey-darken-3">點讚內容</h3>
  </div>

  <v-container class="pa-0 px-4" max-width="900">
    <v-row>
      <v-col v-for="item in items" :key="item.id" cols="12">
        <v-card class="mb-3 rounded-lg" border flat elevation="0">
          <div class="d-flex flex-column flex-sm-row">
            <v-img
              :src="item.img"
              width="100%"
              max-width="200"
              height="140"
              cover
              class="flex-shrink-0 d-none d-sm-block rounded-s-lg"
            ></v-img>

            <v-img
              :src="item.img"
              height="180"
              cover
              class="d-sm-none rounded-t-lg"
            ></v-img>

            <div class="flex-grow-1 d-flex flex-column">
              <v-card-item>
                <v-card-title class="text-body-1 font-weight-bold pt-0">
                  {{ item.title }}
                </v-card-title>
                <v-card-subtitle class="text-body-2 mt-1">
                  {{ item.content }}
                </v-card-subtitle>
              </v-card-item>

              <v-spacer></v-spacer>

              <v-card-actions class="px-4 pb-3">
                <v-btn
                  color="primary"
                  variant="text"
                  size="small"
                  @click="item.show = !item.show"
                >
                  {{ item.show ? "收起詳情" : "閱讀更多" }}
                </v-btn>
                <v-spacer></v-spacer>
                <v-btn
                  :icon="item.show ? 'mdi-chevron-up' : 'mdi-chevron-down'"
                  variant="text"
                  color="grey"
                  density="comfortable"
                  @click="item.show = !item.show"
                ></v-btn>
              </v-card-actions>
            </div>
          </div>

          <v-expand-transition>
            <div v-show="item.show" class="bg-grey-lighten-5">
              <v-divider></v-divider>
              <v-card-text class="text-body-2 text-grey-darken-2 pa-6">
                {{ item.details }}
              </v-card-text>
            </div>
          </v-expand-transition>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
