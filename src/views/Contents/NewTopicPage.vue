<script setup>
import { reactive, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import apiClient from "@/api/request.js";
import DefaultNavbar from "@/examples/navbars/NavbarDefault.vue";
import DefaultFooter from "@/examples/footers/FooterDefault.vue";

const router = useRouter();

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
      router.push({ name: "forum" });
    })
    .catch((error) => {
      console.error("發表失敗:", error);
      alert("發表失敗，請稍後再試");
    });
};

const cancelNewTopic = () => {
  router.push({ name: "forum" });
};
</script>

<template>
  <DefaultNavbar light />

  <div class="container mt-5 mb-5">
    <div class="row justify-content-center">
      <div class="col-lg-8">
        <div class="card shadow-sm">
          <div class="card-header bg-white py-3">
            <h4 class="mb-0">📝 發表新主題</h4>
          </div>
          <div class="card-body p-4">
            <form @submit.prevent="submitNewTopic">
              <div class="mb-4">
                <label class="form-label fw-bold"
                  >標題 <span class="text-danger">*</span></label
                >
                <input
                  v-model="newTopicState.title"
                  class="form-control form-control-lg"
                  placeholder="請輸入主題標題"
                  autofocus
                />
              </div>

              <div class="mb-4">
                <label class="form-label fw-bold"
                  >內容 <span class="text-danger">*</span></label
                >
                <textarea
                  v-model="newTopicState.body"
                  class="form-control"
                  rows="8"
                  placeholder="請輸入主題內容..."
                ></textarea>
              </div>

              <div class="mb-4">
                <label class="form-label fw-bold"
                  >分類 <span class="text-danger">*</span></label
                >
                <select
                  v-model="newTopicState.categoryId"
                  class="form-select form-select-lg"
                >
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

              <div class="mb-4">
                <label class="form-label fw-bold">回覆至（可選）</label>
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
                <div class="form-text">
                  如果您想回覆某個已存在的主題，請先選擇分類後再選擇要回覆的主題
                </div>
              </div>

              <div class="d-flex justify-content-end gap-2">
                <button
                  type="button"
                  class="btn btn-secondary btn-lg"
                  @click="cancelNewTopic"
                >
                  <i class="fas fa-times me-2"></i>取消
                </button>
                <button type="submit" class="btn btn-primary btn-lg">
                  <i class="fas fa-paper-plane me-2"></i>發表
                </button>
              </div>
            </form>
          </div>
        </div>

        <div class="card shadow-sm mt-4">
          <div class="card-body">
            <h6 class="mb-3">📌 發表須知</h6>
            <ul class="text-muted mb-0">
              <li>請確保您的內容符合社區規範</li>
              <li>標題應簡潔明瞭，準確描述主題內容</li>
              <li>內容應詳細且有建設性</li>
              <li>請選擇正確的分類以便其他用戶查找</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>

  <DefaultFooter />
</template>

<style scoped>
.form-label {
  color: #344767;
}

.form-control:focus,
.form-select:focus {
  border-color: #cb0c9f;
  box-shadow: 0 0 0 0.2rem rgba(203, 12, 159, 0.25);
}

.card {
  border: none;
}

.gap-2 {
  gap: 0.5rem;
}
</style>
