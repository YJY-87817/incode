-- 打賞的點數紀錄改用暱稱（全專案第二次檢查 29）
--
-- 改版前 PointLogService.TipAsync 把對方的 Email 寫進點數紀錄副標：
--   送出方：「給 {對方 Email}」    收到方：「來自 {對方 Email}」
-- 程式已改為寫入暱稱；這支腳本把已存在的紀錄換成暱稱（Email 與副標完全相符才會改），可重複執行。
-- 執行：sqlcmd -S <server> -d incodedatabase -i docs/database/migrations/2026-11-12-tip-log-remove-email.sql

SET NOCOUNT ON;

UPDATE pl
SET pl.display_source_sub_title = N'給 ' + u.username
FROM point_log AS pl
JOIN [user] AS u ON pl.display_source_sub_title = N'給 ' + u.email
WHERE pl.source_type = 'point_tip';

PRINT CONCAT(N'送出方紀錄已更新：', @@ROWCOUNT, N' 筆');

UPDATE pl
SET pl.display_source_sub_title = N'來自 ' + u.username
FROM point_log AS pl
JOIN [user] AS u ON pl.display_source_sub_title = N'來自 ' + u.email
WHERE pl.source_type = 'point_tip';

PRINT CONCAT(N'收到方紀錄已更新：', @@ROWCOUNT, N' 筆');
