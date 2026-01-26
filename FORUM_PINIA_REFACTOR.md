# Forum 組件 Pinia 重構總結

## 📋 重構目標

將 forum.vue 及所有子組件從 **props/emit 模式**改為 **Pinia 統一狀態管理**，並啟用持久化 (persist: true)。

## ✅ 已完成的變更

### 1. **Pinia Store 擴充** (`stores/forumStore.js`)

完整的狀態管理中心，包含：

#### State (狀態)

- `windowWidth`: 視窗寬度
- `categories`: 分類列表
- `selectedCategory`: 選中的分類
- `topics`: 主題列表
- `isLoading`: 載入狀態
- `pagination`: 分頁資訊 (currentPage, pageSize, totalCount, totalPages)
- `selectedThread`: 選中的主題 ID
- `threadDetail`: 主題詳情物件
- `threadPosts`: 回文列表
- `threadComments`: 留言列表
- `threadCurrentPage`: 帖文分頁當前頁
- `threadItemsPerPage`: 每頁項目數
- `showNewTopicBox`: 新主題表單顯示狀態
- `newTopic`: 新主題表單資料 (title, body, categoryId)
- `isSubmittingNewTopic`: 提交中標記
- `isUploadingNewTopicImages`: 圖片上傳中標記

#### Actions (方法)

- `updateWindowWidth()`: 更新視窗寬度
- `fetchCategories()`: 獲取分類列表
- `selectCategory(categoryId)`: 選擇/取消分類
- `fetchTopics(categoryId, page)`: 獲取主題列表 (支援分類篩選與分頁)
- `changePage(page)`: 主題列表換頁
- `fetchThreadDetail(threadId)`: 獲取主題詳情 (包含回文、留言)
- `openThread(topicId)`: 打開主題詳情
- `closeThread()`: 關閉主題詳情
- `changeThreadPage(page)`: 回文列表換頁
- `toggleNewTopicBox()`: 切換新主題表單顯示
- `submitNewTopic()`: 提交新主題
- `cancelNewTopic()`: 取消新主題
- `updateNewTopicCategory(categoryId)`: 更新新主題分類

#### Getters (計算屬性)

- `threadTotalPages`: 回文總頁數
- `paginatedThreadPosts`: 當前頁的回文列表
- `displayPages`: 分頁顯示計算 (主題列表)
- `selectedCategoryName`: 當前選中的分類名稱

#### 持久化

```javascript
persist: true; // 使用 pinia-plugin-persistedstate
```

---

### 2. **組件重構清單**

#### ✅ ForumHeader.vue (已更新)

- **移除**: `defineProps(['windowWidth'])`, `defineEmits(['toggle-new-topic'])`
- **改用**: `const forumStore = useForumStore()`
- **按鈕事件**: `@click="forumStore.toggleNewTopicBox()"`

#### ✅ CategoryFilter.vue (已更新)

- **移除**: 所有 props (categories, selectedCategory, isLoading)
- **移除**: 所有 emits (select-category)
- **改用**: 直接從 `forumStore` 讀取狀態和調用方法
- **綁定範例**:
  - `v-if="forumStore.isLoading && forumStore.categories.length === 0"`
  - `v-for="category in forumStore.categories"`
  - `@click="forumStore.selectCategory(category.id)"`
  - `:class="{ 'category-active': forumStore.selectedCategory === category.id }"`

#### ✅ NewTopicForm_new.vue (全新創建)

- **檔案**: `NewTopicForm_new.vue` (Pinia 版)
- **移除**: `defineProps(['categories', 'windowWidth', 'isSubmitting', 'isUploadingImages'])`
- **移除**: `defineEmits(['submit', 'cancel', 'images-uploaded', 'category-change'])`
- **改用**:
  - `forumStore.newTopic` (雙向綁定 title, body, categoryId)
  - `forumStore.categories` (分類下拉選單)
  - `forumStore.isSubmittingNewTopic` (提交狀態)
  - `forumStore.isUploadingNewTopicImages` (圖片上傳狀態)
  - `forumStore.submitNewTopic()` (提交方法)
  - `forumStore.cancelNewTopic()` (取消方法)

#### ✅ TopicListView_new.vue (全新創建)

- **檔案**: `TopicListView_new.vue` (Pinia 版)
- **移除**: 所有 props (topics, categories, isLoading, pagination, selectedThread, threadDetail, threadPosts, threadComments, threadCurrentPage, threadItemsPerPage)
- **移除**: 所有 emits (page-change, thread-open, thread-close, reply-submitted, thread-page-change)
- **改用**:
  - `forumStore.topics` (主題列表)
  - `forumStore.selectedThread` (控制顯示列表/詳情)
  - `forumStore.threadDetail` (主題詳情)
  - `forumStore.pagination` (分頁資訊)
  - `forumStore.changePage(page)` (換頁方法)
  - `forumStore.openThread(id)` / `forumStore.closeThread()` (切換詳情)

#### ✅ forum_pinia.vue (全新主文件)

- **檔案**: `forum_pinia.vue` (簡化版主頁面)
- **移除**: 所有內部狀態管理 (reactive data, 本地方法)
- **改用**: 純粹組合子組件，所有邏輯由 `forumStore` 處理
- **結構**:
  ```vue
  <ForumHeader />
  <NewTopicForm v-if="forumStore.showNewTopicBox" />
  <CategoryFilter />
  <TopicListView />
  ```
- **生命週期**:
  - `onMounted`: 初始化視窗寬度、獲取分類、獲取主題
  - `onUnmounted`: 清理事件監聽
  - `watch`: 監聽 `lastRefreshTime` 觸發刷新

---

### 3. **工具函數** (`forumUtils.ts`)

保持不變，提供純函數工具：

- `parseBodyContent()`: 解析內容與圖片
- `calculateReplyStats()`: 計算回覆統計
- `formatDate()`: 格式化日期
- `formatTimeAgo()`: 相對時間顯示
- `toAbsoluteUrl()`: URL 處理
- `appendUrls()`: 圖片 URL 附加

---

## 📂 檔案對應關係

| 舊檔案 (Props/Emit 版) | 新檔案 (Pinia 版)             | 狀態 |
| ---------------------- | ----------------------------- | ---- |
| `ForumHeader.vue`      | `ForumHeader.vue` (已更新)    | ✅   |
| `CategoryFilter.vue`   | `CategoryFilter.vue` (已更新) | ✅   |
| `NewTopicForm.vue`     | `NewTopicForm_new.vue`        | ✅   |
| `TopicListView.vue`    | `TopicListView_new.vue`       | ✅   |
| `forum.vue` (1497 行)  | `forum_pinia.vue` (77 行)     | ✅   |

---

## 🔄 架構變化對比

### **Before (Props/Emit)**

```vue
<!-- 父組件 -->
<NewTopicForm
  :categories="categories"
  :windowWidth="windowWidth"
  :isSubmitting="isSubmitting"
  @submit="handleSubmit"
  @cancel="handleCancel"
/>

<!-- 子組件 -->
<script setup>
const props = defineProps(["categories", "windowWidth", "isSubmitting"]);
const emit = defineEmits(["submit", "cancel"]);
const localForm = reactive({ title: "", body: "" });

const handleSubmit = () => {
  emit("submit", localForm);
};
</script>
```

### **After (Pinia)**

```vue
<!-- 父組件 -->
<NewTopicForm />
<!-- 無需任何 props/emit -->

<!-- 子組件 -->
<script setup>
import { useForumStore } from "@/stores/forumStore";
const forumStore = useForumStore();

// 直接使用 store 狀態 (雙向綁定)
// v-model="forumStore.newTopic.title"
// @click="forumStore.submitNewTopic()"
</script>
```

---

## 🎯 優勢

1. **解耦合**: 組件不再依賴父組件傳遞資料
2. **持久化**: 狀態自動保存到 localStorage (persist: true)
3. **全局訪問**: 任何組件都可直接讀取/修改論壇狀態
4. **簡化維護**: 減少 props 傳遞層級，降低複雜度
5. **類型安全**: Store 集中管理，易於維護和除錯

---

## 🚀 下一步建議

1. **測試新組件**: 在 router 中更新路徑指向 `forum_pinia.vue`
2. **刪除舊檔案**: 確認無誤後移除舊版本組件
3. **完善 ThreadPost.vue**: 如需要也改用 Pinia
4. **添加 TypeScript**: 為 forumStore 添加類型定義

---

## 📝 使用範例

### 在任何組件中使用論壇狀態

```vue
<script setup>
import { useForumStore } from "@/stores/forumStore";

const forumStore = useForumStore();

// 讀取狀態
console.log(forumStore.topics);
console.log(forumStore.selectedCategory);

// 調用方法
forumStore.fetchTopics();
forumStore.selectCategory(5);
forumStore.openThread(123);

// 修改狀態
forumStore.newTopic.title = "新標題";
</script>
```

---

**作者**: GitHub Copilot  
**日期**: 2025  
**版本**: Pinia 重構 v1.0
