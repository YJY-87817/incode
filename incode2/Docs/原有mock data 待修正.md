USE incodedatabase;

--owen
--user
-- 必須先執行這段，確保 role_id 0, 1, 2 存在
INSERT INTO [role] (role_id, role_name, authority) VALUES 
(0, 'superadmin', 0),
(1, 'admin', 1),
(2, 'member', 2);

INSERT INTO [user] 
(email, username, password, image_url, phone, birthday, address, created_at, is_active, role_id)
VALUES 
('alex.chen@example.com', N'陳志明', '$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkbXtUaF.EA', 'https://example.com/img/u1.jpg', '0912345678', '1990-05-15', N'台北市信義區信義路五段7號', '2023-01-10 09:00:00', 1, 1),
('betty.lin@example.com', N'林雅婷', '$2a$12$dX.1/k.X99w/j2l.11kLYO.S8d3/12345abcdef', 'https://example.com/img/u2.jpg', '0922123456', '1992-08-22', N'新北市板橋區文化路一段100號', '2023-02-14 10:30:00', 1, 2),
('charlie.wu@example.com', N'吳建豪', '$2a$12$aBcDeFgHiJkLmIoPqRsTuVwXyZ1234567890', 'https://example.com/img/u3.jpg', '0933987654', '1988-12-01', N'台中市西屯區台灣大道三段251號', '2023-03-05 14:20:00', 1, 2),
('david.huang@example.com', N'黃家豪', '$2a$12$mNjKiLoHuYgTfRdEsWaQzXcVBnM123098', 'https://example.com/img/u4.jpg', '0911222333', '1995-03-10', N'高雄市左營區博愛二路777號', '2023-03-20 16:45:00', 1, 2),
('eva.chang@example.com', N'張怡君', '$2a$12$qWERtYHuIoPaSdFgHjKlZxCvBnM567', 'https://example.com/img/u5.jpg', '0955666777', '1993-07-04', N'台南市安平區安平路88號', '2023-04-01 11:10:00', 1, 2),
('frank.liu@example.com', N'劉冠宇', '$2a$12$pOiuYtReWqAsDfGhJkLmnBbVcCxDz', 'https://example.com/img/u6.jpg', '0966888999', '1985-11-30', N'桃園市中壢區中正路50號', '2023-04-15 08:50:00', 1, 2),
('grace.wang@example.com', N'王欣儀', '$2a$12$lkJhOgFdSzAxCvBnMmytRcwQe345', 'https://example.com/img/u7.jpg', '0977000111', '1998-02-14', N'新竹市東區光復路二段101號', '2023-05-01 13:00:00', 1, 2),
('henry.yang@example.com', N'楊俊傑', '$2a$12$mnBCcVxZzAaScDdFfGgHhJjKkLl12', 'https://example.com/img/u8.jpg', '0988222333', '1991-09-09', N'基隆市仁愛區愛三路10號', '2023-05-20 17:30:00', 0, 2),
('irene.tsai@example.com', N'蔡佩珊', '$2a$12$oiUyTrRewQaSzDxFcGvHkJnLm567', 'https://example.com/img/u9.jpg', '0910555666', '1996-06-18', N'嘉義市西區垂楊路200號', '2023-06-05 09:15:00', 1, 2),
('jack.ho@example.com', N'何志強', '$2a$12$plOmNiUbYvCtRxEzKwAq12345678', 'https://example.com/img/u10.jpg', '0920888777', '1989-10-25', N'宜蘭縣宜蘭市神農路一段1號', '2023-06-25 15:40:00', 1, 2),
('kelly.kuo@example.com', N'郭美玲', '$2a$12$zAqWsXcEdRfVtGbYnHuJmMikOLp098', 'https://example.com/img/u11.jpg', '0930111222', '1994-01-30', N'花蓮縣花蓮市中正路300號', '2023-07-01 10:00:00', 1, 2),
('leo.cheng@example.com', N'鄭宗翰', '$2a$12$xSwCdEeVrFbtGnyHujMikOylP12345', 'https://example.com/img/u12.jpg', '0950333444', '1997-04-12', N'台東縣台東市更生路50號', '2023-07-15 12:20:00', 1, 2),
('mandy.hsu@example.com', N'許雅雯', '$2a$12$cVfRbGtHnYiUkJmIlOoLpQaSdWzEx', 'https://example.com/img/u13.jpg', '0960555999', '1990-11-11', N'彰化縣彰化市中山路二段2號', '2023-08-05 14:50:00', 1, 2),
('nick.lai@example.com', N'賴承恩', '$2a$12$bNmMjKkLlOoIpUyHtGrFGeDcwSxqA', 'https://example.com/img/u14.jpg', '0970888111', '1987-05-05', N'南投縣南投市中興路100號', '2023-08-20 09:30:00', 0, 2),
('olivia.pan@example.com', N'潘思妤', '$2a$12$mNkJiLoHuyGtFrDeSwAqZxcVbN123', 'https://example.com/img/u15.jpg', '0980222666', '1999-09-20', N'雲林縣斗六市大學路三段123號', '2023-09-01 11:45:00', 1, 2),
('peter.yeh@example.com', N'葉冠廷', '$2a$12$lKoJiUhGyFtDrSeAwQzXcVbNmM567', 'https://example.com/img/u16.jpg', '0919333555', '1992-12-25', N'屏東縣屏東市自由路456號', '2023-09-15 16:10:00', 1, 2),
('queen.lo@example.com', N'羅婉婷', '$2a$12$kJiOhUyGtFrDeSwAqZxcVbNmMl098', 'https://example.com/img/u17.jpg', '0929777888', '1995-07-07', N'苗栗縣苗栗市中正路88號', '2023-10-01 08:20:00', 1, 2),
('ricky.chu@example.com', N'朱宏偉', '$2a$12$jIhUgGyFtDrSeAwQzXcVbNmMlk123', 'https://example.com/img/u18.jpg', '0939000999', '1986-03-28', N'新竹縣竹北市縣政九路1號', '2023-10-15 13:50:00', 1, 2),
('sandy.kao@example.com', N'高詩涵', '$2a$12$hGfFdDsSaAwQqWwEeRrTtYyUuIi12', 'https://example.com/img/u19.jpg', '0959111333', '1993-10-10', N'澎湖縣馬公市中正路10號', '2023-11-01 10:40:00', 1, 2),
('tom.hsieh@example.com', N'謝明哲', '$2a$12$gFeDcBaZyXwVuTsRqPoNmLkJiH765', 'https://example.com/img/u20.jpg', '0969222444', '1991-04-20', N'台北市內湖區瑞光路399號', '2023-11-20 15:30:00', 1, 2);

INSERT INTO [follow] (follower_id, followee_id, created_at) VALUES 
(2, 1, '2023-12-01 10:00:00'), -- 林雅婷 追蹤 陳志明
(3, 1, '2023-12-02 11:30:00'), -- 吳建豪 追蹤 陳志明
(4, 1, '2023-12-05 09:15:00'), -- 黃家豪 追蹤 陳志明
(1, 2, '2023-12-10 14:00:00'), -- 陳志明 追蹤 林雅婷
(5, 2, '2023-12-12 16:45:00'), -- 張怡君 追蹤 林雅婷
(20, 1, '2023-12-15 08:20:00'), -- 謝明哲 追蹤 陳志明
(15, 20, '2024-01-01 12:00:00');-- 潘思妤 追蹤 謝明哲

INSERT INTO user_loginlog (user_id, logintime, ip_address) 
VALUES 
(1, '2024-01-15 08:30:00', '220.135.12.5'),    -- 模擬家用固定 IP
(1, '2024-01-16 09:15:00', '220.135.12.5'),    -- 同 User 同 IP (習慣位置)
(2, '2024-01-15 10:00:00', '59.124.100.21'),   -- 模擬公司 IP
(3, '2024-01-16 14:20:00', '114.34.88.99'),    
(4, '2024-01-17 16:45:00', '101.10.35.4'),     -- 模擬手機網路 IP
(10, '2024-01-18 11:00:00', '61.230.55.12'),
(20, '2024-01-18 15:30:00', '36.225.14.7'),
(1, '2024-01-19 22:15:00', '192.168.0.105');   -- 模擬內網或不同裝置



INSERT INTO user_notification (user_id, title, content, target_url, is_read, type, created_at)
VALUES 
(3, N'新回覆通知', N'王小美 在您的文章「週末旅遊推薦」中留言了：「看起來真棒！」', 'https://example.com/post/101#c-55', 0, 'reply', '2024-01-18 10:20:00'),

(3, N'獲得新讚', N'李四 喜歡您的攝影作品。', 'https://example.com/post/101', 1, 'like', '2024-01-18 09:15:00'),

(15, N'系統維護', N'系統將於 1/20 凌晨 02:00 進行例行維護，屆時將暫停服務 30 分鐘。', 'https://example.com/news/maintenance', 0, 'system', '2024-01-19 12:00:00'),

(7, N'有人標記了您', N'張三 在一則貼文中標記了您：「@陳志明 快來看這個優惠！」', 'https://example.com/post/888', 0, 'reply', '2024-01-19 14:30:00'),

(8, N'密碼修改成功', N'您的登入密碼已修改成功，若非本人操作請立即聯繫客服。', 'https://example.com/settings/security', 1, 'system', '2024-01-15 20:10:00'),

(2, N'獲得新讚', N'Chen123 對您的留言按讚。', 'https://example.com/post/500', 0, 'like', '2024-01-19 15:05:00')


--tim
-- =============================================
-- 1. 建立互動與通知分類 (interaction_category)
-- =============================================
-- 用於 content_interaction (按讚/收藏) 與 content_notification (通知類型)
INSERT INTO interaction_category (name, description, created_at, updated_at, is_active)
VALUES 
(N'Like', N'按讚 / 有人按讚你的內容', GETDATE(), GETDATE(), 1),        -- ID: 1
(N'Dislike', N'倒讚 / 有人倒讚你的內容', GETDATE(), GETDATE(), 1),      -- ID: 2
(N'Bookmark', N'收藏 / 有人收藏你的內容', GETDATE(), GETDATE(), 1),     -- ID: 3
(N'Reply', N'回覆 / 有人回覆你的內容', GETDATE(), GETDATE(), 1),        -- ID: 4 (通知專用)
(N'System', N'系統通知 / 官方公告', GETDATE(), GETDATE(), 1),           -- ID: 5 (通知專用)
(N'Hide', N'隱藏自己的內容', GETDATE(), GETDATE(), 1);                  -- ID: 6

-- =============================================
-- 2. 建立內容分類 (content_category)
-- =============================================
-- 第一層：主分類 (Parent ID 為 NULL)
INSERT INTO content_category (parent_category_id, category_name, description, is_active)
VALUES 
(NULL, N'電影討論', N'所有電影的綜合討論區', 3), -- ID: 1 (3:官方提供)
(NULL, N'影城評價', N'各地影城的評價與心得', 3), -- ID: 2
(NULL, N'閒聊專區', N'自由討論區', 3);           -- ID: 3

-- 第二層：子分類 (依賴上方的 ID)
INSERT INTO content_category (parent_category_id, category_name, description, is_active)
VALUES 
(1, N'歐美電影', N'好萊塢與歐洲電影', 3),       -- ID: 4 (Parent: 1)
(1, N'亞洲電影', N'日韓台港電影', 3),           -- ID: 5 (Parent: 1)
(2, N'威秀影城', N'全台威秀影城討論', 3),       -- ID: 6 (Parent: 2)
(2, N'秀泰影城', N'全台秀泰影城討論', 3);       -- ID: 7 (Parent: 2)

-- =============================================
-- 3. 建立內容 (content)
-- =============================================
-- 情境：發文 (Type: 1)
INSERT INTO content (parent_content_id, category_id, user_id, title, body, type, created_at, updated_at, is_active)
VALUES 
-- ID: 1
(NULL, 4, 1, N'奧本海默 IMAX 觀後感', N'畫面震撼，音效極佳，諾蘭迷必看！', 1, DATEADD(hour, -10, GETDATE()), DATEADD(hour, -10, GETDATE()), 1),
-- ID: 2
(NULL, 6, 2, N'信義威秀座位舒適度', N'昨天去看了早場，覺得座位有升級，皮椅很軟。', 1, DATEADD(hour, -5, GETDATE()), DATEADD(hour, -5, GETDATE()), 1),
-- ID: 3
(NULL, 5, 3, N'寄生上流這部片', N'即使過了這麼久，重看還是覺得劇本很神。', 1, DATEADD(hour, -2, GETDATE()), DATEADD(hour, -2, GETDATE()), 1),
-- ID: 4
(NULL, 2, 4, N'台北電影節心得', N'今年片單很精彩，尤其是紀錄片部分。', 1, DATEADD(hour, -8, GETDATE()), DATEADD(hour, -8, GETDATE()), 1),
-- ID: 5
(NULL, 3, 5, N'復仇者聯盟重映', N'大銀幕看起來依舊熱血沸騰。', 1, DATEADD(hour, -7, GETDATE()), DATEADD(hour, -7, GETDATE()), 1),
-- ID: 6
(NULL, 1, 6, N'小丑電影分析', N'心理描寫很深刻，演技令人印象深刻。', 1, DATEADD(hour, -6, GETDATE()), DATEADD(hour, -6, GETDATE()), 1),
-- ID: 7
(NULL, 4, 2, N'威秀爆米花口味', N'新出的焦糖口味很好吃，下次還會買。', 1, DATEADD(hour, -4, GETDATE()), DATEADD(hour, -4, GETDATE()), 1),
-- ID: 8
(NULL, 5, 7, N'黑暗騎士三部曲', N'重看依然覺得是超級英雄片的巔峰。', 1, DATEADD(hour, -3, GETDATE()), DATEADD(hour, -3, GETDATE()), 1),
-- ID: 9
(NULL, 6, 8, N'歌劇魅影舞台劇', N'音樂與舞台設計都非常震撼。', 1, DATEADD(hour, -12, GETDATE()), DATEADD(hour, -12, GETDATE()), 1),
-- ID: 10
(NULL, 2, 9, N'Netflix 新片推薦', N'最近的韓劇題材新穎，值得追。', 1, DATEADD(hour, -15, GETDATE()), DATEADD(hour, -15, GETDATE()), 1),
-- ID: 11
(NULL, 3, 10, N'動畫電影心得', N'畫風很美，劇情也很感人。', 1, DATEADD(hour, -20, GETDATE()), DATEADD(hour, -20, GETDATE()), 1),
-- ID: 12
(NULL, 1, 11, N'宮崎駿作品回顧', N'每一部都充滿童心與哲理。', 1, DATEADD(hour, -25, GETDATE()), DATEADD(hour, -25, GETDATE()), 1),
-- ID: 13
(NULL, 4, 12, N'台中大遠百影城', N'座位寬敞，音響效果不錯。', 1, DATEADD(hour, -30, GETDATE()), DATEADD(hour, -30, GETDATE()), 1),
-- ID: 14
(NULL, 5, 13, N'蜘蛛人新宇宙', N'動畫風格很特別，故事也很精彩。', 1, DATEADD(hour, -35, GETDATE()), DATEADD(hour, -35, GETDATE()), 1),
-- ID: 15
(NULL, 6, 14, N'音樂劇觀後感', N'演員唱功了得，舞台效果驚人。', 1, DATEADD(hour, -40, GETDATE()), DATEADD(hour, -40, GETDATE()), 1),
-- ID: 16
(NULL, 2, 15, N'驚悚片推薦', N'劇情緊湊，氛圍營造得很好。', 1, DATEADD(hour, -45, GETDATE()), DATEADD(hour, -45, GETDATE()), 1),
-- ID: 17
(NULL, 3, 16, N'浪漫愛情片心得', N'雖然套路，但演員演技很自然。', 1, DATEADD(hour, -50, GETDATE()), DATEADD(hour, -50, GETDATE()), 1),
-- ID: 18
(NULL, 1, 17, N'紀錄片觀後感', N'內容真實，讓人深思。', 1, DATEADD(hour, -55, GETDATE()), DATEADD(hour, -55, GETDATE()), 1),
-- ID: 19
(NULL, 4, 18, N'科幻片心得', N'特效很棒，劇情也有深度。', 1, DATEADD(hour, -60, GETDATE()), DATEADD(hour, -60, GETDATE()), 1),
-- ID: 20
(NULL, 5, 19, N'經典老片回顧', N'黑白片依然有魅力，值得一看。', 1, DATEADD(hour, -65, GETDATE()), DATEADD(hour, -65, GETDATE()), 1);



-- 情境：回文/留言 (Type: 2 或 3, Parent ID 指向 Post)
INSERT INTO content (parent_content_id, category_id, user_id, title, body, type, created_at, updated_at, is_active)
VALUES 
-- ID: 4 (回覆文章 1)
(1, 4, 2, N'Re: 奧本海默', N'真的！尤其是最後那段試爆，完全安靜的處理方式太絕了。', 2, DATEADD(hour, -9, GETDATE()), DATEADD(hour, -9, GETDATE()), 1),
-- ID: 5 (回覆文章 1)
(1, 4, 3, N'Re: 奧本海默', N'但我看的時候覺得冷氣有點太冷 XD', 3, DATEADD(hour, -8, GETDATE()), DATEADD(hour, -8, GETDATE()), 1),
-- ID: 6 (回覆文章 2)
(2, 6, 1, N'Re: 信義威秀', N'請問是第幾廳？聽說只有旗艦廳有換椅子。', 3, DATEADD(hour, -4, GETDATE()), DATEADD(hour, -4, GETDATE()), 1);

-- 情境：留言的回覆 (Type: 4, 巢狀結構)
INSERT INTO content (parent_content_id, category_id, user_id, title, body, type, created_at, updated_at, is_active)
VALUES 
-- ID: 7 (回覆留言 6)
(6, 6, 2, N'', N'是第 10 廳喔！', 4, DATEADD(hour, -3, GETDATE()), DATEADD(hour, -3, GETDATE()), 1);

-- =============================================
-- 4. 建立互動紀錄 (content_interaction)
-- =============================================
INSERT INTO content_interaction (category_id, content_id, user_id, created_at, updated_at)
VALUES 
(1, 1, 2, GETDATE(), GETDATE()), -- User 2 按讚 文章 1
(1, 1, 3, GETDATE(), GETDATE()), -- User 3 按讚 文章 1
(3, 1, 4, GETDATE(), GETDATE()), -- User 4 收藏 文章 1
(1, 2, 1, GETDATE(), GETDATE()), -- User 1 按讚 文章 2
(1, 4, 1, GETDATE(), GETDATE()); -- User 1 按讚 回文 4

-- =============================================
-- 5. 建立歷史紀錄 (content_history)
-- =============================================
INSERT INTO content_history (content_id, user_id, old_title, old_body, created_at, updated_at)
VALUES 
(1, 1, N'奧本海默觀後感', N'畫面不錯。', DATEADD(hour, -10, GETDATE()), GETDATE()), -- 文章 1 曾被修改過
(2, 2, N'威秀心得', N'椅子不錯。', DATEADD(hour, -5, GETDATE()), GETDATE());    -- 文章 2 曾被修改過

-- =============================================
-- 6. 建立通知 (content_notification)
-- =============================================
INSERT INTO content_notification (user_id, content_id, category_id, title, description, created_at, updated_at, isread)
VALUES 
-- User 1 收到通知：有人按讚他的文章
(1, 1, 1, N'有人按讚了你的文章', N'林雅婷 (User 2) 按讚了「奧本海默 IMAX 觀後感」', GETDATE(), GETDATE(), 0),
-- User 1 收到通知：有人回覆他的文章
(1, 4, 4, N'有人回覆了你的文章', N'林雅婷 (User 2) 回覆了「奧本海默 IMAX 觀後感」', GETDATE(), GETDATE(), 0),
-- User 2 收到通知：有人回覆他的留言
(2, 6, 4, N'有人回覆了你的留言', N'陳志明 (User 1) 在「信義威秀座位舒適度」回覆了你', GETDATE(), GETDATE(), 1);

-- =============================================
-- 7. 圖片管理 (user_image)
-- =============================================
INSERT INTO user_image (user_id, image_url, caption, display_order, created_at, updated_at)
VALUES 
(1, N'/uploads/content/oppenheimer_ticket.jpg', N'奧本海默票根', 1, GETDATE(), GETDATE()),
(2, N'/uploads/content/cinema_seat.jpg', N'威秀新座椅實拍', 1, GETDATE(), GETDATE());



--qiu


INSERT INTO point_type_definition (type_name, display_name, description, is_active, created_at, updated_at)
VALUES
(N'mission_reward', N'任務獎勵', N'完成一般任務獲得點數', 1, GETDATE(), NULL),
(N'entrust_reward', N'委託獎勵', N'完成委託任務獲得點數', 1, GETDATE(), NULL),
(N'recharge', N'儲值點數', N'點數儲值', 1, GETDATE(), NULL),
(N'admin_adjust', N'後台調整', N'系統人工調整', 1, GETDATE(), NULL),
(N'refund', N'退款返還', N'交易退款返還', 1, GETDATE(), NULL),
(N'penalty', N'懲罰扣點', N'違規扣點', 1, GETDATE(), NULL),
(N'event_reward', N'活動獎勵', N'活動贈送點數', 1, GETDATE(), NULL),
(N'login_bonus', N'登入獎勵', N'每日登入獎勵', 1, GETDATE(), NULL),
(N'level_up', N'升級獎勵', N'等級提升獎勵', 1, GETDATE(), NULL),
(N'other', N'其他', N'其他來源', 1, GETDATE(), NULL);

INSERT INTO mission (mission_name, mission_detail, target_type, target_count, reward_points, reward_exp, reset_type, is_active, created_at)
VALUES
(N'每日登入', N'每日登入一次即可完成', N'login', 1, 10, 5, N'daily', 1, GETDATE()),
(N'發表文章', N'發表一篇文章', N'post', 1, 50, 20, N'none', 1, GETDATE()),
(N'留言互動', N'留言 5 次', N'comment', 5, 30, 10, N'daily', 1, GETDATE()),
(N'分享內容', N'分享內容 3 次', N'share', 3, 40, 15, N'weekly', 1, GETDATE()),
(N'完成委託', N'完成 1 個委託任務', N'entrust', 1, 100, 50, N'none', 1, GETDATE()),
(N'累積登入', N'累積登入 7 天', N'login', 7, 80, 30, N'weekly', 1, GETDATE()),
(N'收藏內容', N'收藏 5 個內容', N'favorite', 5, 25, 10, N'daily', 1, GETDATE()),
(N'追蹤用戶', N'追蹤 3 位用戶', N'follow', 3, 20, 10, N'none', 1, GETDATE()),
(N'升級成就', N'升級一次', N'level', 1, 60, 40, N'none', 1, GETDATE()),
(N'年度任務', N'年度活躍任務', N'activity', 100, 500, 200, N'yearly', 1, GETDATE());

INSERT INTO mission_progress (user_id, mission_id, current_count, status, accepted_at, completed_at, rewarded_at)
VALUES
(1, 1, 1, N'rewarded', GETDATE(), GETDATE(), GETDATE()),
(2, 2, 1, N'completed', GETDATE(), GETDATE(), NULL),
(3, 3, 3, N'ongoing', GETDATE(), NULL, NULL),
(4, 4, 3, N'rewarded', GETDATE(), GETDATE(), GETDATE()),
(5, 5, 1, N'rewarded', GETDATE(), GETDATE(), GETDATE()),
(6, 6, 4, N'ongoing', GETDATE(), NULL, NULL),
(7, 7, 5, N'completed', GETDATE(), GETDATE(), NULL),
(8, 8, 1, N'rewarded', GETDATE(), GETDATE(), GETDATE()),
(9, 9, 1, N'completed', GETDATE(), GETDATE(), NULL),
(10, 10, 20, N'ongoing', GETDATE(), NULL, NULL);

INSERT INTO entrust_mission (user_id, content_id, title, detail, reward_points, max_participants, status, is_active, created_at)
VALUES
(1, 1, N'電影字幕校對', N'協助校對電影字幕中的錯字與時間軸', 120, 1, N'open', 1, GETDATE()),
(2, 2, N'電影海報設計', N'設計電影宣傳海報或周邊視覺圖', 250, 1, N'open', 1, GETDATE()),
(3, 3, N'電影預告剪輯', N'剪輯電影預告片或精華片段', 350, 1, N'closed', 1, GETDATE()),
(4, 4, N'電影資料整理', N'整理電影角色、劇情與上映資訊', 150, 2, N'open', 1, GETDATE()),
(5, 5, N'電影影評翻譯', N'將電影影評由中文翻譯成英文', 280, 1, N'open', 1, GETDATE()),
(6, 6, N'電影功能測試', N'測試電影播放或購票相關功能', 130, 3, N'open', 1, GETDATE()),
(7, 7, N'電影周邊 UI 建議', N'針對電影周邊商品頁提供 UI 改善建議', 100, 5, N'closed', 1, GETDATE()),
(8, 8, N'電影內容審核', N'審核電影介紹與周邊商品內容是否合規', 200, 2, N'open', 1, GETDATE()),
(9, 9, N'電影宣傳文案撰寫', N'撰寫電影或電影周邊商品宣傳文案', 240, 1, N'cancelled', 1, GETDATE()),
(10, 10, N'電影觀眾數據分析', N'分析電影觀眾行為與票房相關數據', 450, 1, N'open', 1, GETDATE());

INSERT INTO entrust_mission_progress (user_id, entrust_mission_id, status, accepted_at, completed_at, rewarded_at)
VALUES
(2, 1, N'ongoing', GETDATE(), NULL, NULL),
(3, 2, N'completed', GETDATE(), GETDATE(), NULL),
(4, 3, N'rewarded', GETDATE(), GETDATE(), GETDATE()),
(5, 4, N'ongoing', GETDATE(), NULL, NULL),
(6, 5, N'completed', GETDATE(), GETDATE(), NULL),
(7, 6, N'rewarded', GETDATE(), GETDATE(), GETDATE()),
(8, 7, N'cancelled', GETDATE(), NULL, NULL),
(9, 8, N'ongoing', GETDATE(), NULL, NULL),
(10, 9, N'falsed', GETDATE(), NULL, NULL),
(1, 10, N'completed', GETDATE(), GETDATE(), NULL);

INSERT INTO entrust_mission_point_reserve (user_id, entrust_mission_id, reserved_points, status, created_at, released_at)
VALUES
(1, 1, 100, N'reserved', GETDATE(), NULL),
(2, 2, 200, N'consumed', GETDATE(), GETDATE()),
(3, 3, 300, N'released', GETDATE(), GETDATE()),
(4, 4, 150, N'reserved', GETDATE(), NULL),
(5, 5, 250, N'consumed', GETDATE(), GETDATE()),
(6, 6, 120, N'reserved', GETDATE(), NULL),
(7, 7, 80, N'released', GETDATE(), GETDATE()),
(8, 8, 180, N'reserved', GETDATE(), NULL),
(9, 9, 220, N'released', GETDATE(), GETDATE()),
(10, 10, 400, N'consumed', GETDATE(), GETDATE());

INSERT INTO point_recharge (user_id, order_no, cash_amount, points_granted, payment_method, status, created_at)
VALUES
(1, N'ORD001', 100.00, 1000, N'credit_card', N'success', GETDATE()),
(2, N'ORD002', 50.00, 500, N'line_pay', N'success', GETDATE()),
(3, N'ORD003', 30.00, 300, N'credit_card', N'failed', GETDATE()),
(4, N'ORD004', 200.00, 2200, N'paypal', N'success', GETDATE()),
(5, N'ORD005', 150.00, 1600, N'credit_card', N'pending', GETDATE()),
(6, N'ORD006', 80.00, 800, N'line_pay', N'success', GETDATE()),
(7, N'ORD007', 60.00, 600, N'credit_card', N'expired', GETDATE()),
(8, N'ORD008', 120.00, 1300, N'paypal', N'success', GETDATE()),
(9, N'ORD009', 40.00, 400, N'line_pay', N'failed', GETDATE()),
(10, N'ORD010', 300.00, 3500, N'credit_card', N'success', GETDATE());

-- 先假設每個使用者初始餘額為 0，逐筆累加
INSERT INTO point_log (user_id, source_type, source_id, point_type_definition_id, change_amount, status, created_at)
VALUES
(1, N'mission', 1, 1, 10, N'success', DATEADD(MINUTE, -50, GETDATE())),
(1, N'recharge', 1, 3, 1000, N'success', DATEADD(MINUTE, -40, GETDATE())),
(1, N'entrust', 1, 2, 120, N'success', DATEADD(MINUTE, -30, GETDATE())),
(2, N'mission', 2, 1, 50, N'success', DATEADD(MINUTE, -20, GETDATE())),
(3, N'recharge', NULL, 3, 300, N'pending', GETDATE()),
(4, N'admin_adjust', NULL, 4, 100, N'success', GETDATE()),
(5, N'penalty', NULL, 6, -20, N'success', GETDATE()),
(6, N'login_bonus', 1, 8, 10, N'success', GETDATE()),
(7, N'event_reward', NULL, 7, 200, N'success', GETDATE()),
(8, N'level_up', 1, 9, 60, N'success', GETDATE());

INSERT INTO notifications (target_user_id, title, content, category, related_id, link_url, is_read, created_at)
VALUES
(1, N'任務完成', N'你已完成每日登入任務', N'mission', N'1', N'/mission/1', 0, GETDATE()),
(2, N'點數入帳', N'儲值成功，點數已入帳', N'point', N'ORD002', N'/wallet', 1, GETDATE()),
(3, N'任務進度', N'任務即將完成', N'mission', N'3', N'/mission/3', 0, GETDATE()),
(4, N'委託完成', N'你發布的委託已完成', N'entrust', N'4', N'/entrust/4', 0, GETDATE()),
(5, N'系統通知', N'系統維護公告', N'system', NULL, N'/notice', 0, GETDATE()),
(6, N'點數扣除', N'違規扣除點數', N'point', NULL, N'/wallet', 1, GETDATE()),
(7, N'活動獎勵', N'你獲得活動獎勵', N'event', NULL, N'/event', 0, GETDATE()),
(8, N'委託進行中', N'有人接受你的委託', N'entrust', N'8', N'/entrust/8', 0, GETDATE()),
(9, N'升級通知', N'你已成功升級', N'level', NULL, N'/profile', 1, GETDATE()),
(10, N'年度任務', N'年度任務已開放', N'mission', N'10', N'/mission/10', 0, GETDATE());

--yjy
--product 假資料 (描述優化版)
INSERT INTO Product (name, description, image_name, type, is_physical, point, stock, created_at, updated_at, on_sale)
VALUES 
(N'電影兌換卷*1', N'全台通用電影兌換券，不限平假日皆可使用。適用於首輪電影，提供最震撼的感官體驗，是下班休閒或週末約會的最佳選擇。', '2026010611432290af.jpg', N'兌換票券', 1, 2000, 200, '2026-01-06 11:43:22.827', '2026-01-06 11:43:22.827', 1),

(N'電影兌換卷*4', N'超值四入組合包！適合全家大小或是好友團體同樂，一次兌換四張更划算。為您省下更多點數，盡情享受年度強檔大片。', '20260106114404ab10.jpg', N'兌換票券', 1, 7500, 2000, '2026-01-06 11:44:04.000', '2026-01-06 11:44:25.487', 1),

(N'哈利波特魔杖(款一)', N'哈利波特系列經典巫師魔杖收藏品。精準還原電影比例，細膩的手感紋理彷彿帶你走進斜角巷，隨附精美收藏盒，哈迷絕對不能錯過。', '202601061148366c60.webp', N'電影周邊', 1, 1500, 300, '2026-01-06 11:48:36.000', '2026-01-06 11:49:45.153', 1),

(N'哈利波特魔杖(款二)', N'此款魔杖擁有獨特的雕刻工藝與復古塗裝，展現優雅且神秘的魔法氣息。材質堅固且細節豐富，適合作為居家擺飾或角色扮演道具。', '202601061149141861.webp', N'電影周邊', 1, 1500, 200, '2026-01-06 11:49:14.203', '2026-01-06 11:49:14.203', 1),

(N'哈利波特魔杖(隨機)', N'驚喜盲盒款！內含一款隨機款式的巫師魔杖，讓命運決定你專屬的守護器物。無論是作為交換禮物還是給自己的小驚喜都非常適合。', '202601061150266977.webp', N'電影周邊', 1, 1450, 100, '2026-01-06 11:50:26.523', '2026-01-06 11:50:26.523', 1),

(N'星戰黑武士T-shirt', N'星際大戰經典黑武士(Darth Vader)圖樣，採用100%優質純棉打造，透氣乾爽。經典不敗的高質感印花，讓原力隨時與你的日常穿搭同在。', '20260106115244530b.jpg', N'電影周邊', 1, 2500, 30, '2026-01-06 11:52:44.890', '2026-01-06 11:52:44.890', 1),

(N'星戰紀念胸章', N'全球限量金屬紀念胸章，採用立體浮雕設計，質感厚實。適合作為裝飾別於包袋或外套，是星際大戰資深粉絲值得收藏的精緻小物。', '20260106115633af93.webp', N'電影周邊', 1, 2500, 300, '2026-01-06 11:56:33.957', '2026-01-06 11:56:33.957', 1),

(N'星戰光劍吊飾', N'迷你版光劍造型掛飾，還原絕地武士的力量象徵。體積輕巧且具備金屬光澤，可輕鬆掛於鑰匙或後背包，隨時展現你的科幻情懷。', '20260106115713bff2.webp', N'電影周邊', 1, 2000, 40, '2026-01-06 11:57:13.273', '2026-01-06 11:57:13.273', 1),

(N'星際大戰R2D2 LED鑰匙圈', N'獲官方授權的R2D2 LED造型鑰匙圈，不僅外型可愛，按下開關後腳底還會發出明亮光芒，結合實用性與趣味性，在暗處也能輕鬆找到鑰匙孔。', '20260121090841d60d.webp', N'電影周邊', 1, 2000, 50, '2026-01-06 11:57:13.273', '2026-01-06 11:57:13.273', 1);


 --orders 假資料
INSERT INTO orders (user_id, total_point, order_date, remark, status, canceled_at)
VALUES
(1, 3500, '2026-01-10 10:00:00', N'電影票＋魔杖', N'已完成', NULL),
(2, 4000, '2026-01-11 11:20:00', N'電影周邊組合', N'已完成', NULL),
(3, 4500, '2026-01-12 14:30:00', N'星戰收藏', N'已出貨', NULL),
(4, 9500, '2026-01-13 09:10:00', N'電影票大量兌換', N'已完成', NULL),
(5, 4500, '2026-01-14 16:40:00', N'周邊商品', N'已出貨', NULL),
(6, 2950, '2026-01-15 12:00:00', N'魔杖收藏', N'已完成', NULL),
(7, 5000, '2026-01-16 18:30:00', N'星戰 T-shirt 組', N'待處理', NULL),
(8, 2000, '2026-01-17 10:50:00', N'光劍吊飾', N'已取消', '2026-01-17 12:00:00'),
(9, 3000, '2026-01-18 15:20:00', N'哈利波特系列', N'已完成', NULL),
(10, 4000, '2026-01-19 09:30:00', N'電影票組合', N'已完成', NULL),
(11, 4500, '2026-01-20 13:10:00', N'星戰胸章組', N'已出貨', NULL),
(12, 3500, '2026-01-21 17:00:00', N'鑰匙圈＋票券', N'已完成', NULL),
(13, 3000, '2026-01-22 11:45:00', N'魔杖雙入', N'已完成', NULL),
(14, 9500, '2026-01-23 14:00:00', N'電影票大量', N'已完成', NULL),
(15, 5000, '2026-01-24 16:20:00', N'星戰服飾組', N'已出貨', NULL),
(16, 2950, '2026-01-25 10:10:00', N'隨機魔杖組', N'已完成', NULL),
(17, 4000, '2026-01-26 12:30:00', N'電影票二張', N'待處理', NULL),
(18, 5000, '2026-01-27 15:40:00', N'星戰收藏組', N'已完成', NULL),
(19, 4000, '2026-01-28 09:00:00', N'光劍＋票券', N'已完成', NULL),
(20, 3000, '2026-01-29 18:10:00', N'魔杖組合', N'已完成', NULL);

 --orderdetail 假資料
INSERT INTO orderdetail (order_id, product_id, quantity, unit_point_at_purchase, requires_shipping)
VALUES
(1, 1, 1, 2000, 0),
(1, 3, 1, 1500, 1),
(2, 1, 1, 2000, 0),
(2, 8, 1, 2000, 1),
(3, 6, 1, 2500, 1),
(3, 7, 1, 2000, 1),
(4, 2, 1, 7500, 0),
(4, 1, 1, 2000, 0),
(5, 9, 1, 2000, 1),
(5, 8, 1, 2000, 1),
(5, 1, 1, 2000, 0),
(6, 5, 1, 1450, 1),
(6, 3, 1, 1500, 1),
(7, 6, 2, 2500, 1),
(8, 8, 1, 2000, 1),
(9, 3, 1, 1500, 1),
(9, 4, 1, 1500, 1),
(10, 1, 2, 2000, 0),
(11, 7, 1, 2500, 1),
(11, 8, 1, 2000, 1),
(12, 9, 1, 2000, 1),
(12, 1, 1, 2000, 0),
(13, 3, 1, 1500, 1),
(13, 4, 1, 1500, 1),
(14, 2, 1, 7500, 0),
(14, 1, 1, 2000, 0),
(15, 6, 1, 2500, 1),
(15, 7, 1, 2500, 1),
(16, 5, 2, 1450, 1),
(17, 1, 2, 2000, 0),
(18, 7, 2, 2500, 1),
(19, 8, 1, 2000, 1),
(19, 1, 1, 2000, 0),
(20, 3, 1, 1500, 1),
(20, 4, 1, 1500, 1);

--shipped_detail  假資料
INSERT INTO shipped_detail
(orderdetail_id, recipient_name, recipient_address, recipient_phone, shipped_date, arrival_date, is_failed, remark, is_completed)
VALUES
(2,  N'陳志明', N'台北市信義區信義路五段7號', N'0912345678', '2026-01-11', '2026-01-13', 0, N'完成', 1),
(4,  N'林雅婷', N'新北市板橋區文化路一段100號', N'0922123456', '2026-01-12', NULL, 0, N'配送中', 0),
(5,  N'吳建豪', N'台中市西屯區台灣大道三段251號', N'0933987654', '2026-01-13', NULL, 0, N'配送中', 0),
(6,  N'吳建豪', N'台中市西屯區台灣大道三段251號', N'0933987654', '2026-01-13', '2026-01-15', 0, N'完成', 1),
(9,  N'張怡君', N'台南市安平區安平路88號', N'0955666777', '2026-01-15', NULL, 0, N'物流處理中', 0),
(10, N'張怡君', N'台南市安平區安平路88號', N'0955666777', '2026-01-15', '2026-01-17', 0, N'完成', 1),
(12, N'劉冠宇', N'桃園市中壢區中正路50號', N'0966888999', '2026-01-16', '2026-01-18', 0, N'完成', 1),
(15, N'蔡佩珊', N'嘉義市西區垂楊路200號', N'0910555666', '2026-01-19', '2026-01-21', 0, N'完成', 1),
(17, N'郭美玲', N'花蓮縣花蓮市中正路300號', N'0930111222', '2026-01-21', NULL, 0, N'配送中', 0),
(19, N'許雅雯', N'彰化縣彰化市中山路二段2號', N'0960555999', '2026-01-23', '2026-01-25', 0, N'完成', 1),
(21, N'潘思妤', N'雲林縣斗六市大學路三段123號', N'0980222666', '2026-01-25', NULL, 0, N'配送中', 0),
(22, N'潘思妤', N'雲林縣斗六市大學路三段123號', N'0980222666', '2026-01-25', '2026-01-27', 0, N'完成', 1);
