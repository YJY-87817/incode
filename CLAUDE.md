# CLAUDE.md

本檔案提供給 Claude Code（與接手的開發者）在此專案工作時的背景、慣例與目前進度。
專案目前由個人維護（原為多人分組專案，程式中的 `//owen`、`//tim`、`//yjy`、`//qiu` 註解為原分工標記）。

## 專案概觀

- **後端（本 repo）**：`incode2/`，ASP.NET Core 9 (`net9.0`) 單一專案，解決方案 `incode2.sln`
  - 同時提供 **前台 REST API**（`ApiControllers/`，給 Vue 前台）與 **後台管理 MVC**（`Controllers/` + `Views/`，Razor）
  - EF Core 9 + SQL Server（Database-First，`Models/` 為產生的實體，擴充放 `Models/Partials/`）
  - SignalR 聊天室 `/chatHub`（`Hubs/ChatHub.cs`），Gemini AI 客服（`Services/Chat/GeminiService.cs`）
  - 綠界 ECPay 點數儲值（`Services/Point/EcpayService.cs`）
- **前端**：另一個 repo `YJY-87817/incode_Vue`（Vue 3 + Vite + Pinia + Bootstrap），透過 CORS 帶 Cookie 呼叫 API

### 目錄分層

```
incode/
├── incode2.sln
├── docs/                       專案文件（資料表設計 dbdiagram、Content API 導覽）
└── incode2/
    ├── Program.cs              DI 註冊、驗證、CORS、Middleware
    ├── GlobalUsings.cs         專案內所有命名空間的 global using（新增資料夾時記得補上）
    ├── ApiControllers/<模組>/  前台 REST API（api/[controller]）；Common/ 放共用的圖片 API
    ├── Controllers/<模組>/     後台 MVC（HomeController 在根目錄）
    ├── Views/                  後台 Razor View（依 Controller 名稱，_ViewImports 已引入 ViewModels.*）
    ├── Hubs/                   SignalR
    ├── Services/<模組>/        商業邏輯（IXxxService + 實作）；Common/ 放圖片上傳與檢查
    ├── Models/                 EF 產生的實體（勿手改）
    │   ├── Partials/           實體 / DbContext 的 partial 擴充（命名空間必須是 incode2.Models）
    │   └── Metadata/           [ModelMetadataType] 用的驗證 / 顯示設定
    ├── Dtos/<模組>/            API 傳輸物件（ResultDTO、PagedResultDto 在根目錄）
    ├── ViewModels/<模組>/      後台頁面模型
    ├── Constants/              常數與列舉（OrderStatus、PointType、RoleType）
    ├── Extensions/             擴充方法（ClaimsExtensions、ResultDTOExtensions）
    ├── Validation/             自訂驗證屬性
    ├── Prompts/Prompts.md      AI 客服系統提示詞（執行時讀取，路徑不可改）
    └── wwwroot/                靜態檔；contents/ 與 img/ 下為執行時產生的資料（論壇內文、上傳圖片）
```

- 模組名稱統一：`Chat`、`Contents`、`Point`、`PointShop`、`Users`，**命名空間 = `incode2.<資料夾路徑>`**
  （例：`Services/Users/AuthService.cs` → `incode2.Services.Users`）。
- 專案內命名空間由 `GlobalUsings.cs` 統一引入，檔案內不需再寫 `using incode2.*`。
- 原始碼一律 UTF-8（部分舊檔原為 Big5，已轉換；Big5 在非 Windows 環境編譯時中文字串會變亂碼）。

### 模組

| 模組 | API | 後台 | Service |
|---|---|---|---|
| 會員 | `ApiControllers/Users/*` | `Account`、`Member`、`Community` | `Services/Users/*` |
| 論壇內容 | `ApiControllers/Contents/*` | `Content*` | `Services/Contents/*` |
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
- **圖片上傳**：先用 `Services/Common/ImageFileValidator.Validate()` 檢查（僅 jpg/png/gif/webp + 檔頭比對，單檔 20 MB）；
  刪除檔案時檔名以資料庫為準並 `Path.GetFileName()`，不可使用表單傳入的路徑。
- **後台訊息**：Controller 用 `TempData["SuccessMessage"] / TempData["ErrorMessage"]`，View 放 `<partial name="_TempDataAlert" />`。
- **表單**：POST 表單要有 `method="post"`，Action 加 `[ValidateAntiForgeryToken]`。
- 檔案編碼：保持原檔的 BOM / 換行格式，避免整檔 diff。
- **靜態檔**：沒有打包 / 壓縮流程，`wwwroot/css`、`wwwroot/js` 只保留原始檔（不要另放 `.min` 副本）；
  `wwwroot/lib` 只保留實際引用的檔案。CORS 由 `app.UseCors("VueApp")`（在 `UseRouting` 之後、`UseStaticFiles` 之前）
  統一套用到 API 與靜態檔，允許來源只在 `Program.cs` 的 `AddCors` 設定一處。

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
4. 後端整理：
   - 資料夾與命名空間統一（見「目錄分層」），新增 `GlobalUsings.cs`；Big5 原始檔轉 UTF-8
   - 刪除未使用的類別（`StringExtensions`、命名空間錯誤的重複 `Partials/User`、`Follow`、`AddRewardRequest`、`NewOrderDTO` 等）、
     沒有對應 Action 的 View（Content 的 Create / Edit / Delete、ChatMessages/Details、ContentCategory/Index_Old）與連到它們的按鈕
   - 刪除未使用的 JS（qr、qrcode、site1、site2）、與原始檔內容相同的 `.min` 副本、`bundleconfig.json`、
     `wwwroot/lib` 中未引用的檔案（datatables 整包：版面原本的本地 fallback 路徑錯誤、從未生效）
   - 修正版面引用不存在的檔案（`site.min.js`、`jquery-easing`），登入頁改用與後台相同的 `sb-admin-2.js`
   - csproj 移除未使用的套件（Identity、EF Sqlite、SchemaCompare、Google.GenAI）與指向 repo 外部的 ECPay DLL 參考；
     移除誤加的 using（`Google.GenAI.Types`、`Microsoft.Build.*`、`NuGet.*`、`Identity.Data`）
   - `Program.cs`：移除重複的 `UseRouting` / `UseCors` / `UseStaticFiles` / `MapControllers` 與未使用的 Razor Pages，
     靜態檔 CORS 改由 `UseCors` 統一處理（原本手寫的允許來源清單少了 ngrok 網址）
   - 文件移到 repo 根目錄 `docs/`（`CONTENT_API_ONBOARDING` 由 Big5 轉 UTF-8 並改為 .md）

## 全專案檢查結果（2026-11 完成，尚未修正）

點數商城以外的前後端模組已全部檢查過，以下問題**都還沒修**，之後有空依序處理。
程度：🔴 嚴重（可直接被利用，影響點數 / 帳號 / 資料）、🟠 高、🟡 中、⚪ 低。
建議修正順序：先處理 🔴 的 1～4（加 `[Authorize]` / 擁有者檢查，改動小、效果大），再處理金鑰外洩與 XSS。

### 🔴 嚴重

1. **任何會員都能替自己加任意點數**：`ApiControllers/Point/PointLogApiController.cs` 的 `POST add-reward`，
   `changeAmount` 由前端表單決定，直接寫入點數紀錄。
   → 刪除此 API，委託任務獎勵只能由 `ContentInteractionService.SelectBestAnswerAsync` 發放。
2. **後台點數相關頁面沒有登入檢查**：`Controllers/Point/` 的 `PointLogController`、`PointRechargeController`、
   `MissionController`、`MissionProgressController`、`EntrustMissionController` 都沒有 `[Authorize]`。
   匿名使用者可以進後台，例如 `PointLog/Create` 新增任意點數紀錄。`Controllers/Users/CommunityController.cs` 也沒有（會列出所有會員）。
   → class 加上 `[Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]`。
3. **會員 API 的 `[Authorize]` 被註解掉**（`ApiControllers/Users/UserApiController.cs`，路由 `api/User`）：
   - `DELETE api/User/{id}`：**匿名就能刪除任何會員**（權限檢查也被註解掉）
   - `GET api/User`：匿名取得所有會員的 Email
   - `POST api/User/images`：匿名上傳任意檔案到 `wwwroot/img/user`，沒有檢查格式（可上傳 .html 造成 XSS）
   - `PUT api/User/profile` 回傳整個 `User` 實體，包含密碼雜湊與 `ResetToken`
   → 恢復 `[Authorize]`；刪除限管理員或本人；`GET` 全部會員限管理員；上傳改用 `ImageFileValidator`；回傳改用 DTO。
4. **客服聊天沒有權限與擁有者檢查**：
   - `Hubs/ChatHub.cs` 完全沒有驗證：匿名連線就能 `JoinSupportGroup`（收到所有客服通知）、`JoinSessionGroup` 任意對話（竊聽），
     `SendMessage` 可自訂發送者名稱（冒充客服）
   - `ApiControllers/Chat/ChatMessagesApiController.cs`：`GetActiveSupportSessions`（所有人的客服對話與最後訊息）、
     `AdminReply`（以客服身分回覆任何對話）只檢查有登入，任何會員都能用；`GetHistory/{sessionId}` 可讀任何人的對話
   - `ApiControllers/Chat/ChatSessionsApiController.cs` 沒有 `[Authorize]`：`User/{userId}` 讀任何人的對話列表、
     `Create` 替任何人建立、`Delete/{id}` 刪除任何人的對話、`AutoTitle` 可匿名一直呼叫 Gemini（費用）
   → Hub 加 `[Authorize]`，加入群組前檢查對話屬於本人，客服群組 / AdminReply / 客服列表限管理員；API 一律用登入者 ID 檢查擁有者。
5. **其他沒有登入檢查的 API**：
   - `PointRechargeApiController`：`GET` / `GET {id}` / `PUT {id}` / `DELETE {id}` 可匿名列出、修改、刪除所有人的儲值紀錄
   - `ContentNotificationApiController`：整個 controller 可匿名讀取 / 新增 / 修改 / 刪除所有通知
   - `CategoryApiController`：`POST` 可匿名新增論壇分類
   - `EntrustMissionApiController`：`DELETE {id}` 可匿名刪除任何委託任務（已預扣的點數不會退）
   - `PointLogApiController` `GET`、`EntrustMissionProgressApiController` `GET`：匿名取得所有人的點數紀錄 / 任務進度
   → 不需要的 CRUD 直接刪除；需要的加 `[Authorize]` 與擁有者 / 管理員檢查。

### 🟠 高

6. **API 金鑰提交進 git 歷史**：`incode2/appsettings.json` 的歷史版本中有 `GeminiApiKey`、`TmdbApiKey` 的真實值
   （目前檔案已清空，但歷史仍可取得）。→ 到 Google / TMDB 後台**撤銷並重新產生金鑰**，改用 user-secrets 或環境變數。
   ECPay 目前放的是綠界公開的測試帳號（`3002607`），正式上線時正式金鑰不可放進 repo。
7. **任務進度可以自己增加**：`MissionProgressApiController` 的 `POST {missionId}/increment` 讓會員自訂增量，
   可以直接把任何任務（登入、購買、按讚…）刷到完成再領獎。→ 刪除此 API，進度只由伺服器端 `TriggerMissionAsync` 觸發。
8. **點數併發問題**：`PointLogService.ChangePointsAsync` 以「讀取最後餘額 + 變動」計算，打賞（`TipAsync`）、
   任務領獎（`MissionProgressService.ClaimRewardAsync`，先檢查 `RewardedAt` 再寫入）、委託任務沒有鎖，
   同時送出多個請求可以重複扣款 / 重複領獎、餘額扣成負數。點數商城已用 `sp_getapplock` 處理。
   → 把 `OrderService.LockUserPointsAsync` 的做法抽到 `PointLogService`，所有點數異動都在交易內先鎖定會員。
9. **ECPay 回呼冪等性**（`PointRechargeApiController.EcpayReturn`）：「檢查是否已成功 → 加點 → 更新狀態」沒有鎖，
   綠界重送或同時回呼可能重複加點；`AddPointsFromRechargeAsync` 先 commit 加點，之後更新儲值狀態失敗時，重試會再加一次。
   → 同一交易內以條件更新 `UPDATE ... SET Status='success' WHERE OrderNo=@no AND Status<>'success'`，影響 1 筆才加點。
10. **聊天室 XSS**（前端 `src/components/chat/ChatWidget.vue`）：訊息以 `v-html` 顯示，客服 / SignalR 收到的訊息、使用者輸入、
    `marked.parse` 的結果都沒有經過 DOMPurify。配合第 4 點（任何人都能以客服身分發訊息），可以對其他會員執行腳本。
    → `v-html` 前一律用 `utils/sanitizeHtml.js`（DOMPurify）處理。
11. **聊天圖片上傳**（`ChatMessagesApiController` UploadImage）：檔名直接使用原始 `FileName`（`Guid + "_" + FileName`），
    在 Windows 上可用 `..\` 路徑寫到其他目錄；也沒有檢查格式。→ 改用 `ImageFileValidator` 與 Guid 檔名。

### 🟡 中

12. **CSRF**：會員 Cookie 設定 `SameSite=None`，接受 `[FromForm]` / multipart 的 API（例如上面的 `add-reward`、各圖片上傳）
    可以被其他網站以表單直接送出。→ 會員 Cookie 改 `SameSite=Lax`（前後端同站時）或對表單 API 加 AntiForgery。
13. **論壇互動可以送出任意分類**：`ContentInteractionApiController` `POST` 接受任何 `InteractionCategoryId`，
    會員可以自己替回覆加上「最佳解答」等系統分類（假徽章）；按讚可反覆取消 / 重按，每次都觸發按讚任務。
    → 限制會員可用的分類；任務改為同一內容只計一次。
14. **委託任務可匿名刪除後點數不退**（見第 5 點），刪除流程應該走釋放預扣點數。
15. **沒有登入嘗試次數限制**：會員 / 後台登入、忘記密碼寄信都沒有 rate limit 或鎖定。→ 使用 ASP.NET Core Rate Limiting middleware。
16. 儲值金額 `CashAmount` 沒有驗證（0、負數、超大值）。
17. `InventoryController` `POST add`：會員可以替自己加任意道具與數量（目前前端沒有使用這個功能）。

### ⚪ 低 / 程式品質

- 重設密碼：`ResetPasswordAsync` 沒有檢查 `ResetToken` 是否為 null（目前靠 `[ApiController]` 隱含的必填驗證擋住），
  新密碼沒有長度規則；信件連結中的 email 沒有 URL encode；前端網址寫死 `https://localhost:5173`（Google 登入導回也是）。
- `CreateContent` 的 `[Authorize]` 被註解掉，未登入時會以 UserId 0 寫入而失敗（500）。
- 論壇內容在前端以 `HtmlIframe` 顯示，`allow-scripts` + `allow-same-origin` 時會移除 sandbox；內容已用 DOMPurify 過濾，
  但建議保留 sandbox（不允許腳本或不同源）作為第二層防護。
- 錯誤訊息直接回傳 `ex.Message`：聊天、打賞、會員上傳等多處。
- `wwwroot/img/` 下的使用者上傳圖片被提交進 git，建議加入 `.gitignore`。
