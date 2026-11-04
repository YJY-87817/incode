-- =============================================================================
-- incode 基本資料 (SQL Server)
-- 在 schema.sql 之後執行；可重複執行（已存在的資料不會重複新增）。
--
-- 第 1 部分為程式依賴的固定資料，ID 不可更動；第 2 部分為範例資料，可依需要調整或略過。
-- =============================================================================

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

-- -----------------------------------------------------------------------------
-- 1-1. 角色 role
--      程式以 role_id 判斷身分：1 = 管理員（可登入後台），2 = 一般會員（註冊預設）
--      對應 incode2/Constants/RoleType.cs
-- -----------------------------------------------------------------------------
MERGE [role] AS t
USING (VALUES
    (0, 'superadmin', 0),
    (1, 'admin',      1),
    (2, 'member',     2),
    (3, 'guest',      3)
) AS s (role_id, role_name, authority)
ON t.role_id = s.role_id
WHEN NOT MATCHED THEN
    INSERT (role_id, role_name, authority) VALUES (s.role_id, s.role_name, s.authority);

-- -----------------------------------------------------------------------------
-- 1-2. 點數異動類型 point_type_definition
--      ID 必須與 incode2/Constants/PointEnum.cs 的 PointType 列舉一致
-- -----------------------------------------------------------------------------
SET IDENTITY_INSERT [point_type_definition] ON;

MERGE [point_type_definition] AS t
USING (VALUES
    (1,  N'income_mission',                  N'平台任務獎勵', 'income'),
    (2,  N'income_entrust_mission',          N'委託任務獎勵', 'income'),
    (3,  N'income_point_recharge',           N'點數儲值',     'income'),
    (4,  N'income_orders_refund',            N'商品退款',     'income'),
    (5,  N'income_entrust_mission_refund',   N'委託任務退款', 'income'),
    (6,  N'expense_entrust_mission_reserve', N'委託預扣點數', 'expense'),
    (7,  N'expense_orders',                  N'商品消費',     'expense'),
    (8,  N'system_adjustment',               N'系統調整',     'system'),
    (9,  N'expense_tip_out',                 N'打賞支出',     'expense'),
    (10, N'income_tip_in',                   N'獲得打賞',     'income')
) AS s (id, type_name, display_name, category)
ON t.point_type_definition_id = s.id
WHEN NOT MATCHED THEN
    INSERT (point_type_definition_id, type_name, display_name, category, is_active)
    VALUES (s.id, s.type_name, s.display_name, s.category, 1);

SET IDENTITY_INSERT [point_type_definition] OFF;

-- -----------------------------------------------------------------------------
-- 1-3. 論壇互動分類 interaction_category
--      前端以 ID 呼叫：1 = 按讚、2 = 隱藏、3 = 收藏、4 = 檢舉 (incode_Vue/src/api/contentInteractionApi.js)
--      後端以 ID 1 判斷按讚任務，以名稱 best_answer 找最佳解答分類
-- -----------------------------------------------------------------------------
SET IDENTITY_INSERT [interaction_category] ON;

MERGE [interaction_category] AS t
USING (VALUES
    (1, N'like',        N'按讚'),
    (2, N'hide',        N'隱藏'),
    (3, N'bookmark',    N'收藏'),
    (4, N'report',      N'檢舉'),
    (5, N'best_answer', N'最佳解答（委託任務）'),
    (6, N'tip',         N'打賞')
) AS s (id, name, description)
ON t.interaction_category_id = s.id
WHEN NOT MATCHED THEN
    INSERT (interaction_category_id, name, description, is_active)
    VALUES (s.id, s.name, s.description, 1);

SET IDENTITY_INSERT [interaction_category] OFF;

-- =============================================================================
-- 2. 範例資料（可依需要調整）
-- =============================================================================

-- 2-1. 論壇分類 content_category（is_active：1 = 會員新增、2 = 官方提供、3 = 停用）
IF NOT EXISTS (SELECT 1 FROM [content_category])
BEGIN
    INSERT INTO [content_category] (parent_category_id, category_name, description, is_active) VALUES
        (NULL, N'電影討論', N'電影心得、劇情與推薦', 2),
        (NULL, N'影城資訊', N'影城、場次與票價資訊', 2),
        (NULL, N'其他',     N'其他話題',             2);
END;

-- 2-2. 平台任務 mission
--      target_type 對應程式觸發的動作：login、post、comment、like、purchase、mission_complete
--      reset_type：none / daily / weekly / monthly
IF NOT EXISTS (SELECT 1 FROM [mission])
BEGIN
    INSERT INTO [mission]
        (mission_name, mission_detail, target_type, target_count, reward_points, reward_exp,
         category, difficulty, reset_type, is_featured, is_active)
    VALUES
        (N'每日登入',   N'每天登入一次',             N'login',    1, 10, 5,  N'基礎',     N'簡單', N'daily',  1, 1),
        (N'發表文章',   N'在論壇發表一篇文章',       N'post',     1, 20, 10, N'內容創作', N'簡單', N'daily',  0, 1),
        (N'熱心回覆',   N'在論壇留言 3 次',          N'comment',  3, 15, 10, N'社群互動', N'簡單', N'daily',  0, 1),
        (N'按讚支持',   N'替 5 篇文章按讚',          N'like',     5, 10, 5,  N'社群互動', N'簡單', N'daily',  0, 1),
        (N'首次兌換',   N'在點數商城完成一筆兌換',   N'purchase', 1, 50, 20, N'基礎',     N'中等', N'none',   1, 1);
END;

-- -----------------------------------------------------------------------------
-- 建立管理員帳號：
--   1. 從前台註冊一個帳號（密碼以 BCrypt 雜湊儲存，無法直接用 SQL 建立）
--   2. 執行下列語句把該帳號設為管理員，即可登入後台
--      UPDATE [user] SET role_id = 1 WHERE email = N'你的帳號@example.com';
-- -----------------------------------------------------------------------------
