# ContentInteraction API 完整文件

## ?? 概述

本文件說明 ContentInteraction API 的完整功能，包括查詢互動記錄、最佳解答管理等功能。

**基礎 URL：** `https://localhost:7190/api/contentinteractionapi`

**專案環境：**
- .NET 9
- ASP.NET Core Razor Pages
- SQL Server Database

---

## ?? API 端點總覽

| HTTP 方法 | 端點 | 說明 |
|-----------|------|------|
| GET | `/content/{contentId}` | 取得特定內容的所有互動 |
| GET | `/user/{contentId}` | 取得當前使用者對特定內容的互動（需登入） |
| GET | `/category` | 取得所有互動分類 |
| GET | `/stats/{contentId}` | 統計特定內容的互動數量 |
| GET | `/best-answer/{entrustMissionId}` | 查詢委託任務的最佳解答（路徑參數） |
| GET | `/best-answer?entrustMissionId={id}` | 查詢委託任務的最佳解答（Query String） |
| GET | `/?interactionCategoryId={id}` | 依條件查詢互動記錄 |
| POST | `/` | 新增或切換互動（Toggle） |
| POST | `/best-answer` | 指定最佳解答 |
| DELETE | `/{contentId}/{userId}/{interactionCategoryId}` | 刪除特定互動 |

---

## ?? API 詳細說明

### 1. 查詢委託任務的最佳解答

#### **路徑參數版本**

```
GET /api/contentinteractionapi/best-answer/{entrustMissionId}
```

**參數：**
- `entrustMissionId` (路徑參數, int) - 委託任務 ID

**回應範例：**

```json
{
  "entrustMissionId": 12,
  "bestAnswerContentId": 45
}
```

**如果沒有最佳解答：**

```json
{
  "entrustMissionId": 12,
  "bestAnswerContentId": null
}
```

---

#### **Query String 版本**

```
GET /api/contentinteractionapi/best-answer?entrustMissionId={id}
```

**參數：**
- `entrustMissionId` (Query String, int) - 委託任務 ID

**範例請求：**

```javascript
// Axios 範例
const response = await axios.get('/api/contentinteractionapi/best-answer', {
  params: { entrustMissionId: 12 }
})

// Fetch 範例
const response = await fetch(
  'https://localhost:7190/api/contentinteractionapi/best-answer?entrustMissionId=12'
)
```

**回應格式：** 與路徑參數版本相同

---

### 2. 彈性查詢互動記錄

```
GET /api/contentinteractionapi
```

**Query 參數（全部可選）：**
- `contentId` (int, optional) - 內容 ID
- `userId` (int, optional) - 使用者 ID
- `interactionCategoryId` (int, optional) - 互動分類 ID
- `entrustMissionId` (int, optional) - 委託任務 ID

**範例請求 1：** 查詢「最佳解答」類別的所有互動
```
GET /api/contentinteractionapi?interactionCategoryId=5
```

**範例請求 2：** 查詢委託任務的互動記錄
```
GET /api/contentinteractionapi?entrustMissionId=12
```

**範例請求 3：** 查詢特定使用者對特定內容的互動
```
GET /api/contentinteractionapi?contentId=10&userId=3
```

**回應範例：**

```json
{
  "interactions": [
    {
      "contentInteractionId": 123,
      "interactionCategoryId": 5,
      "interactionCategoryName": "最佳解答",
      "contentId": 45,
      "userId": 8,
      "createdAt": "2025-01-15T10:30:00"
    }
  ],
  "count": 1
}
```

**如果沒有找到任何互動：**

```json
{
  "interactions": [],
  "count": 0
}
```

---

### 3. 取得特定內容的所有互動

```
GET /api/contentinteractionapi/content/{contentId}
```

**參數：**
- `contentId` (路徑參數, int) - 內容 ID

**回應範例：**

```json
{
  "contentId": 10,
  "interactions": [
    {
      "contentInteractionId": 1,
      "interactionCategoryId": 1,
      "interactionCategoryName": "讚",
      "contentId": 10,
      "userId": 5,
      "createdAt": "2025-01-15T10:00:00"
    },
    {
      "contentInteractionId": 2,
      "interactionCategoryId": 2,
      "interactionCategoryName": "收藏",
      "contentId": 10,
      "userId": 8,
      "createdAt": "2025-01-15T11:00:00"
    }
  ],
  "count": 2
}
```

---

### 4. 取得當前使用者對特定內容的互動 ??

```
GET /api/contentinteractionapi/user/{contentId}
```

**需要登入：** ? 是

**參數：**
- `contentId` (路徑參數, int) - 內容 ID

**回應範例：**

```json
{
  "contentId": 10,
  "userId": 3,
  "interactions": [
    {
      "contentInteractionId": 1,
      "interactionCategoryId": 1,
      "interactionCategoryName": "讚",
      "contentId": 10,
      "userId": 3,
      "createdAt": "2025-01-15T10:00:00"
    }
  ],
  "count": 1
}
```

**如果使用者未互動：**

```json
{
  "message": "此使用者尚未對該內容進行互動",
  "interactions": []
}
```

---

### 5. 新增或切換互動（Toggle） ??

```
POST /api/contentinteractionapi
```

**需要登入：** ? 是

**Request Body：**

```json
{
  "interactionCategoryId": 1,
  "contentId": 10
}
```

**行為說明：**
- 如果使用者已經有該互動 → 移除互動
- 如果使用者沒有該互動 → 新增互動

**回應範例（新增互動）：**

```json
{
  "message": "互動已新增或更新",
  "isRemoved": false,
  "data": {
    "contentInteractionId": 123,
    "interactionCategoryId": 1,
    "interactionCategoryName": "讚",
    "contentId": 10,
    "userId": 3,
    "createdAt": "2025-01-15T10:30:00"
  }
}
```

**回應範例（移除互動）：**

```json
{
  "message": "互動已移除",
  "isRemoved": true,
  "data": null
}
```

---

### 6. 指定最佳解答 ??

```
POST /api/contentinteractionapi/best-answer
```

**需要登入：** ? 是（且必須是委託任務發起者）

**Request Body：**

```json
{
  "entrustMissionId": 12,
  "contentId": 45
}
```

**成功回應：**

```json
{
  "ok": true,
  "code": 200,
  "message": "最佳解答已選定",
  "data": {
    "entrustMissionId": 12,
    "bestAnswerContentId": 45,
    "rewardPoints": 100,
    "rewardedUserId": 8
  }
}
```

**錯誤回應範例：**

```json
{
  "ok": false,
  "code": 403,
  "message": "只有委託任務發起者可以選擇最佳解答"
}
```
```json
{
  "ok": false,
  "code": 409,
  "message": "最佳解答已經設定過"
}
```

---

### 7. 取得互動分類列表

```
GET /api/contentinteractionapi/category
```

**回應範例：**

```json
[
  {
    "interactionCategoryId": 1,
    "name": "讚",
    "isActive": 1
  },
  {
    "interactionCategoryId": 2,
    "name": "收藏",
    "isActive": 1
  },
  {
    "interactionCategoryId": 5,
    "name": "最佳解答",
    "isActive": 1
  }
]
```

---

### 8. 統計內容的互動數量

```
GET /api/contentinteractionapi/stats/{contentId}
```

**參數：**
- `contentId` (路徑參數, int) - 內容 ID

**回應範例：**

```json
{
  "contentId": 10,
  "interactionStats": [
    {
      "interactionType": "讚",
      "count": 15
    },
    {
      "interactionType": "收藏",
      "count": 8
    }
  ],
  "totalInteractions": 23
}
```

---

### 9. 刪除特定互動

```
DELETE /api/contentinteractionapi/{contentId}/{userId}/{interactionCategoryId}
```

**參數：**
- `contentId` (路徑參數, int) - 內容 ID
- `userId` (路徑參數, int) - 使用者 ID
- `interactionCategoryId` (路徑參數, int) - 互動分類 ID

**成功回應：**

```json
{
  "message": "互動已移除"
}
```

**失敗回應：**

```json
{
  "message": "找不到此互動記錄"
}
```

---

## ?? 資料模型

### ContentInteractionDto

```typescript
interface ContentInteractionDto {
  contentInteractionId: number
  interactionCategoryId: number
  interactionCategoryName: string
  contentId: number
  userId: number
  createdAt: string  // ISO 8601 格式
}
```

### InteractionCategory

```typescript
interface InteractionCategory {
  interactionCategoryId: number
  name: string
  isActive: number  // 1 = 啟用, 0 = 停用
}
```

---

## ?? 前端整合範例

### 範例 1：查詢委託任務的最佳解答

```javascript
async function getBestAnswer(entrustMissionId) {
  try {
    const response = await axios.get('/api/contentinteractionapi/best-answer', {
      params: { entrustMissionId }
    })
    
    if (response.data.bestAnswerContentId) {
      console.log('最佳解答 ID:', response.data.bestAnswerContentId)
      // 在 UI 上標記該回覆為最佳解答
    } else {
      console.log('尚無最佳解答')
      // 顯示「選擇最佳解答」按鈕
    }
  } catch (error) {
    console.error('查詢失敗:', error)
  }
}
```

---

### 範例 2：Toggle 使用者的「讚」互動

```javascript
async function toggleLike(contentId) {
  try {
    const response = await axios.post('/api/contentinteractionapi', {
      interactionCategoryId: 1,  // 1 = 讚
      contentId: contentId
    })
    
    if (response.data.isRemoved) {
      console.log('已取消讚')
      // 更新 UI：移除讚的圖示
    } else {
      console.log('已按讚')
      // 更新 UI：顯示已按讚
    }
  } catch (error) {
    console.error('操作失敗:', error)
  }
}
```

---

### 範例 3：查詢特定分類的互動

```javascript
async function getBestAnswerInteractions(entrustMissionId) {
  try {
    const response = await axios.get('/api/contentinteractionapi', {
      params: {
        interactionCategoryId: 5,  // 5 = 最佳解答
        entrustMissionId: entrustMissionId
      }
    })
    
    console.log('最佳解答互動記錄:', response.data.interactions)
  } catch (error) {
    console.error('查詢失敗:', error)
  }
}
```

---

### 範例 4：選擇最佳解答

```javascript
async function selectBestAnswer(entrustMissionId, contentId) {
  try {
    const response = await axios.post('/api/contentinteractionapi/best-answer', {
      entrustMissionId: entrustMissionId,
      contentId: contentId
    })
    
    if (response.data.ok) {
      console.log('最佳解答已設定')
      console.log('獎勵點數:', response.data.data.rewardPoints)
      // 刷新頁面或更新 UI
    }
  } catch (error) {
    if (error.response?.status === 409) {
      alert('最佳解答已經設定過')
    } else if (error.response?.status === 403) {
      alert('只有委託任務發起者可以選擇最佳解答')
    }
  }
}
```

---

## ?? 認證說明

### 需要登入的端點

以下端點需要有效的 Cookie 認證：

- `GET /api/contentinteractionapi/user/{contentId}`
- `POST /api/contentinteractionapi`
- `POST /api/contentinteractionapi/best-answer`

### Cookie 設定

前端需確保請求包含認證 Cookie：

```javascript
// Axios 全域設定
axios.defaults.withCredentials = true

// Fetch 設定
fetch(url, {
  credentials: 'include'
})
```

---

## ?? 錯誤處理

### HTTP 狀態碼

| 狀態碼 | 說明 |
|--------|------|
| 200 | 成功 |
| 400 | 請求參數錯誤 |
| 401 | 未登入或認證失效 |
| 403 | 權限不足 |
| 404 | 資源不存在 |
| 409 | 衝突（例如：重複設定最佳解答） |
| 500 | 伺服器錯誤 |

### 錯誤回應格式

```json
{
  "message": "錯誤訊息描述"
}
```

或（最佳解答相關）：

```json
{
  "ok": false,
  "code": 403,
  "message": "只有委託任務發起者可以選擇最佳解答"
}
```

---

## ?? 重要注意事項

### 1. 最佳解答查詢

- 如果 `bestAnswerContentId` 為 `null`，表示**真的沒有設定最佳解答**
- 前端**不應該**預設將第一則回覆當作最佳解答
- 應顯示「尚無最佳解答」或「選擇最佳解答」按鈕

### 2. 互動 Toggle 機制

- 同一個 `content + user + category` 組合，第一次呼叫會**新增**互動
- 再次呼叫會**移除**互動
- 不同的互動類別可以同時存在（例如同時按讚和收藏）

### 3. 最佳解答限制

- 只有委託任務發起者可以選擇最佳解答
- 發起者不能選擇自己的回覆為最佳解答
- 每個委託任務只能設定一次最佳解答（無法更改）

### 4. 查詢彈性

使用 `GET /api/contentinteractionapi` 可以組合多種條件：
- 單一條件：`?contentId=10`
- 多個條件：`?contentId=10&userId=3`
- 所有參數都是可選的

---

## ?? CORS 設定

後端已設定允許以下來源：

- `https://localhost:5173`
- `https://localhost:5174`
- `http://localhost:5173`
- `http://localhost:5174`
- `http://localhost:8080`
- `http://localhost:3000`
- `http://localhost:7190`
- `http://localhost:3001`

---

## ?? 聯絡資訊

如有問題或需要協助，請聯絡後端開發團隊。

**API 版本：** v1.0  
**最後更新：** 2025-01-15

---

## 附錄：完整 TypeScript 類型定義

```typescript
// 互動記錄 DTO
interface ContentInteractionDto {
  contentInteractionId: number
  interactionCategoryId: number
  interactionCategoryName: string
  contentId: number
  userId: number
  createdAt: string
}

// 互動分類
interface InteractionCategory {
  interactionCategoryId: number
  name: string
  isActive: number
}

// 新增互動請求
interface CreateContentInteractionDto {
  interactionCategoryId: number
  contentId: number
}

// 選擇最佳解答請求
interface SelectBestAnswerDto {
  entrustMissionId: number
  contentId: number
}

// 最佳解答回應
interface BestAnswerResponse {
  entrustMissionId: number
  bestAnswerContentId: number | null
}

// 互動列表回應
interface InteractionsResponse {
  interactions: ContentInteractionDto[]
  count: number
}

// 統計回應
interface StatsResponse {
  contentId: number
  interactionStats: Array<{
    interactionType: string
    count: number
  }>
  totalInteractions: number
}

// 標準回應
interface ResultDTO {
  ok: boolean
  code: number
  message: string
  data?: any
}
```
