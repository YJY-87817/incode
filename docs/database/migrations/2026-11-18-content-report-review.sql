-- 後台檢舉處理紀錄（全專案第二次檢查 43）
--
-- 新增資料表 content_report_review：後台在「檢舉清單」駁回、下架（content.is_active = 3）或恢復上架時各寫一筆，
-- 記錄哪位管理員在何時處理了哪篇內容。一篇內容最後一次處理之後才出現的檢舉，才會再列入「待處理」。
-- 既有的檢舉（content_interaction 分類 4）不需要轉換，執行後全部列為待處理。
--
-- 可重複執行：資料表已存在時不做任何事。
-- 執行：sqlcmd -S <server> -d incodedatabase -I -f 65001 -i docs/database/migrations/2026-11-18-content-report-review.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.content_report_review', N'U') IS NOT NULL
BEGIN
    PRINT N'content_report_review 已存在，略過';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    CREATE TABLE dbo.[content_report_review] (
        [content_report_review_id] int NOT NULL IDENTITY,
        [content_id] int NOT NULL,
        [admin_user_id] int NOT NULL,
        [action] nvarchar(20) NOT NULL,
        [note] nvarchar(500) NULL,
        [created_at] datetime NOT NULL,
        CONSTRAINT [PK_content_report_review] PRIMARY KEY ([content_report_review_id]),
        CONSTRAINT [FK_content_report_review_admin_user] FOREIGN KEY ([admin_user_id]) REFERENCES dbo.[user] ([user_id]),
        CONSTRAINT [FK_content_report_review_content] FOREIGN KEY ([content_id]) REFERENCES dbo.[content] ([content_id])
    );

    CREATE INDEX [IX_content_report_review_admin_user_id] ON dbo.[content_report_review] ([admin_user_id]);
    CREATE INDEX [IX_content_report_review_content_id_created_at] ON dbo.[content_report_review] ([content_id], [created_at]);

    COMMIT TRANSACTION;
    PRINT N'已建立 content_report_review';
END
