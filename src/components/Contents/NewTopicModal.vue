<script setup>
import { reactive, onMounted, watch, ref, computed } from "vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";

const forumStore = useForumStore();

const newTopicState = reactive({
  title: forumStore.newTopic.title || "",
  body: forumStore.newTopic.body || "",
  categoryId: forumStore.newTopic.categoryId ?? null,
  parentContentId: forumStore.newTopic.parentContentId ?? null,
  topics: [],
});

const categorySearch = ref(forumStore.newTopic.categorySearch || "");
const showCategoryDropdown = ref(false);
const isCreatingCategory = ref(false);
const showCreateCategoryModal = ref(false);
const newCategoryName = ref("");
const newCategoryDescription = ref("");
const showCategoryDescriptionModal = ref(false);
const showCategoryNameModal = ref(false);

const filteredCategories = computed(() => {
  const keyword = categorySearch.value.trim().toLowerCase();
  const categories = forumStore.categories || [];
  if (!keyword) return categories;
  return categories.filter((cat) =>
    String(cat.name || "").toLowerCase().includes(keyword)
  );
});

const categorySuggestions = computed(() => {
  const input = categorySearch.value.trim();
  if (!input) {
    return (forumStore.categories || []).slice(0, 12);
  }
  return filteredCategories.value.slice(0, 12);
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

  if (!categorySearch.value && newTopicState.categoryId) {
    const match = (forumStore.categories || []).find(
      (cat) => cat.id === newTopicState.categoryId
    );
    if (match?.name) {
      categorySearch.value = match.name;
    }
  }
});

watch(
  () => newTopicState.title,
  (value) => {
    forumStore.newTopic.title = value;
  }
);

watch(
  () => newTopicState.body,
  (value) => {
    forumStore.newTopic.body = value;
  }
);

watch(
  () => newTopicState.categoryId,
  (value) => {
    forumStore.newTopic.categoryId = value;
  }
);

watch(
  () => newTopicState.parentContentId,
  (value) => {
    forumStore.newTopic.parentContentId = value;
  }
);

// 監聽 store 的 selectedCategory 變化
watch(
  () => forumStore.selectedCategory,
  (newCategory) => {
    if (newCategory) {
      newTopicState.categoryId = newCategory;
      const match = (forumStore.categories || []).find(
        (cat) => cat.id === newCategory
      );
      if (match?.name) {
        categorySearch.value = match.name;
      }
    }
  }
);

watch(
  () => forumStore.categories,
  (categories) => {
    if (!categorySearch.value && newTopicState.categoryId) {
      const match = (categories || []).find(
        (cat) => cat.id === newTopicState.categoryId
      );
      if (match?.name) {
        categorySearch.value = match.name;
      }
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

watch(
  () => categorySearch.value,
  (value) => {
    forumStore.newTopic.categorySearch = value;
    const input = String(value || "").trim();
    if (!input) {
      newTopicState.categoryId = null;
      return;
    }

    const match = (forumStore.categories || []).find(
      (cat) => String(cat.name || "").toLowerCase() === input.toLowerCase()
    );
    newTopicState.categoryId = match ? match.id : null;
  }
);

const selectCategoryFromDropdown = (category) => {
  if (!category) return;
  categorySearch.value = category.name || "";
  newTopicState.categoryId = category.id;
  showCategoryDropdown.value = false;
};

const hideCategoryDropdown = () => {
  setTimeout(() => {
    showCategoryDropdown.value = false;
  }, 150);
};

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

  newCategoryName.value = name;
  newCategoryDescription.value = "";
  showCreateCategoryModal.value = true;
};

const submitCreateCategory = async () => {
  const name = newCategoryName.value.trim();
  if (!name) {
    showCategoryNameModal.value = true;
    return;
  }

  const description = newCategoryDescription.value.trim();
  if (!description) {
    showCategoryDescriptionModal.value = true;
    return;
  }

  try {
    isCreatingCategory.value = true;
    const created = await forumStore.createCategory(
      name,
      description,
      null
    );
    if (created?.id) {
      newTopicState.categoryId = created.id;
      categorySearch.value = created.name || name;
    } else {
      const fallback = (forumStore.categories || []).find(
        (cat) => cat.name === name
      );
      if (fallback) {
        newTopicState.categoryId = fallback.id;
        categorySearch.value = fallback.name || name;
      }
    }
    showCreateCategoryModal.value = false;
  } catch (error) {
    console.error("新增分類失敗:", error);
    alert("新增分類失敗，請稍後再試");
  } finally {
    isCreatingCategory.value = false;
  }
};

const closeCreateCategoryModal = () => {
  showCreateCategoryModal.value = false;
};

const closeCategoryDescriptionModal = () => {
  showCategoryDescriptionModal.value = false;
};

const closeCategoryNameModal = () => {
  showCategoryNameModal.value = false;
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
      resetNewTopicDraft();
      closeNewTopic();
    })
    .catch((error) => {
      console.error("[NewTopicModal] submitNewTopic - 發表失敗:", error);
      alert("發表失敗，請稍後再試");
    });
};

const closeNewTopic = () => {
  forumStore.toggleNewTopicModal(false);
};

const resetNewTopicDraft = () => {
  newTopicState.title = "";
  newTopicState.body = "";
  newTopicState.categoryId = null;
  newTopicState.parentContentId = null;
  categorySearch.value = "";
  forumStore.newTopic.title = "";
  forumStore.newTopic.body = "";
  forumStore.newTopic.categoryId = null;
  forumStore.newTopic.parentContentId = null;
  forumStore.newTopic.categorySearch = "";
};
</script>

<template>
  <Teleport to="body">
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
                <div class="category-input-wrapper">
                  <input
                    v-model="categorySearch"
                    class="form-control category-input"
                    list="category-options"
                    placeholder="輸入或搜尋分類..."
                    @focus="showCategoryDropdown = true"
                    @blur="hideCategoryDropdown"
                    @keyup.enter.prevent="
                      categorySuggestions.length === 0
                        ? createCategoryFromSearch()
                        : null
                    "
                  />
                  <button
                    v-if="categorySearch"
                    type="button"
                    class="category-input-clear"
                    aria-label="清除分類輸入"
                    @mousedown.prevent
                    @click="categorySearch = ''"
                  >
                    ×
                  </button>
                  <span
                    class="category-input-dropdown"
                    aria-hidden="true"
                    @mousedown.prevent="showCategoryDropdown = true"
                  >
                    ▾
                  </span>
                </div>
                <datalist id="category-options">
                  <option
                    v-for="cat in categorySuggestions"
                    :key="cat.id"
                    :value="cat.name"
                  />
                </datalist>

                <div
                  v-if="showCategoryDropdown"
                  class="category-dropdown border rounded mt-2"
                >
                  <button
                    v-for="cat in categorySuggestions"
                    :key="cat.id"
                    type="button"
                    class="dropdown-item w-100 text-start"
                    @mousedown.prevent="selectCategoryFromDropdown(cat)"
                  >
                    {{ cat.name }}
                  </button>
                  <div
                    v-if="categorySuggestions.length === 0"
                    class="text-muted small px-3 py-2"
                  >
                    沒有符合的分類
                  </div>
                </div>

                <div
                  class="d-flex justify-content-between align-items-center mt-2"
                >
                  <button
                    v-if="categorySearch && filteredCategories.length === 0"
                    type="button"
                    class="btn btn-sm btn-forum-secondary"
                    :disabled="isCreatingCategory"
                    @click="createCategoryFromSearch"
                  >
                    {{ isCreatingCategory ? "建立中..." : "手動新增分類" }}
                  </button>
                  <small class="text-muted">
                    {{
                      categorySearch.trim()
                        ? `找到 ${filteredCategories.length} 個分類`
                        : `共 ${forumStore.categories?.length || 0} 個分類`
                    }}
                  </small>
                </div>
                <small class="text-muted d-block mt-1">
                  選擇或輸入分類名稱，找不到時可直接建立
                </small>
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
              <button
                class="btn btn-forum-secondary me-2"
                @click="closeNewTopic"
              >
                取消
              </button>
              <button type="submit" class="btn btn-forum-primary">發表</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div
      v-if="showCreateCategoryModal"
      class="create-category-backdrop"
      @click.self="closeCreateCategoryModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <form @submit.prevent="submitCreateCategory">
            <div
              class="modal-header d-flex justify-content-between align-items-center mb-3"
            >
              <h5 class="modal-title mb-0">新增分類</h5>
              <button
                type="button"
                class="btn-close"
                @click="closeCreateCategoryModal"
              ></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">分類名稱</label>
                <input v-model="newCategoryName" class="form-control" />
              </div>
              <div class="mb-3">
                <label class="form-label">描述</label>
                <textarea
                  v-model="newCategoryDescription"
                  class="form-control"
                  rows="3"
                  placeholder="請輸入分類描述"
                ></textarea>
              </div>
            </div>
            <div class="modal-footer d-flex justify-content-end">
              <button
                type="button"
                class="btn btn-forum-secondary me-2"
                @click="closeCreateCategoryModal"
              >
                取消
              </button>
              <button type="submit" class="btn btn-forum-primary">
                送出
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div
      v-if="showCategoryDescriptionModal"
      class="create-category-backdrop"
      @click.self="closeCategoryDescriptionModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <div
            class="modal-header d-flex justify-content-between align-items-center mb-3"
          >
            <h5 class="modal-title mb-0">提醒</h5>
            <button
              type="button"
              class="btn-close"
              @click="closeCategoryDescriptionModal"
            ></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">分類描述為必填，請先填寫描述。</p>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button
              type="button"
              class="btn btn-forum-primary"
              @click="closeCategoryDescriptionModal"
            >
              我知道了
            </button>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="showCategoryNameModal"
      class="create-category-backdrop"
      @click.self="closeCategoryNameModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <div
            class="modal-header d-flex justify-content-between align-items-center mb-3"
          >
            <h5 class="modal-title mb-0">提醒</h5>
            <button
              type="button"
              class="btn-close"
              @click="closeCategoryNameModal"
            ></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">分類名稱為必填，請先填寫名稱。</p>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button
              type="button"
              class="btn btn-forum-primary"
              @click="closeCategoryNameModal"
            >
              我知道了
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
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

.create-category-backdrop {
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

.create-category-dialog {
  background: #fff;
  border-radius: 0.3rem;
  width: min(520px, 92%);
  padding: 0.75rem 1rem;
  position: relative;
  z-index: 2100;
  border: 1px solid #d1d3d4;
  max-height: 75vh;
  overflow-y: auto;
}

.create-category-content .form-control,
.create-category-content textarea.form-control {
  font-size: 0.95rem;
}

.create-category-content textarea.form-control {
  min-height: 100px;
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

.category-input-wrapper {
  position: relative;
}

.category-input {
  padding-right: 3.5rem;
}

.category-input-clear {
  position: absolute;
  top: 50%;
  right: 2rem;
  transform: translateY(-50%);
  border: none;
  background: transparent;
  color: #9aa0a6;
  font-size: 1.1rem;
  line-height: 1;
  padding: 0.25rem;
  cursor: pointer;
}

.category-input-clear:hover {
  color: #145369;
}

.category-input-dropdown {
  position: absolute;
  top: 50%;
  right: 0.75rem;
  transform: translateY(-50%);
  color: #9aa0a6;
  font-size: 0.9rem;
  pointer-events: auto;
}

.form-control:focus,
.form-select:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}

.category-dropdown {
  max-height: 220px;
  overflow-y: auto;
  background: #fff;
  box-shadow: 0 8px 16px rgba(20, 83, 105, 0.12);
  border-color: #d1d3d4 !important;
}

.dropdown-item {
  padding: 0.5rem 0.75rem;
  border: none;
  background: transparent;
  color: #145369;
}

.dropdown-item:hover {
  background: rgba(255, 240, 221, 0.7);
}
</style>
