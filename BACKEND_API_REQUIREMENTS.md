# 前端方案 - 論壇最後回應時間

## 實作方式：前端計算（✅ 不需修改後端 API）

由於 `GET /api/ContentApi` 已經可以查詢所有 content，我們直接在前端處理，**不需要後端新增任何 API 或欄位**。

## 資料表結構（參考）

```sql
table content {
  content_id int [pk, increment]
  parent_content_id int  -- 指向父內容
  category_id int
  user_id int [not null]
  title nvarchar(200) [not null]
  body nvarchar(max) [not null]
  type int [not null]  -- 1:貼文, 2:回文, 3:留言, 4:留言回覆
  created_at datetime
  updated_at datetime
  is_active int [not null]
}
```

## 前端處理邏輯

### 1. 獲取資料

使用現有的 `/api/ContentApi` 兩次查詢：

```javascript
// 查詢 1: 獲取貼文列表（分頁）
const postData = await apiClient.get("/ContentApi", {
  params: {
    page: 1,
    pageSize: 10,
    type: 1, // 只獲取貼文
  },
});

// 查詢 2: 獲取所有回文
const replyData = await apiClient.get("/ContentApi", {
  params: {
    type: 2, // 只獲取回文
    pageSize: 9999, // 獲取所有（或根據需求調整）
  },
});
```

### 2. 計算統計資料

對每篇貼文，從所有回文中篩選並計算：

```javascript
const calculateReplyStats = (postId, allReplies) => {
  // 篩選：parent_content_id === postId 且 type === 2
  const replies = allReplies.filter(
    (c) => c.parentContentId === postId && c.type === 2
  );

  if (replies.length === 0) {
    return {
      replyCount: 0,
      lastReplyTime: null,
      lastReplyUser: null,
    };
  }

  // 排序取最新
  const sortedReplies = replies.sort(
    (a, b) => new Date(b.createdAt) - new Date(a.createdAt)
  );
  const lastReply = sortedReplies[0];

  return {
    replyCount: replies.length,
    lastReplyTime: lastReply.createdAt,
    lastReplyUser: lastReply.username,
  };
};
```

### 3. 組合資料

```javascript
const topics = posts.map((post) => {
  const stats = calculateReplyStats(post.contentId, allReplies);

  return {
    id: post.contentId,
    title: post.title,
    author: post.username,
    createdAt: post.createdAt,

    // 從前端計算的統計
    replyCount: stats.replyCount,
    lastReplyTime: stats.lastReplyTime,
    lastReplyUser: stats.lastReplyUser,
    isHot: stats.replyCount > 10, // 自動判斷熱門
  };
});
```

## 優點

✅ **不需修改後端**：使用現有 API  
✅ **靈活計算**：可自訂熱門文章標準等邏輯  
✅ **即時生效**：無需等待後端部署

## 缺點與優化建議

⚠️ **性能考量**：如果回文數量很大，建議：

- 限制回文查詢範圍（如只查最近 30 天）
- 增加快取機制
- 考慮後端提供聚合欄位（長期方案）

⚠️ **記憶體使用**：一次載入所有回文可能耗費記憶體

- 可改為只載入當前頁面貼文的相關回文

## 後端 API 現狀

目前使用的 API：`GET https://localhost:7190/api/ContentApi`

### 查詢參數

- `type`: 內容類型（1=貼文, 2=回文, 3=留言, 4=留言回覆）
- `page`: 頁碼
- `pageSize`: 每頁筆數
- `categoryId`: 分類 ID（可選）

### 響應格式

```json
{
  "items": [
    {
      "contentId": 1,
      "parentContentId": null, // 貼文的 parent 為 null
      "title": "標題",
      "body": "內容",
      "username": "作者",
      "categoryName": "分類",
      "createdAt": "2026-01-25T10:30:00Z",
      "type": 1
    }
  ],
  "totalCount": 100,
  "totalPages": 10,
  "page": 1,
  "pageSize": 10
}
```

## 實作狀態

✅ 前端已完成修改（`src/views/Contents/forum.vue`）  
✅ 移除對後端新欄位的依賴  
✅ UI 正確顯示回應數量和最後回應資訊  
✅ 無回應時顯示「尚無回應」  
✅ 熱門文章自動判斷（回應數 > 10）
