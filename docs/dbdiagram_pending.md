// Pending
// 電影資料表
// table "movies" {
//   "movies_id" INT [pk, increment, note: '電影ID']
//   "title" NVARCHAR(200) [not null, note: '電影名稱']
//   "original_title" NVARCHAR(200) [note: '原文片名']
//   "director" NVARCHAR(100) [note: '導演']
//   "genre" NVARCHAR(100) [note: '類型（可用逗號分隔多個）']
//   "release_date" DATE [note: '上映日期']
//   "duration" INT [note: '片長（分鐘）']
//   "rating" DECIMAL(3,1) [note: '平均評分 (0-10)']
//   "poster_url" NVARCHAR(500) [note: '海報圖片URL']
//   "synopsis" NVARCHAR(MAX) [note: '劇情簡介']
//   "trailer_url" NVARCHAR(500) [note: '預告片連結']
//   "imdb_id" VARCHAR(50) [note: 'IMDB編號']
//   "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
//   "updated_at" DATETIME [default: `CURRENT_TIMESTAMP`]
// }

// // 影廳/電影院資料表
// table "cinemas" {
//   "cinemas_id" INT [pk, increment, note: '影廳ID']
//   "name" NVARCHAR(100) [not null, note: '影廳名稱']
//   "chain" NVARCHAR(50) [note: '連鎖品牌（威秀、國賓等）']
//   "address" NVARCHAR(200) [not null, note: '地址']
//   "city" NVARCHAR(50) [note: '城市']
//   "district" NVARCHAR(50) [note: '區域']
//   "latitude" DECIMAL(10,7) [note: '緯度（地圖用）']
//   "longitude" DECIMAL(10,7) [note: '經度（地圖用）']
//   "phone" VARCHAR(20) [note: '電話']
//   "website" NVARCHAR(200) [note: '官網']
//   // 用記錄去算總分即可
//   // "rating" DECIMAL(3,1) [note: '平均評分 (0-5)']
//   "facilities" NVARCHAR(500) [note: '設施（JSON格式）']
//   "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
//   "updated_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  
// }

// // 電影評論表
// table "movie_reviews" {
//   "movie_reviews_id" INT [pk, increment, note: '評論ID']
//   "movies_id" INT [not null, note: '電影ID (外鍵)']
//   "user_id" INT [not null, note: '評論者ID']
  
//   // 評分細項
//   "overall_rating" DECIMAL(3,1) [not null, note: '總體評分 (0-10)']
//   "story_rating" DECIMAL(3,1) [note: '劇情評分 (0-10)']
//   "acting_rating" DECIMAL(3,1) [note: '演技評分 (0-10)']
//   "visual_rating" DECIMAL(3,1) [note: '視覺效果評分 (0-10)']
//   "music_rating" DECIMAL(3,1) [note: '配樂評分 (0-10)']
  
//   // 評論內容
//   "title" NVARCHAR(200) [note: '評論標題']
//   "content" NVARCHAR(MAX) [note: '評論內容']
//   "is_spoiler" BIT [default: 0, note: '是否有劇透 (0=無, 1=有)']
  
//   // 統計數據
//   "helpful_count" INT [default: 0, note: '有用按鈕計數']
//   "view_count" INT [default: 0, note: '瀏覽次數']
  
//   // 關聯到文章（可選）
//   "linked_post_id" INT [null, note: '關聯的論壇文章ID（如嵌入文章）']
//   "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
//   "updated_at" DATETIME [null]
  
// }

// // 影廳評論表
// table "cinema_reviews" {
//   "cinema_reviews_id" INT [pk, increment, note: '評論ID']
//   "cinemas_id" INT [not null, note: '影廳ID (外鍵)']
//   "user_id" INT [not null, note: '評論者ID']
  
//   // 評分細項
//   // 由每一筆紀錄count，不存總數
//   // "overall_rating" DECIMAL(3,1) [not null, note: '總體評分 (0-5)']
//   "seat_rating" DECIMAL(3,1) [note: '座位舒適度 (0-5)']
//   "screen_rating" DECIMAL(3,1) [note: '螢幕/音響品質 (0-5)']
//   "service_rating" DECIMAL(3,1) [note: '服務品質 (0-5)']
//   "environment_rating" DECIMAL(3,1) [note: '環境清潔度 (0-5)']
//   "price_rating" DECIMAL(3,1) [note: '價格合理度 (0-5)']
  
//   // 評論內容
//   "title" NVARCHAR(200) [note: '評論標題']
//   "content" NVARCHAR(MAX) [not null, note: '評論內容']
  
//   // 觀影體驗
//   "visit_date" DATE [note: '觀影日期']
//   "movie_watched" NVARCHAR(200) [note: '觀看的電影']
  
//   // 統計數據
//   "helpful_count" INT [default: 0, note: '有用按鈕計數']
//   "view_count" INT [default: 0, note: '瀏覽次數']
  
//   // 關聯到文章（可選）
//   "linked_post_id" INT [null, note: '關聯的論壇文章ID']
  
//   "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
//   "updated_at" DATETIME [null]
  
// }



// // 評論有用/沒用投票表
// table "review_helpful_votes" {
//   "review_helpful_votes_id" INT [pk, increment]
//   "review_type" VARCHAR(20) [not null, note: 'movie 或 cinema']
//   "review_id" INT [not null]
//   "user_id" INT [not null, note: '投票者ID']
//   "is_helpful" BIT [not null, note: '1=有用, 0=沒用']
//   "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
// }

// // 觀影清單（想看/看過）
// // 儲存清單想看的，類似 table save_posts
// table "user_movie_list" {
//   "user_movie_list_id" INT [pk, increment]
//   "user_id" INT [not null]
//   "movies_id" INT [not null]
//   "status" VARCHAR(20) [not null, note: 'want_to_watch, watched']
//   "watch_date" DATE [note: '觀看日期']
//   "personal_rating" DECIMAL(3,1) [note: '個人評分']
//   "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
// }
// // rating_李宥廷外鍵
// // ========== 外鍵關聯 ==========
// // 電影評論關聯
// Ref: "user"."user_id" < "movie_reviews"."user_id"
// Ref: "movies"."movies_id" < "movie_reviews"."movies_id"
// Ref: "posts"."posts_id" < "movie_reviews"."linked_post_id"
// // 影廳評論關聯
// Ref: "user"."user_id" < "cinema_reviews"."user_id"
// Ref: "cinemas"."cinemas_id" < "cinema_reviews"."cinemas_id"
// Ref: "posts"."posts_id" < "cinema_reviews"."linked_post_id"
// // 評論投票關聯
// Ref: "user"."user_id" < "review_helpful_votes"."user_id"
// // 觀影清單關聯
// Ref: "user"."user_id" < "user_movie_list"."user_id"
// Ref: "movies"."movies_id" < "user_movie_list"."movies_id"