# CLAUDE.md

本檔案提供給 Claude Code（與接手的開發者）在此專案工作時的背景、慣例與目前進度。
專案目前由個人維護。後端在另一個 repo `YJY-87817/incode`（ASP.NET Core 9），其 CLAUDE.md 有完整架構與全專案檢查清單。

## 專案概觀

- Vue 3（`<script setup>`）+ Vite + Pinia（`pinia-plugin-persistedstate`）+ Vue Router
- UI：Bootstrap 5（含 `bootstrap-vue-next`）、Vuetify、SweetAlert2、Material Kit 套版（`src/examples/`）
- 後端 API：`VITE_API_BASE_URL`（開發為 `https://localhost:7190/api`），圖片等靜態檔：`VITE_BASE_URL`
- 登入：後端 Cookie 驗證，axios 必須帶 `withCredentials: true`
  - 全域 `axios`：`main.js` 設定 `axios.defaults.withCredentials` / `baseURL`，並有 401 攔截器（清除登入狀態、導向登入頁）
  - `src/api/request.js` 的 `apiClient`：`axios.create` 建立時就複製預設值，所以自己設定了 `withCredentials: true`
- 登入狀態：`src/stores/user/auth.js`（`App.vue` 掛載時呼叫 `checkAuth()`）。
  元件中請用 `computed(() => authStore.isAuthenticated)`，**不要**存成常數（會失去響應式）

## 目錄

| 路徑 | 內容 |
|---|---|
| `src/views/` | 各模組頁面（`PointShop/`、`point/`、論壇等） |
| `src/components/` | 共用元件（`PointShop/Cart.vue` 為全站購物車側欄，掛在 `App.vue`） |
| `src/api/` | 各模組 API 呼叫 |
| `src/stores/` | Pinia store |
| `src/router/index.js` | 路由 |

## 點數商城（`src/views/PointShop/`）

```
PointShop/
├── ProductView.vue / OrderView.vue / CheckoutView.vue   頁面（匯入 styles/pointshop.css）
├── components/                                          元件
├── services/
│   ├── PointShopApi.js       API 呼叫
│   ├── cartStore.js          購物車 (persist)；addToCart / updateQuantity 會限制不超過庫存；syncWithProducts 以最新商品資料同步
│   ├── pointShopSwal.js      SweetAlert 共用設定與 alertSuccess / alertError / alertWarning / confirmDialog
│   ├── pointShopUtils.js     productImageUrl、ORDER_STATUS、validateShipping、日期格式、ensureOk / errorText / isBusinessError
│   ├── useAddToCart.js       加入購物車 + 提示
│   └── useDesktopExpand.js   篩選面板桌面版預設展開
└── styles/pointshop.css      .btn-custom-primary 與篩選面板共用樣式
```

### 慣例

- **錯誤處理**：後端業務失敗回 4xx + `{ ok: false, message }`（舊 API 可能是 200 + `ok: false`）。
  成功路徑用 `ensureOk(res)`，失敗用 `errorText(error)` 顯示訊息、`isBusinessError(error)` 判斷是否要重新整理資料。
- 提示框一律用 `pointShopSwal.js`，不要每個元件自己寫 `didOpen` 改按鈕顏色。
- 插入 SweetAlert `html` 的使用者輸入要先 `escapeHtml()`。
- 訂單狀態用 `ORDER_STATUS`，會員只能修改 / 取消「待處理」訂單（後端也會檢查）。
- 訂單明細在展開訂單時才載入（`OrderItem.vue`），列表重新整理時不要清空列表（保留展開狀態）。
- 搜尋用 debounce + 請求序號，只採用最後一次回應。

## 建置與驗證

- `npm install`（repo 沒有 lockfile）、`npm run dev`、`npm run build`
- `vite.config.js` 的 dev server 需要根目錄的 `key.pem` / `cert.pem`（已 gitignore）；
  只要 build 時也會讀取，雲端環境可臨時產生自簽憑證：
  `openssl req -x509 -newkey rsa:2048 -nodes -keyout key.pem -out cert.pem -days 1 -subj /CN=localhost`，用完刪除
- ESLint 9 沒有 `eslint.config.js`，`npm run lint` 目前無法執行
- 沒有自動化測試。曾以 Playwright（環境內建 Chromium）+ 攔截 API 回應的方式做瀏覽器冒煙測試

## 已知問題

- **Linux 上 build 失敗**：`src/views/point/Task.vue` 從 `@/components/Task/` 匯入，實際資料夾是 `src/components/task/`（大小寫不符）。
  Windows / macOS 不區分大小寫所以本機正常，部署到 Linux 會失敗。
- `src/api/imageapi.js` 的 `deleteImage` 呼叫 `/image`，後端實際是 `DELETE /api/images/delete?path=`（且僅限後台管理員），目前無作用。
- 匯入路徑請與實際檔名大小寫一致（例如 `components/filterorder.vue`）。
- **聊天室 XSS**：`src/components/ChatWidget.vue` 以 `v-html` 顯示訊息（客服訊息、使用者輸入、`marked.parse` 結果）沒有經過 DOMPurify，
  後端聊天權限問題又讓任何人能以客服身分發訊息。修正時 `v-html` 前一律用 `utils/sanitizeHtml.js`。
- `src/components/HtmlIframe.vue`：`allow-scripts` + `allow-same-origin` 時會移除 sandbox，論壇內容雖已用 DOMPurify 過濾，仍建議保留 sandbox。
- 後端還有多項權限問題（點數、會員、聊天 API），完整清單與修正建議見後端 repo CLAUDE.md 的「全專案檢查結果」。

## 修正紀錄

分支 `claude/vibrant-ride-w4z8f7`：

1. 點數商城錯誤修正：檔名大小寫、取消 / 改寄送資料沒檢查 `ok`、購物車可超過庫存、重複結帳、搜尋未編碼與競態、HTML 注入等
2. 點數商城重構：共用模組（Swal、utils、composables、共用 CSS）、訂單明細延遲載入、購物車價格同步、Cart.vue 巢狀互動元素
3. 配合後端 HTTP 狀態碼調整錯誤處理；`request.js` 加上 `withCredentials`
