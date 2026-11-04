<script setup>
import { ref, onMounted } from "vue";
import axios from "axios";

const items = ref([]);
const isLoading = ref(false);
const STATIC_BASE_URL = import.meta.env.VITE_BASE_URL; // 後端靜態檔網址 (.env)

const extractFirstImage = (htmlContent) => {
  if (!htmlContent) return null;
  const parser = new DOMParser();
  const doc = parser.parseFromString(htmlContent, "text/html");
  const imgTag = doc.querySelector("img");
  if (!imgTag) return null;
  let src = imgTag.getAttribute("src");
  if (src && !src.startsWith("http")) {
    const cleanPath = src.replace(/^[\.\/]+/, "");
    return `${STATIC_BASE_URL}/${cleanPath}`;
  }
  return src;
};

const stripHtml = (html) => {
  if (!html) return "";
  const doc = new DOMParser().parseFromString(html, "text/html");
  return doc.body.textContent || "";
};
const fetchBookmarkedContents = async () => {
  isLoading.value = true;
  try {
    const response = await axios.get("/contentapi/me/bookmarked-contents");

    const listData = response.data.items || [];

    const processedItems = await Promise.all(
      listData.map(async (post) => {
        let extractedImg = null;
        let displayText = "載入中...";
        let bodyPath = post.body;
        const defaultImage =
          "https://cdn.vuetifyjs.com/images/cards/forest.jpg";

        if (bodyPath && bodyPath.toLowerCase().endsWith(".html")) {
          try {
            const cleanPath = bodyPath.startsWith("/")
              ? bodyPath.substring(1)
              : bodyPath;
            const targetUrl = `${STATIC_BASE_URL}/${cleanPath}`;

            const htmlRes = await axios.get(targetUrl);
            const rawHtml = htmlRes.data;

            extractedImg = extractFirstImage(rawHtml);
            const textContent = stripHtml(rawHtml).trim();

            if (textContent.length > 0) {
              displayText =
                textContent.substring(0, 60) +
                (textContent.length > 60 ? "..." : "");
            } else {
              displayText = "本篇文章僅包含圖片";
            }
          } catch (err) {
            console.warn(`無法讀取收藏文章內容: ${bodyPath}`, err);
            displayText = "無法預覽內容";
          }
        } else {
          displayText = stripHtml(bodyPath || "");
        }

        return {
          id: post.contentId,
          title: post.title,
          type: post.categoryName || "未分類",
          date: post.createdAt ? post.createdAt.split("T")[0] : "",
          img: extractedImg || defaultImage,
          content: displayText,
        };
      }),
    );

    items.value = processedItems;
  } catch (error) {
    console.error("取得收藏列表失敗:", error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchBookmarkedContents();
});
</script>

<template>
  <div class="mb-4 px-4 mt-4">
    <h3 class="text-h6 font-weight-bold text-grey-darken-3">收藏文章</h3>
  </div>

  <v-container class="pa-0 px-4 mb-4" fluid>
    <v-row v-if="isLoading">
      <v-col cols="12" class="text-center py-10">
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
      </v-col>
    </v-row>

    <v-row v-else-if="items.length === 0">
      <v-col cols="12" class="text-center text-grey py-10">
        您還沒有收藏任何文章
      </v-col>
    </v-row>

    <v-row v-else>
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
          <v-img :src="item.img" height="200" cover class="rounded-t-lg">
            <template v-slot:error>
              <v-img
                src="https://cdn.vuetifyjs.com/images/cards/forest.jpg"
                cover
              ></v-img>
            </template>

            <div class="d-flex justify-end pa-2">
              <v-btn
                icon="mdi-bookmark"
                color="primary"
                variant="flat"
                size="small"
                title="已收藏"
              ></v-btn>
            </div>
          </v-img>

          <v-card-item class="pt-3 flex-grow-1">
            <div class="text-overline text-primary mb-1">
              {{ item.category }}
            </div>
            <div class="text-h6 font-weight-bold mb-1" style="line-height: 1.4">
              {{ item.title }}
            </div>
            <div class="text-caption text-grey mb-3">{{ item.date }}</div>
            <div class="text-body-2 text-medium-emphasis text-truncate-2">
              {{ item.content }}
            </div>
          </v-card-item>

          <v-spacer></v-spacer>

          <v-divider class="mx-4 mb-2"></v-divider>

          <v-card-actions class="px-2 pb-3">
            <v-btn
              prepend-icon="mdi-share-variant-outline"
              variant="text"
              color="grey-darken-1"
              size="small"
              @click.prevent=""
            >
              分享
            </v-btn>
            <v-spacer></v-spacer>
            <router-link :to="`/forum/${item.id}`">
              <v-btn
                variant="flat"
                color="primary"
                size="small"
                class="px-4"
                rounded="pill"
              >
                查看
              </v-btn>
            </router-link>
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
