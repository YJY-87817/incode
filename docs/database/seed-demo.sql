-- =============================================================================
-- incode 展示 / 測試用假資料 (SQL Server)
-- 在 schema.sql、seed.sql 之後執行（Docker 的 db-init 會自動執行，見 docker/db/init.sh）。
--
-- 只在 [user] 資料表是空的時候才建議執行（會員 ID 與其他資料互相對應）；
-- docker/db/init.sh 會自動判斷，手動執行前請先確認。整個腳本是單一交易，失敗會全部還原。
--
-- 內容：250 位會員（1～5 為管理員）、論壇文章 / 互動、任務、委託、點數紀錄（帳本連續）、儲值、
--       商城商品 / 110 筆訂單 / 寄送、通知、登入紀錄。
-- 所有會員的密碼都是  Demo1234!   （只供開發測試，不要用在正式環境）
--
-- 注意：
--   * 文章內文 body 是檔案路徑（/contents/content-N.html），檔案放在 App:UploadRoot，
--     本腳本不會產生這些檔案，所以文章內文會是空的；商品圖片同理（image_name 指向的檔案不存在）。
--   * 資料庫固定資料（角色、點數類型、互動分類）由 seed.sql 提供，這裡不重複建立。
-- =============================================================================

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @pw nvarchar(255) = N'$2a$11$JgX3gFx45m0anJdsFkuNNOJD9XEenCwu6SxBEUSBS3gB4Yxx9d1oC'; -- Demo1234!

-- =============================================================================
-- 1. 會員 user（1～5 管理員、6～251 一般會員）
-- =============================================================================
INSERT INTO [user]
    (email, username, password, image_url, phone, birthday, address, created_at, is_active, role_id, email_verified)
SELECT v.email, v.username, @pw, CONCAT('https://api.dicebear.com/7.x/lorelei/svg?seed=', 500 + v.n),
       v.phone, v.birthday, v.address, v.created_at, v.is_active, v.role_id, 1
FROM (VALUES
 (1,  'incode@example.com',          N'影庫incode', '0912345678', '1995-10-10', N'台北市信義區', '2026-02-02 06:45:36', 1, 1),
 (2,  'gina@example.com',            N'王曉明', '0922111222', '1996-05-15', N'新北市板橋區中山路一段', '2026-02-02 07:02:15', 1, 1),
 (3,  'harry@example.com',           N'許俊翰', '0933222333', '1992-08-20', N'台中市西屯區台灣大道', '2026-02-02 07:02:55', 1, 1),
 (4,  'iris@example.com',            N'楊雅玲', '0944333444', '1998-12-10', N'台南市東區中華東路', '2026-02-02 07:03:05', 1, 1),
 (5,  'jacky@example.com',           N'郭佩雯', '0955444555', '1990-03-05', N'高雄市左營區博愛二路', '2026-02-02 07:03:20', 1, 1),
 (6,  'alex.chen@example.com',       N'陳志明', '0912345678', '1990-05-15', N'台北市信義區信義路五段7號', '2025-08-14 14:22:15', 1, 2),
 (7,  'betty.lin@example.com',       N'林雅婷', '0922123456', '1992-08-22', N'新北市板橋區文化路一段100號', '2025-03-25 09:45:30', 1, 2),
 (8,  'charlie.wu@example.com',      N'吳建豪', '0933987654', '1988-12-01', N'台中市西屯區台灣大道三段251號', '2025-11-03 16:12:05', 1, 2),
 (9,  'david.huang@example.com',     N'黃家豪', '0911222333', '1995-03-10', N'高雄市左營區博愛二路777號', '2025-05-14 11:30:45', 1, 2),
 (10, 'eva.chang@example.com',       N'張怡君', '0955666777', '1993-07-04', N'台南市安平區安平路88號', '2025-01-21 08:20:12', 1, 2),
 (11, 'frank.liu@example.com',       N'劉冠宇', '0966888999', '1985-11-30', N'桃園市中壢區中正路50號', '2025-09-18 13:55:00', 1, 2),
 (12, 'grace.wang@example.com',      N'王欣儀', '0977000111', '1998-02-14', N'新竹市東區光復路二段101號', '2025-04-05 10:10:33', 1, 2),
 (13, 'henry.yang@example.com',      N'楊俊傑', '0988222333', '1991-09-09', N'基隆市仁愛區愛三路10號', '2025-12-22 17:05:18', 0, 2),
 (14, 'irene.tsai@example.com',      N'蔡佩珊', '0910555666', '1996-06-18', N'嘉義市西區垂楊路200號', '2025-02-10 09:15:40', 1, 2),
 (15, 'jack.ho@example.com',         N'何志強', '0920888777', '1989-10-25', N'宜蘭縣宜蘭市神農路一段1號', '2025-06-28 15:40:22', 1, 2),
 (16, 'kelly.kuo@example.com',       N'郭美玲', '0930111222', '1994-01-30', N'花蓮縣花蓮市中正路300號', '2025-10-03 12:00:55', 1, 2),
 (17, 'leo.cheng@example.com',       N'鄭宗翰', '0950333444', '1997-04-12', N'台東縣台東市更生路50號', '2025-07-15 18:20:10', 1, 2),
 (18, 'mandy.hsu@example.com',       N'許雅雯', '0960555999', '1990-11-11', N'彰化縣彰化市中山路二段2號', '2025-05-02 14:50:33', 1, 2),
 (19, 'nick.lai@example.com',        N'賴承恩', '0970888111', '1987-05-05', N'南投縣南投市中興路100號', '2025-01-20 08:30:15', 1, 2),
 (20, 'olivia.pan@example.com',      N'潘思妤', '0980222666', '1999-09-20', N'雲林縣斗六市大學路三段123號', '2025-08-05 11:45:50', 1, 2),
 (21, 'peter.yeh@example.com',       N'葉冠廷', '0919333555', '1992-12-25', N'屏東縣屏東市自由路456號', '2025-11-18 16:10:12', 1, 2),
 (22, 'queen.lo@example.com',        N'羅婉婷', '0929777888', '1995-07-07', N'苗栗縣苗栗市中正路88號', '2025-02-11 08:20:44', 1, 2),
 (23, 'ricky.chu@example.com',       N'朱宏偉', '0939000999', '1986-03-28', N'新竹縣竹北市縣政九路1號', '2025-06-15 13:50:55', 1, 2),
 (24, 'sandy.kao@example.com',       N'高詩涵', '0959111333', '1993-10-10', N'澎湖縣馬公市中正路10號', '2025-04-07 10:40:20', 1, 2),
 (25, 'tom.hsieh@example.com',       N'謝明哲', '0969222444', '1991-04-20', N'台北市內湖區瑞光路399號', '2025-12-22 15:30:11', 1, 2),
 (26, 'manager.wu@example.com',      N'吳淑芬', '0911223344', '1985-03-12', N'台北市大安區和平東路二段100號', '2025-07-05 10:00:22', 1, 2),
 (27, 'david.lee@example.com',       N'李大衛', '0922334455', '1982-11-05', N'新北市板橋區文化路一段50號', '2025-09-12 11:30:45', 1, 2),
 (28, 'grace.ho@example.com',        N'何雅雯', '0933445566', '1991-07-20', N'台中市西屯區台灣大道三段300號', '2025-01-18 09:15:10', 1, 2),
 (29, 'peter.chang@example.com',     N'張志強', '0944556677', '1988-01-30', N'高雄市左營區博愛二路200號', '2025-10-24 14:00:33', 1, 2),
 (30, 'julia.lin@example.com',       N'林怡君', '0955667788', '1993-09-15', N'台南市東區中華東路一段88號', '2025-03-09 16:45:55', 1, 2),
 (31, 'kevin.chen@example.com',      N'陳建宏', '0966778899', '1995-02-14', N'桃園市中壢區中正路1號', '2025-08-22 08:30:12', 1, 2),
 (32, 'sandy.huang@example.com',     N'黃欣怡', '0977889900', '1998-06-22', N'新竹市東區光復路二段101號', '2025-06-10 09:00:44', 1, 2),
 (33, 'micheal.wang@example.com',    N'王家豪', '0988990011', '1990-12-05', N'台北市士林區中正路200號', '2025-11-28 10:15:33', 1, 2),
 (34, 'emily.liu@example.com',       N'劉佩珊', '0912345679', '2000-04-18', N'新北市新莊區中正路500號', '2025-02-05 11:20:10', 1, 2),
 (35, 'eric.yang@example.com',       N'楊宗翰', '0923456780', '1992-08-30', N'台北市內湖區瑞光路300號', '2025-05-18 13:00:55', 1, 2),
 (36, 'alice.hsu@example.com',       N'許雅婷', '0934567891', '1996-10-10', N'基隆市仁愛區愛三路50號', '2025-09-02 14:30:22', 1, 2),
 (37, 'brian.tsai@example.com',      N'蔡俊宏', '0945678902', '1989-05-25', N'嘉義市西區垂楊路200號', '2025-12-14 15:45:40', 1, 2),
 (38, 'cindy.cheng@example.com',     N'鄭淑惠', '0956789013', '1994-01-08', N'彰化縣彰化市中山路二段100號', '2025-04-05 17:00:15', 1, 2),
 (39, 'daniel.hung@example.com',     N'洪偉哲', '0967890124', '1997-11-12', N'雲林縣斗六市大學路三段123號', '2025-08-22 18:15:33', 1, 2),
 (40, 'fiona.kuo@example.com',       N'郭郁雯', '0978901235', '1999-03-28', N'屏東縣屏東市中正路88號', '2025-01-08 19:30:10', 1, 2),
 (41, 'george.lai@example.com',      N'賴建銘', '0989012346', '1993-07-05', N'宜蘭縣宜蘭市神農路一段1號', '2025-11-14 20:00:55', 1, 2),
 (42, 'hannah.tseng@example.com',    N'曾思妤', '0990123457', '1991-09-09', N'花蓮縣花蓮市中正路50號', '2025-05-05 21:15:22', 1, 2),
 (43, 'ian.yeh@example.com',         N'葉冠宇', '0901234568', '1995-12-25', N'台東縣台東市更生路100號', '2025-10-20 08:45:40', 1, 2),
 (44, 'jessica.lu@example.com',      N'盧曉薇', '0912345670', '1987-04-14', N'南投縣南投市中興新村', '2025-03-01 09:30:15', 1, 2),
 (45, 'ken.weng@example.com',        N'翁志豪', '0923456781', '1990-06-30', N'苗栗縣苗栗市中正路300號', '2025-07-15 10:45:33', 1, 2),
 (46, 'jason.wu@example.com',        N'吳宗憲', '0944556677', '1988-04-05', N'高雄市前金區中正四路200號', '2025-09-11 09:00:10', 1, 2),
 (47, 'tina.chen@example.com',       N'陳怡君', '0955667788', '1992-06-15', N'台北市松山區民生東路三段100號', '2025-04-12 10:30:55', 1, 2),
 (48, 'george.lin@example.com',      N'林冠宇', '0966778899', '1995-09-20', N'新北市新店區北新路二段50號', '2025-02-20 14:15:22', 1, 2),
 (49, 'sarah.yang@example.com',      N'楊詠晴', '0977889900', '1998-01-10', N'桃園市桃園區中正路1號', '2025-11-01 11:00:40', 1, 2),
 (50, 'steven.huang@example.com',    N'黃柏翰', '0988990011', '1990-12-12', N'台中市北區崇德路一段88號', '2025-08-05 16:20:15', 1, 2),
 (51, 'li.dabi@example.com',         N'李大鼻', '0988994011', '1995-11-11', N'台北市北區崇德路一段88號', '2025-09-23 16:20:15', 1, 2)
) AS v (n, email, username, phone, birthday, address, created_at, is_active, role_id)
ORDER BY v.n;

-- 52～251：產生的一般會員
;WITH n AS (SELECT TOP (200) 51 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO [user] (email, username, password, image_url, phone, birthday, address, created_at, is_active, role_id, email_verified)
SELECT CONCAT('user', i, '@example.com'),
       CONCAT(N'使用者', i),
       @pw,
       CONCAT('https://api.dicebear.com/7.x/lorelei/svg?seed=', 500 + i),
       CONCAT('09', RIGHT('00000000' + CAST(ABS(CHECKSUM(NEWID())) % 100000000 AS varchar(8)), 8)),
       DATEADD(YEAR, -(20 + (i % 30)), GETDATE()),
       CASE (i % 10)
            WHEN 0 THEN N'台北市中正區重慶南路一段' WHEN 1 THEN N'新北市新莊區中正路'
            WHEN 2 THEN N'台中市北區三民路三段'     WHEN 3 THEN N'台南市中西區中山路'
            WHEN 4 THEN N'高雄市苓雅區四維路'       WHEN 5 THEN N'桃園市桃園區中正路'
            WHEN 6 THEN N'新竹市東區光復路'         WHEN 7 THEN N'嘉義市西區中山路'
            WHEN 8 THEN N'台北市大安區敦化南路'     ELSE N'新北市板橋區文化路'
       END + CAST(i AS nvarchar(10)) + N'號',
       DATEADD(DAY, -(i * 2), GETDATE()), 1, 2, 1
FROM n;

-- 收件地址簿：每位前 50 位會員一筆
INSERT INTO shipaddress (user_id, recipient_name, recipient_address, recipient_phone)
SELECT user_id, username, LEFT(address, 50), phone FROM [user] WHERE user_id <= 50;

-- =============================================================================
-- 2. 追蹤 follow
-- =============================================================================
-- 5～50（排除 13 以外都追蹤 1 號；原資料排除 2、3、4、20）
INSERT INTO follow (follower_id, followee_id, created_at)
SELECT u.user_id, 1, DATEADD(MINUTE, 15 * u.user_id, '2024-01-01 08:00:00')
FROM [user] u WHERE u.user_id BETWEEN 5 AND 50 AND u.user_id <> 20;

-- 2 號追蹤 3～50
INSERT INTO follow (follower_id, followee_id, created_at)
SELECT 2, u.user_id, DATEADD(MINUTE, 5 * u.user_id, '2024-01-02 09:00:00')
FROM [user] u WHERE u.user_id BETWEEN 3 AND 50;

-- 一些相鄰會員互相追蹤
INSERT INTO follow (follower_id, followee_id, created_at)
SELECT u.user_id, u.user_id + 1, DATEADD(DAY, u.user_id, '2024-02-01')
FROM [user] u WHERE u.user_id BETWEEN 3 AND 33 AND u.user_id <> 20
  AND NOT EXISTS (SELECT 1 FROM follow f WHERE f.follower_id = u.user_id AND f.followee_id = u.user_id + 1);

-- 51～250 追蹤前 50 位會員
INSERT INTO follow (follower_id, followee_id, created_at)
SELECT u.user_id, 1 + (u.user_id % 50), DATEADD(DAY, -(u.user_id % 100), GETDATE())
FROM [user] u WHERE u.user_id BETWEEN 51 AND 250
  AND NOT EXISTS (SELECT 1 FROM follow f WHERE f.follower_id = u.user_id AND f.followee_id = 1 + (u.user_id % 50));

INSERT INTO follow (follower_id, followee_id, created_at)
SELECT u.user_id, 1 + ((u.user_id + 10) % 50), DATEADD(DAY, -(u.user_id % 90), GETDATE())
FROM [user] u WHERE u.user_id BETWEEN 51 AND 250 AND u.user_id % 2 = 0
  AND NOT EXISTS (SELECT 1 FROM follow f WHERE f.follower_id = u.user_id AND f.followee_id = 1 + ((u.user_id + 10) % 50));

-- =============================================================================
-- 3. 登入紀錄 user_loginlog
-- =============================================================================
INSERT INTO user_loginlog (user_id, logintime, ip_address) VALUES
-- 6 號：晚間規律登入，最後一筆為「不可能的旅行」（半小時後 IP 變成歐洲節點）
(6,'2024-01-10 19:30:00','114.34.88.10'),(6,'2024-01-11 20:15:00','114.34.88.10'),(6,'2024-01-12 18:45:00','49.216.77.88'),
(6,'2024-01-13 19:20:00','114.34.88.10'),(6,'2024-01-14 21:05:00','114.34.88.10'),(6,'2024-01-15 19:40:00','49.216.77.88'),
(6,'2024-01-16 20:00:00','114.34.88.10'),(6,'2024-01-17 19:10:00','114.34.88.10'),(6,'2024-01-18 20:50:00','49.216.77.88'),
(6,'2024-01-19 19:55:00','114.34.88.10'),(6,'2024-01-20 18:30:00','114.34.88.10'),(6,'2024-01-21 21:15:00','114.34.88.10'),
(6,'2024-01-22 19:05:00','49.216.77.88'),(6,'2024-01-23 20:25:00','114.34.88.10'),(6,'2024-01-24 19:45:00','114.34.88.10'),
(6,'2024-01-24 20:15:00','193.106.191.50'),
-- 7 號：平日早晨、固定公司 IP
(7,'2024-01-02 08:50:00','210.242.155.10'),(7,'2024-01-03 08:55:00','210.242.155.10'),(7,'2024-01-04 09:05:00','210.242.155.10'),
(7,'2024-01-05 08:45:00','210.242.155.10'),(7,'2024-01-08 09:10:00','210.242.155.10'),(7,'2024-01-09 08:58:00','210.242.155.10'),
(7,'2024-01-10 09:02:00','210.242.155.10'),(7,'2024-01-11 08:50:00','210.242.155.10'),(7,'2024-01-12 09:15:00','210.242.155.10'),
(7,'2024-01-15 08:55:00','210.242.155.10'),(7,'2024-01-16 09:00:00','210.242.155.10'),(7,'2024-01-17 08:48:00','210.242.155.10'),
(7,'2024-01-18 09:05:00','210.242.155.10'),(7,'2024-01-19 08:52:00','210.242.155.10'),(7,'2024-01-22 09:10:00','210.242.155.10'),
(7,'2024-01-23 08:59:00','210.242.155.10'),(7,'2024-01-24 09:01:00','210.242.155.10'),(7,'2024-01-25 08:45:00','210.242.155.10'),
(7,'2024-01-26 09:06:00','210.242.155.10'),(7,'2024-01-29 08:50:00','210.242.155.10'),
-- 8 號：自由工作者，家用與手機 IP 間切換
(8,'2024-01-05 10:15:00','36.226.50.12'),(8,'2024-01-05 14:30:00','27.242.10.88'),(8,'2024-01-06 11:20:00','36.226.50.12'),
(8,'2024-01-07 15:45:00','27.242.10.88'),(8,'2024-01-08 10:05:00','36.226.50.12'),(8,'2024-01-09 20:10:00','36.226.50.12'),
(8,'2024-01-11 12:30:00','27.242.10.88'),(8,'2024-01-12 09:50:00','36.226.50.12'),(8,'2024-01-13 16:15:00','27.242.10.88'),
(8,'2024-01-14 11:40:00','36.226.50.12'),(8,'2024-01-15 13:25:00','27.242.10.88'),(8,'2024-01-17 10:30:00','36.226.50.12'),
(8,'2024-01-18 19:45:00','36.226.50.12'),(8,'2024-01-20 14:10:00','27.242.10.88'),(8,'2024-01-21 09:20:00','36.226.50.12'),
(8,'2024-01-22 15:55:00','27.242.10.88'),(8,'2024-01-24 11:05:00','36.226.50.12'),(8,'2024-01-25 18:30:00','36.226.50.12'),
(8,'2024-01-27 13:45:00','27.242.10.88'),(8,'2024-01-28 10:15:00','36.226.50.12');

;WITH n AS (SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO user_loginlog (user_id, logintime, ip_address)
SELECT 1 + (i % 50), DATEADD(HOUR, -(i % 720), GETDATE()),
       CONCAT(1 + (i % 254), '.', 1 + ((i * 2) % 254), '.', 1 + ((i * 3) % 254), '.', 1 + ((i * 4) % 254))
FROM n;

-- =============================================================================
-- 4. 論壇：分類、文章、互動、修改紀錄
--    content.type：1 主題、2 回文、3 留言、4 留言回覆（見 Constants/ContentTypes.cs）
-- =============================================================================
-- 4-1. 子分類（頂層分類由 seed.sql 建立；is_active 2 = 官方）
DECLARE @catMovie int = (SELECT TOP 1 content_category_id FROM content_category WHERE parent_category_id IS NULL AND category_name = N'電影討論');
DECLARE @catCinema int = (SELECT TOP 1 content_category_id FROM content_category WHERE parent_category_id IS NULL AND category_name = N'影城資訊');

INSERT INTO content_category (parent_category_id, category_name, description, is_active)
SELECT v.parent_id, v.name, v.descr, 2
FROM (VALUES
    (@catMovie,  N'歐美電影', N'好萊塢與歐洲電影'),
    (@catMovie,  N'亞洲電影', N'日韓台港電影'),
    (@catCinema, N'威秀影城', N'全台威秀影城討論'),
    (@catCinema, N'秀泰影城', N'全台秀泰影城討論')
) AS v (parent_id, name, descr)
WHERE NOT EXISTS (SELECT 1 FROM content_category c WHERE c.category_name = v.name AND c.parent_category_id = v.parent_id);

DECLARE @cEU int = (SELECT TOP 1 content_category_id FROM content_category WHERE category_name = N'歐美電影');
DECLARE @cAsia int = (SELECT TOP 1 content_category_id FROM content_category WHERE category_name = N'亞洲電影');
DECLARE @cVieshow int = (SELECT TOP 1 content_category_id FROM content_category WHERE category_name = N'威秀影城');
DECLARE @cAmuz int = (SELECT TOP 1 content_category_id FROM content_category WHERE category_name = N'秀泰影城');

-- 4-2. 文章 1～50（指定 ID，後面的互動、委託會引用）
SET IDENTITY_INSERT content ON;
INSERT INTO content (content_id, parent_content_id, category_id, user_id, title, body, type, created_at, updated_at, is_active)
SELECT v.id, v.parent_id, v.cat, v.user_id, v.title, CONCAT('/contents/content-', v.id, '.html'), v.type,
       DATEADD(HOUR, -v.hrs, GETDATE()), DATEADD(HOUR, -v.hrs, GETDATE()), 1
FROM (VALUES
 -- 主題
 (1,  NULL, @cEU,     1, 1, N'奧本海默 IMAX 觀後感', 10),
 (2,  NULL, @cVieshow,1, 1, N'信義威秀座位舒適度', 5),
 (3,  NULL, @cAsia,   1, 1, N'寄生上流這部片', 2),
 -- 主題 3 的回文 / 留言
 (4,  3, @cAsia, 1, 2, N'台北電影節心得', 8),
 (5,  3, @cAsia, 1, 3, N'復仇者聯盟重映', 7),
 (6,  3, @cAsia, 2, 2, N'小丑電影分析', 6),
 (7,  3, @cAsia, 2, 3, N'威秀爆米花口味', 4),
 -- 留言 7 的回覆
 (8,  7, @cAsia, 2, 4, N'黑暗騎士三部曲', 3),
 (9,  7, @cAsia, 3, 4, N'歌劇魅影舞台劇', 12),
 (10, 7, @cAsia, 3, 4, N'Netflix 新片推薦', 15),
 (11, 7, @cAsia, 3, 4, N'動畫電影心得', 20),
 (12, 7, @cAsia, 3, 4, N'宮崎駿作品回顧', 25),
 (13, 7, @cAsia, 3, 4, N'台中大遠百影城', 30),
 (14, 7, @cAsia, 3, 4, N'蜘蛛人新宇宙', 35),
 (15, 7, @cAsia, 3, 4, N'音樂劇觀後感', 40),
 (16, 7, @cAsia, 3, 4, N'驚悚片推薦', 45),
 (17, 7, @cAsia, 1, 4, N'浪漫愛情片心得', 50),
 (18, 3, @cAsia, 1, 2, N'紀錄片觀後感', 55),
 (19, 3, @cAsia, 2, 3, N'科幻片心得', 60),
 (20, 3, @cAsia, 1, 2, N'經典老片回顧', 65),
 (21, 3, @cAsia, 2, 3, N'《沙丘：第二部》', 25),
 (22, 3, @cAsia, 3, 2, N'《奧本海默》', 18),
 (23, 3, @cAsia, 1, 3, N'《芭比》', 12),
 (24, 3, @cAsia, 2, 2, N'《蜘蛛人：穿越新宇宙》', 30),
 (25, 3, @cAsia, 4, 3, N'《玩命關頭X》', 7),
 (26, 3, @cAsia, 1, 2, N'《瞬息全宇宙》', 15),
 (27, 3, @cAsia, 2, 3, N'《捍衛戰士：獨行俠》', 22),
 (28, 3, @cAsia, 3, 2, N'《可憐的東西》', 9),
 (29, 3, @cAsia, 1, 3, N'《首爾之春》', 28),
 (30, 3, @cAsia, 2, 2, N'《奇蹟男孩》', 11),
 (31, 3, @cAsia, 4, 3, N'《移工怨曲》', 19),
 (32, 3, @cAsia, 1, 2, N'《鈴芽之旅》', 14),
 (33, 3, @cAsia, 2, 3, N'《教宗的繼承》', 21),
 (34, 3, @cAsia, 3, 2, N'《滿江紅》', 8),
 (35, 3, @cAsia, 1, 3, N'《坂本龍一：Opus》', 16),
 (36, 3, @cAsia, 2, 2, N'《惡女》', 13),
 (37, 3, @cAsia, 4, 3, N'《花月殺手》', 26),
 (38, 3, @cAsia, 1, 2, N'《周處除三害》', 17),
 (39, 3, @cAsia, 2, 3, N'《法拉利》', 20),
 (40, 3, @cAsia, 3, 2, N'《金孫爆滿》', 24),
 (41, 3, @cAsia, 1, 3, N'《碧血狂殺》', 6),
 (42, 3, @cAsia, 2, 2, N'《首映夜》', 29),
 (43, 3, @cAsia, 3, 3, N'《機密同盟》', 23),
 (44, 3, @cAsia, 1, 2, N'《老狐狸》', 27),
 -- 另一個主題
 (45, NULL, @cAsia, 2, 1, N'《沙贊！眾神之怒》', 31),
 -- 主題 3 與 2 的回覆、留言的回覆
 (46, 3, @cAsia,    2, 2, N'Re: 奧本海默', 9),
 (47, 3, @cAsia,    3, 3, N'Re: 奧本海默', 8),
 (48, 3, @cAsia,    2, 3, N'Re: 信義威秀', 4),
 (49, 2, @cVieshow, 3, 2, N'Re: 信義威秀座位舒適度', 3),
 (50, 47, @cAsia,   2, 4, N'星際效應', 3)
) AS v (id, parent_id, cat, user_id, type, title, hrs);
SET IDENTITY_INSERT content OFF;

-- 4-3. 其餘主題文章 51～246
;WITH n AS (SELECT TOP (196) 50 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO content (parent_content_id, category_id, user_id, title, body, type, created_at, updated_at, is_active)
SELECT NULL,
       CHOOSE(1 + (i % 4), @cEU, @cAsia, @cVieshow, @cAmuz),
       1 + (i % 50),
       CONCAT(N'電影討論文章 #', i),
       CONCAT('/contents/content-', 1 + (i % 30), '.html'),
       1, DATEADD(HOUR, -(i * 2), GETDATE()), DATEADD(HOUR, -(i * 2), GETDATE()), 1
FROM n;

-- 4-4. 互動（1 按讚、3 收藏、4 檢舉；分類 ID 見 seed.sql）
INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
SELECT 1, 3, u.user_id, DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY u.user_id), GETDATE()), GETDATE()
FROM [user] u WHERE u.user_id BETWEEN 2 AND 51 AND u.user_id <> 13;

INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
SELECT 1, 4, u.user_id, DATEADD(MINUTE, -(49 + ROW_NUMBER() OVER (ORDER BY u.user_id)), GETDATE()), GETDATE()
FROM [user] u
WHERE u.user_id <= 9 OR u.user_id IN (11, 14, 51) OR (u.user_id % 2 = 0 AND u.user_id BETWEEN 16 AND 50);

INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
SELECT 1, 5, u.user_id, DATEADD(MINUTE, -(79 + ROW_NUMBER() OVER (ORDER BY u.user_id)), GETDATE()), GETDATE()
FROM [user] u WHERE u.user_id BETWEEN 1 AND 21 AND u.user_id <> 13;

INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
SELECT 1, 6, u.user_id, DATEADD(MINUTE, -(99 + ROW_NUMBER() OVER (ORDER BY u.user_id)), GETDATE()), GETDATE()
FROM [user] u WHERE u.user_id BETWEEN 22 AND 36;

INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
SELECT 3, v.content_id, v.user_id, DATEADD(MINUTE, -(114 + ROW_NUMBER() OVER (ORDER BY v.content_id, v.user_id)), GETDATE()), GETDATE()
FROM (VALUES (3,2),(3,4),(3,6),(3,8),(3,10),(4,12),(4,14),(4,16),(5,18),(5,20),(5,22),
             (6,24),(6,26),(7,28),(7,30),(8,32),(8,34),(9,36)) AS v (content_id, user_id);

-- 其他互動（含少量檢舉，後台「檢舉清單」可以看到）
;WITH n AS (SELECT TOP (20) 5 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
SELECT 1 + (i % 4), 1 + (i % 49), 1 + (i % 50), GETDATE(), GETDATE()
FROM n
WHERE NOT EXISTS (SELECT 1 FROM content_interaction ci
                  WHERE ci.category_id = 1 + (i % 4) AND ci.content_id = 1 + (i % 49) AND ci.user_id = 1 + (i % 50));

-- 4-5. 修改紀錄
INSERT INTO content_history (content_id, user_id, old_title, old_body, created_at, updated_at) VALUES
(1, 1, N'奧本海默觀後感', N'/contents/history-1.html', DATEADD(HOUR, -10, GETDATE()), GETDATE()),
(2, 1, N'威秀心得',       N'/contents/history-2.html', DATEADD(HOUR, -5, GETDATE()),  GETDATE());

-- =============================================================================
-- 5. 平台任務與進度（seed.sql 已有 5 個基本任務，這裡補充範例；以名稱比對避免重複）
-- =============================================================================
INSERT INTO mission
    (mission_name, mission_detail, target_type, target_count, reward_points, reward_exp, category, difficulty,
     estimated_duration, reset_type, is_featured, is_active)
SELECT v.*, 1
FROM (VALUES
 (N'初出茅廬', N'完成個人檔案的所有資料填寫。', N'profile', 1, 50, 50, N'成就', N'簡單', N'一次性', N'none', 0),
 (N'每天簽到', N'每日登入平台即可獲得獎勵', N'login', 1, 10, 5, N'基礎', N'簡單', N'每日', N'daily', 0),
 (N'每日發表一篇 200 字以上文章', N'分享你的想法或創作', N'post', 1, 30, 15, N'內容創作', N'簡單', N'每日', N'daily', 0),
 (N'每日對不同文章點讚 10 次', N'多多支持優質內容', N'like', 10, 25, 12, N'社群互動', N'中等', N'每日', N'daily', 0),
 (N'每日留言 10 次', N'和大家交流心得', N'comment', 10, 25, 12, N'社群互動', N'中等', N'每日', N'daily', 0),
 (N'每日完成 5 個平台任務', N'多多參與平台活動', N'mission_complete', 5, 50, 30, N'平台任務', N'中等', N'每日', N'daily', 0),
 (N'每日結案 10 個委託任務', N'幫助他人解決問題', N'commission_complete', 10, 80, 50, N'委託任務', N'高難度', N'每日', N'daily', 0),
 (N'每週購買商品 1 次', N'本週內完成至少一次商品購買即可獲得獎勵', N'purchase', 1, 50, 20, N'購物', N'簡單', N'7 天', N'weekly', 0),
 (N'社群傳播者', N'本週累計分享內容到外部社群 5 次。', N'share', 5, 100, 50, N'週常', N'簡單', N'7 天', N'weekly', 0),
 (N'百萬瀏覽', N'你的影評累計獲得超過 1,000 次觀看。', N'view_gain', 1000, 1000, 500, N'成就', N'困難', N'不限', N'none', 1),
 (N'金牌委託人', N'成功發布並結案 10 個委託任務。', N'entrust_publish', 10, 500, 250, N'成就', N'中等', N'不限', N'none', 0),
 (N'糾察隊隊長', N'檢舉 5 則確實違規的內容。', N'report_success', 5, 200, 100, N'成就', N'中等', N'不限', N'none', 0),
 (N'年度觀影王', N'一年之內在系統標記 100 部「已看過」的電影。', N'watch_count', 100, 2000, 1000, N'成就', N'困難', N'1 年', N'yearly', 1)
) AS v (mission_name, mission_detail, target_type, target_count, reward_points, reward_exp, category, difficulty,
        estimated_duration, reset_type, is_featured)
WHERE NOT EXISTS (SELECT 1 FROM mission m WHERE m.mission_name = v.mission_name);

-- 進度：一次性任務用固定週期起點（見 MissionResetTypes.OneTimeCycleStart），週期任務用今天
INSERT INTO mission_progress
    (user_id, mission_id, current_count, status, accepted_at, completed_at, rewarded_at, cycle_start_at, cycle_end_at)
SELECT v.user_id, m.mission_id, v.cnt, v.status,
       DATEADD(DAY, v.accepted_days, GETDATE()),
       CASE WHEN v.status IN (N'completed', N'rewarded') THEN GETDATE() END,
       CASE WHEN v.status = N'rewarded' THEN GETDATE() END,
       CASE WHEN m.reset_type IN (N'daily', N'weekly', N'monthly', N'yearly') THEN CAST(GETDATE() AS date) ELSE '2000-01-01' END,
       CASE m.reset_type WHEN N'daily' THEN DATEADD(DAY, 1, CAST(GETDATE() AS date))
                         WHEN N'weekly' THEN DATEADD(DAY, 7, CAST(GETDATE() AS date)) END
FROM (VALUES
 (1, N'初出茅廬',       1,  N'rewarded', -1),
 (1, N'每日留言 10 次', 10, N'completed', -30),
 (2, N'每日留言 10 次', 10, N'completed', -30),
 (3, N'每天簽到',       1,  N'rewarded', -2),
 (4, N'每日完成 5 個平台任務', 1, N'ongoing', -3),
 (5, N'每週購買商品 1 次',     1, N'completed', -7)
) AS v (user_id, mission_name, cnt, status, accepted_days)
JOIN mission m ON m.mission_name = v.mission_name;

-- =============================================================================
-- 6. 委託任務與預扣點數（content_id 指向該會員自己發的主題文章）
-- =============================================================================
INSERT INTO entrust_mission
    (user_id, content_id, title, detail, reward_points, category, deadline, status, is_urgent, is_active,
     created_at, updated_at, best_content_id, best_user_id, selected_at)
SELECT v.user_id,
       COALESCE(v.content_id, (SELECT c.content_id FROM content c
                               WHERE c.user_id = v.user_id AND c.type = 1 AND c.parent_content_id IS NULL AND c.content_id > 50
                               ORDER BY c.content_id OFFSET v.k ROWS FETCH NEXT 1 ROWS ONLY)),
       v.title, v.detail, v.points, v.category, DATEADD(DAY, 14, GETDATE()), v.status, v.urgent, 1,
       GETDATE(), GETDATE(),
       CASE WHEN v.status = N'completed' THEN 49 END,
       CASE WHEN v.status = N'completed' THEN 3 END,
       CASE WHEN v.status = N'completed' THEN GETDATE() END
FROM (VALUES
 (1, 1,    0, N'《復仇者聯盟》字幕校對', N'協助校對繁體中文字幕', 120, N'翻譯', N'open', 0),
 (1, 2,    0, N'電影海報風格二改', N'將現有海報改成極簡主義風格', 300, N'設計', N'completed', 0),
 (1, 3,    0, N'求電影原著翻譯對照', N'需要對照專有名詞譯名', 150, N'翻譯', N'open', 0),
 (1, NULL, 0, N'協助剪輯 30 秒預告', N'素材已準備好，需轉場特效', 500, N'影音', N'open', 0),
 (1, NULL, 1, N'尋找某部老國片的片名', N'描述：主角穿紅衣，結局在雨中', 50, N'諮詢', N'open', 0),
 (2, NULL, 0, N'IMDb 數據抓取腳本', N'需要抓取特定導演的歷年評分', 800, N'技術', N'open', 0),
 (2, NULL, 1, N'英文影評潤稿', N'已寫好草稿，需母語等級校對', 200, N'翻譯', N'open', 0),
 (2, NULL, 2, N'戲院設備調研', N'調查戲院 IMAX 銀幕尺寸', 120, N'資料', N'open', 0),
 (3, NULL, 0, N'【急】徵求首映會代排', N'明天下午在西門町', 1000, N'跑腿', N'cancelled', 1),
 (3, NULL, 1, N'影評插畫繪製', N'為我的專欄畫一張插圖', 450, N'設計', N'open', 0)
) AS v (user_id, content_id, k, title, detail, points, category, status, urgent);

INSERT INTO entrust_mission_point_reserve (user_id, entrust_mission_id, reserved_points, status, created_at, released_at)
SELECT user_id, entrust_mission_id, reward_points,
       CASE status WHEN N'open' THEN N'reserved' WHEN N'completed' THEN N'consumed' ELSE N'released' END,
       created_at,
       CASE WHEN status <> N'open' THEN GETDATE() END
FROM entrust_mission;

-- =============================================================================
-- 7. 點數儲值 point_recharge（點數 = 金額 × 10，見 PointRechargeService.PointsPerCash）
-- =============================================================================
INSERT INTO point_recharge (user_id, order_no, cash_amount, points_granted, payment_method, status, created_at)
SELECT v.user_id, v.order_no, v.cash, v.cash * 10, v.method, N'success', GETDATE()
FROM (VALUES
 (1, 'PR483920174650298371', 100, N'credit'), (2, 'PR920174658302947561', 50,  N'cvs'),
 (3, 'PR374650298371920184', 30,  N'credit'), (4, 'PR658302947561920473', 200, N'cvs'),
 (5, 'PR120394857620394857', 150, N'credit'), (6, 'PR857620394857120394', 80,  N'cvs'),
 (7, 'PR304958176203948576', 60,  N'credit'), (8, 'PR948576203049581762', 120, N'cvs'),
 (9, 'PR762039485761203948', 40,  N'cvs'),    (10,'PR581762039485761203', 300, N'credit')
) AS v (user_id, order_no, cash, method);

;WITH n AS (SELECT TOP (40) 10 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO point_recharge (user_id, order_no, cash_amount, points_granted, payment_method, status, created_at)
SELECT 1 + (i % 50),
       CONCAT('PR', RIGHT(REPLICATE('0', 18) + CAST(ABS(CHECKSUM(NEWID())) AS varchar(18)), 18)),
       50.00 + (i % 10) * 50, (50 + (i % 10) * 50) * 10,
       CASE WHEN i % 2 = 0 THEN N'credit' ELSE N'cvs' END, N'success', DATEADD(DAY, -(i % 60), GETDATE())
FROM n;

-- =============================================================================
-- 8. 點數紀錄 point_log（帳本；balance_after 在最後重新計算，確保每位會員依時間連續）
--    point_type_definition_id：1 任務、2 委託收入、3 儲值、6 委託預扣、7 商城消費、8 系統調整
-- =============================================================================
INSERT INTO point_log
    (user_id, source_type, source_id, display_source, display_source_sub_title,
     point_type_definition_id, change_amount, balance_after, status, created_at)
SELECT user_id, source_type, source_id, display_source, sub_title, type_id, amount, 0, N'success', created_at
FROM (VALUES
-- User 1
(1, N'point_recharge', 1, N'線上儲值', N'信用卡支付', 3, 2000, '2026-02-05 21:00:00'),
(1, N'entrust_mission_point_reserve', 1, N'委託任務預扣', N'來自 委託 #1 - 《復仇者聯盟》字幕校對', 6, -120, '2026-02-06 10:00:00'),
(1, N'mission', 7, N'任務：每日簽到', N'連續登入第 3 天', 1, 13, '2026-02-11 08:00:00'),
(1, N'mission', 8, N'內容創作獎勵', N'文章《奧本海默》獲得精選', 1, 157, '2026-02-11 12:30:00'),
(1, N'order', 601, N'商城購買', N'購買數位收藏品：復古放映機', 7, -499, '2026-02-12 15:45:00'),
(1, N'entrust_mission_point_reserve', 1, N'委託任務預扣', N'來自 委託 #1 額外支出', 6, -122, '2026-02-13 09:00:00'),
(1, N'mission', 9, N'社群互動', N'累計點讚 50 次', 1, 31, '2026-02-13 22:00:00'),
(1, N'system', NULL, N'補償點數', N'伺服器維護補償', 8, 88, '2026-02-14 10:00:00'),
(1, N'mission', 7, N'任務：每日簽到', N'連續登入第 4 天', 1, 14, '2026-02-14 11:00:00'),
(1, N'order', 602, N'商城購買', N'購買論壇頭像框', 7, -250, '2026-02-15 18:20:00'),
(1, N'mission', 10, N'任務：推廣分享', N'分享影評至社群平台', 1, 47, '2026-02-16 09:15:00'),
(1, N'entrust_mission', 42, N'委託任務收入', N'協助解決標籤分類問題', 2, 213, '2026-02-16 23:00:00'),
(1, N'mission', 7, N'任務：每日簽到', N'連續登入第 5 天', 1, 15, '2026-02-18 08:10:00'),
(1, N'mission', 9, N'社群互動', N'評論被點讚 20 次', 1, 19, '2026-02-18 14:20:00'),
(1, N'order', 610, N'商城購買', N'電影主題貼圖包', 7, -88, '2026-02-19 19:30:00'),
(1, N'mission', 8, N'內容創作獎勵', N'影評《沙丘：第二部》進入熱門', 1, 221, '2026-02-20 11:00:00'),
(1, N'mission', 7, N'任務：每日簽到', N'連續登入第 6 天', 1, 16, '2026-02-21 07:50:00'),
(1, N'entrust_mission', 55, N'委託任務收入', N'幫忙翻譯短片字幕', 2, 452, '2026-02-21 21:00:00'),
(1, N'order', 611, N'商城購買', N'限定版數位收藏卡', 7, -345, '2026-02-22 15:00:00'),
(1, N'mission', 16, N'週年活動', N'參與問答小遊戲', 1, 77, '2026-02-23 13:00:00'),
-- User 2
(2, N'mission', 7, N'任務：每日簽到', N'每日登入平台即可獲得獎勵', 1, 10, '2026-02-07 10:00:00'),
(2, N'mission', 7, N'任務：每日簽到', N'連續登入第 1 天', 1, 11, '2026-02-11 07:30:00'),
(2, N'point_recharge', 10, N'線上儲值', N'超商繳費', 3, 1500, '2026-02-12 14:00:00'),
(2, N'entrust_mission_point_reserve', 6, N'委託任務預扣', N'來自 委託 #6', 6, -789, '2026-02-12 15:00:00'),
(2, N'mission', 8, N'內容創作獎勵', N'《小丑》電影分析獲得熱烈討論', 1, 266, '2026-02-13 16:00:00'),
(2, N'mission', 9, N'社群互動', N'獲得 10 個回覆', 1, 23, '2026-02-14 08:00:00'),
(2, N'order', 603, N'商城購買', N'電影早場優惠券', 7, -199, '2026-02-14 12:00:00'),
(2, N'mission', 7, N'任務：每日簽到', N'連續登入第 2 天', 1, 12, '2026-02-15 07:00:00'),
(2, N'system', NULL, N'節慶紅包', N'元宵節活動贈獎', 8, 168, '2026-02-15 10:00:00'),
(2, N'mission', 11, N'觀影成就', N'標記 10 部科幻片', 1, 55, '2026-02-16 20:00:00'),
(2, N'order', 604, N'商城購買', N'數位海報組', 7, -333, '2026-02-17 14:30:00'),
(2, N'mission', 7, N'任務：每日簽到', N'連續登入第 3 天', 1, 13, '2026-02-18 09:00:00'),
(2, N'mission', 9, N'社群互動', N'文章被分享 5 次', 1, 62, '2026-02-18 16:45:00'),
(2, N'entrust_mission_point_reserve', 7, N'委託任務預扣', N'來自 委託 #7', 6, -211, '2026-02-19 11:20:00'),
(2, N'mission', 8, N'內容創作獎勵', N'《沙贊》觀後感發表', 1, 41, '2026-02-20 09:30:00'),
(2, N'point_recharge', 13, N'線上儲值', N'LINE Pay', 3, 555, '2026-02-21 14:00:00'),
(2, N'order', 612, N'商城購買', N'VIP 會員月費 (折價)', 7, -599, '2026-02-21 14:10:00'),
(2, N'mission', 7, N'任務：每日簽到', N'連續登入第 4 天', 1, 14, '2026-02-22 08:30:00'),
(2, N'mission', 10, N'推廣分享', N'邀請新用戶註冊', 1, 188, '2026-02-23 11:00:00'),
(2, N'system', NULL, N'系統紅包', N'春季觀影禮', 8, 222, '2026-02-25 09:00:00'),
-- User 3
(3, N'system', NULL, N'系統給予', N'2026 新年快樂', 8, 1000, '2026-02-05 09:00:00'),
(3, N'order', 501, N'商城購買', N'訂單編號 #501', 7, -800, '2026-02-08 20:00:00'),
(3, N'entrust_mission', 2, N'委託任務收入', N'來自 @User1 - 電影海報風格二改', 2, 300, '2026-02-09 21:00:00'),
(3, N'mission', 7, N'任務：每日簽到', N'連續登入第 5 天', 1, 25, '2026-02-11 09:00:00'),
(3, N'mission', 12, N'糾察隊獎勵', N'成功舉報違規內容', 1, 37, '2026-02-11 14:00:00'),
(3, N'entrust_mission', 2, N'委託任務收入', N'來自 @User1 - 海報二改餘款', 2, 297, '2026-02-12 21:00:00'),
(3, N'order', 605, N'商城購買', N'獨家限定徽章', 7, -488, '2026-02-13 15:00:00'),
(3, N'mission', 9, N'社群互動', N'文章被收藏超過 50 次', 1, 89, '2026-02-14 11:00:00'),
(3, N'mission', 7, N'任務：每日簽到', N'連續登入第 6 天', 1, 27, '2026-02-15 09:00:00'),
(3, N'system', NULL, N'問卷回饋', N'填寫用戶體驗調查', 8, 52, '2026-02-15 16:00:00'),
(3, N'point_recharge', 11, N'線上儲值', N'Apple Pay', 3, 3000, '2026-02-16 12:30:00'),
(3, N'entrust_mission_point_reserve', 9, N'委託任務預扣', N'來自 委託 #9', 6, -500, '2026-02-16 13:00:00'),
(3, N'mission', 8, N'內容創作獎勵', N'《金孫爆滿》影評受好評', 1, 141, '2026-02-17 10:00:00'),
(3, N'mission', 7, N'任務：每日簽到', N'連續登入第 7 天', 1, 55, '2026-02-18 07:10:00'),
(3, N'order', 613, N'商城購買', N'數位電影百科全書', 7, -678, '2026-02-19 15:20:00'),
(3, N'mission', 17, N'知識達人', N'電影考題全對', 1, 89, '2026-02-20 20:00:00'),
(3, N'mission', 9, N'社群互動', N'累計獲得 100 點讚', 1, 112, '2026-02-21 18:00:00'),
(3, N'entrust_mission_point_reserve', 10, N'委託任務預扣', N'來自 委託 #10', 6, -450, '2026-02-23 09:30:00'),
(3, N'order', 614, N'商城購買', N'論壇彩色暱稱', 7, -150, '2026-02-24 14:00:00'),
(3, N'system', NULL, N'補償點數', N'APP Bug 修復感謝', 8, 66, '2026-02-25 11:00:00'),
-- User 4
(4, N'system', NULL, N'系統給予', N'參與電影社群活動獎勵', 8, 150, '2026-02-10 19:00:00'),
(4, N'mission', 7, N'任務：每日簽到', N'歡迎新人簽到', 1, 50, '2026-02-11 10:00:00'),
(4, N'mission', 13, N'完善資料', N'填寫完整個人興趣', 1, 88, '2026-02-11 11:00:00'),
(4, N'entrust_mission', 6, N'委託任務收入', N'來自 @User2 - 數據抓取', 2, 792, '2026-02-12 20:00:00'),
(4, N'order', 606, N'商城購買', N'特製評論框', 7, -125, '2026-02-13 14:00:00'),
(4, N'mission', 9, N'社群互動', N'發表 5 則有意義的回覆', 1, 19, '2026-02-14 09:00:00'),
(4, N'mission', 7, N'任務：每日簽到', N'連續登入第 2 天', 1, 12, '2026-02-15 09:30:00'),
(4, N'mission', 10, N'任務：推廣分享', N'邀請好友加入平台', 1, 166, '2026-02-16 11:00:00'),
(4, N'order', 607, N'商城購買', N'電影專題電子雜誌', 7, -99, '2026-02-16 17:00:00'),
(4, N'mission', 8, N'內容創作獎勵', N'首篇短評發表', 1, 33, '2026-02-17 08:00:00'),
(4, N'system', NULL, N'除錯獎勵', N'回報網頁排版問題', 8, 256, '2026-02-17 15:00:00'),
(4, N'mission', 7, N'任務：每日簽到', N'連續登入第 3 天', 1, 13, '2026-02-18 10:30:00'),
(4, N'mission', 8, N'內容創作獎勵', N'《可憐的東西》短評', 1, 28, '2026-02-19 12:00:00'),
(4, N'order', 615, N'商城購買', N'電影專訪特刊 (PDF)', 7, -45, '2026-02-19 16:00:00'),
(4, N'mission', 19, N'觀影成就', N'完成 5 部驚悚片標記', 1, 67, '2026-02-20 22:00:00'),
(4, N'entrust_mission', 66, N'委託任務收入', N'提供老片片源線索', 2, 105, '2026-02-22 13:00:00'),
(4, N'mission', 9, N'社群互動', N'獲得 20 個留言', 1, 34, '2026-02-23 17:00:00'),
(4, N'order', 616, N'商城購買', N'個人頁面背景：IMAX 廳', 7, -188, '2026-02-24 19:00:00'),
(4, N'mission', 7, N'任務：每日簽到', N'連續登入第 5 天', 1, 15, '2026-02-25 08:30:00'),
(4, N'system', NULL, N'新手回饋', N'首月滿月禮', 8, 300, '2026-02-25 12:00:00'),
-- User 5
(5, N'mission', 7, N'任務：每日簽到', N'登入領取', 1, 10, '2026-02-11 08:30:00'),
(5, N'mission', 14, N'觀影成就', N'收藏 100 部電影至願望清單', 1, 122, '2026-02-12 10:00:00'),
(5, N'order', 608, N'商城購買', N'電影票手繪風格貼紙', 7, -77, '2026-02-13 13:00:00'),
(5, N'mission', 9, N'社群互動', N'文章獲得 50 個點讚', 1, 58, '2026-02-14 16:00:00'),
(5, N'mission', 7, N'任務：每日簽到', N'連續登入第 2 天', 1, 12, '2026-02-15 08:30:00'),
(5, N'point_recharge', 12, N'線上儲值', N'Google Pay', 3, 500, '2026-02-15 20:00:00'),
(5, N'mission', 15, N'電影達人', N'參與電影知識問答全對', 1, 333, '2026-02-16 19:00:00'),
(5, N'order', 609, N'商城購買', N'限定實體酷卡兌換券', 7, -666, '2026-02-17 11:00:00'),
(5, N'mission', 8, N'內容創作獎勵', N'發表深度解析文章', 1, 189, '2026-02-17 21:00:00'),
(5, N'system', NULL, N'活動獎勵', N'參與春季觀影馬拉松', 8, 444, '2026-02-18 10:00:00'),
(5, N'mission', 7, N'任務：每日簽到', N'連續登入第 2 天', 1, 12, '2026-02-19 08:00:00'),
(5, N'mission', 20, N'社群成就', N'追蹤超過 10 位影評人', 1, 44, '2026-02-20 14:00:00'),
(5, N'order', 617, N'商城購買', N'電影首映會直播門票', 7, -399, '2026-02-20 20:00:00'),
(5, N'mission', 7, N'任務：每日簽到', N'連續登入第 3 天', 1, 13, '2026-02-21 08:00:00'),
(5, N'mission', 8, N'內容創作獎勵', N'發布《周處除三害》解析', 1, 172, '2026-02-22 11:30:00'),
(5, N'mission', 9, N'社群互動', N'回覆被標記為最佳解答', 1, 95, '2026-02-23 15:00:00'),
(5, N'order', 618, N'商城購買', N'虛擬周邊：芭比粉字體', 7, -50, '2026-02-24 18:00:00'),
(5, N'mission', 7, N'任務：每日簽到', N'連續登入第 4 天', 1, 14, '2026-02-25 08:00:00'),
(5, N'point_recharge', 14, N'線上儲值', N'街口支付', 3, 1000, '2026-02-25 10:30:00'),
(5, N'system', NULL, N'驚喜獎勵', N'隨機幸運掉落', 8, 7, '2026-02-25 11:30:00')
) AS v (user_id, source_type, source_id, display_source, sub_title, type_id, amount, created_at);

-- 其他會員的少量點數收入（只有收入，避免餘額變負）
;WITH n AS (SELECT TOP (36) 9 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO point_log
    (user_id, source_type, source_id, display_source, display_source_sub_title,
     point_type_definition_id, change_amount, balance_after, status, created_at)
SELECT 6 + (i % 45), N'mission', i, N'任務獎勵', N'完成平台任務', 1, 20 + (i % 5) * 10, 0, N'success', DATEADD(DAY, -(i % 60), GETDATE())
FROM n;

-- 重新計算每位會員的餘額（依時間、ID 累加），讓帳本連續
;WITH r AS (
    SELECT point_log_id,
           SUM(change_amount) OVER (PARTITION BY user_id ORDER BY created_at, point_log_id ROWS UNBOUNDED PRECEDING) AS bal
    FROM point_log)
UPDATE p SET balance_after = r.bal FROM point_log p JOIN r ON r.point_log_id = p.point_log_id;

-- =============================================================================
-- 9. 通知 notifications
-- =============================================================================
INSERT INTO notifications (target_user_id, title, content, category, related_id, link_url, is_read) VALUES
(1, N'點數預扣通知', N'您發布了委託，已暫時預扣 500 點。', N'system', N'101', N'/wallet/log', 1),
(2, N'恭喜完成任務', N'資深影癡任務已達成！請前往領取 500 點獎勵。', N'mission', N'5', N'/mission/center', 0),
(4, N'委託酬勞入帳', N'您完成的【電影海報風格二改】酬勞 300 點已發放。', N'entrust_mission', N'201', N'/wallet/log', 0),
(5, N'儲值失敗通知', N'訂單 ORD005 授權失敗，請檢查您的支付帳戶。', N'point_recharge', N'ORD005', N'/wallet/recharge', 0),
(6, N'補償通知', N'由於昨日伺服器連線異常，系統補償 150 點，請查收。', N'system', NULL, N'/wallet/log', 0);

;WITH n AS (SELECT TOP (20) 5 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO notifications (target_user_id, title, content, category, related_id, link_url, is_read)
SELECT 1 + (i % 50), CONCAT(N'系統通知 #', i), CONCAT(N'這是第 ', i, N' 則系統通知訊息'),
       CASE (i % 4) WHEN 0 THEN N'system' WHEN 1 THEN N'mission' WHEN 2 THEN N'order' ELSE N'entrust_mission' END,
       CAST(i AS nvarchar(20)), CONCAT('/notification/', i), CASE WHEN i % 3 = 0 THEN 1 ELSE 0 END
FROM n;

-- =============================================================================
-- 10. 點數商城：商品、訂單、明細、寄送
-- =============================================================================
INSERT INTO product (name, series, description, image_name, type, is_physical, point, stock, created_at, updated_at, on_sale)
SELECT v.name, v.series, v.descr, v.img, v.type, 1, v.point, v.stock, v.created, v.created, 1
FROM (VALUES
 (N'電影兌換卷*1', N'票券', N'全台通用電影兌換券，不限平假日皆可使用。適用於首輪電影，提供最震撼的感官體驗，是下班休閒或週末約會的最佳選擇。', '2026010611432290af.jpg', N'兌換票券', 2000, 200, '2026-01-06 11:43:22'),
 (N'電影兌換卷*4', N'票券', N'超值四入組合包！適合全家大小或是好友團體同樂，一次兌換四張更划算。為您省下更多點數，盡情享受年度強檔大片。', '20260106114404ab10.jpg', N'兌換票券', 7500, 20, '2026-01-06 11:44:04'),
 (N'哈利波特魔杖(款一)', N'哈利波特', N'哈利波特系列經典巫師魔杖收藏品。精準還原電影比例，細膩的手感紋理彷彿帶你走進斜角巷，隨附精美收藏盒，哈迷絕對不能錯過。', '202601061148366c60.webp', N'電影周邊', 1500, 300, '2026-01-06 11:48:36'),
 (N'哈利波特魔杖(款二)', N'哈利波特', N'此款魔杖擁有獨特的雕刻工藝與復古塗裝，展現優雅且神秘的魔法氣息。材質堅固且細節豐富，適合作為居家擺飾或角色扮演道具。', '202601061149141861.webp', N'電影周邊', 1500, 200, '2026-01-06 11:49:14'),
 (N'哈利波特魔杖(隨機)', N'哈利波特', N'驚喜盲盒款！內含一款隨機款式的巫師魔杖，讓命運決定你專屬的守護器物。無論是作為交換禮物還是給自己的小驚喜都非常適合。', '202601061150266977.webp', N'電影周邊', 1450, 100, '2026-01-06 11:50:26'),
 (N'星際大戰黑武士T-shirt', N'星際大戰', N'星際大戰經典黑武士(Darth Vader)圖樣，採用100%優質純棉打造，透氣乾爽。經典不敗的高質感印花，讓原力隨時與你的日常穿搭同在。', '20260106115244530b.jpg', N'電影周邊', 2500, 30, '2026-01-06 11:52:44'),
 (N'星際大戰紀念胸章', N'星際大戰', N'全球限量金屬紀念胸章，採用立體浮雕設計，質感厚實。適合作為裝飾別於包袋或外套，是星際大戰資深粉絲值得收藏的精緻小物。', '20260106115633af93.webp', N'電影周邊', 2500, 60, '2026-01-06 11:56:33'),
 (N'星際大戰光劍吊飾', N'星際大戰', N'迷你版光劍造型掛飾，還原絕地武士的力量象徵。體積輕巧且具備金屬光澤，可輕鬆掛於鑰匙或後背包，隨時展現你的科幻情懷。', '20260106115713bff2.webp', N'電影周邊', 2000, 40, '2026-01-06 11:57:13'),
 (N'星際大戰R2D2 LED鑰匙圈', N'星際大戰', N'獲官方授權的R2D2 LED造型鑰匙圈，不僅外型可愛，按下開關後腳底還會發出明亮光芒，結合實用性與趣味性，在暗處也能輕鬆找到鑰匙孔。', '20260121090841d60d.webp', N'電影周邊', 2000, 50, '2026-01-06 11:57:13'),
 (N'哈利波特霍格華茲卡片組合', N'哈利波特', N'霍格華茲魔法學校由4個各有不同創立宗旨的學院組成。這盒10卡＋10信封的組合，白色卡片上鑲有金箔的霍格華茲徽章圖案，紫色底繪以金色線條，清爽、高雅又氣派，哈利波特的粉絲一定要收藏喔！', '20260202093002a4e3.jpg', N'電影周邊', 2000, 50, '2026-02-02 09:30:02'),
 (N'哈利波特葛萊芬多卡片組合', N'哈利波特', N'葛來分多學院由高錐客·葛來分多創立，希望學員充滿勇敢、決心，代表顏色是紅色，吉祥物是獅子。這盒10卡＋10信封的組合，以紅色為底，繪以金色線條，高雅大方。', '20260202093118696b.webp', N'電影周邊', 2000, 40, '2026-02-02 09:31:18'),
 (N'哈利波特赫夫帕夫卡片組合', N'哈利波特', N'赫夫帕夫學院由海加‧赫夫帕夫創立，希望學生擁有勤勞、耐心、正義和公平的素質，代表顏色是黃色和黑色，吉祥物是貛。這盒10卡＋10信封的組合，以黃色為底，繪以金色線條。', '202602020932164f59.webp', N'電影周邊', 2000, 48, '2026-02-02 09:32:16'),
 (N'哈利波特霍格華茲校徽金屬胸針', N'哈利波特', N'官方授權收藏級胸針，精緻還原霍格華茲校徽，集結四大學院象徵動物。採用復古銅色工藝與立體浮雕技術，是所有哈迷入門必備的經典收藏。', '11f0eca4e50242ac110002.jpg', N'電影周邊', 1200, 100, '2026-02-11 11:30:00'),
 (N'哈利波特葛萊芬多學院金屬胸針', N'哈利波特', N'代表勇氣與騎士精神！這款葛萊芬多徽章以鮮紅琺瑯為底，襯托出金色的獅子圖騰。邊緣採歐式雕花裝飾，質感厚實。', '1173ec88620242ac110003.jpg', N'電影周邊', 1100, 80, '2026-02-11 11:31:00'),
 (N'哈利波特史萊哲林學院金屬胸針', N'哈利波特', N'專為展現野心與機智的巫師設計。徽章核心為靈動的銀色靈蛇，搭配深綠色閃粉琺瑯材質，在光線下呈現神祕的光澤。', '11d4ecbc9e0242ac110003.webp', N'電影周邊', 1100, 75, '2026-02-11 11:32:00'),
 (N'哈利波特四大學院徽章珍藏組', N'哈利波特', N'一次擁有四大學院的榮耀！內含葛萊芬多、史萊哲林、雷文克勞、赫夫帕夫四款獨立徽章，金屬手感紮實，適合套裝收藏或與好友分享。', '113dec88620242ac110003.webp', N'電影周邊', 3800, 30, '2026-02-11 11:33:00'),
 (N'哈利波特霍格華茲魔法胸針(展示卡裝)', N'哈利波特', N'除了精美的立體校徽胸針外，特別隨附哈利波特電影版主題展示底卡。精緻的包裝適合直接作為禮物送給熱愛魔法世界的朋友。', '11e4eca4e50242ac110002.webp', N'電影周邊', 1350, 50, '2026-02-11 11:34:00'),
 (N'哈利波特赫夫帕夫學院徽章鑰匙圈', N'哈利波特', N'赫夫帕夫學院徽章造型鑰匙圈，主色為黃黑配色並搭配獾的象徵圖騰。金屬質感立體浮雕，隨附珠鍊設計，可掛在鑰匙、背包或票卡套上。', '175403514730_1200.jpg', N'電影周邊', 900, 120, '2026-02-22 09:10:01'),
 (N'哈利波特葛萊芬多學院徽章鑰匙圈', N'哈利波特', N'葛來分多學院徽章鑰匙圈，採用紅金配色與獅子圖騰，呈現勇氣與榮耀。細節刻紋清晰，適合作為日常配件。', '175403514660_1200.jpg', N'電影周邊', 900, 120, '2026-02-22 09:12:15'),
 (N'哈利波特雷文克勞學院徽章鑰匙圈', N'哈利波特', N'雷文克勞學院徽章鑰匙圈，以藍銀色系搭配渡鴉圖騰，象徵智慧與創造力。徽章採立體設計並附珠鍊。', '175403514750_1200.jpg', N'電影周邊', 900, 120, '2026-02-22 09:13:40'),
 (N'哈利波特史萊哲林學院徽章鑰匙圈', N'哈利波特', N'史萊哲林學院徽章鑰匙圈，深綠配色搭配銀色靈蛇圖騰，展現沉著與野心。金屬光澤與雕刻細節讓質感更上一層。', '175403514711_1200.jpg', N'電影周邊', 900, 120, '2026-02-22 09:15:05'),
 (N'哈利波特霍格華茲校徽鑰匙圈', N'哈利波特', N'霍格華茲校徽造型鑰匙圈，集結四大學院代表元素並以彩色琺瑯呈現，外框採復古金色質感，附珠鍊可掛於包袋或鑰匙串上。', '17540351486_1200.jpg', N'電影周邊', 1300, 100, '2026-02-22 09:25:10'),
 (N'星際大戰安納金天行者光劍鑰匙圈', N'星際大戰', N'安納金·天行者光劍造型迷你鑰匙圈，細節刻紋還原經典握把比例，隨附珠鍊與 STAR WARS 吊牌。', 'P5429209805999_4_105049311.png', N'電影周邊', 1200, 80, '2026-02-22 09:20:10'),
 (N'星際大戰尤達光劍鑰匙圈', N'星際大戰', N'尤達(Yoda)光劍造型迷你鑰匙圈，小巧卻精緻，金屬質感與握把細節清晰，附珠鍊可輕鬆配戴。', 'P5429209805999_4_105049311.png', N'電影周邊', 1200, 90, '2026-02-22 09:21:05'),
 (N'星際大戰達斯維達光劍鑰匙圈', N'星際大戰', N'達斯·維達光劍造型迷你鑰匙圈，黑銀配色呈現黑武士的壓迫感，附珠鍊與 STAR WARS 吊牌。', 'P5429209805999_4_105049311.png', N'電影周邊', 1200, 70, '2026-02-22 09:22:00'),
 (N'星際大戰路克天行者光劍鑰匙圈', N'星際大戰', N'路克·天行者光劍造型迷你鑰匙圈，握把結構與層次感到位，搭配珠鍊與吊牌更具收藏感。', 'P5429209805999_4_105049311.png', N'電影周邊', 1200, 85, '2026-02-22 09:22:50'),
 (N'星際大戰凱羅忍光劍鑰匙圈', N'星際大戰', N'凱羅·忍十字護手光劍造型迷你鑰匙圈，黑紅風格突出，細節呈現側刃結構特色，附珠鍊方便吊掛。', 'P5429209805999_4_105049311.png', N'電影周邊', 1200, 75, '2026-02-22 09:23:40')
) AS v (name, series, descr, img, type, point, stock, created);

-- 訂單（ID 固定為 1～110）；status：C 已完成、S 已出貨、P 待處理、X 已取消
SET IDENTITY_INSERT orders ON;
INSERT INTO orders (order_id, user_id, total_point, order_date, remark, status, canceled_at)
SELECT v.id, v.user_id, 0,
       COALESCE(CAST(v.dt AS datetime), DATEADD(MINUTE, (v.id - 73) * 37, '2026-02-09 09:15:00')),
       COALESCE(v.remark, N''),
       CASE v.st WHEN 'C' THEN N'已完成' WHEN 'S' THEN N'已出貨' WHEN 'X' THEN N'已取消' ELSE N'待處理' END,
       NULL
FROM (VALUES
 (1,3,'2026-01-07 14:23','C',N'希望能盡快出貨'),(2,7,'2026-01-08 09:15','C',NULL),(3,12,'2026-01-08 16:47','S',N'請小心包裝'),
 (4,5,'2026-01-09 11:32','X',NULL),(5,15,'2026-01-10 13:58','C',NULL),(6,1,'2026-01-11 10:12','C',N'生日禮物，麻煩幫忙包裝精美一點'),
 (7,9,'2026-01-12 17:41','C',NULL),(8,18,'2026-01-13 08:27','S',NULL),(9,4,'2026-01-14 15:19','C',N'公司團購使用'),
 (10,11,'2026-01-15 12:05','X',NULL),(11,20,'2026-01-16 09:38','C',NULL),(12,6,'2026-01-17 14:52','S',NULL),
 (13,14,'2026-01-18 11:15','C',N'三款魔杖各一支'),(14,8,'2026-01-19 16:44','P',NULL),(15,2,'2026-01-20 10:29','S',NULL),
 (16,16,'2026-01-21 13:17','C',NULL),(17,10,'2026-01-22 15:36','P',NULL),(18,13,'2026-01-23 09:48','S',N'急件！請優先處理'),
 (19,19,'2026-01-24 14:23','C',NULL),(20,17,'2026-01-25 11:07','P',N'想要驚喜款'),(21,25,'2026-01-26 10:15','C',NULL),
 (22,32,'2026-01-26 14:30','C',N'請晚上配送'),(23,45,'2026-01-26 16:45','C',NULL),(24,12,'2026-01-27 09:20','C',NULL),
 (25,8,'2026-01-27 11:05','C',N'商品檢查'),(26,3,'2026-01-27 15:30','S',NULL),(27,16,'2026-01-28 10:12','C',NULL),
 (28,21,'2026-01-28 13:45','X',NULL),(29,39,'2026-01-28 17:20','C',N'送禮用'),(30,50,'2026-01-29 08:55','S',NULL),
 (31,15,'2026-01-29 11:40','C',NULL),(32,28,'2026-01-29 14:15','C',NULL),(33,7,'2026-01-29 16:50','S',NULL),
 (34,1,'2026-01-30 10:00','C',NULL),(35,42,'2026-01-30 12:30','C',N'量大請包裝好'),(36,19,'2026-01-30 15:10','P',NULL),
 (37,2,'2026-01-31 11:20','X',NULL),(38,34,'2026-01-31 14:55','C',NULL),(39,1,'2026-01-31 16:40','S',NULL),
 (40,11,'2026-02-01 09:15','C',NULL),(41,48,'2026-02-01 11:45','C',NULL),(42,23,'2026-02-01 14:20','S',NULL),
 (43,1,'2026-02-01 17:05','C',NULL),(44,1,'2026-02-02 08:30','C',NULL),(45,30,'2026-02-02 10:55','P',NULL),
 (46,2,'2026-02-02 13:40','C',NULL),(47,17,'2026-02-02 16:20','S',NULL),(48,2,'2026-02-03 09:50','C',NULL),
 (49,4,'2026-02-03 12:15','C',NULL),(50,37,'2026-02-03 14:55','X',NULL),(51,3,'2026-02-03 17:30','C',NULL),
 (52,26,'2026-02-04 09:20','S',NULL),(53,14,'2026-02-04 11:45','C',NULL),(54,1,'2026-02-04 14:10','C',NULL),
 (55,8,'2026-02-04 16:35','P',NULL),(56,44,'2026-02-05 08:50','C',NULL),(57,20,'2026-02-05 11:25','C',NULL),
 (58,2,'2026-02-05 14:00','S',NULL),(59,31,'2026-02-05 16:45','C',NULL),(60,5,'2026-02-06 09:10','C',NULL),
 (61,40,'2026-02-06 11:55','S',NULL),(62,16,'2026-02-06 14:30','C',NULL),(63,1,'2026-02-06 17:15','C',NULL),
 (64,22,'2026-02-07 10:20','X',NULL),(65,49,'2026-02-07 12:45','C',NULL),(66,10,'2026-02-07 15:30','S',NULL),
 (67,3,'2026-02-07 17:55','C',NULL),(68,33,'2026-02-08 09:40','C',NULL),(69,6,'2026-02-08 11:20','P',NULL),
 (70,2,'2026-02-08 14:15','C',NULL),(71,27,'2026-02-08 16:50','S',NULL),(72,13,'2026-02-09 08:30','P',N'今天剛下單'),
 -- 73 以後都是待處理，下單時間依序往後排
 (73,46,NULL,'P',NULL),(74,1,NULL,'P',NULL),(75,35,NULL,'P',NULL),(76,18,NULL,'P',NULL),(77,2,NULL,'P',NULL),
 (78,9,NULL,'P',NULL),(79,41,NULL,'P',NULL),(80,3,NULL,'P',NULL),(81,36,NULL,'P',NULL),(82,24,NULL,'P',NULL),
 (83,3,NULL,'P',NULL),(84,43,NULL,'P',NULL),(85,29,NULL,'P',NULL),(86,4,NULL,'P',NULL),(87,38,NULL,'P',NULL),
 (88,11,NULL,'P',NULL),(89,47,NULL,'P',NULL),(90,2,NULL,'P',NULL),(91,50,NULL,'P',NULL),(92,33,NULL,'P',NULL),
 (93,15,NULL,'P',NULL),(94,4,NULL,'P',NULL),(95,42,NULL,'P',NULL),(96,19,NULL,'P',NULL),(97,1,NULL,'P',NULL),
 (98,28,NULL,'P',NULL),(99,7,NULL,'P',NULL),(100,1,NULL,'P',NULL),(101,1,NULL,'P',NULL),(102,2,NULL,'P',NULL),
 (103,3,NULL,'P',NULL),(104,1,NULL,'P',NULL),(105,2,NULL,'P',NULL),(106,3,NULL,'P',NULL),(107,1,NULL,'P',NULL),
 (108,2,NULL,'P',NULL),(109,3,NULL,'P',NULL),(110,1,NULL,'P',NULL)
) AS v (id, user_id, dt, st, remark);
SET IDENTITY_INSERT orders OFF;

-- 明細（單價與總點數稍後依商品定價計算）
INSERT INTO orderdetail (order_id, product_id, quantity, unit_point_at_purchase, requires_shipping)
SELECT order_id, product_id, qty, 0, 1
FROM (VALUES
 (1,1,1),(2,2,1),(2,6,1),(3,3,1),(4,1,2),(5,7,1),(6,3,1),(6,8,1),(6,5,1),(7,1,1),(8,4,1),(8,8,2),(9,2,1),(10,3,1),
 (11,3,1),(11,4,1),(11,5,1),(12,6,1),(13,1,3),(14,9,1),(15,4,1),(15,1,1),(16,2,1),(16,3,1),(17,8,1),(18,7,1),(18,9,1),
 (19,6,1),(20,5,1),(21,1,1),(22,8,2),(23,3,1),(24,2,1),(25,7,1),(26,9,2),(27,10,1),(28,3,1),(28,8,1),(29,11,3),(30,1,1),
 (31,4,1),(31,6,1),(31,12,1),(32,5,1),(33,1,1),(34,6,1),(35,2,1),(35,1,1),(36,8,1),(37,1,1),(38,8,2),(39,3,1),(40,2,1),
 (41,7,1),(42,10,1),(43,1,1),(44,3,1),(44,8,1),(45,5,1),(46,11,1),(47,1,2),(48,6,1),(49,1,1),(50,3,1),(50,4,1),(50,11,1),
 (50,5,1),(51,6,1),(52,8,1),(53,9,2),(54,3,1),(55,7,1),(56,1,1),(57,3,1),(57,6,1),(58,10,1),(59,5,1),(59,1,1),(60,8,1),
 (61,9,2),(62,4,1),(63,6,1),(64,1,1),(65,7,1),(65,8,1),(66,5,1),(67,11,1),(68,10,2),(69,1,1),(70,2,1),(71,6,1),(72,8,1),
 (73,9,1),(74,1,2),(75,3,1),(76,7,1),(77,10,1),(78,6,1),(78,9,1),(79,4,1),(80,7,1),(81,8,1),(82,5,1),(82,1,1),(83,11,1),
 (84,11,3),(85,1,1),(86,6,1),(87,3,1),(88,7,1),(89,10,1),(90,9,2),(91,4,1),(92,6,1),(93,8,1),(94,7,1),(94,1,1),(95,3,1),
 (96,11,1),(97,10,2),(98,9,1),(99,2,1),(100,6,1),(101,1,1),(101,19,1),(102,8,1),(103,3,1),(103,1,1),(104,7,1),(105,4,1),
 (106,9,2),(107,10,1),(108,11,1),(109,6,1),(110,8,1)
) AS v (order_id, product_id, qty);

UPDATE d SET unit_point_at_purchase = p.point
FROM orderdetail d JOIN product p ON p.product_id = d.product_id;

UPDATE o SET total_point = t.total
FROM orders o JOIN (SELECT order_id, SUM(quantity * unit_point_at_purchase) AS total FROM orderdetail GROUP BY order_id) t
  ON t.order_id = o.order_id;

UPDATE orders SET canceled_at = DATEADD(HOUR, 6, order_date) WHERE status = N'已取消';

-- 寄送資料：已取消的訂單沒有；收件人用會員資料；出貨 / 到貨時間依訂單狀態
INSERT INTO shipped_detail
    (order_id, recipient_name, recipient_address, recipient_phone, shipped_date, arrival_date, is_failed, remark, is_completed)
SELECT o.order_id, u.username, LEFT(u.address, 50), u.phone,
       CASE WHEN o.status IN (N'已出貨', N'已完成') THEN DATEADD(HOUR, 20, o.order_date) END,
       CASE WHEN o.status = N'已完成' THEN DATEADD(DAY, 2, o.order_date) END,
       0, N'', CASE WHEN o.status = N'已完成' THEN 1 ELSE 0 END
FROM orders o JOIN [user] u ON u.user_id = o.user_id
WHERE o.status <> N'已取消';

COMMIT;

-- 簡單檢查
SELECT N'user' AS tbl, COUNT(*) AS cnt FROM [user]
UNION ALL SELECT N'content', COUNT(*) FROM content
UNION ALL SELECT N'point_log', COUNT(*) FROM point_log
UNION ALL SELECT N'orders', COUNT(*) FROM orders
UNION ALL SELECT N'point_log 餘額為負的筆數', COUNT(*) FROM point_log WHERE balance_after < 0;

PRINT N'展示資料已建立（所有會員密碼：Demo1234!）';
