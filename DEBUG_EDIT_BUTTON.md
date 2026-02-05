# 編輯按鈕不顯示 - 診斷指南

## 🔍 快速診斷步驟

### 第 1 步：檢查是否已登入

**在瀏覽器 Console 輸入**：

```javascript
// 檢查登入狀態
const authStore = window.$cookies?.get("auth") || localStorage.getItem("auth");
console.log("Auth Store:", authStore);

// 或者在 Vue DevTools 查看
// Pinia > auth > user
```

**預期結果**：

- ✅ 應該看到 `user` 對象，包含 `id` 欄位
- ❌ 如果是 `null` 或沒有 `id`，需要先登入

---

### 第 2 步：檢查 post.userId 是否存在

**在瀏覽器 Console 輸入**：

```javascript
// 方法 1：查看 forumStore 的數據
const forumStore =
  window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps[0]?.$pinia?.state.value.forum;
console.log("Topics:", forumStore?.topics);
console.log("ThreadDetail:", forumStore?.threadDetail);
console.log("ThreadPosts:", forumStore?.threadPosts);

// 方法 2：檢查第一個 topic 是否有 userId
console.log("First topic userId:", forumStore?.topics[0]?.userId);
```

**預期結果**：

- ✅ 每個 post 應該有 `userId` 欄位（數字）
- ❌ 如果是 `null` 或 `undefined`，表示後端沒有返回

---

### 第 3 步：檢查 authorUserId 是否傳遞

**在瀏覽器 Console 輸入**：

```javascript
// 在頁面上找一個貼文元素，查看 props
// 這個需要在 Vue DevTools 中查看
// Components > ThreadPost > props > post > userId
```

**或者在 InteractionButtons 組件中添加 console.log**：

```javascript
// 臨時添加到 InteractionButtons.vue 的 script setup 區塊
console.log("InteractionButtons - authorUserId:", props.authorUserId);
console.log("InteractionButtons - currentUserId:", authStore.user?.id);
console.log("InteractionButtons - isOwnContent:", isOwnContent.value);
```

---

## 🐛 常見問題與解決方案

### 問題 1：未登入

**症狀**：authStore.user 是 null

**解決方案**：

1. 確認已經登入
2. 檢查 localStorage 或 cookies 中是否有 token
3. 重新登入一次

---

### 問題 2：userId 未從後端返回

**症狀**：post.userId 是 undefined

**檢查後端返回**：
在 Network 面板查看 API 返回：

- `GET /api/ContentApi`
- `GET /api/ContentApi/{id}`
- `GET /api/ContentApi/{id}/comments`

**應該看到**：

```json
{
  "contentId": 123,
  "userId": 456, // ⭐ 必須有這個欄位
  "title": "...",
  "body": "..."
}
```

**如果沒有 userId**：
後端已經修改了，但可能：

1. 需要清除快取（Ctrl + F5）
2. 檢查是否有舊的數據快取
3. 後端是否已重新編譯和重啟

---

### 問題 3：userId 類型不匹配

**症狀**：userId 存在但按鈕仍不顯示

**檢查**：

```javascript
// 在 Console 檢查類型
console.log(typeof authStore.user.id); // 應該是 "number"
console.log(typeof props.authorUserId); // 應該是 "number"
```

**如果類型不一致**（例如一個是 string，一個是 number）：

修改 InteractionButtons.vue：

```javascript
const isOwnContent = computed(() => {
  const currentUserId = authStore.user?.id;
  const authorId = props.authorUserId;

  console.log("Checking ownership:", {
    currentUserId,
    authorId,
    currentType: typeof currentUserId,
    authorType: typeof authorId,
    match:
      currentUserId && authorId && Number(currentUserId) === Number(authorId),
  });

  return (
    currentUserId && authorId && Number(currentUserId) === Number(authorId)
  );
});
```

---

### 問題 4：forumStore 未更新 userId

**症狀**：API 返回有 userId，但 forumStore 的數據沒有

**檢查**：打開 `forumStore.js`，確認以下位置都有添加 userId：

1. **fetchTopics** - 第 337 行附近

```javascript
return {
  id: item.contentId,
  userId: item.userId ?? item.user_id ?? null, // ⭐ 必須有這行
  title: item.title,
  // ...
};
```

2. **fetchThreadDetail** - 第 417 行附近

```javascript
this.threadDetail = {
  id: detail.contentId,
  userId: detail.userId ?? detail.user_id ?? null, // ⭐ 必須有這行
  title: detail.title,
  // ...
};
```

3. **threadPosts 和 threadComments** - 檢查是否都有 userId

---

## 🔧 臨時測試方法

### 方法 1：強制顯示按鈕（測試用）

臨時修改 InteractionButtons.vue：

```javascript
const isOwnContent = computed(() => {
  return true; // 強制顯示，測試按鈕是否能正常工作
});
```

如果這樣能看到按鈕，表示問題在於權限判斷邏輯。

---

### 方法 2：添加詳細日誌

在 InteractionButtons.vue 的 script setup 添加：

```javascript
import { watch } from "vue";

// 監視變化
watch(
  [() => authStore.user?.id, () => props.authorUserId],
  ([userId, authorId]) => {
    console.log("🔍 權限檢查:", {
      當前使用者ID: userId,
      作者ID: authorId,
      是否匹配: userId === authorId,
      是否顯示按鈕: isOwnContent.value,
    });
  },
  { immediate: true },
);
```

---

## 📝 完整檢查清單

按順序檢查：

- [ ] 1. 已經登入（authStore.user.id 存在）
- [ ] 2. API 返回包含 userId 欄位
- [ ] 3. forumStore 正確存儲 userId
- [ ] 4. ThreadPost 正確傳遞 authorUserId
- [ ] 5. InteractionButtons 收到 authorUserId
- [ ] 6. isOwnContent 計算正確
- [ ] 7. 「更多」按鈕存在於 DOM（檢查元素）

---

## 🎯 最可能的原因（按機率排序）

### 1. 未登入或 user.id 是 undefined (60%)

**快速檢查**：

```javascript
console.log(
  window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps[0]?.$pinia?.state.value.auth?.user
    ?.id,
);
```

### 2. 後端未返回 userId 或返回為 null (25%)

**快速檢查**：
打開 Network 面板，查看 API 返回的 JSON

### 3. 類型不匹配（string vs number） (10%)

**快速檢查**：

```javascript
// 在 Console 查看類型
const authStore =
  window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps[0]?.$pinia?.state.value.auth;
console.log("User ID type:", typeof authStore?.user?.id);

const forumStore =
  window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps[0]?.$pinia?.state.value.forum;
console.log("Post userId type:", typeof forumStore?.topics[0]?.userId);
```

### 4. 瀏覽器快取（舊代碼） (5%)

**解決**：Ctrl + Shift + R 強制重新載入

---

## 💡 快速修復腳本

把這段代碼貼到瀏覽器 Console，一次性檢查所有問題：

```javascript
(function debugEditButton() {
  console.log("=== 編輯按鈕診斷工具 ===\n");

  // 1. 檢查登入狀態
  const pinia = window.__VUE_DEVTOOLS_GLOBAL_HOOK__?.apps[0]?.$pinia;
  const authStore = pinia?.state.value.auth;
  const forumStore = pinia?.state.value.forum;

  console.log("1️⃣  登入狀態:");
  console.log("  - 已登入:", !!authStore?.isAuthenticated);
  console.log("  - 使用者 ID:", authStore?.user?.id);
  console.log("  - 使用者 ID 類型:", typeof authStore?.user?.id);
  console.log("");

  // 2. 檢查第一筆貼文數據
  const firstTopic = forumStore?.topics?.[0];
  console.log("2️⃣  第一筆貼文數據:");
  console.log("  - 貼文 ID:", firstTopic?.id);
  console.log("  - 作者 userId:", firstTopic?.userId);
  console.log("  - userId 類型:", typeof firstTopic?.userId);
  console.log("  - 作者名稱:", firstTopic?.author);
  console.log("");

  // 3. 檢查權限匹配
  const currentUserId = authStore?.user?.id;
  const authorUserId = firstTopic?.userId;

  console.log("3️⃣  權限匹配檢查:");
  console.log("  - 當前使用者 ID:", currentUserId, `(${typeof currentUserId})`);
  console.log("  - 貼文作者 ID:", authorUserId, `(${typeof authorUserId})`);
  console.log("  - 嚴格相等 (===):", currentUserId === authorUserId);
  console.log(
    "  - 轉換後相等:",
    Number(currentUserId) === Number(authorUserId),
  );
  console.log(
    "  - 應該顯示編輯按鈕:",
    !!(currentUserId && authorUserId && currentUserId === authorUserId),
  );
  console.log("");

  // 4. 檢查 threadDetail
  const threadDetail = forumStore?.threadDetail;
  if (threadDetail?.id) {
    console.log("4️⃣  討論串詳情:");
    console.log("  - 討論串 ID:", threadDetail.id);
    console.log("  - 作者 userId:", threadDetail.userId);
    console.log("  - userId 類型:", typeof threadDetail.userId);
    console.log("");
  }

  // 5. 診斷結果
  console.log("📊 診斷結果:");
  if (!authStore?.isAuthenticated) {
    console.log("❌ 問題：未登入");
    console.log("💡 解決：請先登入系統");
  } else if (!currentUserId) {
    console.log("❌ 問題：使用者 ID 不存在");
    console.log("💡 解決：檢查 authStore.user.id");
  } else if (!authorUserId && firstTopic) {
    console.log("❌ 問題：貼文缺少 userId 欄位");
    console.log("💡 解決：檢查後端 API 是否返回 userId");
  } else if (currentUserId === authorUserId) {
    console.log("✅ 一切正常！應該能看到編輯按鈕");
    console.log("💡 如果仍然看不到，請檢查：");
    console.log("   1. 清除瀏覽器快取（Ctrl + Shift + R）");
    console.log("   2. 檢查 DOM 中是否有 .more-menu-container 元素");
    console.log("   3. 檢查 CSS 是否有 display: none");
  } else {
    console.log("⚠️  這不是你發布的內容");
    console.log("💡 只有內容作者才能看到編輯按鈕");
  }

  console.log("\n=== 診斷完成 ===");
})();
```

---

## 📞 還是不行？

如果以上都檢查過了還是不行，請提供：

1. 上面診斷腳本的輸出結果
2. Network 面板中 `/api/ContentApi` 的返回 JSON
3. Vue DevTools 中 authStore 的截圖
4. Console 中是否有任何錯誤訊息

這樣我才能更精確地找出問題！
