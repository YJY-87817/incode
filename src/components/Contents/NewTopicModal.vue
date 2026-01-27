<script setup>
import { reactive, onMounted, watch, ref } from "vue";
import { useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import { uploadImage } from "@/api/imageapi.js";

const forumStore = useForumStore();

const isUploadingImages = ref(false);

const newTopicState = reactive({
  title: "",
  body: "",
  categoryId: null,
  parentContentId: null,
  categories: [],
  topics: [],
});

const appendUrls = (original, urls) => {
  const list = Array.isArray(urls) ? urls : [urls];
  const chunk = list.filter(Boolean).join("\n");
  if (!chunk) return original;
  return [original.trim(), chunk].filter(Boolean).join("\n");
};

const handleImageUpload = async (event) => {
  const files = event.target?.files;
  if (!files || !files.length) return;
  isUploadingImages.value = true;
  try {
    const uploaded = await uploadImage(files);
    newTopicState.body = appendUrls(newTopicState.body, uploaded);
  } catch (error) {
    console.error("圖片上傳失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  } finally {
    isUploadingImages.value = false;
    if (event.target) event.target.value = "";
  }
};

const fetchCategories = () => {
  apiClient
    .get("/CategoryApi")
    .then((data) => {
      newTopicState.categories = data;
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
    .then((data) => {
      newTopicState.topics = data.items.map((item) => ({
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
  // 同步 store 中選中的分類
  if (forumStore.selectedCategory) {
    newTopicState.categoryId = forumStore.selectedCategory;
  }
});

// 監聽 store 的 selectedCategory 變化
watch(
  () => forumStore.selectedCategory,
  (newCategory) => {
    if (newCategory) {
      newTopicState.categoryId = newCategory;
    }
  }
);

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
  
  // 若有 parentContentId，不需檢查 categoryId（會使用 parent 的 category）
  // 若無 parentContentId，需檢查 categoryId
  if (!newTopicState.parentContentId && !newTopicState.categoryId) {
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
              <div
                class="mt-2 d-flex flex-column flex-sm-row gap-2 align-items-start"
              >
                <input
                  type="file"
                  accept="image/*"
                  multiple
                  class="form-control form-control-sm"
                  @change="handleImageUpload"
                  :disabled="isUploadingImages"
                />
                <small class="text-muted">
                  上傳後會自動把圖片網址加入內容。{{
                    isUploadingImages ? "上傳中..." : ""
                  }}
                </small>
              </div>
            </div>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button class="btn btn-forum-secondary me-2" @click="closeNewTopic">
              取消
            </button>
            <button type="submit" class="btn btn-forum-primary">發表</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #432f28;
  border-color: #432f28;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #432f28;
}

.btn-forum-secondary {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #432f28;
  transition: all 0.3s ease;
}

.btn-forum-secondary:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #432f28;
}

.custom-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(67, 47, 40, 0.5);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.modal-dialog {
  background: #fff;
  border-radius: 0.3rem;
  max-width: 550px;
  width: 100%;
  padding: 1rem;
  position: relative;
  z-index: 2100;
  border: 1px solid #d1d3d4;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  border-bottom-color: #d1d3d4;
}

.modal-footer {
  border-top-color: #d1d3d4;
}

.form-control,
.form-select {
  border-color: #d1d3d4;
  padding-left: 0.75rem;
}

.form-control:focus,
.form-select:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}
</style>
