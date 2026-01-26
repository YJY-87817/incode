<script>
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

export default {
  components: {
    DefaultNavbar,
    DefaultFooter,
  },
};
</script>

<template>
  <DefaultNavbar light />

  <section class="forum-list container py-3 py-md-5">
    <ForumSearchBar @search="onSearch" />
    <div class="row g-3 g-md-4 mt-2 mt-md-4">
      <div
        class="col-12 col-sm-6 col-lg-4"
        v-for="topic in paginatedTopics"
        :key="topic.id"
      >
        <div class="card h-100 shadow-sm">
          <div class="card-body d-flex flex-column p-3">
            <h5 class="card-title fs-6 fs-md-5 text-break">
              {{ topic.title }}
            </h5>
            <p class="card-text text-truncate flex-grow-1 small">
              {{ topic.excerpt }}
            </p>
            <div
              v-if="topic.imageUrls && topic.imageUrls.length"
              class="d-flex flex-wrap gap-2 mt-2 mb-2 topic-image-list"
              style="max-height: 150px; overflow: hidden"
            >
              <img
                v-for="(url, idx) in topic.imageUrls.slice(0, 3)"
                :key="url + idx"
                :src="url"
                alt="貼文圖片"
                class="topic-image-thumb"
                loading="lazy"
                style="
                  max-width: 100px;
                  max-height: 100px;
                  object-fit: cover;
                  border-radius: 4px;
                "
              />
              <div
                v-if="topic.imageUrls.length > 3"
                class="d-flex align-items-center justify-content-center text-muted small"
                style="
                  width: 100px;
                  height: 100px;
                  background-color: #f0f0f0;
                  border-radius: 4px;
                "
              >
                +{{ topic.imageUrls.length - 3 }} 張
              </div>
            </div>
            <div
              class="mt-3 d-flex flex-column flex-sm-row justify-content-between align-items-start align-items-sm-center gap-2"
            >
              <small class="text-muted text-truncate" style="max-width: 100%"
                >by {{ topic.author }}</small
              >
              <router-link
                :to="{ name: 'thread-detail', params: { id: topic.id } }"
                class="btn btn-sm btn-forum-primary w-100 w-sm-auto"
                >檢視</router-link
              >
            </div>
          </div>
        </div>
      </div>
    </div>

    <nav v-if="totalPages > 1" class="mt-3 mt-md-4">
      <ul
        class="pagination pagination-sm justify-content-center flex-wrap gap-1"
      >
        <li
          class="page-item"
          :class="{ disabled: currentPage === 1 }"
          @click="prevPage"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">
            <span class="d-none d-sm-inline"
              ><i class="fa-solid fa-caret-left"></i
            ></span>
            <span class="d-inline d-sm-none"
              ><i class="fa-solid fa-caret-left"></i
            ></span>
          </a>
        </li>
        <li
          class="page-item"
          v-for="page in totalPages"
          :key="page"
          :class="{ active: currentPage === page }"
          @click="goToPage(page)"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">{{
            page
          }}</a>
        </li>
        <li
          class="page-item"
          :class="{ disabled: currentPage === totalPages }"
          @click="nextPage"
        >
          <a class="page-link px-2 px-md-3" href="javascript:void(0)">
            <span class="d-none d-sm-inline"
              ><i class="fa-solid fa-caret-right"></i
            ></span>
            <span class="d-inline d-sm-none"
              ><i class="fa-solid fa-caret-right"></i
            ></span>
          </a>
        </li>
      </ul>
    </nav>
  </section>

  <DefaultFooter />
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import ForumSearchBar from "@/components/Contents/ForumSearchBar.vue";
import apiClient from "@/api/request.js";

// 取得不含 /api 的基底網址
const apiBaseUrl = (apiClient.defaults?.baseURL || "").replace(/\/api$/, "");

const toAbsoluteUrl = (url) => {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;
  if (url.startsWith("/")) return `${apiBaseUrl}${url}`;
  return `${apiBaseUrl}/${url}`;
};

// 從內容中提取圖片
const parseBodyContent = (body) => {
  const raw = body || "";
  // 改進的正則：匹配完整的 http/https 圖片 URL
  const imagePattern = /https?:\/\/[^\s()[\]]+\.(?:png|jpe?g|gif|webp|bmp)/gi;
  const matches = raw.match(imagePattern) || [];
  const uniqueImages = Array.from(new Set(matches));

  const imageUrls = uniqueImages
    .map((url) => url.trim())
    .filter(Boolean)
    .map(toAbsoluteUrl);

  const textContent = raw
    .replace(/https?:\/\/[^\s()[\]]+\.(?:png|jpe?g|gif|webp|bmp)/gi, "")
    .replace(/\n{2,}/g, "\n")
    .trim();
  const displayText = textContent || (imageUrls.length ? "(已附圖片)" : "");

  return {
    textContent: displayText,
    imageUrls,
  };
};

const topics = ref([]);

const fetchTopics = async () => {
  try {
    const data = await apiClient.get("/ContentApi");
    topics.value = data.items.map((item) => {
      const { textContent, imageUrls } = parseBodyContent(item.body);
      return {
        id: item.contentId,
        title: item.title,
        excerpt: textContent,
        imageUrls: imageUrls,
        author: item.username,
      };
    });
  } catch (error) {
    console.error("Failed to fetch topics:", error);
  }
};

onMounted(() => {
  fetchTopics();
});

const searchQuery = ref("");

const filteredTopics = computed(() => {
  if (!searchQuery.value) return topics.value;
  const q = searchQuery.value.toLowerCase();
  return topics.value.filter(
    (t) =>
      t.title.toLowerCase().includes(q) ||
      t.excerpt.toLowerCase().includes(q) ||
      t.author.toLowerCase().includes(q)
  );
});

const itemsPerPage = 9;
const currentPage = ref(1);
const totalPages = computed(() =>
  Math.ceil(filteredTopics.value.length / itemsPerPage)
);

const paginatedTopics = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage;
  return filteredTopics.value.slice(start, start + itemsPerPage);
});

function onSearch(query) {
  searchQuery.value = query;
  currentPage.value = 1;
}

function goToPage(page) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page;
  }
}
function prevPage() {
  if (currentPage.value > 1) currentPage.value--;
}
function nextPage() {
  if (currentPage.value < totalPages.value) currentPage.value++;
}
</script>

<style scoped>
/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
  text-decoration: none;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #432f28;
  text-decoration: none;
}

.forum-list .card {
  transition: transform 0.2s;
  border-color: #d1d3d4;
}

.forum-list .card:hover {
  transform: translateY(-3px);
  box-shadow: 0 4px 12px rgba(134, 176, 189, 0.3);
}

.forum-list .card-title {
  color: #432f28;
}

.topic-image-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.topic-image-thumb {
  cursor: pointer;
  transition: transform 0.2s ease;
  border: 1px solid #d1d3d4;
}

.topic-image-thumb:hover {
  transform: scale(1.05);
}

/* 分頁樣式 */
.pagination .page-item.active .page-link {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
}

.pagination .page-link {
  color: #432f28;
  border-color: #d1d3d4;
}

.pagination .page-link:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}
</style>
