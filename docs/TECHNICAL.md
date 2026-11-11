# incode 技術文件

本文件說明整個專案（後端 `incode.MVC/`、前端 `incode.Vue/`、資料庫）的架構、各模組的運作方式與跨模組的重要機制。

- 開發環境、啟動方式：[`README.md`](../README.md)
- 建置、測試與驗證：[`TESTING.md`](TESTING.md)
- 開發慣例、修正紀錄、待處理問題：[`CLAUDE.md`](../CLAUDE.md)、[`incode.Vue/CLAUDE.md`](../incode.Vue/CLAUDE.md)
- 資料表欄位意義：[`dbdiagram.md`](dbdiagram.md)；建表與基本資料：[`database/`](database/README.md)
- 論壇 Content API 導覽：[`CONTENT_API_ONBOARDING.md`](CONTENT_API_ONBOARDING.md)

> 本文件依 2026-11 的程式碼撰寫。端點與欄位細節以程式為準（`ApiControllers/`、`Dtos/`）。

---

## 1. 系統總覽

「影庫」是以電影為主題的社群平台：論壇、點數經濟（任務、儲值、商城）、AI / 真人客服。

```
┌────────────────────┐   HTTPS + Cookie     ┌──────────────────────────────────────────┐
│ incode.Vue (SPA)   │ ───────────────────▶ │ incode.MVC (ASP.NET Core 10)             │
│ Vue 3 / Vite       │   REST  /api/...     │                                          │
│ Pinia / Router     │ ◀─────────────────── │  ApiControllers  → Services → EF Core ──┼──▶ SQL Server
│                    │   SignalR /chatHub   │  Controllers+Views (後台 Razor)          │
└────────────────────┘                      │  Hubs/ChatHub                            │
                                            │  wwwroot (後台靜態檔、上傳圖片、論壇內文) │
管理員瀏覽器 ─── 後台 MVC 頁面 ────────────▶ └───────┬───────────────┬──────────────────┘
                                                    │               │
                                          Gemini API (AI 客服)   綠界 ECPay（儲值）
                                          SMTP（忘記密碼信）      Google OAuth（登入）
```

重點：

- **單一後端同時服務兩種使用者**：會員透過 Vue 前台呼叫 REST API；管理員直接使用後端 Razor 後台。兩者的 Cookie 與驗證方案不同（見 §3）。
- **前後端可以在不同網域**：靠 CORS 帶 Cookie；因此 Cookie 使用 `SameSite=None`，CSRF 改由 Origin 檢查負責（見 §3.3）。
- **資料庫優先（Database-First）**：`Models/` 由資料庫產生，專案沒有 EF migration。

### 技術棧

| 層 | 技術 |
|---|---|
| 後端 | ASP.NET Core 10（`net10.0`）、EF Core 10、SQL Server、SignalR、Razor（後台） |
| 前端 | Vue 3（`<script setup>`）、Vite、Pinia（`pinia-plugin-persistedstate`）、Vue Router、Bootstrap 5、Vuetify、SweetAlert2、Tiptap 3、axios |
| 外部服務 | Google OAuth、Gemini（`gemini-2.5-flash`）、綠界 ECPay、SMTP（Gmail） |
| 基礎設施 | Docker Compose（測試用 SQL Server 2022）、GitHub Actions（只做建置） |

---

## 2. 後端結構

```
incode.MVC/
├── Program.cs              DI 註冊、驗證、CORS、Middleware 管線、路由
├── GlobalUsings.cs         專案內命名空間的 global using
├── ApiControllers/<模組>/  前台 REST API（路由 api/[controller]）
├── Controllers/<模組>/     後台 MVC Controller；Views/ 為對應 Razor
├── Hubs/ChatHub.cs         SignalR
├── Services/<模組>/        商業邏輯（IXxxService + 實作）
├── Models/                 EF 實體（勿手改）；Partials/ 擴充；Metadata/ 驗證設定
├── Dtos/<模組>/            API 傳輸物件（ResultDTO、PagedResultDto 在根目錄）
├── ViewModels/<模組>/      後台頁面模型
├── Constants/              OrderStatus、PointType、RoleType、InteractionCategoryIds
├── Extensions/             ClaimsExtensions、ConfigurationExtensions、OriginCheckExtensions、
│                           RateLimitExtensions、ResultDTOExtensions
├── Validation/             自訂驗證屬性
├── Prompts/Prompts.md      AI 客服系統提示詞（執行時讀取）
└── wwwroot/                css/js/lib（後台）、img/（上傳圖片）、contents/（論壇內文 HTML）
```

模組名稱統一為 `Chat`、`Contents`、`Point`、`PointShop`、`Users`，命名空間 = `incode.MVC.<資料夾路徑>`。

### 2.1 請求處理管線（`Program.cs` 順序）

1. `UseExceptionHandler`（正式環境）/ `UseMigrationsEndPoint`（開發）、`UseHsts`
2. `UseHttpsRedirection`
3. `UseRouting`
4. `UseCors("VueApp")`：允許來源 = `App:FrontendUrl` + `App:CorsOrigins`，允許帶 Cookie；靜態檔也套用同一份設定
5. `UseApiOriginCheck`：CSRF 防護（§3.3）
6. `UseStaticFiles`
7. `UseAuthentication` → `UseAuthorization` → `UseRateLimiter`（放在 CORS 之後，429 回應也帶 CORS 標頭）
8. `MapStaticAssets`、`MapControllerRoute`（`{controller=Home}/{action=Index}/{id?}`）、`MapControllers`、`MapHub<ChatHub>("/chatHub")`

上傳大小：`FormOptions.MultipartBodyLengthLimit` 與 Kestrel `MaxRequestBodySize` 皆為 500 MB（整體請求上限），實際單檔大小由 `ImageFileValidator` 及各端點限制（§7.3）。

### 2.2 分層與依賴注入

```
ApiController / Controller  →  Service (介面)  →  incodedatabaseContext (EF Core)
```

- 服務在 `Program.cs` 以 `AddScoped` 註冊（`PasswordService`、`LoginAttemptLimiter` 為 Singleton；`GeminiService` 為 typed HttpClient）。
- 現況：約 39 個 Controller 中有 29 個直接使用 DbContext（例如 `ChatMessagesApiController`），分層並不徹底（見 `CLAUDE.md` 待處理 25）。新程式應把邏輯放進 Service。

### 2.3 API 回應格式

統一使用 `ResultDTO<T>` / `ResultDTO`（`Dtos/ResultDTO.cs`），由 `this.ToActionResult(result)`（`Extensions/ResultDTOExtensions.cs`）轉成 HTTP 回應：

- 成功一律 HTTP 200（`Code` 為 204 時也是 200，避免前端拿不到 body）
- 失敗以 `Code` 作為 HTTP 狀態碼（400 / 404 / 409 / 500）
- 前端以 `error.response.data.message` 顯示錯誤

錯誤處理原則：

- 例外不外洩：catch 後用 `ILogger` 記錄，回通用訊息。
- 要讓使用者看到的業務錯誤丟 `BusinessException`（`Services/Common/`），只有它的 `Message` 可以直接回傳。
- 擁有者檢查：讀寫會員資料時必須確認屬於 `User.GetId()`（`Extensions/ClaimsExtensions.cs`），找不到或非本人一律 404。

---

## 3. 驗證、授權與安全機制

### 3.1 兩套 Cookie 驗證

| 方案 | Cookie | 對象 | 效期 | SameSite |
|---|---|---|---|---|
| `MemberAuth` | `Incode.Member` | 前台會員 | 7 天（滑動） | None（需 HTTPS） |
| `AdminAuth` | `Incode.Admin` | 後台管理員 | 10 小時 | Strict |

預設方案為 `SmartScheme`（PolicyScheme）：請求帶有 `Incode.Admin` Cookie 就用 `AdminAuth`，否則用 `MemberAuth`。另有 Google OAuth（`AddGoogle`）用於會員登入。

- 前台 API / SignalR 未登入或無權限時回 401 / 403，**不轉址**（`OnRedirectToLogin` / `OnRedirectToAccessDenied` 依路徑判斷 `/api`、`/chatHub`）。
- 後台 Controller 一律加：`[Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]`。後台登入（`AccountController`）只允許 `RoleId == Admin`，Claim 角色固定為 `"Admin"`。
- 角色表：`role`（0 superadmin、1 admin、2 member、3 guest），對應 `Constants/RoleType.cs`；註冊預設 member。
- 取得登入者 ID：`User.GetId()`。

### 3.2 登入保護

| 機制 | 位置 | 規則 |
|---|---|---|
| IP 速率限制 | `Extensions/RateLimitExtensions.cs`，端點加 `[EnableRateLimiting(...)]` | 登入 / 註冊 `auth`：每分鐘 20 次；忘記 / 重設密碼 `password-reset`：每 15 分鐘 5 次；超過回 429 |
| 帳號失敗次數 | `Services/Users/LoginAttemptLimiter.cs`（記憶體計數） | 同帳號 15 分鐘內失敗 5 次，暫停 15 分鐘；重啟清除 |

部署在反向代理後面時需設定 ForwardedHeaders，否則所有人共用代理 IP 的額度。

### 3.3 CSRF（Origin 檢查）

`Extensions/OriginCheckExtensions.cs`：`/api` 的寫入請求（POST / PUT / DELETE…）與 `/chatHub` 只接受來自自己或 CORS 允許來源的 `Origin`；沒有 `Origin` 時看 `Referer`，兩者都沒有視為非瀏覽器請求放行。

- 新的前端網址要加到 `App:CorsOrigins`，否則寫入請求會被 403。
- 伺服器對伺服器的回呼（綠界 `ecpay/return`）列在 `ExemptPaths`。

### 3.4 其他安全慣例

- **圖片上傳**：先經 `ImageFileValidator.Validate()`（僅 jpg / png / gif / webp，副檔名與檔頭比對）；刪檔時檔名以資料庫為準並 `Path.GetFileName()`，不用表單傳入的路徑。
- **XSS**：Razor 不對會員 / AI 內容用 `@Html.Raw`，必要時先經 DOMPurify；前端 `v-html` 前一律過濾（論壇 `utils/sanitizeHtml.js`、聊天 `toSafeHtml`）。
- **金額 / 價格**：一律以資料庫為準由後端計算；前端可輸入的金額 / 數量在 DTO 加 `[Range]`。
- **機密設定**：API 金鑰放 user-secrets 或環境變數，`appsettings*.json` 保持空字串。

---

## 4. 設定

`appsettings.json` 主要區段：

| 設定 | 用途 |
|---|---|
| `ConnectionStrings:incodedatabase` | 資料庫連線（預設 LocalDB；Docker profile 以環境變數覆寫） |
| `App:FrontendUrl` | 前端網址：Google 登入導回、重設密碼信連結、綠界 `ClientBackURL`、AI 回覆連結；並自動加入 CORS |
| `App:CorsOrigins` | 額外 CORS 允許來源（其他 port、ngrok 等） |
| `Authentication:Google:ClientId / ClientSecret` | Google OAuth（Secret 放 user-secrets） |
| `EmailSettings:*` | 寄信（忘記密碼）；`EmailService` 讀根層級 |
| `GeminiApiKey` | AI 客服 |
| `TmdbApiKey`、`imgapikey` | 其他選用服務 |
| `ECPay:MerchantID / HashKey / HashIV / ReturnUrl` | 綠界；目前為公開測試帳號，正式金鑰不可進 repo |

取值統一用 `IConfiguration.GetFrontendUrl()`、`GetCorsOrigins()`（`Extensions/ConfigurationExtensions.cs`），不要寫死網址。正式環境用環境變數覆寫（`App__FrontendUrl`、`App__CorsOrigins__0`、`ConnectionStrings__incodedatabase`）。

---

## 5. 資料庫

SQL Server，32 張表（`incodedatabase`）。完整欄位見 [`dbdiagram.md`](dbdiagram.md)，以下為依模組分組的概覽。

| 模組 | 資料表 |
|---|---|
| 會員 | `user`、`role`、`follow`、`user_loginlog`、`user_status`、`user_image`、`user_inventory`（背包）、`notifications` / `user_notification` |
| 論壇 | `content`、`content_category`、`content_interaction`、`interaction_category`、`content_history`、`content_notification` |
| 點數 | `point_log`（帳本）、`point_type_definition`、`mission`、`mission_progress`、`entrust_mission`、`entrust_mission_point_reserve`、`point_recharge`、`point_tip` |
| 商城 | `product`、`productimage`、`orders`、`orderdetail`、`shipped_detail`、`shipaddress`、`pointchange` |
| 客服 | `chat_session`、`chat_message` |

重要設計：

- **`content` 單表承載所有內容**：以 `type` 區分（1 貼文、2 回文、3 留言、4 留言回覆），`parent_content_id` 自我關聯；`is_active`（1 顯示、2 本人刪除、3 官方移除）軟刪除。
- **內文存檔案**：發文內文以 HTML 檔存到 `wwwroot/contents/<檔名>`，資料庫的 `body` 存相對路徑 `/contents/xxx.html`（`ContentService`）；前端再以 fetch 讀取後經 DOMPurify 過濾顯示。程式以 `body.StartsWith("/contents/")` 判斷內文是檔案還是文字。
- **互動分類固定 ID**：`interaction_category` 1 like、2 hide、3 bookmark、4 report、5 best_answer、6 tip（`Constants/InteractionCategoryIds.cs`）。
- **點數帳本** `point_log` 只增不改，每筆記錄異動值、類型（`PointType` 1～10，見 §6.1）、來源與異動後餘額。
- **基本資料**（角色、點數類型、互動分類）程式依賴其固定 ID，不可更動；見 `docs/database/seed.sql`。

資料庫建立與重新產生腳本見 [`database/README.md`](database/README.md)。

---

## 6. 功能模組

### 6.1 點數系統（`Services/Point/`）

#### 點數異動：`IPointLogService`

所有點數增減都必須經 `ChangePointsAsync(userId, changeAmount, sourceType, sourceId, PointType, ...)`：

- **必須在交易內呼叫**（沒有交易會丟例外），只會把 `PointLog` 加入 DbContext，**不會 `SaveChanges`**，由呼叫端提交。
- 內部以 `sp_getapplock`（資源名 `point-user-{userId}`）鎖定該會員到交易結束，同一會員的點數異動排隊執行。
- 餘額計算會包含同一交易中尚未儲存的異動。
- 讀取「會決定能不能異動」的資料之前（餘額、任務是否已領獎、委託狀態…），先呼叫 `LockUsersAsync(userId, ...)`，同一會員的請求才不會讀到舊狀態；多位會員一次傳入，內部依 ID 排序避免死結。

`PointType`：1 平台任務獎勵、2 委託任務獎勵、3 點數儲值、4 商品退款、5 委託退款、6 委託預扣、7 商品消費、8 系統調整、9 打賞支出、10 獲得打賞。

#### 平台任務（`MissionService` / `MissionProgressService`）

- 任務由後台維護（`mission`），會員接取後產生 `mission_progress`（`ongoing` → `completed` → `rewarded`）。
- 進度**只由伺服器端觸發** `IMissionService.TriggerMissionAsync(userId, actionType)`。目前的觸發點：

  | actionType | 觸發位置 |
  |---|---|
  | `login` | `AuthService`（登入成功） |
  | `post` / `comment` | `ContentService`（發文 / 留言） |
  | `purchase` | `OrderService`（下單成功） |
  | `mission_complete` | `MissionProgressService`（完成其他任務） |
  | `like` | `ContentInteractionService`，改用 `SyncMissionCountAsync` |

- 可被反覆切換的動作（按讚）不能每次 +1：`SyncMissionCountAsync` 依資料庫實際資料（本週期內仍按讚中的不同內容，不含自己的）重算，進度只增不減。
- 領獎 `POST api/MissionProgressApi/claim-reward`：先鎖會員 → 檢查狀態 → 加點 → 標記 `rewarded`。

#### 委託任務（`EntrustMissionService` / `PointReserveService`）

會員發文時可附帶「委託」（懸賞點數），由其他會員在討論串回覆，委託人選出最佳解答後發放賞金。

```
建立  POST api/ContentApi/with-entrust-mission
      └ 交易內：建立文章 + entrust_mission + ReserveAsync 預扣點數（PointType 6）
回覆  其他會員在討論串回覆（委託任務沒有「接取」動作）
選解  POST api/ContentInteractionApi/best-answer
      └ 先鎖會員 → ConsumeAsync 把預扣轉給最佳解答者（PointType 2）
取消  POST api/EntrustMissionApi/{id}/cancel
      └ 條件：自己發起、徵求中、尚無其他會員回覆；ReleaseAsync 退回預扣（PointType 5），狀態改 cancelled
```

#### 儲值（綠界 ECPay）

```
Recharge.vue          POST api/PointRechargeApi/recharge        建立 point_recharge 訂單（金額 10～50,000，點數 = 金額 × 10）
   │                        ◀── 回傳綠界表單參數（EcpayService.CreatePayment，含 CheckMacValue）
   ├ 訂單編號存 sessionStorage，前端組表單 POST 到綠界
   ▼
綠界付款頁 ─ 付款結果 ─▶ POST api/PointRechargeApi/ecpay/return   (伺服器對伺服器，Origin 檢查例外)
                          ├ 驗證 CheckMacValue
                          ├ 成功：CompleteRechargeAsync — 同一交易內以條件更新 Status <> 'success' 搶下訂單，
                          │       影響 1 筆才加點（回呼重送不會重複加點）
                          └ 失敗：只更新尚未 success 的訂單為 failed；一律回 "1|OK"（加點失敗回 "0|Fail" 讓綠界重送）
   │
   ▼ 綠界導回 {FrontendUrl}/recharge-result
RechargeResult.vue    GET api/PointRechargeApi/order/{orderNo}   輪詢約 20 秒（只能查自己的訂單）
                      status：pending / success / failed
```

開發時 `ECPay:ReturnUrl` 需為綠界連得到的公開網址（ngrok）。

#### 打賞

`POST api/ContentApi/{id}/tip` → `PointLogService.TipAsync`：先依 ID 順序鎖定雙方，再於同一交易記錄支出（9）與收入（10）。

### 6.2 論壇（`Services/Contents/`）

| 功能 | 端點（`api/ContentApi`、`ContentInteractionApi`、`CategoryApi`） |
|---|---|
| 列表 / 單篇 / 子內容 | `GET /`、`GET /{id}`、`GET /{id}/comments`、`GET /{id}/children` |
| 發文 / 編輯 / 刪除 | `POST /`（需登入）、`PUT /{id}`、`DELETE /{id}` |
| 我的內容 | `GET /me/posts`、`/me/replies`、`/me/comments`、`/me/liked-contents`、`/me/bookmarked-contents` |
| 互動 | `POST ContentInteractionApi`（只接受按讚 / 隱藏 / 收藏 / 檢舉，其他分類回 400）、`GET .../content/{id}`、`.../user/{id}`、`.../stats/{id}` |
| 最佳解答 / 打賞 | `POST ContentInteractionApi/best-answer`、`POST ContentApi/{id}/tip` |
| 分類 | `GET CategoryApi`、`POST CategoryApi`（需登入，會員可新增） |

前端對應：`ForumView.vue` → `TopicListView.vue`（列表）、`ThreadPost.vue`（討論串）、`TiptapEditor.vue`（Tiptap 3 編輯器）、`forumStore.js`（狀態）。內容以 `HtmlIframe` 顯示，並經 `utils/sanitizeHtml.js` 過濾。

### 6.3 點數商城（`Services/PointShop/`）

- `ProductApiController`：商品列表、搜尋（公開）。
- `OrderApiController`（整支需登入）：訂單列表 / 明細、寄送資料、建單（`OrderPost`）、修改（`OrderPut`）、取消（`CancelOrder`）。會員只能操作自己的「待處理」訂單。
- 前台與後台共用 `OrderService`（`CreateOrderAsync` / `UpdateOrderItemsAsync` / `CancelOrderAsync`）：
  - 整筆在交易內（`RunInTransactionAsync`），業務失敗丟 `OrderFailure` → rollback + 回傳訊息
  - 開始先 `LockUsersAsync(userId)`；非 SQL Server（SQLite 測試）時略過
  - 庫存以 `ExecuteUpdateAsync` 原子更新（`WHERE Stock >= 數量`），商品讀取用 `AsNoTracking`，不要改 tracked 實體的 `Stock`
  - 明細同商品合併、數量必須 > 0、新加入的商品必須上架中；修改訂單補扣 / 退點、取消退點退庫存
  - 狀態使用 `Constants/OrderStatus.cs`（待處理、已出貨、已完成、已取消、其他）；已取消不可改回
- 前端：`views/pointshop/`（商品 / 訂單 / 結帳）、`stores/pointshop/cartStore.js`（購物車，persist）、`utils/pointShopSwal.js`、`utils/pointShopUtils.js`。

### 6.4 客服聊天（`Services/Chat/`、`Hubs/ChatHub.cs`）

```
ChatWidget.vue ──REST──▶ ChatMessagesApi/SendMessage
        ▲                      │ 存 chat_message → 推播 ReceiveMessage 給對話群組
        │ SignalR              ├ 真人模式 (Ishumanmode)：通知 "SupportAgents" 群組 (ReceiveSupportRequest)
        └──── /chatHub ◀───────┘ AI 模式：GeminiService → 推播 AI 回覆
後台 ChatMessages/Index.cshtml ◀── 管理員加入 SupportAgents，AdminReply 回覆
```

- **訊息一律走 REST 存檔後由 `IHubContext` 推播**，`ChatHub` 只負責加入 / 離開群組：`JoinSupportGroup`（僅管理員）、`JoinSessionGroup(sessionId)`（只能加入自己的對話，管理員可加入任何對話）。需登入，未登入回 401。
- 群組名稱：每個對話為 `ChatSessionId`；客服群組為 `SupportAgents`。
- **AI 客服**（`GeminiService`，模型 `gemini-2.5-flash`）以兩段式呼叫實作工具判斷：
  1. 以 `Prompts/Prompts.md`（系統提示詞）加上使用者問題請 AI 判斷意圖；回覆若含 `CALL_FUNCTION:GetProductInfo|關鍵字`、`SearchContents|關鍵字`、`PointLogs|關鍵字`，後端查詢資料庫。
  2. 把查詢結果 JSON 與格式規則（Markdown 圖片、連結）再交給 AI 產生最終回覆。
  - 使用者訊息含「生圖」「產生圖片」時走圖片生成；上傳圖片走圖片分析。
  - AI 失敗時固定回覆「AI 客服暫時無法回覆…」，錯誤細節只寫 log。
- 聊天圖片：`POST ChatMessages/UploadImage`，經 `ImageFileValidator`（單檔 10 MB）、Guid 檔名。
- 對話管理：`ChatSessionsApi`（依使用者列出、建立、`AutoTitle` 自動命名、刪除），皆檢查對話擁有者。
- `Prompts/Prompts.md` 於執行時讀取，路徑不可改。

### 6.5 會員（`Services/Users/`）

| 功能 | 端點 |
|---|---|
| 註冊 / 登入 / 登出 / 狀態 | `AuthApi`：`register`、`login`、`logout`、`check` |
| Google 登入 | `AuthApi/login-google` → `google-response`（完成後導回 `App:FrontendUrl`） |
| 忘記 / 重設密碼 | `AuthApi/forgot-password`、`reset-password`（寄信，速率限制） |
| 個人資料 | `UserApi`：`GET/PUT profile`、`{userId}/stats`（公開）、`images`（註冊流程上傳大頭貼，公開） |
| 管理 | `UserApi` 列出全部會員 / 刪除會員（僅管理員） |
| 追蹤 | `Follow`：`followers`、`following`、`POST/DELETE {targetId}` |
| 通知 | `Notification`：列表、未讀數、已讀、全部已讀 |
| 背包 / 狀態 | `Inventory`（列表、使用、刪除）、`Status`（經驗 / 等級 / 點數） |

密碼以 BCrypt 雜湊（`PasswordService`），無法用 SQL 直接建立帳號。

### 6.6 後台管理（`Controllers/` + `Views/`）

Razor MVC，只有管理員可進入。主要頁面：會員（`Member`）、論壇內容 / 分類 / 歷史（`Content*`）、任務與進度（`Mission*`）、委託（`EntrustMission`）、點數紀錄 / 儲值（`PointLog`、`PointRecharge`）、商品 / 訂單 / 出貨（`Product`、`Order`、`Shipped`）、客服（`ChatSessions`、`ChatMessages`）。

慣例：訊息用 `TempData["SuccessMessage"]` / `["ErrorMessage"]` 並在 View 放 `<partial name="_TempDataAlert" />`；POST 表單要有 `method="post"` 且 Action 加 `[ValidateAntiForgeryToken]`。

---

## 7. 前端（`incode.Vue/`）

### 7.1 結構

```
src/
├── main.js / App.vue         進入點；App 掛載時呼叫 authStore.checkAuth()
├── router/index.js           路由（除首頁外全部 () => import() 延遲載入；requiresAuth 守衛）
├── api/                      request.js (apiClient)、pointClient.js、authInterceptor.js、各模組 API
├── stores/                   Pinia：chatStore、forumStore、point/*、pointshop/cartStore、users/auth|user
├── views/                    路由頁面（依模組分資料夾）
├── components/               layout / common / chat / contents / point / pointshop / users
├── composables/ utils/       共用邏輯與工具
├── plugins/vuetifyIcons.js   Vuetify 使用的 SVG 圖示（新圖示需在此登記）
└── assets/                   css / scss（Material Kit）/ img / fonts
```

### 7.2 主要路由

| 路徑 | 頁面 | 備註 |
|---|---|---|
| `/` | `HomeView` | |
| `/forum`、`/forum/:id` | 論壇列表 / 討論串 | |
| `/auth`（`/login`、`/register` 轉址）、`/reset-password` | 登入註冊 / 重設密碼 | |
| `/usersettings`、`/userindex/...` | 個人設定、我的文章 / 收藏 / 按讚 / 任務 | 需登入 |
| `/profile/:username` | 公開個人頁 | |
| `/task`、`/mytask`、`/point-log` | 平台任務、我的任務、點數明細 | |
| `/recharge`、`/recharge-result` | 儲值、儲值結果 | |
| `/PointShop/Product(/:id)`、`/PointShop/Order`、`/PointShop/Checkout` | 商城 | |

### 7.3 與後端的連線

- 後端網址一律讀環境變數：`VITE_API_BASE_URL`（REST，如 `https://localhost:7190/api`）與 `VITE_BASE_URL`（靜態檔 `/img`、`/contents`、SignalR `/chatHub`、點數模組路徑自帶 `/api`）。正式環境用 `.env.production` 覆寫。
- 三個 axios 實例（全域 `axios`、`apiClient`、`pointClient`）都必須 `withCredentials: true`，並安裝 `authInterceptor`：已登入狀態收到 401 → 清除登入狀態、提示並導向登入頁（`?redirect=` 回原頁）；未登入時 `apiClient` / `pointClient` 只把錯誤交給呼叫端（論壇訪客的背景查詢會 401，不能把訪客踢走）。新增 axios 實例時要自行 `installAuthInterceptor()`。
- 登入狀態：`stores/users/auth.js`；元件中用 `computed(() => authStore.isAuthenticated)`，不要存成常數（會失去響應式）。

### 7.4 前端注意事項

- Vuetify 元件由 `vite-plugin-vuetify` 自動匯入；圖示為 SVG，沒有 MDI 字型 CSS。
- Tiptap 相關 import 一律用 `@tiptap/pm/*`，StarterKit 3 已內含 Underline / Link。
- `v-html` 前一律過濾；插入 SweetAlert `html` 的使用者輸入先 `escapeHtml()`。
- 匯入路徑大小寫須與檔名一致（Linux 建置會失敗）。
- 環境、啟動與建置見 README；`npm run lint` 目前因缺少 `eslint.config.js` 無法執行。

---

## 8. 建置、CI 與部署

- **CI**：`.github/workflows/ci.yml`，PR 與推送到 `main` 時在 Linux 執行後端 `dotnet build`（Release）與前端 `npm ci` + `npm run build`。沒有測試步驟。
- **Docker 測試資料庫**：`docker-compose.yml` 啟動 SQL Server 2022 並由 `db-init` 執行 `docs/database/` 腳本；後端用 launch profile `https (Docker DB)`。僅供開發測試。
- **部署需設定**：
  - 後端：`App__FrontendUrl`、`App__CorsOrigins__*`、`ConnectionStrings__incodedatabase`、Google / Email / Gemini / ECPay 金鑰（環境變數或機密儲存）
  - 前端：`.env.production` 的 `VITE_API_BASE_URL`、`VITE_BASE_URL`
  - 反向代理：設定 ForwardedHeaders（否則 IP 速率限制失效）、保持 HTTPS（會員 Cookie 為 `Secure` + `SameSite=None`）
  - 上傳目錄 `App__UploadRoot`（預設 `App_Data/uploads`）設成部署資料夾以外的絕對路徑，重新部署時不可被覆蓋；
    第一次部署前執行 `scripts/copy-legacy-uploads.sh <UploadRoot>` 把改版前的上傳檔從 git 歷史還原進去（見 `CLAUDE.md` 修正紀錄 29）

---

## 9. 擴充指引

**新增一個前台 API 功能**

1. `Dtos/<模組>/` 建 DTO（金額 / 數量加 `[Range]`）
2. `Services/<模組>/` 建 `IXxxService` + 實作，並在 `Program.cs` 註冊
3. `ApiControllers/<模組>/` 建 Controller：`[Authorize]`、以 `User.GetId()` 做擁有者檢查、回傳 `this.ToActionResult(...)`、例外只記 log
4. 前端 `api/` 加呼叫、必要時 `stores/` 與 `views/`，路由用延遲載入
5. 若有新資料夾，補 `GlobalUsings.cs`；若有資料表變動，更新 `docs/database/schema.sql`

**新增會異動點數的功能**：在交易內，先 `LockUsersAsync` → 讀狀態 → `ChangePointsAsync` → 提交；並用併發請求驗證只成立一次（見 `TESTING.md`）。

**新增任務類型**：在伺服器端適當位置呼叫 `TriggerMissionAsync(userId, "<actionType>")`（可反覆切換的動作改用 `SyncMissionCountAsync`），並在後台建立對應 `mission`。

**新增前端網址 / 網域**：加入後端 `App:FrontendUrl` 或 `App:CorsOrigins`；若為伺服器回呼路徑則加入 `OriginCheckExtensions.ExemptPaths`。

---

## 10. 已知限制

完整清單見 [`CLAUDE.md`](../CLAUDE.md)「全專案檢查結果」，重點：

- 沒有自動化測試（只有 CI 建置）。
- 上傳檔案在 git 與 `wwwroot`，部署可能覆蓋；無雲端儲存。
- 後端分層不徹底，多數 Controller 直接操作 DbContext，權限檢查分散。
- 前端同時使用 Bootstrap、Vuetify、Material Kit；`TopicListView.vue`、`forumStore.js` 超過 2700 行。
- 儲值方案顯示的贈點與實際給點（金額 × 10）不一致；建立委託未檢查關聯文章是否為自己的。
- 後台把委託任務改成 cancelled 不會退還預扣點數（取消請走前台 API）。
