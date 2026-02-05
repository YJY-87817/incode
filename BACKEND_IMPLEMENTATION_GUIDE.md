# 後端實現指南 - 內容編輯與刪除功能

## ✅ 現況檢查結果

經過檢查你的後端代碼，**大部分功能已經實現**！👍

### 已完成的功能 ✅

1. ✅ **編輯 API** - `PUT /ContentApi/{id}` 已實現
2. ✅ **刪除 API** - `DELETE /ContentApi/{id}` 已實現
3. ✅ **UserId 字段** - 所有 GET API 已返回 UserId
4. ✅ **編輯權限檢查** - UpdateContent 已驗證擁有者
5. ✅ **軟刪除** - DeleteContent 設置 IsActive = 2
6. ✅ **編輯歷史** - 已保存到 ContentHistory 表

---

## ⚠️ 需要的小修改

只需要做 **2 個小調整**：

### 1. DeleteContent 需要添加權限檢查 ⚠️

**問題**: 目前任何人都可以刪除任何內容

**位置**: `ContentApiController.cs` 第 68-74 行

**現有代碼**:

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteContent(int id)
{
  var success = await _contentService.DeleteContent(id);
  if (!success) return NotFound(new { message = "找不到此內容" });
  return Ok(new { message = "刪除成功" });
}
```

**修改為**:

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteContent(int id)
{
  var currentUserId = User.GetId();
  if (currentUserId <= 0)
  {
    return Unauthorized(new { message = "請先登入" });
  }

  var success = await _contentService.DeleteContent(id, currentUserId);
  if (!success) return NotFound(new { message = "找不到此內容或無權限刪除" });
  return Ok(new { message = "刪除成功" });
}
```

---

### 修改 ContentService.DeleteContent 方法

**位置**: `ContentService.cs` 第 161-169 行

**現有代碼**:

```csharp
public async Task<bool> DeleteContent(int id)
{
  var content = await _context.Content.FindAsync(id);
  if (content == null) return false;

  content.IsActive = 2;
  content.UpdatedAt = DateTime.Now;
  await _context.SaveChangesAsync();
  return true;
}
```

**修改為**:

```csharp
public async Task<bool> DeleteContent(int id, int currentUserId)
{
  var content = await _context.Content.FindAsync(id);
  if (content == null) return false;

  // ⭐ 添加權限檢查
  if (content.UserId != currentUserId)
  {
    return false; // 不是作者，無權刪除
  }

  content.IsActive = 2;
  content.UpdatedAt = DateTime.Now;
  await _context.SaveChangesAsync();
  return true;
}
```

---

### 修改 IContentService 接口

**位置**: `IContentService.cs`（需要找到這個檔案）

**現有簽名**:

```csharp
Task<bool> DeleteContent(int id);
```

**修改為**:

```csharp
Task<bool> DeleteContent(int id, int currentUserId);
```

---

### 2. UpdateContent 返回完整資料（可選，建議修改）

**問題**: 目前只返回 title 和 body，前端可能需要更多資料

**位置**: `ContentService.cs` 第 145-159 行

**現有代碼**:

```csharp
return new UpdateContentDto
{
  Title = content.Title,
  Body = content.Body
};
```

**建議修改為**:

```csharp
return new UpdateContentDto
{
  Title = content.Title,
  Body = content.Body,
  UpdatedAt = content.UpdatedAt  // 添加更新時間
};
```

同時需要在 `UpdateContentDto` 中添加 `UpdatedAt` 屬性：

```csharp
public class UpdateContentDto
{
  public string Title { get; set; }
  public string Body { get; set; }
  public DateTime? UpdatedAt { get; set; }  // ⭐ 添加這行
}
```

---

## 📋 完整修改清單

### 必須修改（權限檢查）

- [ ] `ContentApiController.cs` - DeleteContent 方法添加 currentUserId
- [ ] `ContentService.cs` - DeleteContent 方法添加權限檢查
- [ ] `IContentService.cs` - 更新 DeleteContent 方法簽名

### 可選修改（優化）

- [ ] `UpdateContentDto.cs` - 添加 UpdatedAt 屬性
- [ ] `ContentService.cs` - UpdateContent 返回 UpdatedAt

---

## 🧪 測試建議

### 測試編輯功能

```http
PUT https://localhost:7190/api/ContentApi/123
Content-Type: application/json

{
  "title": "測試編輯標題",
  "body": "<p>測試編輯內容</p>"
}
```

**預期結果**:

- ✅ 作者可以編輯 → 200 OK
- ❌ 非作者嘗試編輯 → 返回 null（前端會看到錯誤）
- ✅ ContentHistory 表會新增一筆記錄

### 測試刪除功能（修改後）

```http
DELETE https://localhost:7190/api/ContentApi/123
```

**預期結果**:

- ✅ 作者可以刪除 → 200 OK，IsActive = 2
- ❌ 非作者嘗試刪除 → 404 Not Found
- ✅ 刪除後內容不出現在列表中
- ✅ 子內容（回文、留言）仍然可見

---

## 🔍 驗證 UserId 是否正確返回

檢查以下 API 是否都有返回 `UserId`:

### ✅ 已確認返回 UserId 的 API

1. **GET /ContentApi** (GetContents)

   ```csharp
   UserId = c.UserId,  // ✅ 第 41 行
   ```

2. **GET /ContentApi/{id}** (GetContent)

   ```csharp
   UserId = c.UserId   // ✅ 第 83 行
   ```

3. **GET /ContentApi/{id}/comments** (GetCommentsByContentId)

   ```csharp
   UserId = c.UserId,  // ✅ 第 183 行
   ```

4. **GET /ContentApi/me/posts**, **me/replies**, **me/comments**
   - 都有返回 UserId ✅

5. **GET /ContentApi/me/liked-contents**, **me/bookmarked-contents**
   - 都有返回 UserId ✅

**結論**: 所有 API 都正確返回了 `UserId`，前端可以正常判斷權限！✅

---

## 🎯 前端已實現的功能

為了方便你理解整體流程，這是前端已經完成的部分：

### 1. 權限判斷

```javascript
// InteractionButtons.vue
const isOwnContent = computed(() => {
  return (
    authStore.user?.id &&
    props.authorUserId &&
    authStore.user.id === props.authorUserId
  );
});
```

### 2. UI 顯示

- 只有作者能看到「更多」按鈕
- 點擊顯示「編輯」和「刪除」選項

### 3. 編輯流程

```javascript
// 1. 點擊編輯 → 開啟 NewTopicModal
// 2. 預填內容（標題、內文）
// 3. 提交 → 調用 PUT /ContentApi/{id}
// 4. 成功 → 重新載入數據
```

### 4. 刪除流程

```javascript
// 1. 點擊刪除 → 顯示確認對話框
// 2. 確認 → 調用 DELETE /ContentApi/{id}
// 3. 成功 → 重新載入數據
```

---

## 📱 API 調用示例

### 前端調用編輯 API

```javascript
// contentInteractionApi.js
updateContent(contentId, { title, body }) {
  return apiClient.put(`/ContentApi/${contentId}`, {
    title,
    body,
  });
}
```

### 前端調用刪除 API

```javascript
// contentInteractionApi.js
deleteContent(contentId) {
  return apiClient.delete(`/ContentApi/${contentId}`);
}
```

---

## ⚡ 快速實施步驟

### 第 1 步：修改 ContentApiController.cs

在 `DeleteContent` 方法中添加 userId 參數：

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteContent(int id)
{
  var currentUserId = User.GetId();
  if (currentUserId <= 0)
    return Unauthorized(new { message = "請先登入" });

  var success = await _contentService.DeleteContent(id, currentUserId);
  if (!success)
    return NotFound(new { message = "找不到此內容或無權限刪除" });

  return Ok(new { message = "刪除成功" });
}
```

### 第 2 步：修改 ContentService.cs

在 `DeleteContent` 方法中添加權限檢查：

```csharp
public async Task<bool> DeleteContent(int id, int currentUserId)
{
  var content = await _context.Content.FindAsync(id);
  if (content == null) return false;

  // 權限檢查
  if (content.UserId != currentUserId) return false;

  content.IsActive = 2;
  content.UpdatedAt = DateTime.Now;
  await _context.SaveChangesAsync();
  return true;
}
```

### 第 3 步：更新 IContentService 接口

```csharp
Task<bool> DeleteContent(int id, int currentUserId);
```

### 完成！🎉

修改完成後，重新編譯和運行專案，功能就完整了！

---

## 💡 其他建議

### 1. 考慮添加管理員權限

如果需要管理員可以刪除任何內容：

```csharp
public async Task<bool> DeleteContent(int id, int currentUserId)
{
  var content = await _context.Content.FindAsync(id);
  if (content == null) return false;

  // 檢查是否為作者或管理員
  var user = await _context.User.FindAsync(currentUserId);
  bool isOwner = content.UserId == currentUserId;
  bool isAdmin = user?.Role == "Admin"; // 假設有 Role 欄位

  if (!isOwner && !isAdmin) return false;

  content.IsActive = 2;
  content.UpdatedAt = DateTime.Now;
  await _context.SaveChangesAsync();
  return true;
}
```

### 2. 考慮添加日誌記錄

在重要操作中添加日誌：

```csharp
// 刪除時記錄
_logger.LogInformation($"User {currentUserId} deleted content {id}");

// 編輯時記錄
_logger.LogInformation($"User {currentUserId} updated content {id}");
```

### 3. 考慮添加軟刪除原因

可以在 Content 表添加 `DeleteReason` 欄位（可選）。

---

## 📞 需要幫助？

如果修改過程中遇到問題，可以提供：

1. 錯誤訊息截圖
2. 修改後的代碼片段
3. 前端 Console 的錯誤訊息

---

## 總結

你的後端代碼質量很好！只需要：

1. ✅ 在 DeleteContent 添加權限檢查（**必須**）
2. ✅ 考慮在 UpdateContent 返回更多資料（可選）

其他功能都已經完整實現了！👏
