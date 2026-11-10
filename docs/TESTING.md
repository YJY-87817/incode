# 建置與測試

後端有 xUnit 測試專案 `incode.MVC.Tests/`（SQLite 記憶體資料庫，`dotnet test incode.sln`），前端有 Vitest（`npm test`）。
尚未涵蓋：`OrderService`、`MissionService`、`EntrustMissionService`、API 權限（`WebApplicationFactory`）與 `sp_getapplock` 併發。目前的品質把關是：

1. CI 建置與測試（每個 PR 自動執行）
2. 前端 lint
3. 以 Docker 資料庫實際啟動後端，手動 / 用 API 做端對端驗證

開發環境與啟動方式見根目錄 [`README.md`](../README.md)，這份文件只談「改完怎麼確認沒壞」。

## 1. 建置檢查（PR 合併前必過）

CI 設定：[`.github/workflows/ci.yml`](../.github/workflows/ci.yml)，在每個 PR 與推送到 `main` 時於 Linux 執行兩個 job，兩個都通過才能合併。
在本機先跑一次可以提早發現問題（Linux 的檔名大小寫錯誤，Windows / macOS 本機不會發現）：

後端（Release 建置，與 CI 相同）：

```bash
dotnet restore incode.sln
```

```bash
dotnet build incode.sln --no-restore --configuration Release
```

前端（依 `package-lock.json` 安裝，再建置）：

```bash
cd incode.Vue
```

```bash
npm ci --no-audit --no-fund
```

```bash
npm run build
```

沒有 .NET 10 SDK 時可用 Docker 映像建置（需 `--network host` 才能走主機的 proxy 並連到 Docker 資料庫）：

```bash
docker run --rm --network host -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet build incode.sln -c Release
```

## 2. 前端 lint

```bash
cd incode.Vue
```

```bash
npm run lint
```

`lint` 帶有 `--fix`，會直接修改檔案，執行後請檢查 `git diff`，避免夾帶無關的格式變更。

## 3. 端對端驗證（建議每次改到 API / 資料流程時做）

### 準備

1. 啟動測試資料庫（第一次會自動建表與寫入基本資料）：

   ```bash
   docker compose up -d
   ```

   確認初始化完成：

   ```bash
   docker compose logs db-init
   ```

   最後應看到 `[db-init] 完成`。要回到全新資料庫：`docker compose down -v` 再 `docker compose up -d`。

2. 以 Docker 資料庫的 profile 啟動後端：

   ```bash
   dotnet run --project incode.MVC --launch-profile "https (Docker DB)"
   ```

3. 需要前台畫面時另外啟動前端：`cd incode.Vue && npm run dev`，開啟 <https://localhost:5173>。

4. 需要後台時，先從前台註冊帳號，再設為管理員（`role_id = 1`），步驟見 [`database/README.md`](database/README.md)。

### 用 API 驗證（不需要前端）

後端登入使用 Cookie，用 `curl -c / -b` 保存 Cookie 即可。注意 `/api` 的寫入請求（POST / PUT / DELETE）會檢查 `Origin`，
**沒有 Origin 與 Referer 視為非瀏覽器請求放行**，所以 curl 不帶這兩個標頭即可；若要模擬其他網站的跨站請求，
帶上 `-H "Origin: https://evil.example"`，應得到 403。

範例（`-k` 因為是本機自簽憑證）：

```bash
curl -k -c cookies.txt -H "Content-Type: application/json" \
  -d '{"email":"me@example.com","password":"..."}' \
  https://localhost:7190/api/AuthApi/login
```

```bash
curl -k -b cookies.txt https://localhost:7190/api/UserApi/profile
```

端點的實際路徑與欄位請看對應的 `ApiControllers/<模組>/*.cs` 與 `Dtos/`。

### 修改後建議確認的情境

依改到的模組挑選，重點是**權限**與**點數 / 庫存的一致性**（這兩類是歷次檢查找到最多問題的地方，見 [`CLAUDE.md`](../CLAUDE.md) 修正紀錄）：

| 模組 | 建議驗證 |
|---|---|
| 權限（所有前台 API） | 未登入回 401；存取別人的資料回 404（不是 403 / 200）；會員呼叫後台 / 管理員功能被拒 |
| 點數異動（打賞、任務領獎、儲值、最佳解答、商城下單） | 餘額與 `point_log` 帳本連續；**同時送出多個相同請求**（例如 10 個）只成立一次，餘額不為負 |
| 點數商城訂單 | 建單扣點扣庫存；修改訂單補扣 / 退點；取消退點退庫存；已取消的訂單不可改回；庫存不足整筆 rollback |
| 委託任務 | 建立時預扣點數；取消退回；已有其他會員回覆後不可取消 |
| 儲值 | 金額限 10～50,000；付款結果查詢只能查自己的訂單；綠界回呼重複送達只加一次點 |
| 登入 | 同帳號連續失敗 5 次暫停 15 分鐘（429）；超過 IP 次數限制回 429 |
| 圖片上傳 | 非 jpg/png/gif/webp、副檔名與檔頭不符、超過大小的檔案被拒絕 |
| 聊天 / 內容顯示 | 訊息含 `<script>`、`<img onerror=...>` 時，前台聊天視窗與後台客服頁面都不會執行 |
| 後台 | 表單 POST 有 AntiForgery；操作後有成功 / 錯誤訊息 |

併發測試範例（10 個同時請求，驗證只成功一次）：

```bash
for i in $(seq 1 10); do
  curl -k -s -o /dev/null -w "%{http_code}\n" -b cookies.txt -X POST \
    https://localhost:7190/api/<要測的端點> &
done; wait
```

### 瀏覽器冒煙測試（前端）

環境內建 Chromium 與 Playwright 時，可以用 Playwright 開啟頁面、攔截 API 回應來檢查畫面（曾用此方式驗證 Tiptap 升級前後
13 個步驟的輸出一致）。重點是頁面能載入、沒有 console error、主要流程（登入 → 瀏覽 → 操作）能走完。
前端相關細節見 [`incode.Vue/CLAUDE.md`](../incode.Vue/CLAUDE.md)。

## 4. 合併前檢查清單

- [ ] 後端 `dotnet build -c Release` 無錯誤
- [ ] 前端 `npm ci` 後 `npm run build` 成功
- [ ] 改到 API 時，用 Docker 資料庫實際啟動並驗證過（至少測過正常流程與「未登入 / 非本人」兩種情況）
- [ ] 改到點數 / 庫存時，測過併發與失敗 rollback
- [ ] 改到資料表時，更新 `docs/database/schema.sql`（做法見 [`database/README.md`](database/README.md)）
- [ ] 沒有提交機密設定或使用者上傳的檔案（`wwwroot/img/`）
- [ ] 前後端同一個功能在同一個 PR
- [ ] GitHub Actions 兩個 job 都通過

## 日後建立自動化測試的建議

目前沒有測試專案，若要開始建立，建議優先順序（依過去出問題的地方）：

1. （`PointLogService`、`PointRechargeService` 已完成）`OrderService`、`MissionService`、`EntrustMissionService` 的整合測試——以 SQLite 記憶體資料庫即可（`OrderService` 已為此略過 SQL Server 專用的 `sp_getapplock`），但併發鎖定行為仍須用真實 SQL Server 驗證。
2. API 權限測試（`WebApplicationFactory`）：未登入 401、非本人 404、非管理員被拒。
3. 前端 Vitest：`utils/sanitizeHtml.js`、`toSafeHtml` 等安全相關的純函式。
4. 建好後把 `dotnet test` / `npm test` 加入 `.github/workflows/ci.yml`。
