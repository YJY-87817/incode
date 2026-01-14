<script setup>
import { reactive, onMounted, watch } from "vue";
import axios from "axios";
import { useForumStore } from "@/stores/forumStore";

const apiBaseUrl = "https://localhost:7190/api";
const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

const forumStore = useForumStore();

const newTopicState = reactive({
  title: "",
  body: "",
  categoryId: null,
  parentContentId: null,
  categories: [],
  topics: [],
});

const fetchCategories = () => {
  apiClient
    .get("/CategoryApi")
    .then((response) => {
      newTopicState.categories = response.data;
    })
    .catch((error) => {
      console.error("載入分類失敗:", error);
    });
};

const fetchTopics = (categoryId = null) => {
  const params = {};
  if (categoryId) params.categoryId = categoryId;
  apiClient
    .get("/ContentApi", { params })
    .then((response) => {
      newTopicState.topics = response.data.items.map((item) => ({
        id: item.contentId,
        title: item.title,
      }));
    })
    .catch((error) => {
      console.error("載入主題失敗:", error);
    });
};

onMounted(() => {
  fetchCategories();
});

watch(
  () => newTopicState.categoryId,
  (newCat) => {
    newTopicState.parentContentId = null;
    if (newCat) {
      fetchTopics(newCat);
    } else {
      newTopicState.topics = [];
    }
  }
);

const submitNewTopic = () => {
  if (!newTopicState.title || !newTopicState.body) {
    alert("請填寫標題與內容");
    return;
  }
  if (!newTopicState.categoryId) {
    alert("請選擇分類");
    return;
  }

  const dto = {
    UserId: 1, // TODO: 改成實際使用者 ID
    CategoryId: newTopicState.categoryId,
    ParentContentId: newTopicState.parentContentId,
    Title: newTopicState.title,
    Body: newTopicState.body,
  };

  apiClient
    .post("/ContentApi", dto)
    .then(() => {
      alert("發表成功");
      forumStore.triggerTopicRefresh();
      closeNewTopic();
    })
    .catch((error) => {
      console.error("發表失敗:", error);
      alert("發表失敗，請稍後再試");
    });
};

const closeNewTopic = () => {
  newTopicState.title = "";
  newTopicState.body = "";
  newTopicState.categoryId = null;
  newTopicState.parentContentId = null;
  forumStore.toggleNewTopicModal(false);
};
</script>

<template>
  <div
    v-if="forumStore.showNewTopicModal"
    class="custom-modal-backdrop"
    @click.self="closeNewTopic"
  >
    <div class="modal-dialog">
      <div class="modal-content">
        <form @submit.prevent="submitNewTopic">
          <div
            class="modal-header d-flex justify-content-between align-items-center mb-3"
          >
            <h5 class="modal-title mb-0">發表新主題</h5>
            <button
              type="button"
              class="btn-close"
              @click="closeNewTopic"
            ></button>
          </div>
          <div class="modal-body">
            <div class="mb-3">
              <label class="form-label">標題</label>
              <input
                v-model="newTopicState.title"
                class="form-control"
                autofocus
              />
            </div>
            <div class="mb-3">
              <label class="form-label">內容</label>
              <textarea
                v-model="newTopicState.body"
                class="form-control"
                rows="4"
              ></textarea>
            </div>
            <div class="mb-3">
              <label class="form-label">分類</label>
              <select v-model="newTopicState.categoryId" class="form-select">
                <option :value="null" disabled>請選擇分類</option>
                <option
                  v-for="cat in newTopicState.categories"
                  :key="cat.id"
                  :value="cat.id"
                >
                  {{ cat.name }}
                </option>
              </select>
            </div>
            <div class="mb-3">
              <label class="form-label">回覆至（可選）</label>
              <select
                v-model="newTopicState.parentContentId"
                class="form-select"
                :disabled="!newTopicState.categoryId"
              >
                <option :value="null">無（發表新主題）</option>
                <option
                  v-for="topic in newTopicState.topics"
                  :key="topic.id"
                  :value="topic.id"
                >
                  {{ topic.title }}
                </option>
              </select>
            </div>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button class="btn btn-secondary me-2" @click="closeNewTopic">
              取消
            </button>
            <button type="submit" class="btn btn-primary">發表</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.custom-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.modal-dialog {
  background: white;
  border-radius: 0.3rem;
  max-width: 500px;
  width: 100%;
  padding: 1rem;
  position: relative;
  z-index: 2100;
}
</style>
