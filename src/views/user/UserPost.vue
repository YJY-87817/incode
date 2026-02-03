<script setup>
import { ref, onMounted } from "vue";
import axios from "axios";

const items = ref([]);
const isLoading = ref(false);

const extractFirstImage = (htmlContent) => {
  if (!htmlContent) return null;
  // 1. 使用 DOMParser 解析 HTML 字串
  const parser = new DOMParser();
  const doc = parser.parseFromString(htmlContent, "text/html");
  // 2. 尋找第一個 <img> 標籤
  const imgTag = doc.querySelector("img");
  // 3. 如果有找到圖片，回傳 src；沒有則回傳 null
  return imgTag ? imgTag.src : null;
};

// 輔助函式：去除 HTML 標籤取得純文字 (用來顯示摘要)
const stripHtml = (html) => {
  if (!html) return "";
  const doc = new DOMParser().parseFromString(html, "text/html");
  return doc.body.textContent || "";
};

const fetchMyPosts = async () => {
  isLoading.value = true;
  try {
    const response = await axios.get("/contentapi/me/posts");
    const listData = response.data.items || [];
    const STATIC_BASE_URL = "https://localhost:7190";
    // 使用 Promise.all 等待所有文章內容都下載完
    const processedItems = await Promise.all(
      listData.map(async (post) => {
        let rawHtml = "";
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

            console.log("嘗試下載:", targetUrl);

            const htmlRes = await axios.get(targetUrl);
            const rawHtml = htmlRes.data;

            extractedImg = extractFirstImage(rawHtml);
            const textContent = stripHtml(rawHtml).trim();

            if (textContent.length > 0) {
              displayText = textContent.substring(0, 60) + "...";
            } else {
              displayText = "本篇文章僅包含圖片";
            }

            // 修正圖片路徑 (如果圖片也是相對路徑)
            if (extractedImg && !extractedImg.startsWith("http")) {
              const cleanImgPath = extractedImg.startsWith("/")
                ? extractedImg.substring(1)
                : extractedImg;
              extractedImg = `${STATIC_BASE_URL}/${cleanImgPath}`;
            }
          } catch (err) {
            console.error(`下載失敗: ${targetUrl}`, err);
            displayText = "無法讀取內容";
          }
        } else {
          displayText = stripHtml(bodyPath || "");
        }

        return {
          id: post.contentId,
          title: post.title,
          category: post.categoryName,
          date: post.createdAt ? post.createdAt.split("T")[0] : "",
          img: extractedImg || defaultImage,
          content: displayText,
        };
      }),
    );

    items.value = processedItems;
  } catch (error) {
    console.error("列表取得失敗:", error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchMyPosts();
});
</script>

<template>
  <div class="d-flex justify-space-between align-center mb-4 mt-4 px-4">
    <h3 class="text-h6 font-weight-bold text-grey-darken-3 mb-0">我的文章</h3>
  </div>

  <v-container class="pa-0 px-4 mb-4" fluid>
    <v-row v-if="isLoading">
      <v-col cols="12" class="text-center py-10">
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
        <div class="mt-2 text-grey">載入文章中...</div>
      </v-col>
    </v-row>
    <v-row v-else-if="items.length === 0">
      <v-col cols="12" class="text-center py-10 text-grey">
        目前還沒有發佈任何文章
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
          <v-img
            :src="item.img"
            height="200"
            cover
            class="rounded-t-lg align-end"
            gradient="to bottom, rgba(0,0,0,0) 60%, rgba(0,0,0,0.6) 100%"
          >
            <template v-slot:error>
              <v-img
                src="https://cdn.vuetifyjs.com/images/cards/forest.jpg"
                cover
              ></v-img>
            </template>
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
                {{ item.category }}
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
            <router-link :to="`/forum/${item.id}`">
              <v-btn
                variant="text"
                color="grey-darken-1"
                class="px-0"
                size="small"
              >
                繼續閱讀
              </v-btn>
            </router-link>
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
