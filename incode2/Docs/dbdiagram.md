// forum_李宥廷
// 內容
table content {
  content_id int [pk, increment]
  parent_content_id int [ref: > content.content_id, note: "CONSTRAINT FK_content.content_id自我關聯，支援巢狀結構"]
  category_id int [ref: > content_category.content_category_id, note:"內容分類"]
  user_id int [not null, ref: > user.user_id, note: "CONSTRAINT FK_content_user"]
  title nvarchar(200) [not null, note: "內容標題"]
  body nvarchar(max) [not null, note: "內容內文"]
  created_at datetime [default:`current_timestamp`]
  updated_at datetime [default:`current_timestamp`]
  ishidden int [note: "自己的內容才可隱藏"]
  note {"發文、留言、回文、留言回覆都在此表， 想支援(1)主文刪除後，子內容持續顯示、(2)從回覆提升成發文(因為下面討論熱烈)"}
}

// 內容分類
table content_category {
  content_category_id int [pk, increment, note:"有1個id是新增用"]
  parent_category_id int [ref: > content_category.content_category_id, note: "自我關聯，支援巢狀結構"]
  category_name nvarchar(100) [not null]
  description nvarchar(200)
  ishidden int [note: "隱藏"]
  note {"先選好再發文，類似麵包屑，電影>動畫片>Zootopia2、影城>屏東縣>中影。可以讓使用者自己新增，官方審核，通過後給點"}
}

// 使用者與內容的互動
table content_interaction {
  content_interaction_id int [pk, increment]
  category_id int [not null, ref: > interaction_category.interaction_category_id, note:"互動分類:like、dislike、block、bookmark、隱藏自己的內容"]
  content_id int [not null, ref: > content.content_id]
  user_id int [not null, ref: > user.user_id]
  created_at datetime [default:`current_timestamp`]
  note {"類似按讚表的擴增"}
}

// 互動分類
table interaction_category {
  interaction_category_id int [pk, increment]
  name nvarchar(100) [not null]
  description nvarchar(200)
  note {"互動分類:like、dislike、block、bookmark、隱藏自己的內容"}
}

// 內容歷史紀錄(編輯版控)
table content_history {
  content_history_id int [pk, increment]
  content_id int [ref: > content.content_id]
  user_id int [ref: > user.user_id]
  old_title nvarchar(200) [note:"舊標題"]
  old_body nvarchar(max) [not null, note:"舊內文"]
  ishidden bit [note: "自己的內容才可隱藏"]
  edited_at datetime [default:`current_timestamp`]
  note {"編輯歷史，只存舊資料"}
}

// 通知(有人回覆/互動)
table content_notification {
  content_notification_id int [pk, increment]
  user_id int [ref: > user.user_id, note:"接收通知的人"]
  content_id int [ref: > content.content_id]
  category_id int [ref: > interaction_category.interaction_category_id, note: "like, reply, bookmark..."] 
  description nvarchar(200) [note:"此通知說明"]
  created_at datetime [default:`current_timestamp`]
  isread bit
  note {"通知(有人回覆/互動)，當互動產"}
}

// 前台圖片管理
table user_image {
  user_image_id int [pk, increment]
  user_id int [not null, ref: > user.user_id, note: '使用者id']
  image_url nvarchar(500) [not null, note: '圖片url']
  caption nvarchar(200) [note: '圖片說明:帥氣的狗(用在優化體驗，當圖片顯示不出來時)']
  display_order int [default: 0, note: '顯示順序:輪播時可以調整順序']
  created_at datetime [default: `current_timestamp`]
}

// User_凃恩雯
// 使用者主表
table user{ 
  user_id integer [primary key, increment]
  account varchar(150) [unique, not null]
  username nvarchar(150) [unique, not null]
  password nvarchar(255) [not null]
  image_url nvarchar(500) [not null]
  phone varchar(15)  
  birthday date
  address  nvarchar(500)
  created_at datetime [default: `CURRENT_TIMESTAMP`]
  is_active bit [default: 1]
}

// 管理者權限
table admin{ 
admin_id integer [primary key]
type varchar [note:'guest,baby-member,member,admin,superadmin']
autority integer
}

// 會員互相追蹤(單向)
table follow { 
  follower_id integer [primary key,ref: >user.user_id]
  followee_id integer [primary key,ref: >user.user_id]
  created_at datetime [default: `CURRENT_TIMESTAMP`]
}

// 登入紀錄
table user_login{ 
  user_id integer [ref: >user.user_id]
  logintime datetime [default: `CURRENT_TIMESTAMP`]
  location varchar 
}

// 目前狀態
table user_status{ 
   user_id integer [ref: >user.user_id]
   experience integer
   rank integer
   item_id integer
   quantity integer
 }

//Point_Mission_Wallet_邱揚雅
//功能:點數/任務/金流
//=========點數===========

//點數交易明細
table point_log {
  point_log_id int [pk, increment, note: '點數異動紀錄主鍵']
  user_id int [not null, note: '獲得/扣除點數的用戶', ref: > user.user_id]
  source_type nvarchar(50) [not null, note:'點數異動來源類型']
  source_id int [not null, note:'點數異動來源類型的id']
  point_type_definition_id int [not null, note: '點數異動類型', ref: > point_type_definition.point_type_definition_id]
  change_amount int [not null, note: '本次變動點數（正數＝收入，負數＝支出）']
  balance_after int [not null, note: '變動後的可用餘額']
  created_at datetime [default: `CURRENT_TIMESTAMP`]
}

//任務類型定義
table point_type_definition {
  point_type_definition_id int [pk, increment, note: '點數類型主鍵']
  type_name nvarchar(50) [unique, not null, note: '程式用代碼，例如：recharge、mission_reward']
  display_name nvarchar(100) [not null, note: '前台顯示名稱，例如：儲值點數、任務獎勵,扣除委託任務點數']
  description nvarchar(100) [note: '後台管理員看的詳細說明，可很長']
}

//=========任務===========
//平台任務 
Table mission {
  mission_id int [pk, increment, note: '平台任務主鍵']
  mission_name nvarchar(100) [not null, note: '任務標題']
  mission_detail nvarchar(500) [not null, note: '任務詳細說明']
  target_type nvarchar(50) [not null, default: '', note: '目標類型，例如：post、comment、login']
  target_count int [not null, check: `[target_count] >= 1`,  note: '需要達成的次數/數量']
  reward_points int [not null, check: `[reward_points] >= 1`, note: '完成後獎勵點數']
  reward_exp int [not null, check: `[reward_exp] >= 1`, note: '完成後獎勵經驗值']
  reset_type NVARCHAR(50) [not null, check: `[reset_type] IN ('none','daily','weekly','monthly','yearly')`, default: 'none', note: '重置週期']
  created_at datetime [default: `CURRENT_TIMESTAMP`]
}

//平台任務:會員接任務狀態(含歷史) 
Table mission_progress {
  mission_progress_id int [pk, increment, note: '用戶任務進度主鍵']
  user_id int [not null,note: '用戶ID', ref: > user.user_id]
  mission_id int [not null,note: '所屬平台任務', ref: > mission.mission_id]
  current_count int [not null, default: 0, note: '目前已完成次數']
  accepted_at datetime [default: `CURRENT_TIMESTAMP`, note: '開始計算進度的時間']
  completed_at datetime [null, note: '實際完成時間']
  rewarded_at datetime [null, note: '實際撥款的時間:user點選領取點數按鈕']
}

//會員委託任務
Table entrust_mission {
  entrust_mission_id int [pk, increment, note: '委託任務主鍵']
  user_id int [not null, note: '發布者ID', ref: > user.user_id]
  title nvarchar(150) [not null, note: '委託任務標題']
  detail nvarchar(500) [not null, note: '委託任務詳細內容']
  reward_points int [not null, check: `[reward_points] >= 1`, note: '完成可得點數']
  max_participants int [not null, default: `1`, note:'接取任務人數只能1位']
  status nvarchar(50) [not null, check: `[status] IN ('open','closed','cancelled')`, default: 'open', note: '任務狀態']
  created_at datetime [default: `CURRENT_TIMESTAMP`, note: '發布時間']
}

//會員委託任務的目前進度
Table entrust_mission_progress {
  entrust_mission_progress_id int [pk, increment, note: '委託任務進度主鍵']
  user_id int [not null, note: '接任務者ID', ref: > user.user_id]
  entrust_mission_id int [not null, note: '所屬委託任務', ref: > entrust_mission.entrust_mission_id]
  status nvarchar(50) [check: `[status] IN ('accepted','submitted','completed','falsed','cancelled')`, default: 'accepted', note: 'pending=已接取 → submitted=已提交證明 → approved=通過發獎 → rejected=駁回']
  accepted_at datetime [default: `CURRENT_TIMESTAMP`, note: '開始計算進度的時間']
  completed_at datetime [null, note: '實際完成時間']
  rewarded_at datetime [null, note: '實際撥款的時間:user點選領取點數按鈕']
}

//會員委託任務預扣點數
Table user_point_wallet {
  user_id int [pk,ref: > user.user_id]
  current_points int [not null, default: 0, note: '可用點數']
  frozen_points int [not null, default: 0, note: '凍結中點數']
  updated_at datetime [default: `CURRENT_TIMESTAMP`]
}

//任務表:從哪裡拉你接的是甚麼任務(留言表)

//金流
//錢包
// 儲值成功紀錄表（只有成功才會寫入這張表）
Table point_deposits {
  point_deposit_id int [pk, increment, note: '儲值成功紀錄主鍵']
  user_id int [not null, note: '用戶ID', ref: > user.user_id]
  order_no varchar(100) [unique, not null, note: '內部或第三方訂單編號']
  cash_amount decimal(12,2) [not null, note: '實際支付台幣金額']
  points_granted int [not null, note: '本次給予的點數']
  payment_method varchar(50) [not null, note: '付款方式：credit_card, convenience_store, line_pay 等']
  related_order_id varchar(100) [note: '第三方金流返回的訂單號（如藍新 MerchantTradeNo）']
  status nvarchar(50) [check: `[status] IN ('pending', 'success', 'failed', 'expired')`, default: 'pending', note: 'pending(待付), success(成功), failed(失敗), expired(過期)']
  created_at datetime [default: `CURRENT_TIMESTAMP`, note: '付款成功時間']
}

//PointShop_嚴家胤 

//商品資料管理
table product {
  product_id INT [pk, increment, note: '商品ID (主鍵)']
  name NVARCHAR(50) [not null, note: '商品名稱']
  description NVARCHAR(300) [note: '商品詳細描述']
  image_name NVARCHAR(50) [note: '圖片檔案名稱']
  type NVARCHAR(30) [note: '商品分類']
  is_physical BIT [not null, note: '是否為實體商品 (1=實體, 0=虛擬)']
  point INT [not null, note: '商品點數']
  stock INT [not null, default: 0, note: '庫存數量']
  created_at DATETIME [not null, default: `CURRENT_TIMESTAMP`, note: '新增時間']
  updated_at DATETIME [not null, default: `CURRENT_TIMESTAMP`, note: '最後修改時間']
  on_sale BIT [not null, default: 1, note: '是否販售(1=販售中, 0=未上架)']
  note {'商品預計只下架、不刪除資料'}
}

//訂單
table orders {
  order_id INT [pk, increment, note: '訂單ID (主鍵)']
  user_id INT [not null, ref:>user.user_id, note: '購買人ID (外來鍵，連結到User表)']
  total_point INT [not null, note: '訂單總點數']
  order_date DATETIME [not null, default: `CURRENT_TIMESTAMP`, note: '訂單成立時間']
  remark NVARCHAR(300) [note: '訂單備註資訊']
  status NVARCHAR(50)  [not null, note:'訂單狀態:待處理/已出貨/已完成/已取消']
  canceled_at DATETIME [note: '取消時間']
  note {'order為資料庫保留字，怕會出問題所以加s，訂單只取消不刪除'}
 }

//訂單Detail
table orderdetail {
  orderdetail_id INT [pk, increment, note: 'ID (主鍵)']
  order_id INT [not null, ref:>orders.order_id , note: '訂單ID (外來鍵)'] 
  product_id INT [not null, ref:> product.product_id ,note: '商品ID (外來鍵)']
  quantity INT [not null, note: '購買數量']
  unit_point_at_purchase INT [not null, note: '購買時單價']
  requires_shipping BIT [not null, note: '是否需寄送商品 (1=需要, 0=不需)']
}

//寄送Detail
table shipped_detail{
  shipped_detail_id INT [pk, increment, note: 'ID (主鍵)']
  orderdetail_id INT [not null, ref:> orderdetail.orderdetail_id , note: '訂單DetailID (外來鍵)']
  recipient_name NVARCHAR(50) [not null, note: '收件人姓名']
  recipient_address NVARCHAR(50) [not null, note: '收件人地址']
  recipient_phone NVARCHAR(50) [not null, note: '收件人電話']
  shipped_date DATETIME [note: '寄送日期']
  arrival_date DATETIME [note: '送達日期']
  is_failed BIT [not null,default: 0, note:'寄送失敗']
  remark NVARCHAR(300) [note: '寄送備註']
  is_completed BIT [not null, default: 0, note: '是否完成']
}

// chatroom_林宜蒨
// 對話工作階段
Table chat_session {
  chat_session_id int [pk, increment, note: "對話房間的唯一識別碼"]
  user_id int [not null, ref: > user.user_id, note: "發起對話的使用者(FK)"]
  title nvarchar(100) [not null, note: "對話標題"]
  created_at datetime [default: `current_timestamp`, note: "當下建立的時間"]
  updated_at datetime [default: `current_timestamp`, note: "當下更新的時間"]
  note { "聊天室列表與狀態紀錄" }
}

// 對話訊息內容
Table chat_message {
  chat_message_id bigint [pk, increment, note: "訊息唯一識別碼"]
  session_id int [not null, ref: > chat_session.chat_session_id, note: "所屬對話ID(FK)"]
  role varchar(20) [not null, note: "角色: system, user, assistant"]
  body nvarchar(max) [not null, note: "訊息內容"]
  created_at datetime [default: `current_timestamp`, note: "當下發送的時間"] 
  note { "對話內容紀錄" }
}