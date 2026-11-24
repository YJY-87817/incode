# CLAUDE.md

本檔案提供給 Claude Code（與接手的開發者）在此專案工作時的背景、慣例與目前進度。
專案目前由個人維護（原為多人分組專案，程式中的 `//owen`、`//tim`、`//yjy`、`//qiu` 註解為原分工標記）。

本 repo 為前後端合一的 monorepo（2026-11 將原 `YJY-87817/incode_Vue` 連同 commit 歷史併入，現為 `incode.Vue/`）：

| 資料夾 | 內容 | 專屬說明 |
|---|---|---|
| `incode.MVC/` | 後端 ASP.NET Core 10 | 本檔案 |
| `incode.Vue/` | 前端 Vue 3 + Vite | `incode.Vue/CLAUDE.md`（在該資料夾工作時會自動載入） |
| `docs/` | 資料表設計、Content API 導覽 | |

- 同一個功能牽涉前後端時，在同一個分支 / PR 一起修改。
- 前端合併前的歷史：`git log dd7e8d8^2`（合併 commit 的第二個 parent）。

## 專案概觀

- **後端**：`incode.MVC/`，ASP.NET Core 10 (`net10.0`，LTS，支援至 2028-11) 單一專案，解決方案 `incode.sln`
  - 同時提供 **前台 REST API**（`ApiControllers/`，給 Vue 前台）與 **後台管理 MVC**（`Controllers/` + `Views/`，Razor）
  - EF Core 10 + SQL Server（Database-First，`Models/` 為產生的實體，擴充放 `Models/Partials/`）
  - SignalR 聊天室 `/chatHub`（`Hubs/ChatHub.cs`），Gemini AI 客服（`Services/Chat/GeminiService.cs`）
  - 綠界 ECPay 點數儲值（`Services/Point/EcpayService.cs`）
- **前端**：`incode.Vue/`（Vue 3 + Vite + Pinia + Bootstrap），開發時另外啟動 Vite，透過 CORS 帶 Cookie 呼叫 API

### 目錄分層

```
incode/
├── incode.sln
├── docs/                       專案文件（資料表設計 dbdiagram、Content API 導覽）
└── incode.MVC/
    ├── Program.cs              DI 註冊、驗證、CORS、Middleware
    ├── GlobalUsings.cs         專案內所有命名空間的 global using（新增資料夾時記得補上）
    ├── ApiControllers/<模組>/  前台 REST API（api/[controller]）；Common/ 放共用的圖片 API
    ├── Controllers/<模組>/     後台 MVC（HomeController 在根目錄）
    ├── Views/                  後台 Razor View（依 Controller 名稱，_ViewImports 已引入 ViewModels.*）
    ├── Hubs/                   SignalR
    ├── Services/<模組>/        商業邏輯（IXxxService + 實作）；Common/ 放圖片上傳與檢查
    ├── Models/                 EF 產生的實體（勿手改）
    │   ├── Partials/           實體 / DbContext 的 partial 擴充（命名空間必須是 incode.MVC.Models）
    │   └── Metadata/           [ModelMetadataType] 用的驗證 / 顯示設定
    ├── Dtos/<模組>/            API 傳輸物件（ResultDTO、PagedResultDto 在根目錄）
    ├── ViewModels/<模組>/      後台頁面模型
    ├── Constants/              常數與列舉（OrderStatus、PointType、RoleType）
    ├── Extensions/             擴充方法（ClaimsExtensions、ResultDTOExtensions）
    ├── Prompts/Prompts.md      AI 客服系統提示詞（執行時讀取，路徑不可改）
    ├── App_Data/uploads/       上傳檔案預設位置（App:UploadRoot，已 gitignore），網址 /img/...、/contents/... 不變
    └── wwwroot/                靜態檔（`img/product/NoImage.png` 是商品預設圖，不是上傳檔）；上傳檔一律在 App:UploadRoot
```

- 模組名稱統一：`Chat`、`Contents`、`Point`、`PointShop`、`Users`，**命名空間 = `incode.MVC.<資料夾路徑>`**
  （例：`Services/Users/AuthService.cs` → `incode.MVC.Services.Users`）。
- 專案內命名空間由 `GlobalUsings.cs` 統一引入，檔案內不需再寫 `using incode.MVC.*`。
- 原始碼一律 UTF-8（部分舊檔原為 Big5，已轉換；Big5 在非 Windows 環境編譯時中文字串會變亂碼）。

### 模組

| 模組 | API | 後台 | Service |
|---|---|---|---|
| 會員 | `ApiControllers/Users/*` | `Account`、`Member` | `Services/Users/*` |
| 論壇內容 | `ApiControllers/Contents/*` | `Content*`（含檢舉清單 `ContentReport`） | `Services/Contents/*` |
| 點數 / 任務 / 儲值 | `ApiControllers/Point/*` | `Mission*`、`EntrustMission`、`PointLog`、`PointRecharge` | `Services/Point/*` |
| 點數商城 | `ApiControllers/PointShop/*` | `Product`、`Order`、`Shipped` | `Services/PointShop/*` |
| 客服聊天 | `ApiControllers/Chat/*` + `ChatHub` | `ChatSessions`、`ChatMessages` | `Services/Chat/*` |

## 驗證機制（`Program.cs`）

- 預設 `SmartScheme`：有 `Incode.Admin` Cookie → `AdminAuth`，否則 → `MemberAuth`（`Incode.Member`）
- 前台 API 未登入回 401 / 403（不轉址）
- **CSRF**：`/api` 寫入請求與 `/chatHub` 會檢查 `Origin`（`UseApiOriginCheck`），新的前端網址要加到 `App:CorsOrigins`，否則會被 403；
  伺服器對伺服器的回呼（如綠界）要加進 `OriginCheckExtensions.ExemptPaths`
- **停用帳號**：用 `UserService.SetActiveAsync`（不刪除會員；已登入的人由 `MemberCookieValidator` 在下個請求登出）
- **登入限制**：登入 / 註冊 / 密碼重設端點加 `[EnableRateLimiting(RateLimitPolicies.Xxx)]`；部署在反向代理後面時需設定
  ForwardedHeaders，否則所有人共用代理 IP 的額度
- 後台 Controller 一律加：`[Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]`
  （後台登入 `AccountController` 只允許 `RoleId == Admin`，Claim 角色固定為 `"Admin"`）
- 取得登入者 ID：`User.GetId()`（`Extensions/ClaimsExtensions.cs`）

## 開發慣例（新程式請遵守）

- **擁有者檢查**：前台 API 讀寫資料時必須確認屬於 `User.GetId()`，找不到或非本人一律回 404。
- **分層**：前台 API Controller 與 SignalR Hub 不直接使用 `incodedatabaseContext`：資料存取、擁有者判斷與業務規則放在 `Services/<模組>/`
  （例如 `ChatService.IsOwnerAsync`、`OrderService.GetOwnOrderAsync`），Controller 只處理 HTTP、授權屬性、狀態碼與 SignalR 推播。
  後台 MVC（`Controllers/`）也一樣只呼叫服務（後台專用的放 `Services/<模組>/Admin*Service`），進行中：
  所有模組已完成（修正紀錄 42～45），後台 Controller 不再使用 DbContext
- **API 回應**：使用 `ResultDTO<T>`，並以 `this.ToActionResult(result)`（`Extensions/ResultDTOExtensions.cs`）回傳。
  成功一律 HTTP 200（`Code` 為 204 時也是 200，避免前端拿不到 body）；失敗使用 `Code` 作為 HTTP 狀態碼（400 / 404 / 409 / 500）。
  前端以 `error.response.data.message` 顯示錯誤。
- **不外洩例外**：catch 後用 `ILogger` 記錄，回傳通用訊息，不要把 `ex.Message` 回給前端。
  要讓使用者看到的業務錯誤丟 `BusinessException`（`Services/Common/`），只有它的 `Message` 可以直接回傳。
- **點數紀錄是帳本**：`point_log` 只能經由服務新增（每筆的 `balance_after` 接續上一筆），後台不提供新增 / 修改 / 刪除；
  後台調整用 `AdminAdjustPointsAsync`，儲值補單用 `AdminCompleteRechargeAsync`
- **點數異動**：透過 `IPointLogService.ChangePointsAsync`，必須在交易內呼叫（沒有交易會丟例外），不會自己 SaveChanges。
  它會以 `sp_getapplock`（`point-user-{userId}`）鎖定該會員到交易結束。**讀取會決定能不能異動的資料之前**
  （餘額、任務是否已領獎、委託狀態…）先呼叫 `LockUsersAsync(userId, ...)`，同一會員的請求才會排隊、不會讀到舊狀態；
  多位會員一次傳入（內部依 ID 排序避免死結）。
- **金額 / 價格**：一律以資料庫為準由後端計算，不信任前端或表單傳入的總額、單價；前端可輸入的金額 / 數量要在 DTO 加 `[Range]`。
- **任務進度**：只由伺服器端觸發。一次性任務（`MissionResetTypes.IsOneTime`）每位會員只有一筆進度、只能領一次獎勵。可以被會員反覆切換的動作（例如按讚）不要用 `TriggerMissionAsync` 每次 +1，
  改用 `SyncMissionCountAsync` 依實際資料計算。論壇互動分類 ID 用 `Constants/InteractionCategoryIds`，系統分類不可讓會員自行寫入。
- **網址**：不要寫死前後端網址。後端用 `IConfiguration.GetFrontendUrl()`（`App:FrontendUrl`），CORS 來源加在 `App:CorsOrigins`；
  前端用 `import.meta.env.VITE_API_BASE_URL` / `VITE_BASE_URL`。
- **訂單狀態**：使用 `Constants/OrderStatus.cs`（待處理、已出貨、已完成、已取消、其他），不要寫死字串。
- **圖片上傳**：先用 `Services/Common/ImageFileValidator.Validate()` 檢查（僅 jpg/png/gif/webp + 檔頭比對，單檔 20 MB）；
  刪除檔案時檔名以資料庫為準並 `Path.GetFileName()`，不可使用表單傳入的路徑。
- **請求大小 / 長度**：全站請求上限 30 MB（`Program.cs`），需要更大的端點個別加 `[RequestSizeLimit]`；前台 DTO 的字串欄位加 `[StringLength]`
  （對照資料庫欄位長度，長度常數放 `ContentLimits` / `ChatLimits` 這類類別，前端輸入框用同樣的 `maxlength`）。允許匿名寫入或會呼叫付費 API 的端點要加速率限制
- **檔案存放**：上傳檔一律透過 `UploadStorage`（`Services/Common/UploadStorage.cs`）寫入 / 刪除，資料夾用 `UploadFolders` 常數，
  檔名由伺服器產生（Guid）。**不要寫進 `wwwroot`**（會被提交進 git、重新部署會被覆蓋）。
  位置由 `App:UploadRoot` 設定（預設 `App_Data/uploads`），正式環境設成部署資料夾以外的絕對路徑
- **顯示使用者內容**：Razor 不要對會員 / AI 產生的內容用 `@Html.Raw`；需要保留格式時先經 DOMPurify（`~/lib/dompurify`）。
  前端 `v-html` 前一律過濾（論壇用 `utils/sanitizeHtml.js`，聊天用 `ChatWidget.vue` 的 `toSafeHtml`）。
- **後台訊息**：Controller 用 `TempData["SuccessMessage"] / TempData["ErrorMessage"]`，View 放 `<partial name="_TempDataAlert" />`。
- **表單**：POST 表單要有 `method="post"`，Action 加 `[ValidateAntiForgeryToken]`。
- 檔案編碼：保持原檔的 BOM / 換行格式，避免整檔 diff。
- **靜態檔**：沒有打包 / 壓縮流程，`wwwroot/css`、`wwwroot/js` 只保留原始檔（不要另放 `.min` 副本）；
  後台的前端套件（jQuery、Bootstrap、DataTables、select2、SignalR、marked、DOMPurify、Font Awesome、Nunito 字型等）
  **由 npm 管理**（`incode.MVC/package.json`，版本固定），`dotnet build` 前自動執行 `npm ci` + `npm run lib`
  （`scripts/copy-lib.mjs`）把用到的檔案複製到 `wwwroot/lib`，**`wwwroot/lib` 不提交進 git**。新增套件：`npm install --save-exact <套件>@<版本>`
  （選發布兩週以上的版本），再把要用的檔案加進 `copy-lib.mjs` 的清單；後台頁面不要再引用外部 CDN。CORS 由 `app.UseCors("VueApp")`（在 `UseRouting` 之後、`UseStaticFiles` 之前）
  統一套用到 API 與靜態檔，允許來源只在 `Program.cs` 的 `AddCors` 設定一處。

### 點數商城訂單（`Services/PointShop/OrderService.cs`）

前台 `OrderApiController` 與後台（`AdminOrderService` → `OrderService`）共用。`CreateOrderAsync` / `UpdateOrderItemsAsync` / `CancelOrderAsync`：

- 整筆在交易內（`RunInTransactionAsync`），業務失敗丟 `OrderFailure` → rollback + 回傳訊息
- 庫存用 `ExecuteUpdateAsync` 原子更新（`WHERE Stock >= 數量`），商品讀取用 `AsNoTracking`，**不要**改 tracked 實體的 `Stock` 再 SaveChanges
- 交易開始先 `IPointLogService.LockUsersAsync(userId)`，同一會員的點數異動排隊；非 SQL Server（測試用 SQLite）時略過
- 明細同商品會合併；數量必須 > 0；新加入的商品必須上架中
- 權限 / 狀態檢查由呼叫端負責：會員只能操作自己的「待處理」訂單；已取消訂單不可改回其他狀態
- 後台修改訂單狀態要和明細修改在同一個交易（`UpdateOrderItemsAsync(newStatus: …)`），**不要**在服務之外另外 `ExecuteUpdate` 狀態（會讓同時被取消的訂單復活）；
  商品庫存不能用表單數字覆蓋，後台用「調整庫存 ±」（`AdminProductService.UpdateProductAsync` 的 `stockDelta`，原子更新）

## 建置與驗證

- 需要 .NET 10 SDK（Visual Studio 2026）、**Node.js 22 以上**（建置後端時產生後台的 `wwwroot/lib`；只跑測試、環境沒有 Node 時可加 `-p:SkipNpmLib=true`）、SQL Server（`appsettings.json` 的 `ConnectionStrings:incodedatabase`，預設 LocalDB）；
  新環境用 `docs/database/schema.sql` + `seed.sql` 建立資料庫，資料表變動後用 `dotnet ef dbcontext script` 重新產生。
  **改資料表（例如新增索引）**：模型設定放 `Models/Partials/incodedatabaseContext.cs` 的 `OnModelCreatingPartial`、更新 `schema.sql`，
  並在 `docs/database/migrations/` 加一支可重複執行的腳本給已上線的資料庫（`MigrationScriptTests` 在 SQL Server 上測試）
- 機密設定（Google OAuth、ECPay、Gemini 等）放 user-secrets 或環境變數，不要提交到 repo
- **測試資料庫**：`docker compose up -d` 啟動 SQL Server 並自動建表與寫入基本資料，後端用 launch profile `https (Docker DB)`；
  `docker compose down -v` 可重建全新資料庫。註冊帳號後 `UPDATE [user] SET role_id = 1 ...` 即可登入後台
- **CI**：`.github/workflows/ci.yml`：後端 `dotnet build` + `dotnet test`，前端 `npm test` + `npm run build`。PR 合併前請確認兩個 job 都通過
- **測試**：後端 `incode.MVC.Tests/`（xUnit，SQLite 記憶體資料庫，涵蓋 `PointLogService`、`PointRechargeService`、`OrderService`、`MissionService`、`MissionProgressService`（領獎）、`EntrustMissionService`（含過期處理）、`ChatToolService`（AI 客服查詢）、Google 帳號建立、帳號安全（`AccountSecurityTests`：登入戳記、修改密碼、Email 驗證、Google 接管）、`LoginAttemptLimiter`、`ImageFileValidator`、發文驗證（`ContentValidationTests`）、後台點數調整 / 人工補單 / 檢舉處理（`AdminModerationTests`）、Origin 檢查，以及用 `WebApplicationFactory` 啟動整個網站的 API 權限測試 `ApiAuthorizationTests`（未登入 401、非管理員 403、非本人 404、停用帳號登出、Origin 檢查；`ApiFactory` 提供 `/__test/login`）；`dotnet test incode.sln`），前端 `npm test`（Vitest）。
  SQLite 不支援 `sp_getapplock`，併發行為由 `ConcurrencyTests`（`[SqlServerFact]`，需環境變數 `INCODE_TEST_SQLSERVER`，沒設定就略過；CI 的 `backend-concurrency` job 以 SQL Server service container 執行）驗證；`TestDb` 會移除模型中 SQL Server 專用的 `getdate()` 預設值。環境沒有 .NET 10 SDK 時可用 Docker 映像 `mcr.microsoft.com/dotnet/sdk:10.0` 建置 / 執行（映像沒有 Node：掛載主機的 Node 並加進 `PATH`，或加 `-p:SkipNpmLib=true`）
  （需 `--network host` 才能走主機的 proxy 與連到 Docker 資料庫）；搭配 Docker 資料庫可實際啟動後端，
  用 API（註冊 / 登入取得 Cookie）做端對端測試
- 上傳檔只放在 `App:UploadRoot`（已 gitignore）。改版前的上傳檔已從 git 移除，**新環境 / pull 之後**要先執行
  `scripts/copy-legacy-uploads.sh <UploadRoot> <備份資料夾>`（Windows 用 Git Bash）從備份（例如舊環境的 UploadRoot）複製到 `UploadRoot`，否則舊文章的圖片與內文會 404
  （git 歷史已經沒有這些檔案，見修正紀錄 39）

## 修正紀錄

（1～4 為合併成 monorepo 之前，前後端各自 repo 的 PR）

1. 點數商城訂單 API：擁有者檢查、修改訂單補扣 / 退點、數量驗證、只有「待處理」可修改 / 取消
2. 點數商城後端重構：交易一致性、原子扣庫存、點數鎖、重複商品合併、後台建單 / 修改改用 OrderService、
   修正後台修改訂單點數類型相反與可改會員、已取消訂單不可恢復、HTTP 狀態碼、圖片上傳驗證與權限、
   後台商品圖片路徑穿越刪檔、寄送資料刪除表單缺 `method="post"`
3. 前端點數商城（`incode.Vue`）：錯誤修正與重構（見 `incode.Vue/CLAUDE.md`）
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
5. 合併前後端為 monorepo：`incode_Vue` 以 `git subtree` 併入（保留歷史），新增根目錄 `README.md`；`appsettings.json` 的 `EmailSettings` 原本誤放在 `Authentication` 底下（`EmailService` 讀取根層級），已移到根層級
6. 權限漏洞修正（全專案檢查 🔴 1～5）：
   - 刪除 `POST api/PointLogApi/add-reward`（會員可自訂點數）、`GET api/PointLogApi`、`POST api/MissionProgressApi/{id}/increment`
   - 後台 `PointLog`、`PointRecharge`、`Mission`、`MissionProgress`、`EntrustMission`、`Community` 加上管理員驗證
   - `UserApiController` 恢復 `[Authorize]`：列出全部會員 / 刪除會員限管理員；公開個人頁、統計、大頭貼上傳為 `[AllowAnonymous]`
     （註冊流程在登入前上傳大頭貼）；上傳改用 `ImageFileValidator`（單檔 5 MB、最多 5 張）；`PUT profile` 不再回傳密碼雜湊
   - `ChatHub` 需登入，只能加入自己的對話群組，客服群組限管理員，移除可冒充發送者的 `SendMessage`；
     `GetActiveSupportSessions`、`AdminReply` 限管理員；`GetHistory` 與 `ChatSessionsApiController` 檢查對話擁有者，
     `AutoTitle` 改為只通知該對話群組
   - 刪除無驗證的 `PointRechargeApi` GET / PUT / DELETE、`ContentNotificationApiController`（前後端皆未使用）、
     `EntrustMissionApi` DELETE（直接刪除不會退預扣點數）、`GET api/EntrustMissionProgressApi`；`POST api/CategoryApi` 需登入
   - `/chatHub` 未登入回 401（原本轉址到登入頁）
7. 部署設定與資料庫腳本（全專案檢查 18、19）：
   - 後端新增 `App:FrontendUrl`、`App:CorsOrigins` 設定（`Extensions/ConfigurationExtensions.cs`），Google 登入導回、
     重設密碼連結、綠界 `ClientBackURL`、AI 回覆連結、CORS 允許來源全部改讀設定；正式環境以環境變數覆寫
   - 前端寫死的 `https://localhost:7190` 全部改用 `.env` 的 `VITE_API_BASE_URL` / `VITE_BASE_URL`
   - 新增 `docs/database/`：由 EF 模型產生的 `schema.sql`、基本資料 `seed.sql`（角色、點數類型、互動分類＋範例分類與任務）與說明
8. Docker 測試資料庫：根目錄 `docker-compose.yml`（SQL Server 2022 + 自動執行 `docs/database` 腳本的 `db-init`）、
   `docker/db/init.sh`、後端 launch profile `https (Docker DB)`；`schema.sql` 補上 `SET QUOTED_IDENTIFIER ON`（sqlcmd 預設 OFF，
   條件式索引會建立失敗）。以 Docker 資料庫實測時另外修正兩個原有錯誤：新增論壇分類時 `description` 為必填（前端送 null，
   功能一直失敗）→ 改為選填；後台 `CommunityController` 從未有對應 View（必定 500）→ 刪除

9. CI（全專案檢查 20）：`.github/workflows/ci.yml` 在每個 PR 與推送到 main 時於 Linux 執行後端 `dotnet build`（Release）
   與前端 `npm install` + `npm run build`，可擋下檔名大小寫錯誤；`vite.config.js` 改為 `key.pem` / `cert.pem` 存在時才啟用 HTTPS
   （原本沒有憑證時連 `npm run build` 都會失敗）
10. 升級 .NET 10（.NET 9 為 STS，2026-11-10 結束支援）：`net10.0`、Microsoft 套件升到 10.x、CI 改用 .NET 10 SDK；
    直接參考 `System.Security.Cryptography.Pkcs` 10.x 取代 `Microsoft.Data.SqlClient` 帶入的 9.0.11（高風險漏洞 GHSA-846f-f764-76mx）。
    `Microsoft.VisualStudio.Web.CodeGeneration.Design`（VS scaffolding）帶入有低風險漏洞的 `NuGet.*` 6.12.1，已於修正紀錄 38 移除
11. 專案更名：後端 `incode2/` → `incode.MVC/`（`incode.MVC.csproj`，命名空間 `incode2.*` → `incode.MVC.*`，組件名稱 `incode.MVC.dll`）、
    前端 `incode_Vue/` → `incode.Vue/`、`incode2.sln` → `incode.sln`。`UserSecretsId` 保持不變（沿用既有的 user-secrets）。
    舊的 commit / 文件中出現的 `incode2`、`incode_Vue` 即為這兩個資料夾
12. 點數併發（全專案檢查 8、9）：`PointLogService.LockUsersAsync`（原 `OrderService` 的 `sp_getapplock`）供所有點數流程共用，
    `ChangePointsAsync` 強制在交易內並自動鎖定、餘額會計入同一交易中尚未儲存的異動；打賞先依 ID 順序鎖定雙方；
    任務領獎、選擇最佳解答、取消委託在讀取狀態前先鎖定會員；綠界付款成功改為 `CompleteRechargeAsync`：
    同一交易內以條件更新 `Status <> 'success'` 搶下訂單，影響 1 筆才加點（失敗通知也不會覆蓋已成功的訂單）。
    以同時送出 10 個請求實測：修正前打賞 10 筆全成功（餘額 100 扣 300）、任務獎勵 / 綠界加點 / 最佳解答獎勵各重複 10 次，
    修正後皆只成立一次、帳本連續
13. 聊天室 XSS 與圖片上傳（全專案檢查 10、11）：
    - 前端 `ChatWidget.vue` 顯示訊息前經 `toSafeHtml`：使用者輸入一律跳脫為純文字，AI / 客服 / 系統訊息以 DOMPurify 過濾
    - **後台客服頁面 `Views/ChatMessages/Index.cshtml` 原本以 `@Html.Raw` 顯示會員訊息**（會員可在管理員的後台執行腳本）：
      改為輸出到 `data-body` 後以 DOMPurify 過濾再顯示，SignalR 即時訊息（`appendMessage`）也一樣；
      DOMPurify 放在 `wwwroot/lib/dompurify/`（現由 npm 管理，見修正紀錄 41）
    - `UploadImage` 改用 `ImageFileValidator`（單檔 10 MB）與 Guid 檔名，不再使用原始檔名；圖片附帶文字存入前 HTML 編碼
14. CSRF 與登入次數限制（全專案檢查 12、15）：
    - `Extensions/OriginCheckExtensions.cs`（`app.UseApiOriginCheck`）：`/api` 的寫入請求與 `/chatHub` 只接受自己與 CORS 允許來源的
      `Origin`（沒有 Origin 時看 `Referer`，都沒有視為非瀏覽器請求放行）；綠界回呼列為例外。會員 Cookie 維持 `SameSite=None`
      （前後端可能不同網域），CSRF 由 Origin 檢查負責。實測修正前，其他網站的頁面能以會員身分上傳聊天圖片
    - `Extensions/RateLimitExtensions.cs`：依 IP 限制登入 / 註冊（每分鐘 20 次）、忘記密碼 / 重設密碼（每 15 分鐘 5 次），超過回 429
    - `Services/Users/LoginAttemptLimiter.cs`：同一帳號 15 分鐘內失敗 5 次即暫停登入 15 分鐘（會員 API 回 429，後台顯示訊息），
      記憶體計數，重啟清除
15. 前端效能與 401 處理（全專案檢查 22 的一部分）：
    - Vuetify 改用 `vite-plugin-vuetify` 只打包用到的元件（原本 `import * as components`）；移除完全沒用到的 `bootstrap-vue-next`
    - 路由除了首頁之外全部改為 `() => import()` 延遲載入
    - Vuetify 圖示由 `@mdi/font`（CSS 418 KB + 字型 403 KB）改為 SVG，只打包用到的圖示（`src/plugins/vuetifyIcons.js`，新圖示要登記）
    - logo `okapi.svg`（1.8 MB 的點陣描圖 SVG，每頁導覽列都會載入）改為 18 KB 的 WebP，favicon 改為 64px PNG
    - 首頁載入的主程式 JS 1,199 KB → 578 KB、CSS 1,687 KB → 1,049 KB
    - `src/api/authInterceptor.js`：全域 axios、`apiClient`、`pointClient` 共用 401 處理。原本 `apiClient` 只印 console、`pointClient` 沒處理，
      登入逾時後畫面仍顯示已登入。已登入狀態收到 401 → 清除 Pinia 狀態、提示並導向登入頁（登入後回原頁）；
      未登入時 `apiClient` / `pointClient` 交給呼叫端（論壇頁訪客的背景查詢會 401，不能把訪客踢去登入頁）
16. 論壇互動、儲值金額、道具（全專案檢查 13、16、17）：
    - `POST api/ContentInteractionApi` 只接受會員可設定的分類（`Constants/InteractionCategoryIds.MemberToggleable`：按讚、隱藏、收藏、檢舉），
      其他（最佳解答、打賞、不存在的分類）回 400；內容不存在回 404，不再回傳例外訊息
    - 刪除 `DELETE api/ContentInteractionApi/{contentId}/{userId}/{categoryId}`：沒有登入檢查且 userId 由網址指定，
      任何人都能刪除別人的互動（含最佳解答紀錄）；前端沒有使用
    - 按讚任務改用 `IMissionService.SyncMissionCountAsync`：進度 = 本週期內目前仍按讚中的不同內容數（不含自己的內容），只增不減。
      原本每按一次 +1，對同一篇反覆取消 / 重按 5 次就能完成任務；任務進度已領獎（`rewarded`）時不再被改回 `completed`
    - 儲值 `PointRechargeDTO` 不再繼承實體，`CashAmount` 限 10～50,000 元（原本 0、負數都能建立訂單，3 億元時點數溢位成負數）；前端同步檢查
    - 刪除 `POST api/Inventory/add`（測試用，會員可替自己加任意道具與數量，前端沒有使用）
17. 前端呼叫不存在的端點（全專案檢查 23）：
    - 儲值完成頁 `RechargeResult.vue` 原本沒有查詢訂單（API 呼叫被註解掉），**一律顯示「儲值成功」**（付款失敗也一樣）。
      後端新增 `GET api/PointRechargeApi/order/{orderNo}`（只能查自己的訂單，否則 404）；`Recharge.vue` 送出前把訂單編號存到
      sessionStorage（綠界導回時不帶訂單編號，`utils/rechargeOrder.js`），結果頁輪詢約 20 秒，依 success / failed / 尚未收到通知分別顯示
    - `taskStore.claimTask` 刪除委託任務分支（呼叫不存在的 `EntrustMissionApi/{id}/entrustMissionClaim`）。委託任務沒有「接取」：
      列表與詳細抽屜本來就只導向討論區，會員回覆後由委託人選最佳解答；`claimTask` 只處理平台任務
18. 委託任務取消（全專案檢查 14）：新增 `POST api/EntrustMissionApi/{id}/cancel`（原本 `CancelEntrustMissionAsync` 已存在但沒有 API）。
    - 只有自己發起（否則 404）、徵求中、且討論串中**還沒有其他會員回覆**的委託可以取消（避免拿到解答後取消、拿回賞金）；
      自己的補充回覆不影響。已取消 / 已結案 / 已有回覆回 409
    - 狀態改為 `cancelled` 並以 `IPointReserveService.ReleaseAsync` 退回預扣點數（同一交易、鎖定委託人）；
      `ReleaseAsync` 改為回傳退點結果（原本忽略失敗）；例外不再回傳 `ex.Message`
    - 前端「我的任務」（`MyTask.vue`）：自己發起且徵求中的委託顯示「取消委託」，已取消的委託列在「已完結」並在歷程顯示退款
19. 前端套件（全專案檢查 24）：
    - Tiptap 2.27 → 3.33（修正 `mergeAttributes()` 原型污染 GHSA-cp6q-959q-f8rh），`npm audit` 0 個漏洞。
      `@tiptap/extension-underline` 移除（StarterKit 3 已內含），新增 `@tiptap/pm`；`TiptapEditor.vue` 的 ProseMirror 改從
      `@tiptap/pm/state` 匯入；StarterKit 3 新增的 Link、TrailingNode 關閉，產生的 HTML 與升級前相同
      （以同一組瀏覽器操作實測升級前後 13 個步驟的輸出完全一致）
    - 前端提交 `package-lock.json`（原本被 `.gitignore` 排除），CI 改用 `npm ci`
20. 發文登入檢查與錯誤訊息外洩（低優先清單）：
    - `POST api/ContentApi` 恢復 `[Authorize]`：原本未登入發文以 UserId 0 寫入而 500，回應中是完整的 `DbUpdateException`；現在回 401
    - 不再把例外細節回給前端（論壇分類、註冊、打賞、聊天 7 處、AI 命名、任務領獎、建立委託）：改為 `ILogger` 記錄 + 通用訊息。
      `GeminiService` 原本把錯誤（含「API Key 未設定」、API 回應內容、連線例外）當成 AI 回覆存進聊天紀錄並顯示給會員，
      改為固定回覆「AI 客服暫時無法回覆…」
    - 新增 `Services/Common/BusinessException`：給使用者看的業務錯誤（例如打賞「點數不足」）丟這個，Controller 才回傳 `Message`；
      其他例外一律回通用訊息
21. 上傳檔案移出網站目錄（全專案檢查 21 的一部分）：新增 `UploadStorage`，論壇圖片、大頭貼、論壇內文 HTML、聊天圖片、
    後台商品圖片改存到 `App:UploadRoot`（預設 `App_Data/uploads`，已 gitignore；正式環境以 `App__UploadRoot` 設成部署資料夾外的絕對路徑）。
    `Program.cs` 以第二個 `UseStaticFiles` 把該資料夾掛在網站根目錄，**網址與資料庫內容都不變**；改版前留在 `wwwroot` 的檔案仍由
    wwwroot 提供，刪除（商品換圖、後台刪圖）時兩處都會找
22. 一次性任務可每天重複領獎：`reset_type` 為 `once` / `none`（例如「首次兌換」）的任務，週期起點原本是「今天」，
    每天會建立新的進度、可以每天再領一次獎勵（實測隔天再兌換，50 點發了第 2 次）。
    - 一次性任務改用固定週期起點 `MissionResetTypes.OneTimeCycleStart`（`Constants/MissionResetTypes.cs`），每位會員只有一筆進度
    - 改版前的舊資料可能已有多筆：查詢進度時一次性任務不看週期起點（優先取已領獎的那筆）；領獎時任何一筆領過就拒絕，
      「我的任務」也不再顯示可領
23. 委託與任務的小問題（低優先清單）：
    - 建立委託（`CreateEntrustMissionAsync`）只能掛在自己的主題文章：別人的文章 404、回覆 400、同一篇已有委託 409。
      原本可以把委託掛在別人的文章上並扣自己的點數；`POST api/EntrustMissionApi` 失敗時改以 `Code` 作為 HTTP 狀態碼（原本 409 變 500）
    - 後台 `EntrustMission/Edit` 只能改標題、詳情、類別、截止日期、緊急；狀態、賞金、最佳解答、關聯內容、啟用不能再直接改
      （原本改成「已取消」不會退點）。新增「取消委託並退款」（`POST EntrustMission/Cancel/{id}` → `CancelEntrustMissionByAdminAsync`，
      不限發起人、已有回覆也可取消）；徵求中的委託不能直接刪除
    - 任務大廳（`MissionList.vue`）改為等後端回應才顯示「領取成功」，失敗只顯示失敗訊息（原本兩個提示都會跳出）；領取中按鈕停用
    - 論壇編輯器工具列的粗體 / 斜體 / 底線「啟用中」狀態可以正常顯示（模板原本寫成 `editor?.value?.isActive`）
24. 重設密碼與論壇內容 sandbox（低優先清單）：
    - `ResetPasswordAsync`：沒有申請過重設（`ResetToken` / 期限為 null）一律失敗，token 以固定時間比對；新密碼與註冊同規則
      （`PasswordRules`，6～100 碼，DTO 與 Service 都檢查；原本 5 碼也能重設成功）；重設成功後解除登入失敗暫停
    - 重設 token 改為 32 bytes 密碼學亂數；信件連結的 email / token 做 URL 編碼並 HTML 編碼；`ForgotPasswordRequest` 檢查 email 格式
    - 寄信失敗（SMTP）改為記錄 log 並回通用訊息（原本回傳完整的 `SmtpException`）
    - `HtmlIframe.vue`：原本傳入 `allow-scripts` 時會直接移除 sandbox（論壇內文等於沒有 sandbox，漏網的腳本可以存取整個網站）。
      改為一律保留 `sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"`、不允許腳本；
      內容在顯示前已經過 DOMPurify（會移除 script / iframe），所以原本的 `allow-scripts` 沒有實際用途
25. 儲值方案點數：`Recharge.vue` 的方案原本寫「1000 點 + 贈 50」等，但後端一律以金額 × 10 給點（95 元 = 950 點）。
    前端改為依後端規則由金額計算點數，方案價格改為整數（50 元 = 500、100 元 = 1000、300 元 = 3000），移除不存在的贈點顯示
26. 前端重複的 Bootstrap CSS（全專案檢查 22 的一部分）：`main.js` 原本在 Material Kit（內含自己編譯的 Bootstrap 5.2）之外又載入一整份
    Bootstrap 5.3 CSS。移除後主 CSS 1,049 KB → 876 KB；5.3 才有、專案有用到的 22 個工具類別（`bg-*-subtle`、`fw-semibold`、`rounded-4`、
    `object-fit-cover` 等）收進 `src/assets/css/bootstrap53-utilities.css`。以改前 / 改後兩份建置、12 個頁面與表單驗證狀態截圖逐像素比對，
    結果完全一致（一開始 `rounded-4` 寫死 1rem 差了 4px：Material Kit 的 `:root` 把 `--bs-border-radius-xl` 設成 .75rem，改為引用變數後一致）。
    Bootstrap JS（offcanvas 等 `data-bs-*`）與 `bootstrap-icons` 仍使用
27. 後端分層（全專案檢查 25）：前台 API Controller 與 `ChatHub` 全部改為只呼叫服務，不再使用 `incodedatabaseContext`
    （後台 MVC 的 15 個 CRUD 頁面維持原樣，後來已逐模組改走服務層）。
    - 新增 `ChatService`（聊天：對話擁有者判斷、取得 / 建立對話、訊息存檔、客服列表、歷史、命名、刪除）、`CategoryService`（分類與統計）、
      `PointRechargeService`（儲值訂單建立、失敗標記、會員查詢；金額換點數的比率 `PointsPerCash` 只定義在這裡）
    - 補強既有服務：`UserService`（管理員列表、依暱稱查詢、更新個人資料、刪除）、`FollowService`（粉絲 / 追蹤名單、追蹤結果 `FollowResult`）、
      `OrderService`（`GetOwnOrderAsync` 等擁有者查詢）；`NotificationController`、`StatusController`、`EntrustMissionApiController` 改用原本
      寫好卻沒人使用的服務
    - 行為變動：他人對話上傳圖片原本因空值例外回 500（回應帶出 `NullReferenceException`）→ 401；追蹤不存在的會員 500 → 404；
      標記別人的通知 403 → 404（與「非本人一律 404」一致）；`GET api/EntrustMissionApi/{id}` 不再回傳已下架的委託
    - 刪除 `EntrustMissionProgressApiController` 與 `IEntrustMissionProgressService`、`EntrustMissionProgressDTO`：服務從來沒有實作也沒有註冊，
      API 一律 500，前端沒有使用
    - 驗證：聊天 25 項、其餘前台 API 52 項（User / Follow / Notification / Status / Category / 儲值含綠界回呼 / 委託）在 main 與分支各跑一次，
      結果相同（上述行為變動除外）；併發 18、互動 16、委託取消 17、後台委託 14、例外訊息 8、權限與訂單 35 項回歸測試通過；
      SignalR `JoinSessionGroup` 以 Hub 連線實測
28. 停用帳號與 AI 命名（低優先清單）：
    - 管理員停用會員：原本 `DELETE api/User/{id}` 因外鍵（註冊就有任務進度等資料）一律 500，已移除；改為 `POST api/User/{id}/deactivate`
      與 `/activate`（不能停用自己、找不到 404），只改 `IsActive`，資料保留。停用後無法登入（原有檢查），
      **已登入的人也會立刻被登出**：`Extensions/MemberCookieValidator` 在會員 Cookie 驗證時檢查帳號是否仍啟用（查無會員視為無效），
      結果快取 60 秒，`UserService.SetActiveAsync` / `UpdateUserAsync`（後台會員編輯）會清除快取，直接改資料庫最慢 60 秒後生效。
      原本停用只擋登入，已登入的 Cookie 效期 7 天且自動延長，停用形同無效
    - `ChatSessionsApi/AutoTitle`：Gemini 無法回覆時不再把「AI 客服暫時無法回覆…」存成對話標題，改回 503、標題不變
29. 舊上傳檔移出 git（全專案檢查 21 的其餘部分）：
    - `wwwroot/img/fromApi|chat|user|product`、`wwwroot/contents` 共 276 個會員上傳的檔案（約 90 MB）從 git 移除，並加進 `.gitignore`；
      `img/product/NoImage.png` 是商品預設圖（前端 `pointShopUtils.js`、後台 `ProductController` 會用到），保留在 repo
    - `scripts/copy-legacy-uploads.sh [UploadRoot] [來源]`：把這些檔案複製到 `UploadRoot`（網址不變），預設從 git 歷史（commit `a1199c9`）
      取出，也可以指定備份資料夾；已存在的檔案不覆蓋，可重複執行。**每個環境都要先執行一次**，否則 pull 後舊文章圖片與內文會 404
    - 實測：腳本還原的 277 個檔案（含 `NoImage.png`）與 git 內容逐檔相同；以還原的資料夾當 `UploadRoot` 啟動後端，
      277 個舊網址全部回 200 且內容一致（含中文與空白檔名）
    - 2026-11-18 已改寫 git 歷史（見修正紀錄 39）：預設來源 `git:a1199c9` 已移除，腳本只能從備份資料夾還原
    - 後台商品列表的預設圖寫成小寫 `noimage.png`，實際檔名是 `NoImage.png`（Linux 大小寫敏感會 404），已修正
30. 第二次全專案檢查 26～28（三項都先寫測試重現，修正後通過）：
    - **26 上傳檔案的儲存型 XSS**：`Extensions/UploadSecurityHeadersExtensions.cs`（`app.UseUploadSecurityHeaders()`，在 `UseStaticFiles` 之前）
      對 `/contents`、`/img/fromApi|chat|user|product`（`UploadFolders`）的回應加上 `Content-Security-Policy: sandbox` 與
      `X-Content-Type-Options: nosniff`：直接開啟會員上傳的內文 HTML 時，瀏覽器放在獨立來源（`null`）並禁止腳本。依網址判斷，
      `UploadRoot` 與還沒搬檔的 wwwroot 舊檔都涵蓋；前端以 fetch 讀內文、以 `<img>` 顯示圖片，不受影響。
      實測：含 `<script>` 的內文檔直接開啟，修正前以 `text/html` 原樣提供、沒有任何標頭；修正後腳本不執行、頁面來源為 `null`
    - **27 後台首頁暱稱注入**：`Views/Home/Index.cshtml` 序列化進 `<script>` 的資料改用 `StringEscapeHandling.EscapeHtml`
      （`<` `>` `&` 引號轉成 `\u003c` 等），屬性名稱不變。測試：暱稱含 `</script><script>…` 的會員，修正前原樣出現在管理員首頁的 HTML 中
    - **28 並行取消 / 修改訂單**：`OrderService.CancelOrderAsync` / `UpdateOrderItemsAsync` 先只查出訂單的會員，
      **取得點數鎖之後才在交易內讀取訂單**、判斷狀態並計算差額；新增 `requiredStatus` 參數，會員 API 傳入「待處理」由服務在鎖內判斷
      （避免檢查後才被管理員出貨仍被取消）。以 SQL Server 實測修正前：同一筆訂單同時取消 10 次全部成功（退點 10 次），
      同時把數量 1 改成 2 共 10 次多扣 270 點；修正後只成立一次。`ConcurrencyTests` 新增 3 項
31. 第二次全專案檢查 29～31（都先寫測試重現）：
    - **29 打賞洩漏 Email**：`PointLogService.TipAsync` 的點數紀錄副標改用暱稱（原本「給 / 來自 {對方 Email}」）。
      **已上線的資料庫要執行一次** `docs/database/migrations/2026-11-12-tip-log-remove-email.sql`（把舊紀錄換成暱稱，可重複執行；
      `MigrationScriptTests` 在 CI 的 SQL Server 上執行這支腳本）
    - **30「我的收藏」用錯分類**：`GetMyBookmarkedContents` 原本以分類 2（隱藏）查詢，改用 `InteractionCategoryIds.Bookmark`；
      我的按讚、`ContentService` 排除隱藏文章也改用常數
    - **31 互動紀錄可匿名查詢**：`GET api/ContentInteractionApi` 只能查自己的互動（`userId` 為本人）、公開的分類（最佳解答，
      前端詳情頁的備援查詢會用到）或由管理員查詢，其餘 403；`GET api/ContentInteractionApi/content/{id}` 限管理員（前端沒有使用）
32. 第二次全專案檢查 33、34、37、38（都先寫測試重現）：
    - **37 綠界測試金鑰**：`appsettings.json` 的 ECPay 設定改為空字串，公開測試帳號只放 `appsettings.Development.json`；
      非 Development 環境使用測試商店（`3002607`）時拒絕啟動（`EcpayService.CheckSettings`），展示環境需明確設 `ECPay__AllowTestMerchant=true`；
      回呼的 `SimulatePaid=1`（綠界後台模擬付款）只在測試商店接受，正式商店回 `1|OK` 但不加點。
      **正式環境要以環境變數設定** `ECPay__MerchantID`、`ECPay__HashKey`、`ECPay__HashIV`、`ECPay__ReturnUrl`
    - **38 分頁參數**：`Services/Common/Paging.Normalize`（page 最小 1、pageSize ≤ 0 用預設值、最大 100），`ContentService` 的 4 個分頁查詢使用。
      修正前 SQL Server 上 `page=0` / `pageSize=0` 會 500（`SqlServerPagingTests` 在 CI 重現），`pageSize=1000000` 一次撈全部；
      前端新增主題時選擇主題原本以 `pageSize=9999` 取全部，改為每頁 100 逐頁取
    - **34 聊天速率限制**：`RateLimitPolicies.AiChat`（每位會員每分鐘 10 次，未登入依 IP），套用在 `SendMessage`、`UploadImage`、`AutoTitle`；
      前端收到 429 顯示「訊息送出太頻繁」
    - **33 請求大小與長度**：全站請求上限 500 MB → 30 MB（論壇多圖上傳 100 MB）；文章標題 200 字、內文 500 萬字元（`ContentLimits`，
      編輯器上傳失敗時會內嵌 base64 圖片，不能太小）、聊天訊息 2000 字（`ChatLimits`），超過回 400（原本標題過長在 SQL Server 會 500）；
      匿名可用的大頭貼上傳加上 `RateLimitPolicies.Upload`（每 10 分鐘 10 次）；前端標題 / 聊天輸入框加 `maxlength`

33. 第二次全專案檢查 32、35、36、39（32、35、36 先寫測試重現）：
    - **35 Google 登入同名**：`UserService.CreateGoogleUserAsync` 的 username 已被使用（同名、都沒有名稱、或與一般會員同名）時改用
      「名稱_2」「名稱_3」…（超過欄位長度 150 時截短），同時建立撞名時重試。修正前第二位同名的使用者建立帳號失敗（唯一索引）→ 500
    - **32 AI 客服查詢**：新增 `Services/Chat/ChatToolService`：商品（上架中，比對名稱 / 說明 / 類型 / 系列，不提供庫存數量）、
      文章（公開的主題文章，比對標題）、點數（**登入者本人**的餘額與最近 5 筆紀錄）改為查資料庫，原本是寫死的假資料（餘額一律 1500）。
      工具呼叫改由 `ChatToolService.ParseToolCall` 解析，AI 回覆沒有「|」或工具名稱不符時當成一般回覆（原本 `Split('|')[1]` 會 500）
    - **36 委託過期**：`EntrustMissionService.ExpireOverdueMissionsAsync`，由背景服務 `EntrustMissionExpiryWorker` 每小時執行
      （`App:EntrustExpiryIntervalMinutes`，0 = 停用）。徵求中、已過截止日期的委託：討論串沒有其他會員回覆 → 立即結案；
      有回覆 → 截止後再等 7 天（`ExpiryGracePeriod`，期間委託人仍可選最佳解答）。結案時狀態改為 **`expired`** 並退還預扣點數
      （點數紀錄「委託過期退款」），同樣鎖定委託人後在交易內重新讀取，與同時選最佳解答只會成立一個（`ConcurrencyTests`）。
      **沒有截止日期的委託不會過期**。過期的委託可以重新在同一篇文章建立委託；「我的任務」列在「已完結」並顯示「已過期」
    - **39 前端測試套件**：vitest 3 → 5.1（`npm audit` 0 個漏洞，需 Node 22.12 以上）。`authInterceptor.test.js` 改為在 `afterEach`
      執行計時器重置去重旗標（vitest 5 重複呼叫 `useFakeTimers` 會丟棄尚未執行的計時器）

34. 第二次全專案檢查 40～44 中不需要改資料表的項目（都先寫測試重現，`LowPriorityFixesTests`、`SqlServerLowPriorityTests`）：
    - **40 帳號**：Google 帳號用密碼登入回 400「此帳號使用 Google 登入」（原本 BCrypt 驗證 `GOOGLE_AUTH` 丟例外 → 500；
      `PasswordService.Verify` 遇到非 BCrypt 值也回 false）。後台 Cookie 加上 `MemberCookieValidator.ValidateAdminAsync`：
      帳號停用或不再是管理員時下一個請求就登出（導向後台登入頁）。Google 登入改寫入暫存 Cookie `AuthSchemes.External`
      （`Incode.External`，5 分鐘），`GoogleResponse` 讀出 Email 後清除：原本寫入預設方案，有後台 Cookie 時會覆蓋後台登入
    - **41 發文**：分類統計只算公開中的文章（原本排除的是不存在的狀態 3）；已刪除的文章不能編輯（404）；
      「發文 + 委託」（`ContentEntrustMissionService`）在同一個交易，委託失敗（例如點數不足）時文章 rollback 並刪除內文檔
      （`EntrustMissionService.CreateEntrustMissionAsync` 在已有交易時加入該交易）；委託標題 / 詳情 HTML 編碼後才接進內文
    - **43 後台**：`Account/Logout` 加防偽 token；會員列表的登入紀錄改為一次查詢（`GetRecentLoginLogsForAllUsersAsync`，原本每位會員各查一次）；
      編輯會員時 Email / 暱稱與其他會員重複顯示錯誤（原本撞唯一索引 500）
    - **44 其他**：`GET api/User/{id}/stats` 的 `points` 只回給本人（其他人為 null，個人頁卡片不顯示；前端原本讀錯欄位，一律顯示 0）；
      大頭貼網址只接受站內上傳的 `/img/user/{檔名}`、Google 預設圖或原本的值（`AvatarUrls`），註冊與修改個人資料都檢查；
      道具使用改為條件更新原子扣減（同時使用 10 次、數量 3 → 只成功 3 次）；`SelectBestAnswerAsync` 失敗時記錄 log
    - 測試：`ApiFactory.LoginAsync(admin: true)` 會把該會員設為管理員（後台 Cookie 每個請求都會檢查角色）

35. 第二次全專案檢查 42（論壇互動 / 追蹤重複寫入，都先寫測試重現）：
    - `content_interaction` 新增唯一索引 `UX_content_interaction_content_user_category`（content_id, user_id, category_id），
      定義在 `Models/Partials/incodedatabaseContext.cs` 的 `OnModelCreatingPartial`（重新產生模型不會覆蓋），`schema.sql` 已更新。
      **已上線的資料庫要執行一次** `docs/database/migrations/2026-11-17-content-interaction-unique.sql`：先刪除重複的互動
      （同一會員對同一內容的同一種互動保留 ID 最小的一筆，會印出刪除筆數），再建立索引；可重複執行（`MigrationScriptTests`）
    - 互動切換（`AddOrUpdateInteraction`）連點兩下：第二個新增撞到唯一索引時視為已新增，回傳資料庫目前的狀態（200）；
      同時取消也不會 500。判斷唯一索引衝突用 `Services/Common/DbErrors.IsUniqueViolation`
    - `follow` 原本就有主鍵（follower_id + followee_id），清單上「沒有唯一索引」有誤；但同時追蹤時第二個請求撞主鍵 → 500，
      改為回「已經追蹤過了」（與原本重複追蹤相同，400）；同時取消追蹤回「尚未追蹤」
    - 修正前在 SQL Server 同時送出 10 個請求：互動會寫入多筆、追蹤有 9 個 500；修正後只有一筆、沒有 500（`SqlServerInteractionUniqueTests`）

36. 第二次全專案檢查 40（帳號，都先寫測試重現，`AccountSecurityTests`）：
    - **登入戳記**：`[user]` 新增 `security_stamp`，登入時寫進 Cookie（`AuthSchemes.SecurityStampClaim`，會員與後台都有），
      `MemberCookieValidator` 比對不符就登出。**重設密碼**（原本舊的登入 Cookie 仍然有效）、**修改密碼**、Google 接管時換新。
      戳記從未換過（NULL）的帳號，改版前沒有戳記的 Cookie 仍有效，上線不會把所有人登出
    - **修改密碼**：`POST api/AuthApi/change-password`（會員 Cookie、需目前的密碼、與登入共用失敗次數限制）；成功後其他裝置登出，
      目前的裝置重新簽發 Cookie（保留「記住我」）。Google 帳號不能修改，「忘記密碼」也不再替 Google 帳號產生重設連結（改寄提醒信，API 回應不變）。
      前端：會員中心卡片的「修改密碼」按鈕（`components/users/ChangePasswordDialog.vue`，Google 帳號不顯示）
    - **Email 驗證**：新增 `email_verified`、`email_verify_token(_expiry)`。註冊後寄驗證信（不擋登入，24 小時有效；寄信失敗只記 log），
      `POST api/AuthApi/verify-email`（前端 `/verify-email` 頁）、`POST api/AuthApi/resend-verification`（會員中心未驗證提示的「重寄驗證信」）；
      重設密碼成功也視為已驗證。`GET api/User/profile` 看自己時回傳 `emailVerified`、`isGoogleAccount`
    - **Google 登入接管**（`AuthService.GetOrCreateGoogleUserAsync`）：同一個 Email 已有「密碼註冊、尚未驗證」的帳號時，
      清除密碼（之後只能用 Google 登入）、換新登入戳記（登出所有裝置）、標記為已驗證並記錄 log；已驗證的密碼帳號維持兩種登入方式。
      避免先用別人的 Email 註冊、等對方用 Google 登入後共用帳號
    - **已上線的資料庫要執行一次** `docs/database/migrations/2026-11-17-user-email-verify-security-stamp.sql`（新增 4 個欄位，可重複執行）：
      **既有的密碼會員一律為未驗證**（之後被同 Email 的 Google 登入會被接管，想保留密碼登入的人從會員中心重寄驗證信），既有 Google 帳號標記為已驗證
    - 測試：`ApiFactory` 以 `RecordingEmailService` 取代寄信；`CreateAnonymousClient(ownIp: true)` 讓用戶端有自己的來源 IP（依 IP 的速率限制不共用額度）

37. 第二次全專案檢查 41（發文驗證，都先寫測試重現，`ContentValidationTests`）：
    - `ContentService.CreateContent` 寫入前檢查（`POST api/ContentApi` 與「發文 + 委託」共用），不合法回 400（`BusinessException`）：
      類型只能是 1～4（`Constants/ContentTypes`）；主題不能有父文章、必須有存在且未停用的分類（`ContentCategoryStatus.Disabled` = 3）；
      回覆類必須有父文章，父文章與所屬主題都存在且未刪除，父文章類型依前端用法限制（`ContentTypes.AllowedParentTypes`：
      回文 → 主題、留言 → 回文或主題（簡易留言）、留言回覆 → 留言）；回覆的分類一律沿用父文章。
      修正前：類型 0 / 5、沒有分類的主題、掛在已刪除文章或主題下的回覆都能建立，不存在的父文章撞外鍵 → 500
    - 關鍵字搜尋只比對標題（內文存成檔案，資料庫沒有內文；移除永遠不成立的內文比對）。
      **注意**：前端導覽列的搜尋框沒有接到任何功能（`forumStore.setSearchKeyword` 沒有被呼叫），目前前台沒有搜尋入口（修正紀錄 38 先註解）
38. 部分修正項目的收尾：
    - **27 暱稱規則**（`Services/Users/UsernameRules`）：2～30 字（以字計算，emoji 算一字），不可前後空白、不可包含 `<` `>`、控制字元與看不見的格式字元
      （換行、零寬空白、文字方向控制等）。註冊、修改個人資料（`PUT api/User/profile`，400）、後台會員新增 / 編輯都檢查；
      **只檢查新設定的暱稱**，改版前不符合規則的暱稱沒改就不擋其他欄位的修改。Google 顯示名稱與註冊時由 Email 前綴產生的暱稱
      會移除禁用字元並截短到 30 字（原本 150）。前端與後台的暱稱輸入框 `maxlength="30"`。測試 `UsernameRulesTests`
    - **導覽列搜尋框**（`NavbarDefault.vue`）先註解，保留同寬的空白區塊讓版面不變；接上搜尋功能後再恢復
    - **22 論壇頁 JS**：Tiptap 編輯器改為開啟發文 / 回覆編輯器時才載入（`components/contents/lazyTiptapEditor.js`，`defineAsyncComponent`），
      論壇頁 JS 615 KB → 153 KB，編輯器另成 463 KB 的 chunk。以瀏覽器實測：進入論壇不下載編輯器，按「發佈新主題」後載入並可輸入
    - **移除 `Microsoft.VisualStudio.Web.CodeGeneration.Design`**：`dotnet list package --vulnerable` 已無漏洞。
      之後要用 Visual Studio「新增 Controller / View」自動產生時，VS 會提示安裝。原本經由它間接引用的 Newtonsoft.Json 不再存在：
      後台首頁的圖表資料改用 `System.Text.Json`（預設編碼器同樣把 `<` `>` 等轉成 `\u003c`，`AdminViewXssTests` 通過）
39. **改寫 git 歷史**（2026-11-18，強制推送 main）：以 `git filter-repo` 從整個歷史刪除會員上傳檔
    （`incode2/` 與 `incode.MVC/` 的 `wwwroot/img/fromApi|chat|user|product`、`wwwroot/contents`，保留 `img/product/NoImage.png`），
    `.git` 115 MB → 32 MB。1555 個 commit 全部保留（只動到上傳檔的 55 個變成空 commit），作者、時間、訊息與合併結構不變，
    main 最新的檔案內容與改寫前完全相同。
    - **所有 commit 的 SHA 都變了**：舊的 clone 不能再 pull，要重新 clone；文件中的 `edfd961` → `dd7e8d8`、`a1199c9` → `97630cd`（已沒有上傳檔）
    - 舊上傳檔只剩各環境的 `UploadRoot`（改寫前確認所有環境都已執行過搬檔腳本）；`scripts/copy-legacy-uploads.sh` 改為必須指定備份資料夾
    - GitHub 上已合併 PR 的 `refs/pull/*` 仍保留舊 commit（clone 不會下載）；舊分支 `claude/intelligent-lovelace-k9v90l` 需要在 GitHub 手動刪除，
      否則一般 clone（抓所有分支）仍會下載舊歷史
40. 第二次全專案檢查 43 的其餘項目（後台，都先寫測試重現，`AdminModerationTests`；併發在 `SqlServerLowPriorityTests`）：
    - **點數紀錄 / 儲值單改為唯讀**：刪除後台 `PointLog`、`PointRecharge` 的 Create / Edit / Delete（Action 與 View）。
      修正前在 main 上重現：後台可以新增一筆手填餘額 999999 的點數紀錄（帳本斷掉）、把儲值單改成 success 但會員沒有加點
    - **手動調整點數**（`PointLog/Adjust`，點數紀錄頁與每列的按鈕）：`IPointLogService.AdminAdjustPointsAsync` 在交易內走 `ChangePointsAsync`
      （類型「系統調整」、`source_type = system`），餘額由系統接續計算、不能扣到負數，單次 ±100,000 點，原因必填（最多 100 字，
      顯示在會員點數紀錄的副標），管理員與原因記在 log。同時 10 筆調整，帳本仍連續
    - **儲值人工補單**（儲值紀錄頁「待付款 / 失敗」那幾列）：`AdminCompleteRechargeAsync` 與綠界回呼共用 `CompleteRechargeAsync`
      （條件更新搶下訂單才加點），需填綠界交易編號或補單備註（寫入 `payment_trade_no`）。已成功 / 已取消的不能補；
      補單與綠界通知同時進來只加一次點
    - **檢舉清單**（後台「討論區 → 檢舉清單」，`ContentReportController` + `ContentReportService`）：列出待處理的檢舉
      （內容、類型、所屬主題、作者、檢舉數、最近檢舉時間），可以**駁回**（內容維持公開）或**下架**（`content.is_active = 3` 官方移除，
      常數 `Constants/ContentStatus`）；已下架的內容可以**恢復上架**（只限官方移除，會員自己刪除的不行）。每次處理寫一筆
      `content_report_review`（管理員、動作、備註、時間），**最後一次處理之後的檢舉才算待處理**；頁面下方列出最近 50 筆處理紀錄。
      下架 / 恢復以條件更新判斷狀態，同時操作只會成立一次
    - **已上線的資料庫要執行一次** `docs/database/migrations/2026-11-18-content-report-review.sql`（新增 `content_report_review`，可重複執行）；
      資料表設定在 `Models/Partials/ContentReportReview.cs` 與 `OnModelCreatingPartial`，`schema.sql` 已更新。既有的檢舉執行後全部列為待處理
41. 後台前端套件改由 npm 管理：
    - `incode.MVC/package.json`（版本固定，`package-lock.json` 提交）取代 `libman.json` 與外部 CDN；`incode.MVC.csproj` 的 `NpmLib` target
      在建置前執行 `npm ci` + `npm run lib`（`scripts/copy-lib.mjs`，複製清單外的檔案不會進 `wwwroot/lib`），套件與腳本沒變動時略過；
      `wwwroot/lib` 加進 `.gitignore`，第一次建置時產生的檔案也會進 publish 與靜態檔清單（以全新 clone `dotnet publish` 驗證）
    - 原本從 CDN 載入的 DataTables 1.13.7（+ FixedHeader、中文語系檔）、select2 4.1.0-rc.0、SignalR 7.0.5、marked（未指定版本）、
      Google Fonts（Nunito，登入頁）全部改為本機檔案：DataTables 1.13.11、`datatables.net-plugins` 1.13.6 的 `zh-HANT.json`、
      select2 4.1.0-rc.0、**SignalR 10.0.11（與後端相同主版本）**、marked 18.1.3、`@fontsource/nunito`（只用 woff2）。
      其餘維持主版本、升到發布兩週以上的最新修正版（jQuery 3.7.1、Bootstrap 5.3.8、Font Awesome 7.3.1、Chart.js 4.6.1、DOMPurify 3.4.17 等）。
      `npm audit` 0 個漏洞
    - 以瀏覽器實測 13 個後台頁面：沒有任何外部請求、沒有 404 與 JS 錯誤；DataTables（分頁、搜尋、中文介面）、select2、SignalR、marked 都有載入。
      原本在 CDN 連不到的環境，後台表格沒有分頁與搜尋
    - 順便修正：`ContentCategory/Index` 的資料網址寫死 `https://localhost:7190`（其他網址的環境載不到分類），改用 `Url.Action`
    - CI 的兩個後端 job 加上 `setup-node`（Node 22）
42. 後台改走服務層（一）點數 / 任務模組（`PointLog`、`PointRecharge`、`Mission`、`MissionProgress`、`EntrustMission`），
    Controller 不再使用 `incodedatabaseContext`；新增 `AdminPointQueryService`（點數紀錄 / 儲值單查詢、會員下拉選單、會員點數明細）、
    `AdminMissionService`（平台任務、任務進度查詢）、`AdminEntrustMissionService`。搬移時修正（都先在 main 重現，`AdminPointMissionTests`）：
    - **任務進度改為唯讀**：刪除後台 `MissionProgress` 的 Create / Edit / Delete。原本可以把進度改成 completed 讓會員領獎、
      或刪除已領獎的一次性任務進度讓會員再領一次；任務進度只由伺服器端觸發，要補點數用「手動調整點數」
    - **後台建立委託改走預扣流程**：原本直接寫入委託（可自填狀態、最佳解答），不扣點，之後選最佳解答或退款會憑空發點
      （在 main 重現：作者 0 點，後台建立了 500 點賞金的委託）。現在選擇公開中、沒有委託的主題文章，委託人為文章作者，
      走 `EntrustMissionService.CreateEntrustMissionAsync` 從作者點數預扣，點數不足顯示錯誤
    - **有會員進度的平台任務不能刪除**：原本撞外鍵 → 500；改為提示「請改為停用」（進度是一次性任務只能領一次的依據）
    - **平台任務表單只能設定可編輯的欄位**：建立時間、參與人數、上次重置時間由系統維護（原本整個實體由表單綁定）；
      新增時寫入建立時間（原本為 null）、預設啟用；獎勵點數 / 經驗值不能是負數（領獎時會變成扣點）、目標次數至少 1；
      新增頁補上難度、重置類型、啟用、精選欄位，重置類型改為下拉選單（舊資料的其他值保留）
43. 後台改走服務層（二）論壇模組（`Content`、`ContentEdit`、`ContentHistory`、`ContentCategory`）：新增 `AdminContentService`（文章列表 / 詳情、
    修改前後對照、修改紀錄）、`AdminCategoryService`。搬移時修正（都先在 main 重現 5/5，`AdminForumTests`）：
    - **文章詳情頁一律 500**：View 的 model 型別（`Content`）與 Controller 傳入的（`ContentDetailViewModel`）不同，檢舉清單的連結也連到這頁。
      重寫頁面：標題、作者、分類、狀態（公開 / 作者已刪除 / 已下架）、父內容、直接回覆；沒有分類的舊資料也能顯示
    - **文章修改紀錄改為唯讀**：刪除 `ContentHistory` 的 Create / Edit / Delete（可以偽造或清除會員的修改歷程；新增 / 修改頁引用不存在的欄位，一律 500）。
      紀錄只由會員編輯文章時寫入（`ContentService`）
    - **分類狀態**：編輯分類原本會把 `is_active` 蓋成 0（停用的分類被重新啟用），後台也無法停用分類。表單加上「狀態」下拉選單
      （官方 / 會員新增 / 停用，`ContentCategoryStatus.Selectable`），後台新增預設為「官方」；改版前不在選單內的狀態（例如舊後台建立的 0）
      會加進選單並選取，只改名稱時狀態不變。名稱 2～50 字、說明最多 200 字（`ContentCategoryStatus.NameMaxLength` 等，原本超過欄位長度會 500）
    - **分類列表的 XSS**：DataTables 預設把資料當 HTML 插入，會員在前台新增的分類名稱（`POST api/CategoryApi`）會在管理員的後台執行；
      所有欄位預設 `render.text()`（`columnDefs` 的 `_all`），只有「操作」欄自己產生連結；資料端點 `IndexJson` 改為 GET。
      以瀏覽器實測名稱為 `<img src=x onerror=…>` 的分類：以文字顯示、腳本沒有執行
    - 分類列表原本沒有新增 / 編輯 / 刪除的入口，補上；父分類可以選「（無）」，不能設成自己或自己的子孫（原本可以形成循環）
    - 還有文章或子分類的分類不能刪除（原本撞外鍵 → 500），提示改為停用；`ContentEdit` 的「查看」連到不存在的 Action（404），改連文章詳情
    - 內容類型 / 狀態的顯示名稱集中在 `ContentTypes.DisplayName`、`ContentStatus.DisplayName`（文章詳情與檢舉清單共用）；
      沒有建立時間的舊內容顯示空白（原本 0001-01-01）
44. 後台改走服務層（三）商城模組（`Order`、`Shipped`、`Product`）：新增 `AdminOrderService`（訂單查詢、表單規則；建立 / 修改 / 取消仍走 `OrderService`）、
    `AdminShippedService`（寄送紀錄）、`AdminProductService`（商品與圖片）。搬移時修正（都先在 main 重現 5/6，`AdminShopTests`；併發在 `ConcurrencyTests`）：
    - **商品庫存被蓋掉**：編輯商品原本以表單庫存覆蓋資料庫，打開編輯頁到儲存之間會員下單扣掉的庫存會被蓋回去（可超賣；main 上重現：庫存 3 被蓋回 10）。
      編輯頁的庫存改為唯讀顯示，加「調整庫存（增加填正數、減少填負數）」：條件更新、不會扣到負數、單次 ±100,000、上限 1,000,000，
      與其他欄位在同一個交易（庫存不足時其他欄位也不儲存）。同時 5 筆訂單 + 5 次補貨，庫存正確（SQL Server）
    - **訂單頁的儲存型 XSS**：「寄送管理」分頁用 JS 把收件人 / 地址 / 電話 / 備註（會員在前台填的）直接串成 HTML，會在管理員的後台執行；
      改為伺服器端以 Razor 輸出表格列（`Order/ShippedRows` → `_ShippedRows.cshtml`，自動 HTML 編碼，取代原本的 `GetShippedData` JSON），
      JS 只負責放進表格並初始化 DataTables。測試直接檢查輸出已編碼。以瀏覽器實測收件人為 `<img src=x onerror=…>` 的寄送資料：以文字顯示、腳本沒有執行
    - **後台改訂單狀態可讓已取消的訂單復活**：明細修改完成之後才另外更新狀態，期間被取消的訂單會被改回「已出貨」，庫存與點數卻已退還。
      `UpdateOrderItemsAsync` 新增 `newStatus`，狀態與明細在同一個交易（鎖內）設定；同時修改與取消 10 筆（SQL Server），最終狀態與庫存 / 點數一致
    - **寄送資料**：新增時指定不存在的訂單 → 500（main 重現）、已取消的訂單不能新增、姓名 / 地址 / 電話各 50 字與備註 300 字（原本超過欄位長度 500，
      只有 SQL Server 會發生）、不能同時「失敗」又「完成」；編輯時所屬訂單不可變更（原本整個實體由表單綁定，可以把紀錄改掛到別的訂單；main 重現）；
      **已出貨 / 已完成訂單的寄送紀錄不能刪除**（出貨證明；main 重現）。訂單狀態在同一個語句判斷：刪除用條件刪除（`ExecuteDelete` 的 WHERE 含訂單狀態），
      新增先對訂單做「不改內容」的條件更新鎖住該筆訂單，檢查與寫入之間狀態不會被同時進行的出貨 / 取消改掉；刪除不存在的紀錄顯示錯誤（原本顯示成功）。
      訂單頁的寄送列表同步隱藏刪除鈕
    - **商品**：欄位綁定改為只收可編輯的欄位（原本圖片檔名、建立時間可由表單指定）；名稱必填且 100 字內、描述 500 字、類別 / 系列 100 字、點數 0～1,000,000；
      圖片先存檔再寫資料庫，資料庫失敗會刪掉新圖；換圖時資料庫更新成功才刪舊圖（原本先刪舊圖，存檔失敗會留下指向已刪除檔案的商品）；
      圖片檔名改為時間戳記 + 8 碼隨機（原本 `yyyyMMddHHss` 少了分鐘、只有 4 碼）；格式錯誤的圖片不會建立商品
    - 新增訂單 / 修改訂單的會員檢查、狀態檢查移到服務；訂單 / 商品 / 寄送資料的失敗訊息在表單上方顯示
    - 商品新增 / 編輯保留模型繫結檢查：數字欄位填 `abc` 等無法轉換的值時顯示「欄位格式不正確」（避免變成 0 就存起來；調整庫存填 `abc` 也不會默默忽略）；
      調整庫存直接比較範圍（`Math.Abs(int.MinValue)` 會 500）；訂單編輯時明細的商品不存在（「無此商品」）回到表單顯示訊息，只有訂單本身不存在才是 404

45. 後台改走服務層（四）聊天與首頁（`ChatMessages`、`ChatSessions`、`Home`）：新增 `AdminChatService`、`AdminDashboardService`（`Services/Common`），
    至此後台所有 Controller 都不再使用 `incodedatabaseContext`。搬移時修正（都先在 main 重現 7/8，`AdminChatTests`）：
    - **聊天訊息與對話改為唯讀**：刪除 `ChatMessages` 的 Create / Edit / Delete 與 `ChatSessions` 的 Create / Edit / Delete。原本可以偽造或竄改會員與 AI 的對話紀錄；
      編輯對話的表單沒有「真人模式」欄位，儲存會把它重設為 false；刪除有訊息的對話撞外鍵 → 500。客服只用客服頁的即時回覆（SignalR）；
      對話列表加「查看訊息」連結
    - **客服頁開啟時顯示對話目前的模式**：View 的 `isHumanMode` 原本固定為 false（Controller 設的 `ViewBag` 沒人讀），開啟真人模式的對話仍顯示「AI自動回覆」，
      要等 SignalR 事件才更新；改由 `ChatIndexViewModel.IsHumanMode` 提供
    - 首頁待處理訂單數改用 `OrderStatus.Pending`
46. 刪除前端沒有使用的 API：`NotificationController`、`StatusController`、`InventoryController`、`ChatSessionsApiController`
    （含 `INotificationService`、`IStatusService` 與其實作及註冊，以及只測這些端點的測試）。`IInventoryService` 當時保留，之後確認沒有任何流程使用而刪除（連同 `InventoryItemDto` 與兩個測試；`UserInventory` 實體 / 資料表保留）。
    `IFollowService` 移除沒有呼叫端的 `SearchUsersAsync`、`GetMyFollowingListAsync`（連同 `FollowingUserDto`）。
    其他沒有呼叫端 / 引用的項目：`IMissionProgressService.GetAllProgressAsync`、`IncrementProgressAsync`（連同 `MissionProgressDTO`）、`IPointLogService.GetDetailedLogsAsync`（連同私有的 `GetOrderDisplayText`）、`IUserService.SearchUsersAsync`、`NewOrderDTO`、`ClaimMissionRequest`、`ContactViewModel` 與 `RequireEmailOrPhoneAttribute`。
    `IChatService` 移除沒有呼叫端的 `GetUserSessionsAsync`、`GetFirstUserMessageAsync`、`SetTitleAsync`、`DeleteSessionAsync`（`CreateSessionAsync` 改為內部私有方法，連同 `ChatSessionDto`）。
    要重新開放前端使用時從 git 歷史取回。`RateLimitPolicies.AiChat` 仍套用在 `SendMessage`、`UploadImage`
47. 日誌改用 Serilog（`Serilog.AspNetCore`）：`Program.cs` 以 `UseSerilog` 讀取 `appsettings` 的 `Serilog` 區段（取代 `Logging`），輸出到 Console 與每日滾動檔案
    `logs/incode-.log`（保留 30 天，`logs/` 已 gitignore；正式環境以 `Serilog__WriteTo__1__Args__path` 設成部署資料夾以外的路徑）。
    `Microsoft`、`Microsoft.EntityFrameworkCore`、`System` 只記 Warning 以上（避免 SQL 內容與會員資料進日誌）；`UseSerilogRequestLogging` 每個請求一筆摘要，
    只含路徑、不含查詢字串（重設密碼 / 驗證信的 token 在查詢字串）。既有的 `ILogger<T>` 不需修改；日誌內容不可寫入密碼、token、Email
    日誌補強：原本吞掉例外卻沒記錄的地方（`MissionService.ClaimMissionAsync`、AI 系統提示詞讀取）改為記錄；
    登入被暫停 / 失敗記錄來源 IP（不記 Email 與密碼）。`MissionService` 的 logger 為選用參數（測試可直接 `new MissionService(context)`）

48. 第三次全專案檢查 45（任務獎勵重複領取，先寫測試重現）：`MissionProgressService.ClaimRewardAsync` 原本只在「領獎時間在本週期」時拒絕，
    但每筆進度只屬於一個週期，昨天已領過的每日任務進度今天再領會成功（每週 / 每月 / 每年任務相同），每天都能把所有舊進度再領一遍。
    改為**進度已領過（`RewardedAt` 有值或狀態為 rewarded）一律拒絕**；新週期的進度照常可領。「我的任務」的可領判斷使用同樣的規則，
    移除不再需要的同週期比對（`IsSameWeek`）。測試：`MissionProgressServiceTests`（四種重置類型的舊週期進度不能再領、新週期仍可領）

## 全專案檢查結果（2026-11）

點數商城以外的前後端模組已全部檢查過。🔴 1～5、6（金鑰已撤銷）、7～25 已修正（見修正紀錄 6～9、12～29、38）。
程度：🔴 嚴重（可直接被利用，影響點數 / 帳號 / 資料）、🟠 高、🟡 中、⚪ 低。
第二次全專案檢查 26～44 已全部修正（見修正紀錄 30～40）。

### 🔴 嚴重（1～5 已修正）

1. ~~`add-reward` 會員可自訂點數~~ 2. ~~後台點數頁面沒有登入檢查~~ 3. ~~會員 API `[Authorize]` 被註解掉~~
4. ~~客服聊天沒有權限與擁有者檢查~~ 5. ~~其他沒有登入檢查的 API~~

### 🟠 高

6. ~~API 金鑰提交進 git 歷史~~（`GeminiApiKey`、`TmdbApiKey` 已於 2026-11 撤銷並重新產生；舊值仍在 git 歷史但已失效。
   金鑰只放 user-secrets 或環境變數，`appsettings*.json` 保持空字串）。
   ECPay 目前放的是綠界公開的測試帳號（`3002607`），正式上線時正式金鑰不可放進 repo。
7. ~~任務進度可以自己增加~~（`increment` API 已刪除，見修正紀錄 6）
8. ~~點數併發問題~~（見修正紀錄 12）
9. ~~ECPay 回呼冪等性~~（見修正紀錄 12）
10. ~~聊天室 XSS~~（含後台客服頁面，見修正紀錄 13）
11. ~~聊天圖片上傳~~（見修正紀錄 13）

### 🟡 中

12. ~~CSRF~~（見修正紀錄 14）
13. ~~論壇互動可以送出任意分類、按讚任務可重複觸發~~（見修正紀錄 16）
14. ~~委託任務沒有取消流程~~（見修正紀錄 18）
15. ~~沒有登入嘗試次數限制~~（見修正紀錄 14）
16. ~~儲值金額沒有驗證~~（見修正紀錄 16）
17. ~~會員可以替自己加道具~~（`POST api/Inventory/add` 已刪除，見修正紀錄 16）

### 🟠 部署 / 維運（2026-11-04 檢查）

18. ~~網址寫死~~（已修正，見修正紀錄 7）
19. ~~資料庫無法重建~~（已補 `docs/database/schema.sql`、`seed.sql` 與 Docker 資料庫，見修正紀錄 7、8）
20. ~~沒有 CI~~（已加 build 檢查，見修正紀錄 9；自動化測試仍未建立）
21. ~~上傳檔案在 git 與 wwwroot~~（見修正紀錄 21、29：新的上傳檔寫到 `App:UploadRoot`，舊檔已從 git 移除，以 `scripts/copy-legacy-uploads.sh` 搬到 `UploadRoot`）。
    git 歷史已改寫，clone 115 MB → 32 MB（見修正紀錄 39）。
22. ~~**前端效能 / 結構**~~（見修正紀錄 15、26、38）：~~Vuetify 全量匯入~~、~~頁面沒有延遲載入~~、~~只有全域 axios 處理 401~~、~~重複載入 Bootstrap 5.3 CSS~~。
    `forumStore.js` 已拆分（2,744 行 → 約 230 行的 state / getters + `stores/forum/` 底下的輔助模組與 6 組 actions），
    `TopicListView.vue` 已拆分（2,755 行 → 外層 82 行 + `ThreadDetailView.vue`、`TopicList.vue` 兩個視圖元件與 8 個 composable，見 `incode.Vue/CLAUDE.md`）。
    論壇頁 JS 615 KB → 153 KB（Tiptap 編輯器改為開啟時才載入，見修正紀錄 38）。
23. ~~前端呼叫不存在的後端端點~~（見修正紀錄 17）
24. ~~套件：Tiptap 漏洞、前端沒有 lockfile~~（見修正紀錄 19）
25. ~~後端分層~~（前台 API 與 Hub 已完成，見修正紀錄 27；後台 MVC 的 15 個 CRUD 頁面逐模組改走服務層，見修正紀錄 42～45；已全部完成）

## 第二次全專案檢查（2026-11-11）

重讀整個專案（後端 API、服務、後台、設定，前端的路由、API 層與 HTML 顯示路徑）後新發現的問題。26～39 已修正（見修正紀錄 30～33）；40～44 已修正（見修正紀錄 34～37、40）。
編號接續上一份清單。都是讀程式碼得出的，尚未實際重現（檢查環境沒有 .NET SDK / Docker）；修正時請先寫出重現步驟或測試。
前端當時 `npm test` 76 項通過、`npm run build` 成功。

### 🔴 高（一般會員可直接利用，已修正，見修正紀錄 30）

26. ~~**論壇內文 HTML 檔可在後端網域執行腳本（儲存型 XSS）**~~：`ContentService.SaveContentBodyAsync` 把會員送來的 HTML 原樣寫成
    `/contents/content-xxx.html`，由後端以靜態檔提供。前端顯示有經 DOMPurify，但直接開啟該檔案網址不會過濾，腳本在後端網域執行：
    同源請求會帶後台 Cookie、Origin 檢查也視為自己，誘導管理員點連結即可操作後台（對會員同樣有效）。
    方向：存檔前在伺服器端過濾，或對 `/contents/` 回應加 `Content-Security-Policy: sandbox` 與 `X-Content-Type-Options: nosniff`
    （改版前的舊檔已搬到 `App:UploadRoot`，見修正紀錄 29；尚未執行搬檔腳本的環境，wwwroot 可能還有舊檔）。
27. ~~**後台首頁可被會員暱稱注入腳本**~~（暱稱規則見修正紀錄 38）：`Views/Home/Index.cshtml` 以 `@Html.Raw(JsonConvert.SerializeObject(...))` 把發文數前 10 名的暱稱
    輸出到 `<script>` 內；暱稱沒有格式限制（`RegisterDto` / `UpdateUserDto`，最長 150 字），Newtonsoft 預設不跳脫 `<`，
    暱稱含 `</script><script>…` 會在管理員登入後的首頁執行。方向：改用 `Json.Serialize` 或跳脫 HTML，並限制暱稱字元與長度。
28. ~~**並行取消 / 修改訂單會重複退點**~~：`OrderService.CancelOrderAsync`、`UpdateOrderItemsAsync` 在交易與點數鎖之前就讀取訂單狀態與明細
    （`OrderApiController` 的狀態檢查也在鎖之前）。兩個並行的取消請求都讀到「待處理」，第二個拿到鎖後用舊資料再退一次點、再加一次庫存；
    並行修改數量同理。`Orders` 沒有 rowversion，`ConcurrencyTests` 沒有涵蓋這條路徑。方向：拿到鎖後在交易內重新讀取訂單再判斷，並補併發測試。

### 🟡 中

29. ~~**打賞紀錄洩漏 Email**~~（見修正紀錄 31）：`PointLogService.TipAsync` 把對方的 Email 寫進點數紀錄副標（「給 xxx@…」「來自 xxx@…」），
    打賞 1 點就能取得任何發文者的 Email。改用暱稱；已寫入的舊紀錄也要處理。
30. ~~**「我的收藏」顯示的是隱藏的文章**~~（見修正紀錄 31）：`ContentApiController.GetMyBookmarkedContents` 以分類 ID 2（隱藏）查詢，收藏是 3。
    改用 `InteractionCategoryIds.Bookmark`（`ContentService.GetContents` 裡寫死的 `hideCategoryId = 2` 等也一併改用常數）。
31. ~~**互動紀錄可匿名查詢**~~（見修正紀錄 31）：`GET api/ContentInteractionApi?userId=`、`GET api/ContentInteractionApi/content/{id}` 不需登入，
    回傳每筆互動的 UserId，可查出誰檢舉、隱藏、收藏了什麼。
32. ~~**AI 客服的「查詢」是寫死的假資料**~~（見修正紀錄 33）：`ChatMessagesApiController` 的 `GetProductInfo` / `SearchContents` / `PointLogs` 回固定內容
    （例如餘額 1500、userId 1），會員問點數會得到錯的答案。另外 `firstResponse.Split('|')[1]` 沒有檢查長度（AI 回覆格式不符時 500）。
33. ~~**請求大小沒有限制**~~（見修正紀錄 32）：全站上限 500 MB（`Program.cs` 的 Kestrel / FormOptions），文章標題、內文、聊天訊息沒有長度限制；
    `POST api/User/images`（大頭貼）允許匿名且沒有速率限制，可塞滿磁碟。
34. ~~**聊天沒有速率限制**~~（見修正紀錄 32）：每則訊息最多呼叫 2 次 Gemini，「生圖」另呼叫付費 API（`GeminiService.GenerateImageAsync`），可被刷費用。
35. ~~**Google 登入同名會 500**~~（見修正紀錄 33）：`username` 有唯一索引，`UserService.CreateGoogleUserAsync` 直接用 Google 顯示名稱，
    第二位同名（或都沒有名稱）的使用者建立帳號失敗。
36. ~~**委託過期沒有處理**~~（見修正紀錄 33）：有人回覆後不能取消，委託人不選最佳解答時預扣點數永遠卡住；截止日期過後也不會退款或結案。
37. ~~**綠界測試金鑰是公開的**~~（見修正紀錄 32）：以 `appsettings.json` 的測試 HashKey / HashIV，任何人都能偽造付款成功通知替自己加點；
    對外公開的環境不可使用這組（回呼也沒有檢查 `SimulatePaid`）。
38. ~~**分頁參數沒有檢查**~~（見修正紀錄 32）：`ContentService.GetContents` / `GetChildrenByContentId` 等，`pageSize=0`、`page=0` 會 500，極大的 `pageSize` 會一次撈全部。
39. ~~**前端測試套件漏洞**~~（見修正紀錄 33）：`npm audit` 回報 3 個（2 個 critical：`tinypool`；1 個 moderate：`@vitest/mocker`），只影響開發與 CI、不進正式建置；
    修正需升級 vitest 5（破壞性更新）。

### ⚪ 低 / 功能缺口

40. ~~帳號：註冊不驗證 Email；重設密碼後舊的登入 Cookie 仍有效；後台 Cookie 沒有停用檢查；會員沒有「修改密碼」功能；
    Google 帳號用密碼登入會 500；瀏覽器同時有後台 Cookie 時使用 Google 登入會覆蓋後台登入~~（見修正紀錄 34、36）。
41. ~~發文：`Type`、父文章、分類都不驗證；「發文 + 委託」不是同一個交易、委託標題 / 詳情未編碼；分類統計算進已刪除的文章；
    可以編輯已刪除的文章~~（見修正紀錄 34、37）。關鍵字搜尋只搜標題（內文存成檔案，決定維持）；前端導覽列的搜尋框沒有接到功能（先註解，見修正紀錄 38）。
42. ~~`content_interaction`、`follow` 沒有唯一索引，連點兩下可能寫入兩筆（按讚數虛增）~~（見修正紀錄 35；`follow` 原本就有主鍵，問題是同時追蹤時 500）。
43. ~~後台：`PointLog`、`PointRecharge` 可以直接新增 / 修改 / 刪除（餘額手填，會破壞帳本連續性；把儲值改成 success 不會加點）；
    沒有檢舉清單、不能下架文章；`Account/Logout` 沒有防偽 token；會員列表每位會員各查一次登入紀錄；後台改 Email 不檢查重複~~（見修正紀錄 34、40）。
44. ~~其他：公開個人頁顯示點數餘額；大頭貼網址可填任意外部網址；`SelectBestAnswerAsync` catch 後沒有記錄 log；道具使用不是原子扣減~~（見修正紀錄 34）。

## 第三次全專案檢查（2026-11-24）

重讀整個專案（後端 API、服務、後台 Controller / View，前端顯示內容的方式）後新發現的問題，編號接續上一份清單。45 已修正（見修正紀錄 48），其餘尚未修正。
45、46 已用暫時的測試重現（測試未提交），其餘是讀程式碼得出的；修正時請先寫出重現測試。

### 🔴 嚴重

45. ~~**任務獎勵可以重複領取**~~（已修正，見修正紀錄 48）：`MissionProgressService.ClaimRewardAsync` 只在 `RewardedAt` 落在本週期時拒絕，
    但每個週期本來就有各自的進度列，昨天已領過的每日任務進度今天再領會通過（測試：`Ok=True`，多拿 50 點）。
    連續登入 N 天後，每天可以對每筆舊進度各呼叫一次 `POST api/MissionProgressApi/claim-reward`；畫面不顯示按鈕但 API 照收。
    每週 / 每月 / 每年任務相同。方向：`RewardedAt` 已有值就拒絕，補跨日測試。

### 🟠 高

46. **會員 Cookie 的 Admin 角色不會重新檢查**（已重現）：管理員從前台（`AuthApi/login`）登入時，會員 Cookie 帶 `Role=Admin`
    （7 天、自動延長）；`MemberCookieValidator.ValidateAsync` 只檢查啟用與登入戳記，不檢查角色。降為一般會員後
    `GET api/User`（所有會員 Email）、`POST api/User/{id}/deactivate` 仍回 200；讀任何人的聊天紀錄（`GetHistory`）、
    加入客服群組、限管理員的互動查詢也一樣。方向：會員 Cookie 驗證時一併比對角色，或會員 Cookie 不放 Admin 角色。

### 🟡 中

47. **任務進度同時觸發回 500，主要操作卻已完成**：兩個「本週期第一次」的觸發同時進來（同時兩則回覆、登入 + 發文），
    `UpdateMissionProgressAsync` 撞唯一索引 `UX_User_Mission_Cycle`，例外沒有攔下；`CreateContent` 已存好文章卻回 500，
    使用者重試就重複發文。按讚、登入同一路徑。`ClaimMissionAsync` 以錯誤訊息字串判斷，應改用 `DbErrors.IsUniqueViolation`。
    尚未重現（需要 SQL Server）。
48. **停用的論壇分類在前台仍顯示**：`CategoryService.GetCategoriesAsync` 不依狀態過濾，`CategoryDto` 沒有狀態欄位；
    停用後仍列出，但在該分類發文失敗（「分類不存在或已停用」）。會員也能在停用的分類底下新增子分類。
49. **已刪除 / 已下架的文章仍可讀取**：內文檔 `/contents/*.html` 在作者刪除或後台下架後仍可直接用網址開啟，
    每次編輯留下的舊版本也都是公開的；後台下架違規內容實際上沒有移除內容。
50. **已登入會員可以塞滿磁碟**：`POST api/images/upload` 每個請求最多 100 MB、沒有速率限制；
    每次發文 / 編輯都寫一個新的內文檔（最多 500 萬字元）、沒有速率限制，編輯後舊檔不刪除。

### ⚪ 低

51. 商城：`OrderItemRequest.Quantity` 沒有 `[Range]`，`TotalPoint += qty * point` 是沒有溢位檢查的 int 運算，
    庫存 × 單價超過約 21 億時總點數變負數、不扣點；會員端 `NewShippedRequest` 收件資料與備註沒有 `[StringLength]`（太長在 SQL Server 500，後台已有限制）。
52. 後台會員編輯的「重設密碼」欄位沒有作用（`MemberController.Edit` 寫入雜湊的那行被註解，仍顯示成功）；
    修改 Email 時 `EmailVerified` 維持原值；可以在這頁停用自己（API 有擋）。
53. 標題很長的文章打賞失敗：`display_source` 為 `nvarchar(200)`，「收到打賞〈標題〉」最長 206 字，標題 195 字以上 500。
54. 分頁：`Paging.Normalize` 沒有限制 `page` 上限，`page=2147483647` 使 offset 溢位為負數，SQL Server 500；
    `GET api/ContentApi/{id}/comments` 默默限制 100 筆（前端沒有使用，可以刪除）。
55. 登入回應透露帳號狀態：停用帳號與 Google 帳號在比對密碼前就回傳專屬訊息，可以查出哪些 Email 是這兩種帳號。
56. 其他：同時送出可在同一篇文章建立兩個委託（只影響發文者自己的點數）；綠界回呼沒有檢查金額與商店代號（多一層保護）；
    Gemini API Key 放在網址查詢字串（改用 `x-goog-api-key` 標頭）；聊天 API 接受任意 SignalR `ConnectionId`；
    後台登入不寫登入紀錄；會員可建立 100 字的分類名稱，後台表單只允許 50 字，無法編輯這些分類。
