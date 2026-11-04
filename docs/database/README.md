# 資料庫

SQL Server（開發預設為 LocalDB）。專案為 Database-First：`incode2/Models/` 由資料庫產生，repo 內沒有 EF migration，
新環境請用本資料夾的腳本建立資料庫。

| 檔案 | 內容 |
|---|---|
| `schema.sql` | 建表腳本：32 張資料表的欄位、預設值、主鍵 / 外鍵、索引（由 EF 模型產生） |
| `seed.sql` | 基本資料：程式依賴的固定資料（角色、點數類型、互動分類）＋範例資料（論壇分類、平台任務） |
| `../dbdiagram.md` | 資料表設計說明（[dbdiagram.io](https://dbdiagram.io) 格式，含欄位意義） |

## 用 Docker 建立（建議）

repo 根目錄的 `docker-compose.yml` 會啟動 SQL Server 2022（Developer 版，僅供開發測試），並自動完成下列步驟 1、2：

```bash
docker compose up -d
```

- 連線字串：`Server=localhost,1433;Database=incodedatabase;User Id=sa;Password=Incode_Dev_2026!;TrustServerCertificate=True`
  （後端 launch profile `https (Docker DB)` 已設定好；SA 密碼可用環境變數 `MSSQL_SA_PASSWORD` 覆寫）
- 初始化腳本：`docker/db/init.sh`（資料庫不存在就建立、尚未建表就執行 `schema.sql`、每次都執行可重複的 `seed.sql`）
- 查看初始化結果：`docker compose logs db-init`
- 重建全新資料庫：`docker compose down -v` 再 `docker compose up -d`
- Apple Silicon（M 系列）Mac：SQL Server 映像只有 amd64，需在 Docker Desktop 開啟 Rosetta 模擬

## 手動建立（LocalDB / 自己的 SQL Server）

1. 建立空資料庫（名稱需與 `incode2/appsettings.json` 的 `ConnectionStrings:incodedatabase` 一致，預設 `incodedatabase`）：

   ```sql
   CREATE DATABASE incodedatabase;
   ```

2. 依序執行 `schema.sql`、`seed.sql`（SSMS / Azure Data Studio 開啟執行，或使用 sqlcmd）：

   ```bash
   sqlcmd -S "(localdb)\ProjectModels" -d incodedatabase -I -f 65001 -i docs/database/schema.sql
   ```

   ```bash
   sqlcmd -S "(localdb)\ProjectModels" -d incodedatabase -I -f 65001 -i docs/database/seed.sql
   ```

3. 建立管理員：從前台註冊一個帳號，再把它設為管理員（密碼以 BCrypt 雜湊儲存，無法直接用 SQL 建立帳號）：

   ```sql
   UPDATE [user] SET role_id = 1 WHERE email = N'你的帳號@example.com';
   ```

## 固定資料（程式依賴，ID 不可更動）

| 資料表 | 內容 | 程式對應 |
|---|---|---|
| `role` | 0 superadmin、1 admin、2 member、3 guest | `Constants/RoleType.cs`；後台登入只允許 role_id = 1，註冊預設 2 |
| `point_type_definition` | 1～10 點數異動類型 | `Constants/PointEnum.cs` 的 `PointType` |
| `interaction_category` | 1 like、2 hide、3 bookmark、4 report、5 best_answer、6 tip | 前端 `api/contentInteractionApi.js` 以 ID 呼叫；後端以名稱 `best_answer` 找最佳解答分類 |

## 重新產生建表腳本

資料表結構有變動（重新從資料庫產生 Models）後，更新 `schema.sql`：

```bash
dotnet tool install --global dotnet-ef
```

```bash
cd incode2
```

```bash
dotnet ef dbcontext script -o ../docs/database/schema.sql
```

產生後請保留檔案開頭的說明，並檢查 `mission.difficulty` 的預設值：EF 模型中這個預設值已變成亂碼 `"??"`
（原本應為兩個中文字，推測在 Big5 轉換時遺失），目前腳本暫定為 `中等`，請依原資料庫確認。

## 注意事項

- 用 sqlcmd 執行時需開啟 `QUOTED_IDENTIFIER`（`mission_progress` 的條件式索引需要）；腳本開頭已加上 `SET QUOTED_IDENTIFIER ON`，
  建議同時加上 `-I` 參數，並用 `-f 65001` 以 UTF-8 讀取（腳本含中文）。
- 腳本由 EF 模型產生，原資料庫若有模型未描述的物件（CHECK 條件、Trigger、View、Stored Procedure），不會包含在內；
  `dbdiagram.md` 中的 CHECK 條件（例如點數類型的 `category IN ('income','expense','system')`）未建立。
- 已在 SQL Server 2022（Docker）實際執行 `schema.sql`、`seed.sql`，並以此資料庫跑過後端的登入、權限、下單流程測試。
