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

  // 如果是相對路徑 (例如 /img/a.jpg)，補上後端網址
  if (src && !src.startsWith("http")) {
    const cleanPath = src.replace(/^[\.\/]+/, "");
    return `${STATIC_BASE_URL}/${cleanPath}`;
  }
  return src;
};

// B. 轉文字：去除 HTML 標籤
const stripHtml = (html) => {
  if (!html) return "";
  const doc = new DOMParser().parseFromString(html, "text/html");
  return doc.body.textContent || "";
};

// ==========================================
// 主邏輯：取得點讚內容
// ==========================================
const fetchLikedContents = async () => {
  isLoading.value = true;
  try {
    // 1. 呼叫 API
    const response = await axios.get("/contentapi/me/liked-contents");
    const listData = response.data.items || [];

    // 2. 二次請求：下載 HTML 並解析
    const processedItems = await Promise.all(
      listData.map(async (post) => {
        let extractedImg = null;
        let summaryText = "載入中...";
        let fullText = "";
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
            fullText = stripHtml(rawHtml).trim();

            // 產生摘要 (給 subtitle 用)
            if (fullText.length > 0) {
              summaryText = fullText.substring(0, 50) + "...";
            } else {
              summaryText = "本篇文章僅包含圖片";
            }
          } catch (err) {
            console.warn(`無法讀取內容: ${bodyPath}`, err);
            summaryText = "無法預覽內容";
            fullText = "內容載入失敗";
          }
        } else {
          fullText = stripHtml(bodyPath || "");
          summaryText = fullText.substring(0, 50);
        }

        return {
          id: post.contentId,
          title: post.title,
          img: extractedImg || defaultImage,
          content: summaryText, // 簡短摘要 (Card Subtitle)
          details: fullText, // 完整純文字 (展開後顯示)
          show: false, // 控制展開/收合的開關 (預設關閉)
        };
      }),
    );

    items.value = processedItems;
  } catch (error) {
    console.error("取得點讚列表失敗:", error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchLikedContents();
});
</script>

<template>
  <v-container class="pa-0 px-4" max-width="900">
    <div class="mt-4 mb-4 px-4">
      <h3 class="text-h6 font-weight-bold text-grey-darken-3">點讚內容</h3>
    </div>
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
        您還沒有對任何文章按讚
      </v-col>
    </v-row>

    <v-row v-else>
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
            >
              <template v-slot:error>
                <v-img
                  src="https://cdn.vuetifyjs.com/images/cards/forest.jpg"
                  cover
                ></v-img>
              </template>
            </v-img>

            <v-img
              :src="item.img"
              height="180"
              cover
              class="d-sm-none rounded-t-lg"
            >
              <template v-slot:error>
                <v-img
                  src="https://cdn.vuetifyjs.com/images/cards/forest.jpg"
                  cover
                ></v-img>
              </template>
            </v-img>

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
