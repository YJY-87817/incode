# CLAUDE.md

本檔案提供給 Claude Code（與接手的開發者）在此專案工作時的背景、慣例與目前進度。
專案目前由個人維護。本資料夾位於前後端合一的 repo 中，後端在同層的 `../incode.MVC/`（ASP.NET Core 10），
根目錄的 `CLAUDE.md` 有整體架構、後端慣例與全專案檢查清單。

## 專案概觀

- Vue 3（`<script setup>`）+ Vite + Pinia（`pinia-plugin-persistedstate`）+ Vue Router
- UI：Bootstrap 5（CSS 只來自 Material Kit 內建的 5.2，**不要再 import `bootstrap/dist/css`**；5.3 專屬的工具類別補在 `src/assets/css/bootstrap53-utilities.css`。JS 與 bootstrap-icons 仍由 `main.js` 載入）、Vuetify、SweetAlert2、Material Kit 套版（樣式 `src/assets/scss/`、外掛 `src/material-kit.js`、版面 `src/components/layout/`）
- 後端 API：`VITE_API_BASE_URL`（開發為 `https://localhost:7190/api`），圖片等靜態檔：`VITE_BASE_URL`
  （**不要在程式中寫死後端網址**，一律讀這兩個變數；正式環境以 `.env.production` 覆寫）
- 登入：後端 Cookie 驗證，axios 必須帶 `withCredentials: true`
  - 全域 `axios`：`main.js` 設定 `axios.defaults.withCredentials` / `baseURL`
  - `src/api/request.js` 的 `apiClient`、`src/api/pointClient.js`：`axios.create` 建立時就複製預設值，所以自己設定了 `withCredentials: true`
  - 401 處理統一在 `src/api/authInterceptor.js`（三個 axios 都有安裝；**新增 axios 實例時也要 `installAuthInterceptor()`**）：
    已登入狀態收到 401 → 清除登入狀態、提示「登入已逾時」並導向登入頁（`?redirect=` 回原頁）；
    未登入時全域 axios 導向登入頁，`apiClient` / `pointClient` 只把錯誤拋給呼叫端
- **Vuetify**：元件由 `vite-plugin-vuetify` 自動匯入（不要 `import * as components`）。圖示為 SVG，
  只有 `src/plugins/vuetifyIcons.js` 登記的 `mdi-xxx` 會顯示，**用新圖示要先在那裡登記**；沒有 MDI 字型 CSS，不能寫 `<i class="mdi mdi-xxx">`
- **儲值流程**：`Recharge.vue` 建立訂單後把訂單編號存到 sessionStorage（`utils/rechargeOrder.js`）再送出綠界表單；
  綠界導回 `/recharge-result`，以 `GET PointRechargeApi/order/{orderNo}` 輪詢實際狀態（不要假設付款成功）
- **任務**：只有平台任務可以「接取」（`taskStore.claimTask`）；委託任務是到討論串回覆，由委託人選最佳解答
- **論壇編輯器**：`components/contents/TiptapEditor.vue`（Tiptap 3）。ProseMirror 相關 import 一律用 `@tiptap/pm/*`，
  不要直接 import `prosemirror-*`（版本不同時 `instanceof` 會失效）；StarterKit 已內含 Underline / Link，不要重複加入
- **路由**：新頁面一律 `component: () => import("@/views/...")` 延遲載入（只有首頁直接 import）
- 論壇內文以 `components/common/HtmlIframe.vue` 顯示（sandbox 固定為 `allow-same-origin allow-popups allow-popups-to-escape-sandbox`）。
  **不要加 `allow-scripts`**：與 `allow-same-origin` 同時存在時 sandbox 形同無效
- `v-html` 前一律過濾：論壇內容用 `utils/sanitizeHtml.js`；聊天訊息用 `ChatWidget.vue` 的 `toSafeHtml`（使用者輸入跳脫為純文字，其餘以 DOMPurify 過濾）
- 登入狀態：`src/stores/users/auth.js`（`App.vue` 掛載時呼叫 `checkAuth()`）。
  元件中請用 `computed(() => authStore.isAuthenticated)`，**不要**存成常數（會失去響應式）

## 目錄

依「類型分層 + 模組子資料夾」組織，模組名稱與後端一致：`chat`、`contents`（論壇）、`point`（點數 / 任務 / 儲值）、`pointshop`、`users`。
資料夾一律小寫，元件 / 頁面檔名 PascalCase，import 一律使用 `@/` 絕對路徑。

```
src/
├── main.js / App.vue / material-kit.js
├── plugins/vuetifyIcons.js  Vuetify 用到的 MDI 圖示（SVG）
├── router/index.js
├── api/                    後端 API 呼叫
│   ├── request.js          共用 axios (apiClient，baseURL = VITE_API_BASE_URL)
│   ├── authInterceptor.js  所有 axios 共用的 401 處理
│   ├── pointClient.js      點數模組用的 axios (baseURL = VITE_BASE_URL，呼叫路徑自帶 /api)
│   └── contentApi.js / contentInteractionApi.js / imageApi.js / pointShopApi.js
├── stores/                 Pinia
│   ├── chatStore.js / forumStore.js
│   ├── point/              missionStore / pointStore / taskStore
│   ├── pointshop/          cartStore (persist)
│   └── users/              auth / user
├── views/                  路由頁面
│   ├── HomeView.vue / NotFoundView.vue
│   ├── contents/ForumView.vue
│   ├── point/  pointshop/  users/
├── components/
│   ├── layout/             NavbarDefault / FooterCentered (套版)
│   ├── common/             HtmlIframe
│   ├── chat/               ChatWidget (全站客服)
│   ├── contents/  point/  users/
│   └── pointshop/          Cart (全站購物車側欄，掛在 App.vue) 與商城元件
├── composables/            useForumPost / useLoginGuardAlert / useWindowsWidth / useAddToCart / useDesktopExpand
├── utils/                  contentBody / sanitizeHtml / pointShopUtils / pointShopSwal
└── assets/                 css (含 pointshop.css) / scss (Material Kit) / js (Material Kit 外掛) / fonts / img
```

## 點數商城

| 位置 | 內容 |
|---|---|
| `views/pointshop/` | `ProductView`（商品列表 / 詳情的父頁）、`OrderView`、`CheckoutView` |
| `components/pointshop/` | `Cart`、`ProductGrid`、`ProductCard`、`ProductDetail`、`FilterPanel`、`OrderList`、`OrderItem`、`FilterOrder`、`CheckoutCart`、`CheckoutForm` |
| `api/pointShopApi.js` | API 呼叫 |
| `stores/pointshop/cartStore.js` | 購物車 (persist)；`addToCart` / `updateQuantity` 不超過庫存；`syncWithProducts` 以最新商品資料同步 |
| `utils/pointShopSwal.js` | SweetAlert 共用設定與 `alertSuccess` / `alertError` / `alertWarning` / `confirmDialog` |
| `utils/pointShopUtils.js` | `productImageUrl`、`ORDER_STATUS`、`validateShipping`、日期格式、`ensureOk` / `errorText` / `isBusinessError` |
| `composables/useAddToCart.js`、`useDesktopExpand.js` | 加入購物車 + 提示、篩選面板桌面版預設展開 |
| `assets/css/pointshop.css` | `.btn-custom-primary` 與篩選面板共用樣式（由商城頁面與 Cart 匯入） |

### 慣例

- **錯誤處理**：後端業務失敗回 4xx + `{ ok: false, message }`（舊 API 可能是 200 + `ok: false`）。
  成功路徑用 `ensureOk(res)`，失敗用 `errorText(error)` 顯示訊息、`isBusinessError(error)` 判斷是否要重新整理資料。
- 提示框一律用 `pointShopSwal.js`，不要每個元件自己寫 `didOpen` 改按鈕顏色。
- 插入 SweetAlert `html` 的使用者輸入要先 `escapeHtml()`。
- 訂單狀態用 `ORDER_STATUS`，會員只能修改 / 取消「待處理」訂單（後端也會檢查）。
- 訂單明細在展開訂單時才載入（`OrderItem.vue`），列表重新整理時不要清空列表（保留展開狀態）。
- 搜尋用 debounce + 請求序號，只採用最後一次回應。

## 建置與驗證

- `npm ci`（依 `package-lock.json` 安裝；新增 / 升級套件用 `npm install <套件>` 並一併提交 lockfile）、`npm run dev`、`npm run build`
- `vite.config.js`：`incode.Vue/` 下有 `key.pem` / `cert.pem`（已 gitignore）時 dev server 使用 HTTPS，沒有時改為 HTTP；`npm run build` 不需要憑證
- CI（repo 根目錄 `.github/workflows/ci.yml`）在 Linux 執行 `npm ci` + `npm run build`，匯入路徑大小寫錯誤會讓 PR 檢查失敗
- ESLint 9 沒有 `eslint.config.js`，`npm run lint` 目前無法執行
- 沒有自動化測試。曾以 Playwright（環境內建 Chromium）+ 攔截 API 回應的方式做瀏覽器冒煙測試

## 已知問題

- `src/api/imageApi.js` 的 `deleteImage` 呼叫 `/image`，後端實際是 `DELETE /api/images/delete?path=`（且僅限後台管理員），目前無作用。
- 匯入路徑請與實際檔名大小寫一致（Windows / macOS 不分大小寫，但 Linux build 會失敗）。
- 完整問題清單見根目錄 `CLAUDE.md` 的「全專案檢查結果」。

## 修正紀錄

（以下為合併進 monorepo 之前，原 `incode_Vue` repo 的修正）

1. 點數商城錯誤修正：檔名大小寫、取消 / 改寄送資料沒檢查 `ok`、購物車可超過庫存、重複結帳、搜尋未編碼與競態、HTML 注入等
2. 點數商城重構：共用模組（Swal、utils、composables、共用 CSS）、訂單明細延遲載入、購物車價格同步、Cart.vue 巢狀互動元素
3. 配合後端 HTTP 狀態碼調整錯誤處理；`request.js` 加上 `withCredentials`
4. 清理模板殘留：刪除 Vue Material Kit 範例圖片 / JS / CSS / 字型、未使用的元件與 store、Visual Studio 快取 `.vs/`、
   模板的 CHANGELOG / ISSUE_TEMPLATE / genezio.yaml，移除 17 個未使用的 npm 套件；
   修正 `Task.vue` 匯入路徑大小寫（Linux build 失敗）；`FORUM_COMMENTS_SUMMARY.md` 移到 `docs/`。
   `src/assets/scss/` 為 `material-kit.scss` 的 sass partial，雖不會出現在模組清單中但有被使用，請勿刪除；
   `LICENSE` 為模板（Creative Tim, MIT）授權聲明，需保留
5. 資料夾整理：PointShop 原本的功能資料夾（`views/PointShop/components|services|styles`）拆回 `components/`、`api/`、`stores/`、
   `utils/`、`composables/`；`examples/` 的版面元件移到 `components/layout/`；資料夾統一小寫並與後端模組同名
   （`Contents`→`contents`、`user`→`users`、`task` 併入 `point`）；檔名統一 PascalCase / camelCase
   （`forum.vue`→`ForumView.vue`、`error404.vue`→`NotFoundView.vue`、`FirstView.vue`→`HomeView.vue`、`userindex.vue`→`UserIndex.vue`、
   `Mytask.vue`→`MyTask.vue`、`chatmessagestores.js`→`chatStore.js`、`UseXxxStore.js`→`xxxStore.js`、`imageapi.js`→`imageApi.js`、
   `api/point/axios.js`→`api/pointClient.js`、`Check.vue`→`CheckoutForm.vue`、`Checkitem.vue`→`CheckoutCart.vue`）；
   import 全部改為 `@/` 絕對路徑。若在 Windows 上 pull 後資料夾大小寫沒有跟著變，重新 clone 即可
