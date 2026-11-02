# CLAUDE.md

本檔案提供給 Claude Code（與接手的開發者）在此專案工作時的背景、慣例與目前進度。
專案目前由個人維護（原為多人分組專案，程式中的 `//owen`、`//tim`、`//yjy`、`//qiu` 註解為原分工標記）。

## 專案概觀

- **後端（本 repo）**：`incode2/`，ASP.NET Core 9 (`net9.0`) 單一專案，解決方案 `incode2.sln`
  - 同時提供 **前台 REST API**（`ApiControllers/`，給 Vue 前台）與 **後台管理 MVC**（`Controllers/` + `Views/`，Razor）
  - EF Core 9 + SQL Server（Database-First，`Models/` 為產生的實體，擴充放 `Partials/`）
  - SignalR 聊天室 `/chatHub`（`hubs/ChatHub.cs`），Gemini AI 客服（`Services/Chat/GeminiService.cs`）
  - 綠界 ECPay 點數儲值（`Services/Point/EcpayService.cs`）
- **前端**：另一個 repo `YJY-87817/incode_Vue`（Vue 3 + Vite + Pinia + Bootstrap），透過 CORS 帶 Cookie 呼叫 API

### 目錄分層

| 資料夾 | 職責 |
|---|---|
| `ApiControllers/` | 前台 API（`api/[controller]`） |
| `Controllers/` + `Views/` | 後台 MVC |
| `Services/` | 商業邏輯（`IXxxService` + 實作，在 `Program.cs` 註冊 DI） |
| `Models/` / `Partials/` | EF 實體 / partial 擴充 |
| `Dtos/` / `ViewModels/` | API 傳輸物件 / Razor 頁面模型 |
| `Constants/`、`Enums/`、`Extensions/` | 常數、列舉、擴充方法 |

### 模組

| 模組 | API | 後台 | Service |
|---|---|---|---|
| 會員 | `ApiControllers/User/*` | `Account`、`Member`、`Community` | `Services/Users/*` |
| 論壇內容 | `ApiControllers/Content/*` | `Content*` | `Services/Contents/*` |
| 點數 / 任務 / 儲值 | `ApiControllers/Point/*` | `Mission*`、`EntrustMission`、`PointLog`、`PointRecharge` | `Services/Point/*` |
| 點數商城 | `ApiControllers/PointShop/*` | `Product`、`Order`、`Shipped` | `Services/PointShop/*` |
| 客服聊天 | `ApiControllers/Chat/*` + `ChatHub` | `ChatSessions`、`ChatMessages` | `Services/Chat/*` |

## 驗證機制（`Program.cs`）

- 預設 `SmartScheme`：有 `Incode.Admin` Cookie → `AdminAuth`，否則 → `MemberAuth`（`Incode.Member`）
- 前台 API 未登入回 401 / 403（不轉址）
- 後台 Controller 一律加：`[Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]`
  （後台登入 `AccountController` 只允許 `RoleId == Admin`，Claim 角色固定為 `"Admin"`）
- 取得登入者 ID：`User.GetId()`（`Extensions/ClaimsExtensions.cs`）

## 開發慣例（新程式請遵守）

- **擁有者檢查**：前台 API 讀寫資料時必須確認屬於 `User.GetId()`，找不到或非本人一律回 404。
- **API 回應**：使用 `ResultDTO<T>`，並以 `this.ToActionResult(result)`（`Extensions/ResultDTOExtensions.cs`）回傳。
  成功一律 HTTP 200（`Code` 為 204 時也是 200，避免前端拿不到 body）；失敗使用 `Code` 作為 HTTP 狀態碼（400 / 404 / 409 / 500）。
  前端以 `error.response.data.message` 顯示錯誤。
- **不外洩例外**：catch 後用 `ILogger` 記錄，回傳通用訊息，不要把 `ex.Message` 回給前端。
- **點數異動**：透過 `IPointLogService.ChangePointsAsync`（不會自己 SaveChanges，由呼叫端在交易內儲存）。
- **金額 / 價格**：一律以資料庫為準由後端計算，不信任前端或表單傳入的總額、單價。
- **訂單狀態**：使用 `Constants/OrderStatus.cs`（待處理、已出貨、已完成、已取消、其他），不要寫死字串。
- **圖片上傳**：先用 `Services/ImageFileValidator.Validate()` 檢查（僅 jpg/png/gif/webp + 檔頭比對，單檔 20 MB）；
  刪除檔案時檔名以資料庫為準並 `Path.GetFileName()`，不可使用表單傳入的路徑。
- **後台訊息**：Controller 用 `TempData["SuccessMessage"] / TempData["ErrorMessage"]`，View 放 `<partial name="_TempDataAlert" />`。
- **表單**：POST 表單要有 `method="post"`，Action 加 `[ValidateAntiForgeryToken]`。
- 檔案編碼：保持原檔的 BOM / 換行格式，避免整檔 diff。

### 點數商城訂單（`Services/PointShop/OrderService.cs`）

前台 `OrderApiController` 與後台 `OrderController` 共用。`CreateOrderAsync` / `UpdateOrderItemsAsync` / `CancelOrderAsync`：

- 整筆在交易內（`RunInTransactionAsync`），業務失敗丟 `OrderFailure` → rollback + 回傳訊息
- 庫存用 `ExecuteUpdateAsync` 原子更新（`WHERE Stock >= 數量`），商品讀取用 `AsNoTracking`，**不要**改 tracked 實體的 `Stock` 再 SaveChanges
- 同一會員的點數異動以 `sp_getapplock`（`point-user-{userId}`）排隊；非 SQL Server（測試用 SQLite）時略過
- 明細同商品會合併；數量必須 > 0；新加入的商品必須上架中
- 權限 / 狀態檢查由呼叫端負責：會員只能操作自己的「待處理」訂單；已取消訂單不可改回其他狀態

## 建置與驗證

- 需要 .NET 9 SDK、SQL Server（`appsettings.json` 的 `ConnectionStrings:incodedatabase`，預設 LocalDB）
- 機密設定（Google OAuth、ECPay、Gemini 等）放 user-secrets 或環境變數，不要提交到 repo
- 專案沒有自動化測試。雲端環境若只有 .NET 8 SDK，可複製一份改 `net8.0`、ASP.NET Core 套件降為 8.x 做編譯檢查；
  訂單邏輯曾以 SQLite in-memory（`Foreign Keys=False`）寫臨時測試驗證
- `wwwroot/img/` 下有使用者上傳的圖片被提交進 git，新增上傳功能時注意不要再提交上傳檔

## 修正紀錄

分支 `claude/vibrant-ride-w4z8f7`（前後端 repo 同名分支）：

1. 點數商城訂單 API：擁有者檢查、修改訂單補扣 / 退點、數量驗證、只有「待處理」可修改 / 取消
2. 點數商城後端重構：交易一致性、原子扣庫存、點數鎖、重複商品合併、後台建單 / 修改改用 OrderService、
   修正後台修改訂單點數類型相反與可改會員、已取消訂單不可恢復、HTTP 狀態碼、圖片上傳驗證與權限、
   後台商品圖片路徑穿越刪檔、寄送資料刪除表單缺 `method="post"`
3. 前端點數商城（`incode_Vue`）：錯誤修正與重構（見前端 repo 的 CLAUDE.md）

## 全專案檢查（進行中）

點數商城以外的模組正在逐一檢查，以下為**已確認、尚未修正**的問題（依嚴重度）：

### 嚴重

- **後台點數相關 Controller 完全沒有 `[Authorize]`**：`Controllers/Point/` 下的
  `PointLogController`、`PointRechargeController`、`MissionController`、`MissionProgressController`、`EntrustMissionController`。
  匿名使用者可直接進入後台頁面，例如在 `PointLog/Create` 新增任意點數紀錄（等於無限加點）。
  `Controllers/Users/CommunityController.cs` 也沒有（會列出所有會員）。
- **`PointRechargeApiController` 的 GET / PUT / DELETE 沒有登入檢查**：任何人可列出、修改、刪除所有人的儲值紀錄（只有 `recharge` 有 `[Authorize]`）。
- **`ChatSessionsApiController` 沒有登入與擁有者檢查**：`User/{userId}` 可讀任何人的對話列表，`Create` 可替任意 userId 建立，`Delete/{id}` 可刪任何人的對話與訊息。
- **`ContentNotificationApiController` 沒有登入檢查**：可匿名讀取 / 修改 / 新增 / 刪除所有通知紀錄。
- **`CategoryApiController`** 沒有 `[Authorize]`，POST 新增分類可匿名呼叫。

### 中

- **ECPay 回呼冪等性**（`PointRechargeApiController.EcpayReturn`）：「檢查是否已成功 → 加點 → 更新狀態」沒有鎖，
  綠界重送或同時回呼可能重複加點；`AddPointsFromRechargeAsync` 先 commit 加點，之後更新儲值狀態失敗時，重試會再加一次。
- 儲值金額 `CashAmount` 沒有驗證（0、負數、超大值）。

### 低 / 程式品質

- 誤加的 `using`：`ViewModels/Users/CommunityViewModel.cs`（`Microsoft.Build.Framework`，目前未造成錯誤）、
  `Services/Point/PointLogService.cs`（`NuGet.Protocol.Plugins`）、`Partials/ContentCategory.cs`（`Microsoft.Build.Evaluation`）。
- `Program.cs`：`UseRouting` / `UseCors` / `MapControllers` 重複呼叫，CORS 允許來源寫了兩份且不一致。
- 專案同時參考 EF Core Sqlite 與 SqlServer，只用到 SqlServer。

### 尚未檢查

會員 Auth（註冊、登入、Google 登入、忘記 / 重設密碼）、`UserApiController`、論壇內容與互動（打賞點數轉移、委託任務最佳解）、
`ChatMessagesApiController`（圖片上傳、AdminReply）、`ChatHub` 群組授權、點數任務領獎流程。
