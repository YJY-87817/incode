#!/bin/bash
# 由 docker-compose.yml 的 db-init 服務執行：建立資料庫並套用 docs/database 的腳本
# - 資料庫不存在 → 建立
# - 尚未建表（沒有 [user] 資料表）→ 執行 schema.sql
# - 每次都執行 seed.sql（腳本可重複執行，已存在的資料不會重複新增）
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
[ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools/bin/sqlcmd

# -C 信任自簽憑證、-b 發生錯誤時結束、-I 開啟 QUOTED_IDENTIFIER、-f 65001 以 UTF-8 讀取腳本 (中文註解與資料)
run() { "$SQLCMD" -S "$DB_HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -b -I -f 65001 "$@"; }

echo "[db-init] 建立資料庫 $DB_NAME（若不存在）"
run -Q "IF DB_ID(N'$DB_NAME') IS NULL CREATE DATABASE [$DB_NAME];"

has_schema=$(run -d "$DB_NAME" -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'[user]') IS NULL THEN 0 ELSE 1 END;")
if [ "$(echo "$has_schema" | tr -d '[:space:]')" = "0" ]; then
  echo "[db-init] 執行 schema.sql"
  run -d "$DB_NAME" -i /scripts/schema.sql
else
  echo "[db-init] 資料表已存在，略過 schema.sql"
fi

echo "[db-init] 執行 seed.sql"
run -d "$DB_NAME" -i /scripts/seed.sql

echo "[db-init] 完成"
