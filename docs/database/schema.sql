-- =============================================================================
-- incode 資料庫建表腳本 (SQL Server)
--
-- 由 EF Core 模型 (incode2/Models/incodedatabaseContext.cs) 產生：
--   cd incode2
--   dotnet ef dbcontext script -o ../docs/database/schema.sql
-- 模型為 Database-First 從原資料庫產生，資料表、欄位、預設值、外鍵、索引皆包含在內；
-- 原資料庫若有 CHECK 條件、Trigger、View 等模型未描述的物件，不會出現在此腳本中。
--
-- 使用方式：先建立空資料庫，再依序執行 schema.sql、seed.sql（詳見 docs/database/README.md）
-- =============================================================================

CREATE TABLE [content_category] (
    [content_category_id] int NOT NULL IDENTITY,
    [parent_category_id] int NULL,
    [category_name] nvarchar(100) NOT NULL,
    [description] nvarchar(200) NULL,
    [is_active] int NOT NULL,
    CONSTRAINT [PK__content___0FFD1C56E08BA7CE] PRIMARY KEY ([content_category_id]),
    CONSTRAINT [FK__content_c__paren__3D2915A8] FOREIGN KEY ([parent_category_id]) REFERENCES [content_category] ([content_category_id])
);
GO


CREATE TABLE [interaction_category] (
    [interaction_category_id] int NOT NULL IDENTITY,
    [name] nvarchar(100) NOT NULL,
    [description] nvarchar(200) NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    [is_active] int NOT NULL,
    CONSTRAINT [PK__interact__45DCC9961C981550] PRIMARY KEY ([interaction_category_id])
);
GO


CREATE TABLE [mission] (
    [mission_id] int NOT NULL IDENTITY,
    [mission_name] nvarchar(100) NOT NULL,
    [mission_detail] nvarchar(500) NOT NULL,
    [target_type] nvarchar(50) NOT NULL,
    [target_count] int NOT NULL,
    [reward_points] int NOT NULL,
    [reward_exp] int NOT NULL,
    [category] nvarchar(50) NOT NULL,
    [difficulty] nvarchar(20) NULL DEFAULT N'中等', -- EF 模型中此預設值已變成亂碼 '??'，暫定為前端選項中的「中等」，請依原資料庫確認
    [estimated_duration] nvarchar(50) NULL,
    [reset_type] nvarchar(50) NOT NULL DEFAULT N'none',
    [participant_count] int NULL DEFAULT 0,
    [is_featured] bit NULL DEFAULT CAST(0 AS bit),
    [last_reset_at] datetime NULL,
    [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL,
    CONSTRAINT [PK__mission__B5419AB286128CD6] PRIMARY KEY ([mission_id])
);
GO


CREATE TABLE [point_type_definition] (
    [point_type_definition_id] int NOT NULL IDENTITY,
    [type_name] nvarchar(50) NOT NULL,
    [display_name] nvarchar(100) NOT NULL,
    [category] varchar(20) NOT NULL,
    [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL,
    CONSTRAINT [PK__point_ty__E69D541F652B6C64] PRIMARY KEY ([point_type_definition_id])
);
GO


CREATE TABLE [product] (
    [product_id] int NOT NULL IDENTITY,
    [name] nvarchar(100) NOT NULL,
    [description] nvarchar(500) NULL,
    [image_name] nvarchar(100) NULL,
    [type] nvarchar(100) NULL,
    [series] nvarchar(100) NULL,
    [is_physical] bit NOT NULL,
    [point] int NOT NULL,
    [stock] int NOT NULL,
    [created_at] datetime NOT NULL DEFAULT ((getdate())),
    [updated_at] datetime NOT NULL DEFAULT ((getdate())),
    [on_sale] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK__product__47027DF552D294D0] PRIMARY KEY ([product_id])
);
GO


CREATE TABLE [role] (
    [role_id] int NOT NULL,
    [role_name] varchar(20) NULL,
    [authority] int NULL,
    CONSTRAINT [PK__role__760965CC2A1CCCE6] PRIMARY KEY ([role_id])
);
GO


CREATE TABLE [pointchange] (
    [pointchange_id] int NOT NULL IDENTITY,
    [product_id] int NOT NULL,
    [point] int NOT NULL,
    [updated_at] datetime NOT NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__pointcha__314D95DCA6FCEB27] PRIMARY KEY ([pointchange_id]),
    CONSTRAINT [FK__pointchan__produ__5F7E2DAC] FOREIGN KEY ([product_id]) REFERENCES [product] ([product_id])
);
GO


CREATE TABLE [productimage] (
    [productimage_id] int NOT NULL IDENTITY,
    [product_id] int NOT NULL,
    [image_name] nvarchar(50) NULL,
    CONSTRAINT [PK__producti__EBDC9A4A3F03438C] PRIMARY KEY ([productimage_id]),
    CONSTRAINT [FK__productim__produ__607251E5] FOREIGN KEY ([product_id]) REFERENCES [product] ([product_id])
);
GO


CREATE TABLE [user] (
    [user_id] int NOT NULL IDENTITY,
    [email] varchar(150) NOT NULL,
    [username] nvarchar(150) NOT NULL,
    [password] nvarchar(255) NOT NULL,
    [image_url] nvarchar(500) NULL,
    [phone] varchar(15) NULL,
    [birthday] date NULL,
    [address] nvarchar(500) NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [is_active] bit NULL DEFAULT CAST(1 AS bit),
    [role_id] int NULL,
    [employeeid] int NULL,
    [ResetToken] nvarchar(500) NULL,
    [ResetTokenExpiry] datetime2 NULL,
    CONSTRAINT [PK__user__B9BE370FD7BBF0C4] PRIMARY KEY ([user_id]),
    CONSTRAINT [FK__user__role_id__46B27FE2] FOREIGN KEY ([role_id]) REFERENCES [role] ([role_id])
);
GO


CREATE TABLE [chat_session] (
    [chat_session_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [title] nvarchar(100) NOT NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    [ishumanmode] bit NOT NULL,
    CONSTRAINT [PK__chat_ses__B39120D4F6412F6B] PRIMARY KEY ([chat_session_id]),
    CONSTRAINT [FK__chat_sess__user___625A9A57] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [content] (
    [content_id] int NOT NULL IDENTITY,
    [parent_content_id] int NULL,
    [category_id] int NULL,
    [user_id] int NOT NULL,
    [title] nvarchar(200) NOT NULL,
    [body] nvarchar(max) NOT NULL,
    [type] int NOT NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    [is_active] int NOT NULL,
    CONSTRAINT [PK__content__655FE51052A8D3D0] PRIMARY KEY ([content_id]),
    CONSTRAINT [FK__content__categor__3B40CD36] FOREIGN KEY ([category_id]) REFERENCES [content_category] ([content_category_id]),
    CONSTRAINT [FK__content__parent___3A4CA8FD] FOREIGN KEY ([parent_content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__content__user_id__3C34F16F] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [follow] (
    [follower_id] int NOT NULL,
    [followee_id] int NOT NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__follow__710D19E63DF3603E] PRIMARY KEY ([follower_id], [followee_id]),
    CONSTRAINT [FK__follow__followee__489AC854] FOREIGN KEY ([followee_id]) REFERENCES [user] ([user_id]),
    CONSTRAINT [FK__follow__follower__47A6A41B] FOREIGN KEY ([follower_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [mission_progress] (
    [mission_progress_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [mission_id] int NOT NULL,
    [current_count] int NOT NULL,
    [status] nvarchar(50) NOT NULL DEFAULT N'ongoing',
    [accepted_at] datetime NULL DEFAULT ((getdate())),
    [completed_at] datetime NULL,
    [rewarded_at] datetime NULL,
    [cycle_start_at] datetime NULL,
    [cycle_end_at] datetime NULL,
    CONSTRAINT [PK__mission___F43E603D79F28407] PRIMARY KEY ([mission_progress_id]),
    CONSTRAINT [FK__mission_p__missi__503BEA1C] FOREIGN KEY ([mission_id]) REFERENCES [mission] ([mission_id]),
    CONSTRAINT [FK__mission_p__user___4F47C5E3] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [notifications] (
    [notifications_id] int NOT NULL IDENTITY,
    [target_user_id] int NULL,
    [title] nvarchar(255) NOT NULL,
    [content] nvarchar(255) NOT NULL,
    [category] nvarchar(50) NULL,
    [related_id] nvarchar(100) NULL,
    [link_url] nvarchar(500) NULL,
    [is_read] bit NULL DEFAULT CAST(0 AS bit),
    [created_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__notifica__144A108BD3C5253D] PRIMARY KEY ([notifications_id]),
    CONSTRAINT [FK__notificat__targe__5AB9788F] FOREIGN KEY ([target_user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [orders] (
    [order_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [total_point] int NOT NULL,
    [order_date] datetime NOT NULL DEFAULT ((getdate())),
    [remark] nvarchar(300) NULL,
    [status] nvarchar(50) NOT NULL,
    [canceled_at] datetime NULL,
    CONSTRAINT [PK__orders__46596229E1323C0A] PRIMARY KEY ([order_id]),
    CONSTRAINT [FK__orders__user_id__5BAD9CC8] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [point_log] (
    [point_log_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [source_type] nvarchar(50) NOT NULL,
    [source_id] int NULL,
    [display_source] nvarchar(200) NULL,
    [display_source_sub_title] nvarchar(100) NULL,
    [point_type_definition_id] int NOT NULL,
    [change_amount] int NOT NULL,
    [balance_after] int NOT NULL,
    [status] nvarchar(50) NOT NULL DEFAULT N'success',
    [created_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__point_lo__E2EBFE7C314689E5] PRIMARY KEY ([point_log_id]),
    CONSTRAINT [FK__point_log__point__4E53A1AA] FOREIGN KEY ([point_type_definition_id]) REFERENCES [point_type_definition] ([point_type_definition_id]),
    CONSTRAINT [FK__point_log__user___4D5F7D71] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [point_recharge] (
    [point_recharge_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [order_no] varchar(100) NOT NULL,
    [cash_amount] decimal(12,2) NOT NULL,
    [points_granted] int NOT NULL,
    [payment_method] nvarchar(50) NULL,
    [payment_trade_no] varchar(100) NULL,
    [status] nvarchar(50) NULL DEFAULT N'pending',
    [created_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__point_re__2FCEC81C6E87916E] PRIMARY KEY ([point_recharge_id]),
    CONSTRAINT [FK__point_rec__user___56E8E7AB] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [shipaddress] (
    [shipaddress_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [recipient_name] nvarchar(50) NOT NULL,
    [recipient_address] nvarchar(50) NOT NULL,
    [recipient_phone] nvarchar(50) NOT NULL,
    CONSTRAINT [PK__shipaddr__92A61417AC5163D5] PRIMARY KEY ([shipaddress_id]),
    CONSTRAINT [FK__shipaddre__user___6166761E] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [user_image] (
    [user_image_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [image_url] nvarchar(500) NOT NULL,
    [caption] nvarchar(200) NULL,
    [display_order] int NULL DEFAULT 0,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__user_ima__A9249C9D9849318A] PRIMARY KEY ([user_image_id]),
    CONSTRAINT [FK__user_imag__user___45BE5BA9] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [user_inventory] (
    [inventory_id] int NOT NULL IDENTITY,
    [user_id] int NULL,
    [item_id] int NULL,
    [quantity] int NULL DEFAULT 0,
    CONSTRAINT [PK__user_inv__B59ACC493B0D5CB7] PRIMARY KEY ([inventory_id]),
    CONSTRAINT [FK__user_inve__user___4B7734FF] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [user_loginlog] (
    [loginlog_id] int NOT NULL IDENTITY,
    [user_id] int NULL,
    [logintime] datetime NULL DEFAULT ((getdate())),
    [ip_address] varchar(50) NULL,
    CONSTRAINT [PK__user_log__2AF68B1E9AA99DEF] PRIMARY KEY ([loginlog_id]),
    CONSTRAINT [FK__user_logi__user___498EEC8D] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [user_notification] (
    [user_notification_id] int NOT NULL IDENTITY,
    [user_id] int NULL,
    [title] nvarchar(50) NULL,
    [content] nvarchar(200) NULL,
    [target_url] nvarchar(500) NULL,
    [is_read] bit NULL DEFAULT CAST(0 AS bit),
    [type] varchar(20) NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__user_not__64BFFBDB07FC62D7] PRIMARY KEY ([user_notification_id]),
    CONSTRAINT [FK__user_noti__user___4C6B5938] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [user_status] (
    [user_id] int NOT NULL,
    [experience] int NULL DEFAULT 0,
    [rank] int NULL DEFAULT 1,
    [points] int NULL DEFAULT 0,
    CONSTRAINT [PK__user_sta__B9BE370F8259466A] PRIMARY KEY ([user_id]),
    CONSTRAINT [FK__user_stat__user___4A8310C6] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [chat_message] (
    [chat_message_id] bigint NOT NULL IDENTITY,
    [session_id] int NOT NULL,
    [role] varchar(20) NOT NULL,
    [body] nvarchar(max) NOT NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [embedding] nvarchar(max) NULL,
    CONSTRAINT [PK__chat_mes__1F897716807E8F54] PRIMARY KEY ([chat_message_id]),
    CONSTRAINT [FK__chat_mess__sessi__634EBE90] FOREIGN KEY ([session_id]) REFERENCES [chat_session] ([chat_session_id])
);
GO


CREATE TABLE [content_history] (
    [content_history_id] int NOT NULL IDENTITY,
    [content_id] int NULL,
    [user_id] int NULL,
    [old_title] nvarchar(200) NOT NULL,
    [old_body] nvarchar(max) NOT NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__content___B78300946449E56B] PRIMARY KEY ([content_history_id]),
    CONSTRAINT [FK__content_h__conte__40F9A68C] FOREIGN KEY ([content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__content_h__user___41EDCAC5] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [content_interaction] (
    [content_interaction_id] int NOT NULL IDENTITY,
    [category_id] int NOT NULL,
    [content_id] int NOT NULL,
    [user_id] int NOT NULL,
    [description] nvarchar(500) NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__content___AA89BFE507276FFB] PRIMARY KEY ([content_interaction_id]),
    CONSTRAINT [FK__content_i__categ__3E1D39E1] FOREIGN KEY ([category_id]) REFERENCES [interaction_category] ([interaction_category_id]),
    CONSTRAINT [FK__content_i__conte__3F115E1A] FOREIGN KEY ([content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__content_i__user___40058253] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [content_notification] (
    [content_notification_id] int NOT NULL IDENTITY,
    [user_id] int NULL,
    [content_id] int NULL,
    [category_id] int NULL,
    [title] nvarchar(200) NOT NULL,
    [description] nvarchar(200) NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL DEFAULT ((getdate())),
    [isread] int NOT NULL,
    CONSTRAINT [PK__content___42595B287658AF8D] PRIMARY KEY ([content_notification_id]),
    CONSTRAINT [FK__content_n__categ__44CA3770] FOREIGN KEY ([category_id]) REFERENCES [interaction_category] ([interaction_category_id]),
    CONSTRAINT [FK__content_n__conte__43D61337] FOREIGN KEY ([content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__content_n__user___42E1EEFE] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [entrust_mission] (
    [entrust_mission_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [content_id] int NOT NULL,
    [title] nvarchar(150) NOT NULL,
    [detail] nvarchar(500) NOT NULL,
    [reward_points] int NOT NULL,
    [category] nvarchar(50) NOT NULL,
    [deadline] datetime NULL,
    [status] nvarchar(50) NOT NULL DEFAULT N'open',
    [is_urgent] bit NULL DEFAULT CAST(0 AS bit),
    [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [created_at] datetime NULL DEFAULT ((getdate())),
    [updated_at] datetime NULL,
    [best_content_id] int NULL,
    [best_user_id] int NULL,
    [selected_at] datetime NULL,
    CONSTRAINT [PK__entrust___19471E95E1AEFB29] PRIMARY KEY ([entrust_mission_id]),
    CONSTRAINT [FK__entrust_m__best___531856C7] FOREIGN KEY ([best_content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__entrust_m__best___540C7B00] FOREIGN KEY ([best_user_id]) REFERENCES [user] ([user_id]),
    CONSTRAINT [FK__entrust_m__conte__5224328E] FOREIGN KEY ([content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__entrust_m__user___51300E55] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [point_tip] (
    [point_tip_id] int NOT NULL IDENTITY,
    [from_user_id] int NOT NULL,
    [to_user_id] int NOT NULL,
    [content_id] int NOT NULL,
    [amount] int NOT NULL,
    [created_at] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__point_ti__7A2BD624F420DB49] PRIMARY KEY ([point_tip_id]),
    CONSTRAINT [FK__point_tip__conte__59C55456] FOREIGN KEY ([content_id]) REFERENCES [content] ([content_id]),
    CONSTRAINT [FK__point_tip__from___57DD0BE4] FOREIGN KEY ([from_user_id]) REFERENCES [user] ([user_id]),
    CONSTRAINT [FK__point_tip__to_us__58D1301D] FOREIGN KEY ([to_user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE TABLE [orderdetail] (
    [orderdetail_id] int NOT NULL IDENTITY,
    [order_id] int NOT NULL,
    [product_id] int NOT NULL,
    [quantity] int NOT NULL,
    [unit_point_at_purchase] int NOT NULL,
    [requires_shipping] bit NOT NULL,
    CONSTRAINT [PK__orderdet__59AE7411EB8DE9D3] PRIMARY KEY ([orderdetail_id]),
    CONSTRAINT [FK__orderdeta__order__5CA1C101] FOREIGN KEY ([order_id]) REFERENCES [orders] ([order_id]),
    CONSTRAINT [FK__orderdeta__produ__5D95E53A] FOREIGN KEY ([product_id]) REFERENCES [product] ([product_id])
);
GO


CREATE TABLE [shipped_detail] (
    [shipped_detail_id] int NOT NULL IDENTITY,
    [order_id] int NOT NULL,
    [recipient_name] nvarchar(50) NOT NULL,
    [recipient_address] nvarchar(50) NOT NULL,
    [recipient_phone] nvarchar(50) NOT NULL,
    [shipped_date] datetime NULL,
    [arrival_date] datetime NULL,
    [is_failed] bit NOT NULL,
    [remark] nvarchar(300) NULL,
    [is_completed] bit NOT NULL,
    CONSTRAINT [PK__shipped___26C93C93A173F99C] PRIMARY KEY ([shipped_detail_id]),
    CONSTRAINT [FK__shipped_d__order__5E8A0973] FOREIGN KEY ([order_id]) REFERENCES [orders] ([order_id])
);
GO


CREATE TABLE [entrust_mission_point_reserve] (
    [entrust_mission_point_reserve_id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [entrust_mission_id] int NOT NULL,
    [reserved_points] int NOT NULL,
    [status] nvarchar(50) NOT NULL DEFAULT N'reserved',
    [created_at] datetime NULL DEFAULT ((getdate())),
    [released_at] datetime NULL,
    CONSTRAINT [PK__entrust___1500647043C9DD34] PRIMARY KEY ([entrust_mission_point_reserve_id]),
    CONSTRAINT [FK__entrust_m__entru__55F4C372] FOREIGN KEY ([entrust_mission_id]) REFERENCES [entrust_mission] ([entrust_mission_id]),
    CONSTRAINT [FK__entrust_m__user___55009F39] FOREIGN KEY ([user_id]) REFERENCES [user] ([user_id])
);
GO


CREATE INDEX [IX_chat_message_session_id] ON [chat_message] ([session_id]);
GO


CREATE INDEX [IX_chat_session_user_id] ON [chat_session] ([user_id]);
GO


CREATE INDEX [IX_content_category_id] ON [content] ([category_id]);
GO


CREATE INDEX [IX_content_parent_content_id] ON [content] ([parent_content_id]);
GO


CREATE INDEX [IX_content_user_id] ON [content] ([user_id]);
GO


CREATE INDEX [IX_content_category_parent_category_id] ON [content_category] ([parent_category_id]);
GO


CREATE INDEX [IX_content_history_content_id] ON [content_history] ([content_id]);
GO


CREATE INDEX [IX_content_history_user_id] ON [content_history] ([user_id]);
GO


CREATE INDEX [IX_content_interaction_category_id] ON [content_interaction] ([category_id]);
GO


CREATE INDEX [IX_content_interaction_content_id] ON [content_interaction] ([content_id]);
GO


CREATE INDEX [IX_content_interaction_user_id] ON [content_interaction] ([user_id]);
GO


CREATE INDEX [IX_content_notification_category_id] ON [content_notification] ([category_id]);
GO


CREATE INDEX [IX_content_notification_content_id] ON [content_notification] ([content_id]);
GO


CREATE INDEX [IX_content_notification_user_id] ON [content_notification] ([user_id]);
GO


CREATE INDEX [IX_entrust_mission_best_content_id] ON [entrust_mission] ([best_content_id]);
GO


CREATE INDEX [IX_entrust_mission_best_user_id] ON [entrust_mission] ([best_user_id]);
GO


CREATE INDEX [IX_entrust_mission_content_id] ON [entrust_mission] ([content_id]);
GO


CREATE INDEX [IX_entrust_mission_user_id] ON [entrust_mission] ([user_id]);
GO


CREATE INDEX [IX_entrust_mission_point_reserve_entrust_mission_id] ON [entrust_mission_point_reserve] ([entrust_mission_id]);
GO


CREATE INDEX [IX_entrust_mission_point_reserve_user_id] ON [entrust_mission_point_reserve] ([user_id]);
GO


CREATE INDEX [IX_follow_followee_id] ON [follow] ([followee_id]);
GO


CREATE INDEX [IX_mission_progress_mission_id] ON [mission_progress] ([mission_id]);
GO


CREATE UNIQUE INDEX [UX_User_Mission_Cycle] ON [mission_progress] ([user_id], [mission_id], [cycle_start_at]) WHERE [cycle_start_at] IS NOT NULL;
GO


CREATE INDEX [IX_notifications_target_user_id] ON [notifications] ([target_user_id]);
GO


CREATE INDEX [IX_orderdetail_order_id] ON [orderdetail] ([order_id]);
GO


CREATE INDEX [IX_orderdetail_product_id] ON [orderdetail] ([product_id]);
GO


CREATE INDEX [IX_orders_user_id] ON [orders] ([user_id]);
GO


CREATE INDEX [idx_point_log_source] ON [point_log] ([source_type], [source_id]);
GO


CREATE INDEX [idx_point_log_user_time] ON [point_log] ([user_id], [created_at]);
GO


CREATE INDEX [IX_point_log_point_type_definition_id] ON [point_log] ([point_type_definition_id]);
GO


CREATE INDEX [point_recharge_index_3] ON [point_recharge] ([user_id], [created_at]);
GO


CREATE UNIQUE INDEX [UQ__point_re__465C81B85405B334] ON [point_recharge] ([order_no]);
GO


CREATE INDEX [IX_point_tip_content_id] ON [point_tip] ([content_id]);
GO


CREATE INDEX [IX_point_tip_from_user_id] ON [point_tip] ([from_user_id]);
GO


CREATE INDEX [IX_point_tip_to_user_id] ON [point_tip] ([to_user_id]);
GO


CREATE UNIQUE INDEX [UQ__point_ty__543C4FD97A2B551B] ON [point_type_definition] ([type_name]);
GO


CREATE INDEX [IX_pointchange_product_id] ON [pointchange] ([product_id]);
GO


CREATE INDEX [IX_productimage_product_id] ON [productimage] ([product_id]);
GO


CREATE INDEX [IX_shipaddress_user_id] ON [shipaddress] ([user_id]);
GO


CREATE INDEX [IX_shipped_detail_order_id] ON [shipped_detail] ([order_id]);
GO


CREATE INDEX [IX_user_role_id] ON [user] ([role_id]);
GO


CREATE UNIQUE INDEX [UQ__user__AB6E616435E209F4] ON [user] ([email]);
GO


CREATE UNIQUE INDEX [UQ__user__F3DBC572D7FD706C] ON [user] ([username]);
GO


CREATE INDEX [IX_user_image_user_id] ON [user_image] ([user_id]);
GO


CREATE INDEX [IX_user_inventory_user_id] ON [user_inventory] ([user_id]);
GO


CREATE INDEX [IX_user_loginlog_user_id] ON [user_loginlog] ([user_id]);
GO


CREATE INDEX [IX_user_notification_user_id] ON [user_notification] ([user_id]);
GO


