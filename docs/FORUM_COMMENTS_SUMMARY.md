# Forum & Content 代碼複雜信息流註解總結

此文檔記錄了在 forum、content 相關代碼中添加的詳細註解。

## 📍 主要信息流轉點

### 1. **forum.vue** - 路由到 store 的連接點

檔案：[src/views/Contents/forum.vue](../src/views/Contents/forum.vue)

#### 信息流說明

```
用戶操作 → 路由改變 → store action 觸發 → 數據更新 → 視圖重新渲染
```

**添加的註解：**

- **L102-107**: 初始化與清理流程註解
  - `updateWindowWidth()` 的作用
  - `fetchCategories()` 和 `fetchTopics()` 的調用流程

- **L124-143**: 【主要信息流轉點】路由參數改變監聽
  - 清晰說明點擊主題 → router.push() → forumStore.openThread() 的完整流程

### 2. **forumStore.js** - 複雜數據轉換與緩存層

檔案：[src/stores/forumStore.js](../src/stores/forumStore.js)

#### 數據轉換函數（信息流跳轉）

添加了多個關鍵函數的詳細註解：

- **buildThreadItem()** (L170-230)
  - 【數據轉換層】：API 回應 → 協調後的內部結構
  - 說明為何需要字段映射（camelCase vs snake_case）
  - 涵蓋：HTML 渲染、贊數提取、字段名統一

- **extractReplyStats()** (L231-250)
  - 從 API 回應中提取回覆統計資訊
  - 說明數據可來自多個來源

- **extractLikeCount()** (L261-280)
  - 複雜的 fallback 連鎖（8+ 個潛在字段名）
  - 最後還原到 interactions 陣列作為終極 fallback

- **fetchEntrustMissionMetaByContentId()** (L330+)
  - 為委託任務信息追加詳細說明

#### 核心 Action 方法

- **fetchThreadDetail()** (L1350+)
  - 【資訊流複雜點中最複雜的方法】
  - 涵蓋 3 個外部 API 調用：
    1. `contentApi.getContent()` - 線程詳情
    2. `fetchBodyHtmlCached()` - HTML 轉換
    3. `fetchEntrustMissionMetaByContentId()` - 委託任務
    4. `contentInteractionApi.getBestAnswerResolution()` - 最佳解
  - 多個緩存機制（本地 localStorage + 記憶體）
  - 數據回落策略

- **openThread()** (L1495-1500)
  - 【入口點】說明此方法被 forum.vue 的 watch 調用
  - 調用 fetchThreadDetail() 的流程

### 3. **TopicListView.vue** - 列表雙視圖組件

檔案：[src/components/Contents/TopicListView.vue](../src/components/Contents/TopicListView.vue)

添加的註解：

- **goToThread()** (L28-41)
  - 【信息流跳轉點】：清晰的視圖導航流程
  - 說明如何觸發 router.push() → route 改變 → forumStore.openThread()
  - 強調組件無 props，全依賴 store 通訊

### 4. **ThreadPost.vue** - 巢狀回覆加載邏輯

檔案：[src/components/Contents/ThreadPost.vue](../src/components/Contents/ThreadPost.vue)

添加的註解：

- **toggleNestedReplies()** (L698-743)
  - 【多層級巢狀加載】複雜流程說明
  - 兩階段加載機制：
    1. **預加載階段**：掛載時調用 `preloadChildRepliesCount()`，只取數量
    2. **全部加載階段**：用戶點擊展開時調用 `ensureChildRepliesLoaded()`
  - 快取機制說明（記憶體中的 childRepliesByParentId）
  - computed 屬性自動觸發 UI 更新

---

## 📊 複雜性分析

### 最複雜的數據流

```
┌─────────────────┐
│  forum.vue      │ (視圖層)
│  (watch/call)   │
└────────┬────────┘
         │ router.push() + watch(route.params.id)
         ↓
┌──────────────────────┐
│  forumStore          │ (狀態管理)
│  openThread()        │
│  ↓                   │
│  fetchThreadDetail() │ ← 【複雜點】3-4 個 API 調用
└────────┬─────────────┘
         │ state 更新：threadDetail, threadPosts, threadComments
         ↓
┌──────────────────────┐
│  TopicListView       │ (組件)
│  (computed watch)    │
└──────────────────────┘
```

### 字段映射的複雜性

後端不同端點返回的字段名不統一：

- **camelCase**: contentId, userId, createdAt
- **snake_case**: content_id, user_id, created_at
- **嵌套位置不同**: 有時在頂層，有時在 data.interactions 中

**解決方案**：各層函數使用 `??` (nullish coalescing) 嘗試多個候選字段。

---

## 🔄 快取機制

1. **主題列表快取** (L1200s)
   - topicPageCache (記憶體)
   - TTL: 30 秒

2. **HTML 內容快取** (L100+)
   - contentBodyCache (記憶體)
   - 避免重複調用 fetchBodyHtml

3. **最佳解快取** (L40-90)
   - BEST_ANSWER_CACHE_KEY (localStorage)
   - 用於存儲委託任務的「最佳解」選擇

4. **子回覆快取** (ThreadPost 中)
   - childRepliesByParentId (store state)
   - 一旦加載過就不再重新查詢

---

## 💡 建議

當修改這些代碼時，請注意：

1. **fetchThreadDetail()** 中的多個 API 調用順序 - 改變順序可能影響用戶體驗
2. **字段映射邏輯** - 後端如果改變字段名，需要同時更新多個位置
3. **快取 TTL** - 若用戶反映數據不夠即時，檢查 topicPageCache 的 30 秒 TTL
4. **最佳解快取** - 使用 localStorage，若後端數據改變，前端可能不會立即反映

---

最後更新：2026年2月23日
