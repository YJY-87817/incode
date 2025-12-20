// rating_李宥廷
// 電影資料表
table "movies" {
  "movies_id" INT [pk, increment, note: '電影ID']
  "title" NVARCHAR(200) [not null, note: '電影名稱']
  "original_title" NVARCHAR(200) [note: '原文片名']
  "director" NVARCHAR(100) [note: '導演']
  "genre" NVARCHAR(100) [note: '類型（可用逗號分隔多個）']
  "release_date" DATE [note: '上映日期']
  "duration" INT [note: '片長（分鐘）']
  "rating" DECIMAL(3,1) [note: '平均評分 (0-10)']
  "poster_url" NVARCHAR(500) [note: '海報圖片URL']
  "synopsis" NVARCHAR(MAX) [note: '劇情簡介']
  "trailer_url" NVARCHAR(500) [note: '預告片連結']
  "imdb_id" VARCHAR(50) [note: 'IMDB編號']
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  "updated_at" DATETIME [default: `CURRENT_TIMESTAMP`]
}

// 影廳/電影院資料表
table "cinemas" {
  "cinemas_id" INT [pk, increment, note: '影廳ID']
  "name" NVARCHAR(100) [not null, note: '影廳名稱']
  "chain" NVARCHAR(50) [note: '連鎖品牌（威秀、國賓等）']
  "address" NVARCHAR(200) [not null, note: '地址']
  "city" NVARCHAR(50) [note: '城市']
  "district" NVARCHAR(50) [note: '區域']
  "latitude" DECIMAL(10,7) [note: '緯度（地圖用）']
  "longitude" DECIMAL(10,7) [note: '經度（地圖用）']
  "phone" VARCHAR(20) [note: '電話']
  "website" NVARCHAR(200) [note: '官網']
  // 用記錄去算總分即可
  // "rating" DECIMAL(3,1) [note: '平均評分 (0-5)']
  "facilities" NVARCHAR(500) [note: '設施（JSON格式）']
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  "updated_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  
  indexes {
    (latitude, longitude) [name: 'idx_location', note: '地理位置索引']
    city [name: 'idx_city']
  }
}

// 電影評論表
table "movie_reviews" {
  "movie_reviews_id" INT [pk, increment, note: '評論ID']
  "movies_id" INT [not null, note: '電影ID (外鍵)']
  "user_id" INT [not null, note: '評論者ID']
  
  // 評分細項
  "overall_rating" DECIMAL(3,1) [not null, note: '總體評分 (0-10)']
  "story_rating" DECIMAL(3,1) [note: '劇情評分 (0-10)']
  "acting_rating" DECIMAL(3,1) [note: '演技評分 (0-10)']
  "visual_rating" DECIMAL(3,1) [note: '視覺效果評分 (0-10)']
  "music_rating" DECIMAL(3,1) [note: '配樂評分 (0-10)']
  
  // 評論內容
  "title" NVARCHAR(200) [note: '評論標題']
  "content" NVARCHAR(MAX) [note: '評論內容']
  "is_spoiler" BIT [default: 0, note: '是否有劇透 (0=無, 1=有)']
  
  // 統計數據
  "helpful_count" INT [default: 0, note: '有用按鈕計數']
  "view_count" INT [default: 0, note: '瀏覽次數']
  
  // 關聯到文章（可選）
  "linked_post_id" INT [null, note: '關聯的論壇文章ID（如嵌入文章）']
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  "updated_at" DATETIME [null]
  
  indexes {
    (movies_id, user_id) [unique, note: '一人對同一電影只能評一次']
    movies_id [name: 'idx_movie']
    user_id [name: 'idx_user']
    overall_rating [name: 'idx_rating']
  }
}

// 影廳評論表
table "cinema_reviews" {
  "cinema_reviews_id" INT [pk, increment, note: '評論ID']
  "cinemas_id" INT [not null, note: '影廳ID (外鍵)']
  "user_id" INT [not null, note: '評論者ID']
  
  // 評分細項
  // 由每一筆紀錄count，不存總數
  // "overall_rating" DECIMAL(3,1) [not null, note: '總體評分 (0-5)']
  "seat_rating" DECIMAL(3,1) [note: '座位舒適度 (0-5)']
  "screen_rating" DECIMAL(3,1) [note: '螢幕/音響品質 (0-5)']
  "service_rating" DECIMAL(3,1) [note: '服務品質 (0-5)']
  "environment_rating" DECIMAL(3,1) [note: '環境清潔度 (0-5)']
  "price_rating" DECIMAL(3,1) [note: '價格合理度 (0-5)']
  
  // 評論內容
  "title" NVARCHAR(200) [note: '評論標題']
  "content" NVARCHAR(MAX) [not null, note: '評論內容']
  
  // 觀影體驗
  "visit_date" DATE [note: '觀影日期']
  "movie_watched" NVARCHAR(200) [note: '觀看的電影']
  
  // 統計數據
  "helpful_count" INT [default: 0, note: '有用按鈕計數']
  "view_count" INT [default: 0, note: '瀏覽次數']
  
  // 關聯到文章（可選）
  "linked_post_id" INT [null, note: '關聯的論壇文章ID']
  
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  "updated_at" DATETIME [null]
  
  // indexes {
  //   (cinema_id, user_id) [unique, note: '一人對同一影廳只能評一次']
  //   cinema_id [name: 'idx_cinema']
  //   user_id [name: 'idx_user']
  //   overall_rating [name: 'idx_rating']
  // }
}

// 圖片管理表（前台共用）
table "images" {
  "images_id" INT [pk, increment]
  "user_id" INT [not null, note: '使用者ID']
  "image_url" NVARCHAR(500) [not null, note: '圖片URL']
  "category" INT [not null, note: '圖片使用在哪裡']
  "caption" NVARCHAR(200) [note: '圖片說明']
  "display_order" INT [default: 0, note: '顯示順序']
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  
  // indexes {
  //   (review_type, review_id) [name: 'idx_review']
  // }
}

// 評論有用/沒用投票表
table "review_helpful_votes" {
  "review_helpful_votes_id" INT [pk, increment]
  "review_type" VARCHAR(20) [not null, note: 'movie 或 cinema']
  "review_id" INT [not null]
  "user_id" INT [not null, note: '投票者ID']
  "is_helpful" BIT [not null, note: '1=有用, 0=沒用']
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  
  // indexes {
  //   (review_type, review_id, user_id) [unique, note: '防止重複投票']
  // }
}

// 觀影清單（想看/看過）
// 儲存清單想看的，類似 table save_posts
table "user_movie_list" {
  "user_movie_list_id" INT [pk, increment]
  "user_id" INT [not null]
  "movies_id" INT [not null]
  "status" VARCHAR(20) [not null, note: 'want_to_watch, watched']
  "watch_date" DATE [note: '觀看日期']
  "personal_rating" DECIMAL(3,1) [note: '個人評分']
  "created_at" DATETIME [default: `CURRENT_TIMESTAMP`]
  
  // indexes {
  //   (user_id, movie_id) [unique]
  // }
}

// rating_李宥廷外鍵
// ========== 外鍵關聯 ==========
// 電影評論關聯
Ref: "user"."user_id" < "movie_reviews"."user_id"
Ref: "movies"."movies_id" < "movie_reviews"."movies_id"
Ref: "posts"."posts_id" < "movie_reviews"."linked_post_id"
// 影廳評論關聯
Ref: "user"."user_id" < "cinema_reviews"."user_id"
Ref: "cinemas"."cinemas_id" < "cinema_reviews"."cinemas_id"
Ref: "posts"."posts_id" < "cinema_reviews"."linked_post_id"
// 評論投票關聯
Ref: "user"."user_id" < "review_helpful_votes"."user_id"
// 觀影清單關聯
Ref: "user"."user_id" < "user_movie_list"."user_id"
Ref: "movies"."movies_id" < "user_movie_list"."movies_id"
// 圖片關聯
Ref: "user"."user_id" < "images"."user_id"


// User_凃恩雯
// 使用者主表
// 安全性(密碼存雜湊)
table user{ 
  user_id integer [primary key, increment] //identity(1,1)
  account nvarchar(150) [unique, not null]
  username varchar(150) [unique, not null]
  password varchar(50) [not null]
  phone integer 
  image_url NVARCHAR(500) [not null, note: '圖片URL']
  created_at datetime [default: `CURRENT_TIMESTAMP`]
  is_active bit [default: 1]
}

// 管理者權限
// guest,baby-member,member,admin,superadmin
table admin{ 
admin_id integer [primary key]
type varchar
autority integer
}

// 會員互相追蹤(單向)
// 複合主鍵(follower_id,followee_id)
table follows { 
  follower_id integer [primary key]
  followee_id integer [primary key]
  created_at datetime [default: `CURRENT_TIMESTAMP`]
  //疑慮??以下這行不用
  primary key (follower_id, followee_id)
}

// 登入紀錄
table user_login{ 
  user_id integer
  logintime datetime [default: `CURRENT_TIMESTAMP`]
  location varchar 
}

// 目前狀態
table user_status{ 
   user_id integer   
   experience integer
   rank integer
   item_id integer
   quantity integer
 }

Ref: user.user_id < follows.follower_id
Ref: user.user_id < follows.followee_id
Ref: user.user_id < user_status.user_id
Ref: user.user_id < user_login.user_id

//Point_Mission_Wallet_邱揚雅

//功能:點數/任務/金流
//=========點數===========

//點數交易明細
table "point_log" {
  "point_log_id" int [pk, increment, note: '點數異動紀錄主鍵']
  "user_id" int [not null, note: '獲得/扣除點數的用戶']
  "source_type" nvarchar(50) [not null, note:'點數異動來源類型']
  "source_id" int [not null, note:'點數異動來源類型的id']
  "point_type_definition_id" int [not null, note: '點數異動類型']
  "change_amount" int [not null, note: '本次變動點數（正數＝收入，負數＝支出）']
  "created_at" datetime [default: `CURRENT_TIMESTAMP`]

  Indexes {
    (user_id, created_at )          // 用戶查點數明細最常用索引
    (source_type, source_id)            // 從來源（如訂單、儲值）反查點數異動
    (point_type_definition_id)          // 若依類型統計時有用
    (user_id, point_log_id)        // 備用，快速取最新一筆
  }
}

//任務類型定義
table "point_type_definition" {
  "point_type_definition_id" int [pk, increment, note: '點數類型主鍵']
  "type_name" nvarchar(50) [unique, not null, note: '程式用代碼，例如：recharge、mission_reward']
  "display_name" nvarchar(100) [not null, note: '前台顯示名稱，例如：儲值點數、任務獎勵,扣除委託任務點數']
  "description" nvarchar(100) [note: '後台管理員看的詳細說明，可很長']
}

//=========任務===========
//平台任務 
Table "mission" {
  "mission_id" int [pk, increment, note: '平台任務主鍵']
  "mission_name" nvarchar(100) [not null, note: '任務標題']
  "mission_detail" nvarchar(500) [not null, note: '任務詳細說明']
  "target_type" nvarchar(50) [not null, default: '', note: '目標類型，例如：post、comment、login']
  "target_count" int [not null, check: `[target_count] >= 1`,  note: '需要達成的次數/數量']
  "reward_points" int [not null, check: `[reward_points] >= 1`, note: '完成後獎勵點數']
  "reward_exp" int [not null, check: `[reward_exp] >= 1`, note: '完成後獎勵經驗值']
  "reset_type" NVARCHAR(50) [not null, check: `[reset_type] IN ('none','daily','weekly','monthly','yearly')`, default: 'none', note: '重置週期']
  "created_at" datetime [default: `CURRENT_TIMESTAMP`]
}

//平台任務:會員接任務狀態(含歷史) 
Table "mission_progress" {
  "mission_progress_id" int [pk, increment, note: '用戶任務進度主鍵']
  "user_id" int [not null,note: '用戶ID']
  "mission_id" int [not null,note: '所屬平台任務']
  "current_count" int [not null, default: 0, note: '目前已完成次數']
  "is_completed" bit [default: '0', note: '是否已完成領獎']
  "accepted_at" datetime [default: `CURRENT_TIMESTAMP`, note: '開始計算進度的時間']
  "completed_at" datetime [note: '實際完成時間']

  Indexes {
    (user_id, mission_id) [unique, name: "mission_progress_index_1"]
  }
}

//會員委託任務
Table "entrust_mission" {
  "entrust_mission_id" int [pk, increment, note: '委託任務主鍵']
  "user_id" int [not null, note: '發布者ID']
  "title" nvarchar(150) [not null, note: '委託任務標題']
  "detail" nvarchar(500) [not null, note: '委託任務詳細內容']
  "reward_points" int [not null, check: `[reward_points] >= 1`, note: '每人完成可得點數']
  "status" nvarchar(50) [not null, check: `[status] IN ('open','closed','cancelled')`, default: 'open', note: '任務狀態']
  "created_at" datetime [default: `CURRENT_TIMESTAMP`, note: '發布時間']
}

//會員委託任務的目前進度
Table "entrust_mission_progress" {
  "entrust_mission_progress_id" int [pk, increment, note: '委託任務進度主鍵']
  "user_id" int [not null, note: '接任務者ID']
  "entrust_mission_id" int [not null, note: '所屬委託任務']
  "status" nvarchar(50) [check: `[status] IN ('accepted','submitted','completed','falsed','cancelled')`, default: 'accepted', note: 'pending=已接取 → submitted=已提交證明 → approved=通過發獎 → rejected=駁回']
  "is_completed" bit [default: '0', note: '是否已完成領獎']
  "created_at" datetime [default: `CURRENT_TIMESTAMP`, note: '接任務時間']

  Indexes {
    (entrust_mission_id, user_id) [unique, name: "entrust_mission_progress_index_2"]
  }
}

//任務表:從哪裡拉你接的是甚麼任務(留言表)

//金流
//錢包
// 儲值成功紀錄表（只有成功才會寫入這張表）
Table "point_deposits" {
  "point_deposit_id" int [pk, increment, note: '儲值成功紀錄主鍵']
  "user_id" int [not null, note: '用戶ID']
  "order_no" varchar(100) [unique, not null, note: '內部或第三方訂單編號']
  "cash_amount" decimal(12,2) [not null, note: '實際支付台幣金額']
  "points_granted" int [not null, note: '本次給予的點數']
  "payment_method" varchar(50) [not null, note: '付款方式：credit_card, convenience_store, line_pay 等']
  "related_order_id" varchar(100) [note: '第三方金流返回的訂單號（如藍新 MerchantTradeNo）']
  "created_at" datetime [default: `CURRENT_TIMESTAMP`, note: '付款成功時間']

  Indexes {
    (user_id)
    (order_no)
    (related_order_id)
    (created_at)
  }
}

// 要新增2個任務table的user_id去 fk user
Ref:"user"."user_id" < "point_log"."user_id"
Ref:"mission"."mission_id" < "mission_progress"."mission_id"
Ref:"point_type_definition"."point_type_definition_id" < "point_log"."point_type_definition_id"
Ref:"entrust_mission"."entrust_mission_id" < "entrust_mission_progress"."entrust_mission_id"
Ref:"user"."user_id" < "mission_progress"."user_id"
Ref:"user"."user_id" < "entrust_mission"."user_id"
Ref:"user"."user_id" < "entrust_mission_progress"."user_id"
Ref: "user"."user_id" < "point_deposits"."user_id"

//PointShop_嚴家胤 

//商品資料管理
table "product" {
  "product_id" INT [pk, increment, note: '商品ID (主鍵)']
  "name" NVARCHAR(50) [not null, note: '商品名稱']
  "description" NVARCHAR(300) [note: '商品詳細描述']
  "image_name" NVARCHAR(50) [note: '圖片檔案名稱']
  "type" NVARCHAR(30) [note: '商品分類']
  "isphysical" BIT [not null, note: '是否為實體商品 (1=實體, 0=虛擬)']
  "point" INT [not null, note: '商品點數']
  "stock" INT [not null, default: 0, note: '庫存數量']
  "created_at" DATETIME [not null, default: `CURRENT_TIMESTAMP`, note: '新增時間']
  "updated_at" DATETIME [not null, default: `CURRENT_TIMESTAMP`, note: '最後修改時間']
  "onsale" BIT [not null, default: 1, note: '是否販售(1=販售中, 0=未上架)']
}

//訂單
table "order" {
  "order_id" INT [pk, increment, note: '訂單ID (主鍵)']
  "user_id" INT [not null, note: '購買人ID (外來鍵，連結到User表)']
  "total_point" INT [not null, note: '訂單總點數']
  "order_date" DATETIME [not null, default: `CURRENT_TIMESTAMP`, note: '訂單成立時間']
  "shipping" BIT [not null, note: '是否需寄送商品 (1=需要, 0=不需)']
  "note" NVARCHAR(300) [note: '訂單備註資訊']
  "cancel_order" BIT [not null, default: 0, note:'取消訂單(1=取消)']
 }

//訂單Detail
table "orderdetail" {
  "orderdetail_id" INT [pk, increment, note: 'ID (主鍵)']
  "order_id" INT [not null, note: '訂單ID (外來鍵)']
  "product_id" INT [not null, note: '商品ID (外來鍵)']
  "quantity" INT [not null, note: '購買數量']
  "unit_point_at_purchase" INT [not null, note: '購買時單價']
  "shipped" BIT [not null, default: 0, note: '是否需寄送']
}

//寄送Detail
table "shipped_detail"{
  "shipped_detail_id" INT [pk, increment, note: 'ID (主鍵)']
  "orderdetail_id" INT [not null, note: '訂單DetailID (外來鍵)']
  "recipient_name" NVARCHAR(50) [not null, note: '收件人姓名']
  "recipient_address" NVARCHAR(50) [not null, note: '收件人地址']
  "recipient_phone" NVARCHAR(50) [not null, note: '收件人電話']
  "shipped_date" DATETIME [note: '寄送日期']
  "arrival_date" DATETIME [note: '送達日期']
  "completed" BIT [not null, default: 0, note: '是否完成']
}

ref:"user"."user_id" < "order"."user_id"
ref:"order"."order_id" < "orderdetail"."order_id" [delete: cascade]
ref:"product"."product_id" < "orderdetail"."product_id"
ref:"orderdetail"."orderdetail_id" < "shipped_detail"."orderdetail_id"

// Post_林宜蒨

// 文章
table "posts" {
  "posts_id" int [pk, increment, not null, note: '文章ID (主鍵)']
  "user_id" int [not null, note: '發文者ID (外鍵)']
  "title" nvarchar(200) [not null, note: '文章標題']
  "content" nvarchar(max) [not null, note: '文章內容 (HTML)']
  "view_count" int [default: 0, note: '文章瀏覽次數']
  "created_at" datetime [note: '發文時間']
  "updated_at" datetime [null, note: '最後更新時間']
  // 委託任務
  "IsCommission" bit [not null, default: 0, note: '0:一般文, 1:委託文']
  
}

// 文章收藏
table "save_posts" {
  "save_posts_id" int [pk, increment, not null, note: '收藏紀錄ID (主鍵)']
  "user_id" int [not null, note: '收藏者ID (外鍵)']
  "post_id" int [not null, note: '被收藏文章ID (外鍵)']
  "created_at" datetime [note: '收藏時間']

  indexes {
    (user_id, post_id) [unique, note: '複合索引: 防止重複收藏同一篇']
  }
}

// 文章按讚
table "post_likes" {
  "post_likes_id" int [pk, increment, not null, note: '文章按讚紀錄ID (主鍵)']
  "post_id" int [not null, note: '被按讚文章ID (外鍵)']
  "user_id" int [not null, note: '按讚者ID (外鍵)']
  "created_at" datetime [note: '按讚時間']

  indexes {
    (post_id, user_id) [unique, note: '複合索引: 防止重複對文章按讚']
  }
}

// 留言
table "post_replies" {
  "post_replies" int [pk, increment, not null, note: '留言ID (主鍵)']
  "post_id" int [not null, note: '所屬文章ID (外鍵)']
  "user_id" int [not null, note: '留言者ID (外鍵)']
  "target_user_id" int [null, note: '被回覆者ID ']
  "content" nvarchar(max) [not null, note: '留言內容']
  "created_at" datetime [note: '留言時間']
}

// 留言按讚
table "post_reply_likes" {
  "reply_like_id" int [pk, increment, not null, note: '留言按讚紀錄ID (主鍵)']
  "reply_id" int [not null, note: '被按讚留言ID (外鍵)']
  "user_id" int [not null, note: '按讚者ID (外鍵)']
  "created_at" datetime [note: '按讚時間']

  indexes {
    (reply_id, user_id) [unique, note: '複合索引: 防止重複對留言按讚']
  }
}

Ref: "user"."user_id" < "posts"."user_id"
Ref: "user"."user_id" < "save_posts"."user_id"
Ref: "user"."user_id" < "post_likes"."user_id"
Ref: "user"."user_id" < "post_replies"."user_id"
Ref: "user"."user_id" < "post_reply_likes"."user_id"