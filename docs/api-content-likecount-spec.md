# ContentApi 回傳按讚數（likeCount）規格

## 1) 需求背景

前端論壇頁目前需要顯示每篇內容（主題/回文）的「按讚總數」。
為了避免前端逐篇打互動 API 造成 N+1 請求，建議由 `ContentApi` 在列表與詳情回傳時直接附上 `likeCount`。

---

## 2) 目標

- 在 `ContentApi` 列表查詢（`GET /ContentApi`）每筆 item 回傳 `likeCount`
- 在 `ContentApi` 單筆詳情（`GET /ContentApi/{id}`）回傳 `likeCount`
- `likeCount` 定義為：該 content 在 `content_interaction` 中，`interaction_category_id = 1`（Like）的有效筆數

> 若目前按讚為「切換式（同一使用者同一篇最多一筆）」設計，`likeCount` 即為目前存在的 Like 紀錄數。

---

## 3) DTO 變更

請在現有 `ContentDto` 新增欄位：

- `likeCount`（型別：`int`，不可為 null）
- 預設值：`0`

### 建議 JSON 命名

若目前 API 已採 camelCase，請回傳：

```json
{
  "contentId": 123,
  "title": "...",
  "type": 1,
  "likeCount": 8
}
```

---

## 4) API 契約（回傳）

### A. `GET /ContentApi`（列表）

- `items[]` 每筆都需包含 `likeCount`

#### 範例

```json
{
  "items": [
    {
      "contentId": 101,
      "title": "第一篇",
      "type": 1,
      "replyCount": 3,
      "likeCount": 12
    },
    {
      "contentId": 102,
      "title": "第二篇",
      "type": 2,
      "replyCount": 0,
      "likeCount": 0
    }
  ],
  "totalCount": 2,
  "currentPage": 1,
  "totalPages": 1
}
```

### B. `GET /ContentApi/{id}`（單筆）

- 回傳物件需包含 `likeCount`

#### 範例

```json
{
  "contentId": 101,
  "title": "第一篇",
  "type": 1,
  "body": "...",
  "likeCount": 12
}
```

---

## 5) 查詢實作建議（重點：一次查完）

## 5.1 互動分類

- Like 類別固定：`interaction_category_id = 1`

## 5.2 列表查詢（避免 N+1）

請在內容列表查詢時，一次性 Join/Group 計算每篇內容 like 數：

### SQL 概念

```sql
SELECT
  c.id AS content_id,
  c.title,
  c.type,
  COALESCE(li.like_count, 0) AS like_count
FROM content c
LEFT JOIN (
  SELECT
    ci.content_id,
    COUNT(*) AS like_count
  FROM content_interaction ci
  WHERE ci.interaction_category_id = 1
  GROUP BY ci.content_id
) li ON li.content_id = c.id
WHERE ...
ORDER BY ...
LIMIT ... OFFSET ...;
```

### EF Core 概念（示意）

1. 先取得當頁 `contentIds`
2. 一次查 `content_interaction`：
   - `Where(ci => ci.InteractionCategoryId == 1 && contentIds.Contains(ci.ContentId))`
   - `GroupBy(ci => ci.ContentId)`
   - `Select(g => new { ContentId = g.Key, LikeCount = g.Count() })`
3. 轉 dictionary 後映射到 DTO，無資料給 `0`

---

## 6) 效能與索引建議

建議建立（或確認）以下索引：

1. `content_interaction (interaction_category_id, content_id)`
2. 若有切換式唯一限制，建議加上唯一鍵：`(user_id, content_id, interaction_category_id)`

這可加速 Like count 聚合與避免重複紀錄。

---

## 7) 相容性與風險

- 新增欄位為向後相容（舊前端可忽略）
- 請確認所有 `ContentApi` 回傳路徑（列表/詳情/可能的 children）命名一致（都用 `likeCount`）
- 若系統有軟刪除/無效標記，count 時需排除無效互動

---

## 8) 驗收標準（DoD）

1. `GET /ContentApi` 的每筆 item 都有 `likeCount`（整數，>= 0）
2. `GET /ContentApi/{id}` 有 `likeCount`（整數，>= 0）
3. 對同一篇文章按讚/取消後，再查詢 `ContentApi`，`likeCount` 會正確變化
4. 列表頁 20 筆資料查詢，不產生逐篇互動 N+1 API 呼叫
5. 壓測下查詢時間維持可接受（依後端 SLA）

---

## 9) 前端對接約定

前端會直接讀取：

- `item.likeCount`（列表）
- `detail.likeCount`（詳情）

若欄位缺失，前端暫時以 `0` 顯示，但請後端盡快補齊欄位以避免顯示不一致。
