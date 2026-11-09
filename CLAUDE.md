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
- 前端合併前的歷史：`git log edfd961^2`（合併 commit 的第二個 parent）。

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
    ├── Validation/             自訂驗證屬性
    ├── Prompts/Prompts.md      AI 客服系統提示詞（執行時讀取，路徑不可改）
    ├── App_Data/uploads/       上傳檔案預設位置（App:UploadRoot，已 gitignore），網址 /img/...、/contents/... 不變
    └── wwwroot/                靜態檔；contents/ 與 img/fromApi、chat、user、product 下是改版前的上傳檔（仍在 git 中）
```

- 模組名稱統一：`Chat`、`Contents`、`Point`、`PointShop`、`Users`，**命名空間 = `incode.MVC.<資料夾路徑>`**
  （例：`Services/Users/AuthService.cs` → `incode.MVC.Services.Users`）。
- 專案內命名空間由 `GlobalUsings.cs` 統一引入，檔案內不需再寫 `using incode.MVC.*`。
- 原始碼一律 UTF-8（部分舊檔原為 Big5，已轉換；Big5 在非 Windows 環境編譯時中文字串會變亂碼）。

### 模組

| 模組 | API | 後台 | Service |
|---|---|---|---|
| 會員 | `ApiControllers/Users/*` | `Account`、`Member` | `Services/Users/*` |
| 論壇內容 | `ApiControllers/Contents/*` | `Content*` | `Services/Contents/*` |
| 點數 / 任務 / 儲值 | `ApiControllers/Point/*` | `Mission*`、`EntrustMission`、`PointLog`、`PointRecharge` | `Services/Point/*` |
| 點數商城 | `ApiControllers/PointShop/*` | `Product`、`Order`、`Shipped` | `Services/PointShop/*` |
| 客服聊天 | `ApiControllers/Chat/*` + `ChatHub` | `ChatSessions`、`ChatMessages` | `Services/Chat/*` |

## 驗證機制（`Program.cs`）

- 預設 `SmartScheme`：有 `Incode.Admin` Cookie → `AdminAuth`，否則 → `MemberAuth`（`Incode.Member`）
- 前台 API 未登入回 401 / 403（不轉址）
- **CSRF**：`/api` 寫入請求與 `/chatHub` 會檢查 `Origin`（`UseApiOriginCheck`），新的前端網址要加到 `App:CorsOrigins`，否則會被 403；
  伺服器對伺服器的回呼（如綠界）要加進 `OriginCheckExtensions.ExemptPaths`
- **登入限制**：登入 / 註冊 / 密碼重設端點加 `[EnableRateLimiting(RateLimitPolicies.Xxx)]`；部署在反向代理後面時需設定
  ForwardedHeaders，否則所有人共用代理 IP 的額度
- 後台 Controller 一律加：`[Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]`
  （後台登入 `AccountController` 只允許 `RoleId == Admin`，Claim 角色固定為 `"Admin"`）
- 取得登入者 ID：`User.GetId()`（`Extensions/ClaimsExtensions.cs`）

## 開發慣例（新程式請遵守）

- **擁有者檢查**：前台 API 讀寫資料時必須確認屬於 `User.GetId()`，找不到或非本人一律回 404。
- **API 回應**：使用 `ResultDTO<T>`，並以 `this.ToActionResult(result)`（`Extensions/ResultDTOExtensions.cs`）回傳。
  成功一律 HTTP 200（`Code` 為 204 時也是 200，避免前端拿不到 body）；失敗使用 `Code` 作為 HTTP 狀態碼（400 / 404 / 409 / 500）。
  前端以 `error.response.data.message` 顯示錯誤。
- **不外洩例外**：catch 後用 `ILogger` 記錄，回傳通用訊息，不要把 `ex.Message` 回給前端。
  要讓使用者看到的業務錯誤丟 `BusinessException`（`Services/Common/`），只有它的 `Message` 可以直接回傳。
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
- **檔案存放**：上傳檔一律透過 `UploadStorage`（`Services/Common/UploadStorage.cs`）寫入 / 刪除，資料夾用 `UploadFolders` 常數，
  檔名由伺服器產生（Guid）。**不要寫進 `wwwroot`**（會被提交進 git、重新部署會被覆蓋）。
  位置由 `App:UploadRoot` 設定（預設 `App_Data/uploads`），正式環境設成部署資料夾以外的絕對路徑
- **顯示使用者內容**：Razor 不要對會員 / AI 產生的內容用 `@Html.Raw`；需要保留格式時先經 DOMPurify（`~/lib/dompurify`）。
  前端 `v-html` 前一律過濾（論壇用 `utils/sanitizeHtml.js`，聊天用 `ChatWidget.vue` 的 `toSafeHtml`）。
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
- 交易開始先 `IPointLogService.LockUsersAsync(userId)`，同一會員的點數異動排隊；非 SQL Server（測試用 SQLite）時略過
- 明細同商品會合併；數量必須 > 0；新加入的商品必須上架中
- 權限 / 狀態檢查由呼叫端負責：會員只能操作自己的「待處理」訂單；已取消訂單不可改回其他狀態

## 建置與驗證

- 需要 .NET 10 SDK（Visual Studio 2026）、SQL Server（`appsettings.json` 的 `ConnectionStrings:incodedatabase`，預設 LocalDB）；
  新環境用 `docs/database/schema.sql` + `seed.sql` 建立資料庫，資料表變動後用 `dotnet ef dbcontext script` 重新產生
- 機密設定（Google OAuth、ECPay、Gemini 等）放 user-secrets 或環境變數，不要提交到 repo
- **測試資料庫**：`docker compose up -d` 啟動 SQL Server 並自動建表與寫入基本資料，後端用 launch profile `https (Docker DB)`；
  `docker compose down -v` 可重建全新資料庫。註冊帳號後 `UPDATE [user] SET role_id = 1 ...` 即可登入後台
- **CI**：`.github/workflows/ci.yml`（只做建置，沒有測試）。PR 合併前請確認兩個 job 都通過
- 專案沒有自動化測試。環境沒有 .NET 10 SDK 時可用 Docker 映像 `mcr.microsoft.com/dotnet/sdk:10.0` 建置 / 執行
  （需 `--network host` 才能走主機的 proxy 與連到 Docker 資料庫）；搭配 Docker 資料庫可實際啟動後端，
  用 API（註冊 / 登入取得 Cookie）做端對端測試
- 改版前的上傳檔（`wwwroot/img/fromApi|chat|user|product`、`wwwroot/contents`）仍在 git 中，新的上傳檔在 `App:UploadRoot`（已 gitignore）

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
    `Microsoft.VisualStudio.Web.CodeGeneration.Design`（VS scaffolding，執行時不使用）仍帶入有低風險漏洞的 `NuGet.*` 6.12.1
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
      DOMPurify 放在 `wwwroot/lib/dompurify/`（libman.json 已登記）
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

## 全專案檢查結果（2026-11）

點數商城以外的前後端模組已全部檢查過。🔴 1～5、6（金鑰已撤銷）、7～20、23、24 已修正，21、22 部分修正（見修正紀錄 6～9、12～24），其餘尚未處理。
程度：🔴 嚴重（可直接被利用，影響點數 / 帳號 / 資料）、🟠 高、🟡 中、⚪ 低。
建議修正順序：把舊上傳檔移出 git（21 的其餘部分，需先搬檔）→ 其餘。

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
21. **上傳檔案在 git 與 wwwroot**（部分修正，見修正紀錄 21）：~~新的上傳檔寫進 wwwroot~~。
    尚未處理：改版前約 93 MB 的上傳檔（`wwwroot/img/fromApi|chat|user|product`、`wwwroot/contents`）仍在 git 中。
    移除前要先把這些檔案複製到各環境的 `App:UploadRoot`（否則 pull 後檔案被刪、網址失效）；git 歷史中的檔案要改寫歷史才會變小。
22. **前端效能 / 結構**（部分修正，見修正紀錄 15）：~~Vuetify 全量匯入~~、~~頁面沒有延遲載入~~、~~只有全域 axios 處理 401~~。
    尚未處理：同時使用 Bootstrap、Vuetify、Material Kit（Material Kit 內含自己編譯的 Bootstrap 5.2，`main.js` 又載入一份
    Bootstrap 5.3 CSS，要移除需逐頁確認樣式）；`TopicListView.vue`、`forumStore.js` 超過 2700 行（論壇頁 JS 約 615 KB，含 Tiptap 3）。
23. ~~前端呼叫不存在的後端端點~~（見修正紀錄 17）
24. ~~套件：Tiptap 漏洞、前端沒有 lockfile~~（見修正紀錄 19）
25. **後端分層**：39 個 Controller 中 29 個直接操作 DbContext（例如 `ChatMessagesApiController` 700 行），權限檢查分散。
