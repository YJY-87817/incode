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
