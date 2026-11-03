# incode 影庫

以電影為主題的社群平台：會員可以在論壇發文討論、透過任務與儲值取得點數、在點數商城兌換商品，並提供 AI / 真人客服聊天。
管理員另有後台可管理會員、內容、任務、點數與訂單。

本 repo 同時包含前端與後端：

| 資料夾 | 內容 | 技術 |
|---|---|---|
| [`incode2/`](incode2/) | 後端：前台 REST API + 後台管理網站 | ASP.NET Core 9、EF Core 9、SQL Server、SignalR |
| [`incode_Vue/`](incode_Vue/) | 前端：會員使用的網站 | Vue 3、Vite、Pinia、Vue Router、Bootstrap 5、Vuetify |
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
├── incode2.sln
├── docs/                   dbdiagram（資料表設計）、Content API 導覽
├── incode2/                後端
│   ├── ApiControllers/     前台 API（/api/...）
│   ├── Controllers/        後台 MVC        ── Views/  後台頁面
│   ├── Services/           商業邏輯
│   ├── Models/             EF Core 實體（Database-First）
│   ├── Dtos/  ViewModels/  Constants/  Extensions/  Validation/  Hubs/
│   └── wwwroot/            後台靜態檔、上傳圖片、論壇內文
└── incode_Vue/             前端
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
| .NET SDK | 9.0 |
| SQL Server | SQL Server 或 LocalDB（Windows） |
| Node.js | 20.19 以上（Vite 7 需求） |

### 後端

1. **資料庫**：預設連線字串在 `incode2/appsettings.json` 的 `ConnectionStrings:incodedatabase`（LocalDB 的 `incodedatabase`）。
   專案為 Database-First，repo 內沒有 migration，資料表結構請參考 [`docs/dbdiagram.md`](docs/dbdiagram.md)。

2. **機密設定**：API 金鑰等不要寫進 `appsettings.json`，改用 user-secrets：

   ```bash
   cd incode2
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
   dotnet run --project incode2 --launch-profile https
   ```

   - 後台：<https://localhost:7190>（管理員帳號登入）
   - 前台 API：`https://localhost:7190/api/...`
   - SignalR：`https://localhost:7190/chatHub`
   - 不指定 `--launch-profile` 時會用第一個 profile（只有 `http://localhost:5187`），前端預設連不到

### 前端

1. 安裝套件：

   ```bash
   cd incode_Vue
   ```

   ```bash
   npm install
   ```

2. **HTTPS 憑證**：開發伺服器使用 HTTPS，需要在 `incode_Vue/` 放 `key.pem` 與 `cert.pem`（已加入 `.gitignore`）。
   可用 [mkcert](https://github.com/FiloSottile/mkcert) 產生受信任的本機憑證：

   ```bash
   mkcert -key-file key.pem -cert-file cert.pem localhost 127.0.0.1
   ```

3. **後端網址**：設定在 `incode_Vue/.env`（`VITE_API_BASE_URL`、`VITE_BASE_URL`），預設指向 `https://localhost:7190`。

4. **啟動**：

   ```bash
   npm run dev
   ```

   開啟 <https://localhost:5173>。正式環境建置：

   ```bash
   npm run build
   ```

## 開發說明

- 架構、程式慣例（權限檢查、API 回應格式、點數異動、圖片上傳…）與待處理問題清單請見 [`CLAUDE.md`](CLAUDE.md)，
  前端的細節見 [`incode_Vue/CLAUDE.md`](incode_Vue/CLAUDE.md)。
- 同一個功能牽涉前後端時，請在同一個分支 / PR 一起修改。
- 前端合併進本 repo 前的 commit 歷史仍保留：`git log edfd961^2`。

## 授權

前端使用 [Vue Material Kit](https://www.creative-tim.com/product/vue-material-kit)（Creative Tim, MIT License）作為版型，
授權聲明見 [`incode_Vue/LICENSE`](incode_Vue/LICENSE)。
