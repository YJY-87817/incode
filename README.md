# incode 影庫

以電影為主題的社群平台：會員可以在論壇發文討論、透過任務與儲值取得點數、在點數商城兌換商品，並提供 AI / 真人客服聊天。
管理員另有後台可管理會員、內容、任務、點數與訂單。

本 repo 同時包含前端與後端：

| 資料夾 | 內容 | 技術 |
|---|---|---|
| [`incode.MVC/`](incode.MVC/) | 後端：前台 REST API + 後台管理網站 | ASP.NET Core 10、EF Core 10、SQL Server、SignalR |
| [`incode.Vue/`](incode.Vue/) | 前端：會員使用的網站 | Vue 3、Vite、Pinia、Vue Router、Bootstrap 5、Vuetify |
| [`docs/`](docs/) | 資料表設計、API 說明文件 | |

## 功能

- **論壇**：分類瀏覽、發文 / 回覆 / 留言、按讚、收藏、打賞點數、委託任務（懸賞點數、選擇最佳解答）
- **點數**：平台任務（登入、按讚、購買…）領取獎勵、綠界 ECPay 儲值、點數明細
- **點數商城**：商品瀏覽與搜尋、購物車、下單扣點、訂單查詢 / 取消、寄送資料管理
- **會員**：註冊 / 登入（含 Google 登入）、忘記密碼、個人頁、追蹤、通知
- **客服**：Gemini AI 客服，可切換為真人客服（SignalR 即時訊息）
- **後台**：會員、論壇內容與分類、任務、點數紀錄、儲值、商品、訂單與出貨管理

## 專案結構

```
incode/
├── README.md
├── CLAUDE.md               開發慣例、架構說明、修正紀錄與待處理問題清單
├── incode.sln
├── docs/                   dbdiagram（資料表設計）、Content API 導覽
├── incode.MVC/             後端
│   ├── ApiControllers/     前台 API（/api/...）
│   ├── Controllers/        後台 MVC        ── Views/  後台頁面
│   ├── Services/           商業邏輯
│   ├── Models/             EF Core 實體（Database-First）
│   ├── Dtos/  ViewModels/  Constants/  Extensions/  Validation/  Hubs/
│   └── wwwroot/            後台靜態檔、上傳圖片、論壇內文
└── incode.Vue/             前端
    └── src/
        ├── views/          頁面
        ├── components/     元件
        ├── api/            後端 API 呼叫
        ├── stores/         Pinia
        ├── router/  composables/  utils/  assets/
```

模組名稱前後端一致：`Chat`（客服）、`Contents`（論壇）、`Point`（點數 / 任務 / 儲值）、`PointShop`（點數商城）、`Users`（會員）。

## 開發環境

| 項目 | 版本 |
|---|---|
| .NET SDK | 10.0（Visual Studio 需 2026 版；VS 2022 不支援 .NET 10） |
| SQL Server | Docker（建議，見下方）、SQL Server 或 LocalDB（Windows） |
| Node.js | 20.19 以上（Vite 7 需求） |

### 後端

1. **資料庫**（二擇一）：
   - **Docker（建議）**：在 repo 根目錄啟動，第一次會自動建立資料庫並執行建表與基本資料腳本：

     ```bash
     docker compose up -d
     ```

     後端用 `https (Docker DB)` 這個 launch profile 啟動即可連到這個資料庫（Visual Studio 選這個 profile，或下方指令）。
     要回到全新的資料庫時執行 `docker compose down -v` 再 `docker compose up -d`。
   - **LocalDB / 自己的 SQL Server**：預設連線字串在 `incode.MVC/appsettings.json` 的 `ConnectionStrings:incodedatabase`；
     建立空資料庫後執行 [`docs/database/schema.sql`](docs/database/schema.sql) 與 [`seed.sql`](docs/database/seed.sql)。

   建立資料庫後，從前台註冊一個帳號並設為管理員才能登入後台，步驟見 [`docs/database/README.md`](docs/database/README.md)。

2. **機密設定**：API 金鑰等不要寫進 `appsettings.json`，改用 user-secrets：

   ```bash
   cd incode.MVC
   ```

   ```bash
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<Google OAuth Client Secret>"
   ```

   ```bash
   dotnet user-secrets set "EmailSettings:Password" "<寄信用 Gmail 應用程式密碼>"
   ```

   ```bash
   dotnet user-secrets set "GeminiApiKey" "<Gemini API Key>"
   ```

   其他可選設定：`TmdbApiKey`、`imgapikey`、`ECPay:*`（`appsettings.json` 目前為綠界公開的測試帳號）。
   未設定 `Authentication:Google:ClientSecret` 時，需要驗證的頁面會無法開啟。

3. **啟動**：

   ```bash
   dotnet run --project incode.MVC --launch-profile https
   ```

   使用 Docker 資料庫時：

   ```bash
   dotnet run --project incode.MVC --launch-profile "https (Docker DB)"
   ```

   - 後台：<https://localhost:7190>（管理員帳號登入）
   - 前台 API：`https://localhost:7190/api/...`
   - SignalR：`https://localhost:7190/chatHub`
   - 不指定 `--launch-profile` 時會用第一個 profile（只有 `http://localhost:5187`），前端預設連不到

### 前端

1. 安裝套件：

   ```bash
   cd incode.Vue
   ```

   ```bash
   npm install
   ```

2. **HTTPS 憑證**：開發伺服器使用 HTTPS，需要在 `incode.Vue/` 放 `key.pem` 與 `cert.pem`（已加入 `.gitignore`；沒有憑證時會改用 HTTP，但後端 Cookie 需要 HTTPS 才能登入）。
   可用 [mkcert](https://github.com/FiloSottile/mkcert) 產生受信任的本機憑證：

   ```bash
   mkcert -key-file key.pem -cert-file cert.pem localhost 127.0.0.1
   ```

3. **後端網址**：設定在 `incode.Vue/.env`（`VITE_API_BASE_URL`、`VITE_BASE_URL`），預設指向 `https://localhost:7190`。
   程式內不寫死網址，一律讀這兩個變數。

4. **啟動**：

   ```bash
   npm run dev
   ```

   開啟 <https://localhost:5173>。正式環境建置：

   ```bash
   npm run build
   ```

## 部署設定

前後端的網址都由設定提供，部署時只需覆寫設定，不用改程式：

| 位置 | 設定 | 用途 |
|---|---|---|
| 後端 `appsettings.json` → `App:FrontendUrl` | 前端網址 | Google 登入導回、重設密碼信件連結、綠界付款完成導回、AI 回覆中的連結；並自動加入 CORS 允許來源 |
| 後端 `App:CorsOrigins` | 額外的 CORS 允許來源 | 其他開發 port、ngrok 等 |
| 後端 `ECPay:ReturnUrl` | 綠界付款結果通知網址 | 需為綠界伺服器連得到的公開網址（開發時用 ngrok） |
| 前端 `.env.production` | `VITE_API_BASE_URL`、`VITE_BASE_URL` | 後端 API 與網站網址（`npm run build` 時使用） |

後端在正式環境以環境變數覆寫，例如：

```bash
export App__FrontendUrl=https://www.example.com
```

```bash
export App__CorsOrigins__0=https://admin.example.com
```

```bash
export ConnectionStrings__incodedatabase="Server=...;Database=incodedatabase;..."
```

## 開發說明

- 架構、程式慣例（權限檢查、API 回應格式、點數異動、圖片上傳…）與待處理問題清單請見 [`CLAUDE.md`](CLAUDE.md)，
  前端的細節見 [`incode.Vue/CLAUDE.md`](incode.Vue/CLAUDE.md)。
- 同一個功能牽涉前後端時，請在同一個分支 / PR 一起修改。
- 建置、lint、端對端驗證與合併前檢查清單見 [`docs/TESTING.md`](docs/TESTING.md)（專案目前沒有自動化測試）。
- 每個 PR 會由 GitHub Actions（`.github/workflows/ci.yml`）自動建置前後端，兩個檢查都通過再合併。
- 前端合併進本 repo 前的 commit 歷史仍保留：`git log edfd961^2`。

## 授權

前端使用 [Vue Material Kit](https://www.creative-tim.com/product/vue-material-kit)（Creative Tim, MIT License）作為版型，
授權聲明見 [`incode.Vue/LICENSE`](incode.Vue/LICENSE)。
