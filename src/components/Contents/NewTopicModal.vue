<script setup>
import { reactive, onMounted, watch, ref, computed } from "vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";

const forumStore = useForumStore();

const newTopicState = reactive({
  title: "",
  body: "",
  categoryId: null,
  parentContentId: null,
  topics: [],
});

const categorySearch = ref("");
const isCreatingCategory = ref(false);

const filteredCategories = computed(() => {
  const keyword = categorySearch.value.trim().toLowerCase();
  const categories = forumStore.categories || [];
  if (!keyword) return categories;
  return categories.filter((cat) =>
    String(cat.name || "").toLowerCase().includes(keyword)
  );
});

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
  if (!forumStore.categories?.length) {
    forumStore.fetchCategories();
  }
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

const createCategoryFromSearch = async () => {
  const name = categorySearch.value.trim();
  if (!name || isCreatingCategory.value) return;

  const existing = (forumStore.categories || []).find(
    (cat) => String(cat.name || "").toLowerCase() === name.toLowerCase()
  );
  if (existing) {
    newTopicState.categoryId = existing.id;
    return;
  }

  try {
    isCreatingCategory.value = true;
    const created = await forumStore.createCategory(name);
    if (created?.id) {
      newTopicState.categoryId = created.id;
    } else {
      const fallback = (forumStore.categories || []).find(
        (cat) => cat.name === name
      );
      if (fallback) newTopicState.categoryId = fallback.id;
    }
  } catch (error) {
    console.error("新增分類失敗:", error);
    alert("新增分類失敗，請稍後再試");
  } finally {
    isCreatingCategory.value = false;
  }
};

const submitNewTopic = () => {
  console.log(
    "[NewTopicModal] submitNewTopic - 當前 body:",
    newTopicState.body
  );

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

  const isReply = Boolean(newTopicState.parentContentId);
  const dto = {
    userId: 1, // TODO: 改成實際使用者 ID
    categoryId: newTopicState.categoryId,
    parentContentId: newTopicState.parentContentId,
    title: newTopicState.title,
    body: newTopicState.body,
    type: isReply ? CONTENT_TYPES.REPLY : CONTENT_TYPES.POST,
  };

  console.log("[NewTopicModal] submitNewTopic - 提交 DTO:", dto);

  apiClient
    .post("/ContentApi", dto)
    .then(() => {
      alert("發表成功");
      // 直接刷新主題列表而不是只觸發信號
      forumStore.pagination.currentPage = 1;
      forumStore.fetchTopics();
      closeNewTopic();
    })
    .catch((error) => {
      console.error("[NewTopicModal] submitNewTopic - 發表失敗:", error);
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
              <label class="form-label">搜尋分類</label>
              <input
                v-model="categorySearch"
                class="form-control"
                placeholder="輸入關鍵字搜尋分類"
              />
              <div class="d-flex justify-content-between align-items-center mt-2">
                <small class="text-muted">
                  找到 {{ filteredCategories.length }} 個分類
                </small>
                <button
                  v-if="categorySearch && filteredCategories.length === 0"
                  type="button"
                  class="btn btn-sm btn-forum-secondary"
                  :disabled="isCreatingCategory"
                  @click="createCategoryFromSearch"
                >
                  {{ isCreatingCategory ? "建立中..." : "建立新分類" }}
                </button>
              </div>
            </div>
            <div class="mb-3">
              <label class="form-label">分類</label>
              <select v-model="newTopicState.categoryId" class="form-select">
                <option :value="null" disabled>請選擇分類</option>
                <option
                  v-for="cat in filteredCategories"
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
              <TiptapEditor
                v-model="newTopicState.body"
                placeholder="請輸入貼文內容..."
              />
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
  background-color: #145369;
  border-color: #145369;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

.btn-forum-secondary {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #145369;
  transition: all 0.3s ease;
}

.btn-forum-secondary:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #145369;
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
  max-width: 90%;
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
  border: 1px solid #d1d3d4;
  padding-left: 0.75rem;
}

.form-control:focus,
.form-select:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}
</style>
