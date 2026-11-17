-- 論壇互動唯一索引（全專案第二次檢查 42）
--
-- content_interaction 原本沒有唯一限制，按讚 / 收藏 / 隱藏 / 檢舉連點兩下可能寫入兩筆（按讚數虛增）。
-- 這支腳本先刪除重複的互動（同一會員對同一內容的同一種互動只保留最早的一筆，即 ID 最小者），
-- 再建立唯一索引 UX_content_interaction_content_user_category (content_id, user_id, category_id)。
-- 按讚任務的進度只增不減，刪除重複資料不會讓已完成的進度減少。
--
-- 可重複執行：索引已存在時不做任何事。整段在同一個交易內，建立索引失敗（例如執行期間又寫入了重複資料）
-- 會整筆 rollback，重新執行即可。
-- 執行：sqlcmd -S <server> -d incodedatabase -i docs/database/migrations/2026-11-17-content-interaction-unique.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'UX_content_interaction_content_user_category'
             AND object_id = OBJECT_ID(N'dbo.content_interaction'))
BEGIN
    PRINT N'唯一索引已存在，略過';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    ;WITH ranked AS (
        SELECT content_interaction_id,
               ROW_NUMBER() OVER (PARTITION BY content_id, user_id, category_id
                                  ORDER BY content_interaction_id) AS rn
        FROM dbo.content_interaction WITH (TABLOCKX, HOLDLOCK)
    )
    DELETE FROM ranked WHERE rn > 1;

    PRINT CONCAT(N'已刪除重複的互動：', @@ROWCOUNT, N' 筆');

    CREATE UNIQUE INDEX [UX_content_interaction_content_user_category]
        ON dbo.content_interaction ([content_id], [user_id], [category_id]);

    COMMIT TRANSACTION;
    PRINT N'已建立唯一索引 UX_content_interaction_content_user_category';
END
