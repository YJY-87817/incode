-- 會員 Email 驗證與登入戳記（全專案第二次檢查 40）
--
-- [user] 新增欄位：
--   email_verified             Email 已驗證（點了驗證信，或以 Google 登入過）。既有會員一律為未驗證（0）：
--                              未驗證的密碼帳號被同一個 Email 的 Google 帳號登入時會被接管（清除密碼、登出所有裝置）
--   email_verify_token(_expiry) 驗證信的 token 與期限
--   security_stamp             登入戳記：重設 / 修改密碼、Google 接管時換新，舊的登入 Cookie 會失效。NULL 表示從未換過
--
-- 可重複執行：欄位已存在就略過。
-- 執行：sqlcmd -S <server> -d incodedatabase -i docs/database/migrations/2026-11-17-user-email-verify-security-stamp.sql

SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.[user]', N'email_verified') IS NULL
BEGIN
    ALTER TABLE dbo.[user] ADD [email_verified] bit NOT NULL
        CONSTRAINT [DF_user_email_verified] DEFAULT CAST(0 AS bit);
    PRINT N'已新增欄位 email_verified';
END

IF COL_LENGTH(N'dbo.[user]', N'email_verify_token') IS NULL
BEGIN
    ALTER TABLE dbo.[user] ADD [email_verify_token] nvarchar(100) NULL;
    PRINT N'已新增欄位 email_verify_token';
END

IF COL_LENGTH(N'dbo.[user]', N'email_verify_token_expiry') IS NULL
BEGIN
    ALTER TABLE dbo.[user] ADD [email_verify_token_expiry] datetime NULL;
    PRINT N'已新增欄位 email_verify_token_expiry';
END

IF COL_LENGTH(N'dbo.[user]', N'security_stamp') IS NULL
BEGIN
    ALTER TABLE dbo.[user] ADD [security_stamp] nvarchar(64) NULL;
    PRINT N'已新增欄位 security_stamp';
END

-- 以 Google 登入建立的帳號（密碼欄為 GOOGLE_AUTH）本來就由 Google 驗證過 Email。
-- 用動態 SQL：同一批次內剛新增的欄位，靜態陳述式在編譯時還看不到
EXEC (N'UPDATE dbo.[user] SET [email_verified] = 1 WHERE [password] = N''GOOGLE_AUTH'' AND [email_verified] = 0;
       PRINT CONCAT(N''Google 帳號標記為已驗證：'', @@ROWCOUNT, N'' 筆'');');
