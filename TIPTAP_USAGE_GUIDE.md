# TiptapEditor 圖片延遲上傳使用指南

## 功能說明

已將 TiptapEditor 的圖片上傳功能改為**延遲上傳**模式：

- 用戶選擇圖片後，先轉換為 **base64** 格式顯示在編輯器中
- 圖片僅存儲在瀏覽器記憶體中（不會立即上傳到伺服器）
- 當用戶點擊「發佈」按鈕時，才批量上傳所有圖片到 MVC 根目錄
- 如果用戶取消發文，圖片不會被上傳，避免伺服器儲存無用文件

## 父組件使用方式

### 1. 在模板中添加 ref

```vue
<template>
  <TiptapEditor ref="editorRef" v-model="content" placeholder="請輸入內容..." />
  <button @click="handlePublish">發佈</button>
</template>
```

### 2. 在 script 中處理發佈邏輯

```vue
<script setup>
import { ref } from "vue";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";

const editorRef = ref(null);
const content = ref("");

const handlePublish = async () => {
  try {
    // 在發佈前，上傳所有待處理的圖片
    // uploadPendingImages() 會將 base64 圖片上傳到伺服器
    // 並返回替換後的 HTML（base64 URL 被替換為實際的伺服器 URL）
    const finalContent = await editorRef.value.uploadPendingImages();

    // 使用 finalContent（含實際圖片 URL）進行發佈
    await publishToServer({
      title: "...",
      body: finalContent,
      // ... 其他欄位
    });

    console.log("發佈成功！");
  } catch (error) {
    console.error("發佈失敗:", error);
    alert("發佈失敗，請稍後再試");
  }
};
</script>
```

### 3. 取消發文時清除圖片

如果用戶取消發文，可以清除待上傳的圖片：

```vue
<script setup>
const handleCancel = () => {
  // 清除所有待上傳的圖片（釋放記憶體）
  editorRef.value?.clearPendingImages();

  // 關閉編輯器或清空內容
  content.value = "";
};
</script>
```

## 完整範例

### NewTopicSidebar.vue 修改建議

```vue
<script setup>
import { ref } from "vue";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";

// 為每個草稿存儲編輯器 ref
const editorRefs = ref({});

const submitDraft = async (draft) => {
  try {
    // 1. 先上傳圖片，獲取含實際 URL 的內容
    const editorRef = editorRefs.value[draft.id];
    const finalBody = editorRef
      ? await editorRef.uploadPendingImages()
      : draft.body;

    // 2. 使用實際 URL 的內容進行發佈
    const success = await publishPost({
      title: draft.title,
      body: finalBody, // 使用包含實際圖片 URL 的內容
      categoryId: draft.categoryId,
      parentContentId: draft.parentContentId,
      contentType: draft.contentType,
    });

    if (!success) {
      return;
    }

    // 3. 發佈成功，刪除草稿
    forumStore.removeDraft(draft.id);
  } catch (error) {
    console.error("發佈失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  }
};

const handleCancelClick = (draft) => {
  // 清除待上傳的圖片
  const editorRef = editorRefs.value[draft.id];
  if (editorRef) {
    editorRef.clearPendingImages();
  }

  // 刪除草稿
  forumStore.removeDraft(draft.id);
};
</script>

<template>
  <div v-for="draft in allDrafts" :key="draft.id">
    <TiptapEditor
      :ref="
        (el) => {
          if (el) editorRefs[draft.id] = el;
        }
      "
      :modelValue="draft.body"
      @update:modelValue="updateDraftBody(draft, $event)"
      placeholder="請輸入貼文內容..."
    />
    <button @click="submitDraft(draft)">發佈</button>
    <button @click="handleCancelClick(draft)">取消</button>
  </div>
</template>
```

### NewTopicModal.vue 修改建議

```vue
<script setup>
import { ref } from "vue";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";

const editorRef = ref(null);

const handleSubmit = async () => {
  try {
    // 上傳待處理的圖片
    const finalBody = await editorRef.value?.uploadPendingImages();

    // 使用包含實際 URL 的內容發佈
    const success = await publishPost({
      title: forumStore.newTopic.title,
      body: finalBody || forumStore.newTopic.body,
      categoryId: forumStore.newTopic.categoryId,
      parentContentId: forumStore.newTopic.parentContentId,
      contentType: forumStore.newTopic.contentType,
    });

    if (success) {
      forumStore.closeNewTopicModal();
    }
  } catch (error) {
    console.error("發佈失敗:", error);
    alert("圖片上傳失敗，請稍後再試");
  }
};

const handleCancel = () => {
  // 清除待上傳的圖片
  editorRef.value?.clearPendingImages();
  forumStore.closeNewTopicModal();
};
</script>

<template>
  <TiptapEditor
    ref="editorRef"
    v-model="forumStore.newTopic.body"
    placeholder="請輸入貼文內容..."
  />
  <button @click="handleSubmit">發佈</button>
  <button @click="handleCancel">取消</button>
</template>
```

## 技術細節

### 暴露的方法

TiptapEditor 組件通過 `defineExpose` 暴露了兩個方法：

1. **`uploadPendingImages()`**
   - 上傳所有待處理的圖片到伺服器
   - 將編輯器中的 base64 URL 替換為實際的伺服器 URL
   - 返回更新後的 HTML 內容
   - 如果沒有待上傳的圖片，直接返回當前內容

2. **`clearPendingImages()`**
   - 清除所有待上傳的圖片（釋放記憶體）
   - 用於取消發文時清理資源

### 工作流程

```
1. 用戶選擇圖片
   ↓
2. 轉換為 base64（客戶端）
   ↓
3. 顯示在編輯器中
   ↓
4. 用戶點擊「發佈」
   ↓
5. 調用 uploadPendingImages()
   ↓
6. 批量上傳到伺服器
   ↓
7. 替換 base64 為實際 URL
   ↓
8. 使用實際 URL 的內容發佈
```

## 優點

✅ **避免浪費伺服器空間** - 取消發文時，圖片不會被上傳  
✅ **減少無效請求** - 只在確認發佈時才上傳  
✅ **用戶體驗更好** - 選擇圖片後立即顯示預覽  
✅ **支持批量上傳** - 多張圖片一次性上傳，提高效率  
✅ **記憶體管理** - 提供清理方法，避免記憶體洩漏

## 注意事項

⚠️ **大型圖片** - base64 會增加 HTML 大小約 33%，建議限制圖片大小  
⚠️ **瀏覽器記憶體** - 大量圖片可能佔用較多記憶體  
⚠️ **錯誤處理** - 上傳失敗時需要適當的錯誤提示  
⚠️ **網絡延遲** - 發佈時需要等待圖片上傳完成，可能需要 loading 提示
