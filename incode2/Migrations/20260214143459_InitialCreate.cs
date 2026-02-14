using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace incode2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_category",
                columns: table => new
                {
                    content_category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_category_id = table.Column<int>(type: "int", nullable: true),
                    category_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___0FFD1C56B60884D1", x => x.content_category_id);
                    table.ForeignKey(
                        name: "FK__content_c__paren__41EDCAC5",
                        column: x => x.parent_category_id,
                        principalTable: "content_category",
                        principalColumn: "content_category_id");
                });

            migrationBuilder.CreateTable(
                name: "interaction_category",
                columns: table => new
                {
                    interaction_category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    is_active = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__interact__45DCC996E58E4ABE", x => x.interaction_category_id);
                });

            migrationBuilder.CreateTable(
                name: "mission",
                columns: table => new
                {
                    mission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mission_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mission_detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    target_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    target_count = table.Column<int>(type: "int", nullable: false),
                    reward_points = table.Column<int>(type: "int", nullable: false),
                    reward_exp = table.Column<int>(type: "int", nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "??"),
                    estimated_duration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reset_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "none"),
                    participant_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    is_featured = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    last_reset_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__mission__B5419AB2248B8A02", x => x.mission_id);
                });

            migrationBuilder.CreateTable(
                name: "point_type_definition",
                columns: table => new
                {
                    point_type_definition_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_ty__E69D541F4BB99E81", x => x.point_type_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    image_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    series = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_physical = table.Column<bool>(type: "bit", nullable: false),
                    point = table.Column<int>(type: "int", nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    on_sale = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__product__47027DF5BF5D2A4F", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false),
                    role_name = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    authority = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__role__760965CC9B04234D", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "pointchange",
                columns: table => new
                {
                    pointchange_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    point = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pointcha__314D95DC84B38CE5", x => x.pointchange_id);
                    table.ForeignKey(
                        name: "FK__pointchan__produ__6442E2C9",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "productimage",
                columns: table => new
                {
                    productimage_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    image_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__producti__EBDC9A4AFEEAF0C9", x => x.productimage_id);
                    table.ForeignKey(
                        name: "FK__productim__produ__65370702",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    role_id = table.Column<int>(type: "int", nullable: true),
                    employeeid = table.Column<int>(type: "int", nullable: true),
                    ResetToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user__B9BE370FD2E69A4F", x => x.user_id);
                    table.ForeignKey(
                        name: "FK__user__role_id__4B7734FF",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                });

            migrationBuilder.CreateTable(
                name: "chat_session",
                columns: table => new
                {
                    chat_session_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ishumanmode = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chat_ses__B39120D4C3F5C300", x => x.chat_session_id);
                    table.ForeignKey(
                        name: "FK__chat_sess__user___671F4F74",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "content",
                columns: table => new
                {
                    content_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_content_id = table.Column<int>(type: "int", nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    is_active = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content__655FE5109FB5E8BA", x => x.content_id);
                    table.ForeignKey(
                        name: "FK__content__categor__40058253",
                        column: x => x.category_id,
                        principalTable: "content_category",
                        principalColumn: "content_category_id");
                    table.ForeignKey(
                        name: "FK__content__parent___3F115E1A",
                        column: x => x.parent_content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content__user_id__40F9A68C",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "follow",
                columns: table => new
                {
                    follower_id = table.Column<int>(type: "int", nullable: false),
                    followee_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__follow__710D19E67895927C", x => new { x.follower_id, x.followee_id });
                    table.ForeignKey(
                        name: "FK__follow__followee__4D5F7D71",
                        column: x => x.followee_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__follow__follower__4C6B5938",
                        column: x => x.follower_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "mission_progress",
                columns: table => new
                {
                    mission_progress_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    mission_id = table.Column<int>(type: "int", nullable: false),
                    current_count = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ongoing"),
                    accepted_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    completed_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    rewarded_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    cycle_start_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    cycle_end_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__mission___F43E603D23E41021", x => x.mission_progress_id);
                    table.ForeignKey(
                        name: "FK__mission_p__missi__55009F39",
                        column: x => x.mission_id,
                        principalTable: "mission",
                        principalColumn: "mission_id");
                    table.ForeignKey(
                        name: "FK__mission_p__user___540C7B00",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notifications_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    target_user_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    related_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    link_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__notifica__144A108B29EE7F3A", x => x.notifications_id);
                    table.ForeignKey(
                        name: "FK__notificat__targe__5F7E2DAC",
                        column: x => x.target_user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    total_point = table.Column<int>(type: "int", nullable: false),
                    order_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    canceled_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__orders__46596229DAC2991C", x => x.order_id);
                    table.ForeignKey(
                        name: "FK__orders__user_id__607251E5",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "point_log",
                columns: table => new
                {
                    point_log_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    source_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<int>(type: "int", nullable: true),
                    display_source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    display_source_sub_title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    point_type_definition_id = table.Column<int>(type: "int", nullable: false),
                    change_amount = table.Column<int>(type: "int", nullable: false),
                    balance_after = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "success"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_lo__E2EBFE7CE354901A", x => x.point_log_id);
                    table.ForeignKey(
                        name: "FK__point_log__point__531856C7",
                        column: x => x.point_type_definition_id,
                        principalTable: "point_type_definition",
                        principalColumn: "point_type_definition_id");
                    table.ForeignKey(
                        name: "FK__point_log__user___5224328E",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "point_recharge",
                columns: table => new
                {
                    point_recharge_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    order_no = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    cash_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    points_granted = table.Column<int>(type: "int", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payment_trade_no = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "pending"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_re__2FCEC81C48014850", x => x.point_recharge_id);
                    table.ForeignKey(
                        name: "FK__point_rec__user___5BAD9CC8",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "shipaddress",
                columns: table => new
                {
                    shipaddress_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    recipient_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recipient_address = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recipient_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__shipaddr__92A61417C2790CE3", x => x.shipaddress_id);
                    table.ForeignKey(
                        name: "FK__shipaddre__user___662B2B3B",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_image",
                columns: table => new
                {
                    user_image_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_ima__A9249C9DB13A59EC", x => x.user_image_id);
                    table.ForeignKey(
                        name: "FK__user_imag__user___4A8310C6",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_inventory",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    item_id = table.Column<int>(type: "int", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_inv__B59ACC4990389B43", x => x.inventory_id);
                    table.ForeignKey(
                        name: "FK__user_inve__user___503BEA1C",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_loginlog",
                columns: table => new
                {
                    loginlog_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    logintime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ip_address = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_log__2AF68B1E41B4CBA6", x => x.loginlog_id);
                    table.ForeignKey(
                        name: "FK__user_logi__user___4E53A1AA",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_notification",
                columns: table => new
                {
                    user_notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    content = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    target_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_not__64BFFBDB54DAD8C5", x => x.user_notification_id);
                    table.ForeignKey(
                        name: "FK__user_noti__user___51300E55",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_status",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    experience = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    rank = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    points = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_sta__B9BE370F8D278919", x => x.user_id);
                    table.ForeignKey(
                        name: "FK__user_stat__user___4F47C5E3",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "chat_message",
                columns: table => new
                {
                    chat_message_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    session_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    embedding = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chat_mes__1F897716BE8B98B0", x => x.chat_message_id);
                    table.ForeignKey(
                        name: "FK__chat_mess__sessi__681373AD",
                        column: x => x.session_id,
                        principalTable: "chat_session",
                        principalColumn: "chat_session_id");
                });

            migrationBuilder.CreateTable(
                name: "content_history",
                columns: table => new
                {
                    content_history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    content_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    old_title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    old_body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___B7830094207CDB21", x => x.content_history_id);
                    table.ForeignKey(
                        name: "FK__content_h__conte__45BE5BA9",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content_h__user___46B27FE2",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "content_interaction",
                columns: table => new
                {
                    content_interaction_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    content_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___AA89BFE526EB6284", x => x.content_interaction_id);
                    table.ForeignKey(
                        name: "FK__content_i__categ__42E1EEFE",
                        column: x => x.category_id,
                        principalTable: "interaction_category",
                        principalColumn: "interaction_category_id");
                    table.ForeignKey(
                        name: "FK__content_i__conte__43D61337",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content_i__user___44CA3770",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "content_notification",
                columns: table => new
                {
                    content_notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    content_id = table.Column<int>(type: "int", nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    isread = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___42595B28D8ECFC5C", x => x.content_notification_id);
                    table.ForeignKey(
                        name: "FK__content_n__categ__498EEC8D",
                        column: x => x.category_id,
                        principalTable: "interaction_category",
                        principalColumn: "interaction_category_id");
                    table.ForeignKey(
                        name: "FK__content_n__conte__489AC854",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content_n__user___47A6A41B",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "entrust_mission",
                columns: table => new
                {
                    entrust_mission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    content_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    reward_points = table.Column<int>(type: "int", nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    deadline = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "open"),
                    is_urgent = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__entrust___19471E95AAAFBE5A", x => x.entrust_mission_id);
                    table.ForeignKey(
                        name: "FK__entrust_m__conte__56E8E7AB",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__entrust_m__user___55F4C372",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "point_tip",
                columns: table => new
                {
                    point_tip_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    from_user_id = table.Column<int>(type: "int", nullable: false),
                    to_user_id = table.Column<int>(type: "int", nullable: false),
                    content_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_ti__7A2BD62485CBD80B", x => x.point_tip_id);
                    table.ForeignKey(
                        name: "FK__point_tip__conte__5E8A0973",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__point_tip__from___5CA1C101",
                        column: x => x.from_user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__point_tip__to_us__5D95E53A",
                        column: x => x.to_user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "orderdetail",
                columns: table => new
                {
                    orderdetail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_point_at_purchase = table.Column<int>(type: "int", nullable: false),
                    requires_shipping = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__orderdet__59AE741192E09FB7", x => x.orderdetail_id);
                    table.ForeignKey(
                        name: "FK__orderdeta__order__6166761E",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "FK__orderdeta__produ__625A9A57",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "shipped_detail",
                columns: table => new
                {
                    shipped_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    recipient_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recipient_address = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recipient_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    shipped_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    arrival_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_failed = table.Column<bool>(type: "bit", nullable: false),
                    remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    is_completed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__shipped___26C93C93EAAA21CC", x => x.shipped_detail_id);
                    table.ForeignKey(
                        name: "FK__shipped_d__order__634EBE90",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                });

            migrationBuilder.CreateTable(
                name: "entrust_mission_point_reserve",
                columns: table => new
                {
                    entrust_mission_point_reserve_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    entrust_mission_id = table.Column<int>(type: "int", nullable: false),
                    reserved_points = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "reserved"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    released_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__entrust___15006470E33BB2B1", x => x.entrust_mission_point_reserve_id);
                    table.ForeignKey(
                        name: "FK__entrust_m__entru__5AB9788F",
                        column: x => x.entrust_mission_id,
                        principalTable: "entrust_mission",
                        principalColumn: "entrust_mission_id");
                    table.ForeignKey(
                        name: "FK__entrust_m__user___59C55456",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "entrust_mission_progress",
                columns: table => new
                {
                    entrust_mission_progress_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    entrust_mission_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "ongoing"),
                    accepted_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    completed_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    rewarded_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__entrust___8F68D7F4C8410FC3", x => x.entrust_mission_progress_id);
                    table.ForeignKey(
                        name: "FK__entrust_m__entru__58D1301D",
                        column: x => x.entrust_mission_id,
                        principalTable: "entrust_mission",
                        principalColumn: "entrust_mission_id");
                    table.ForeignKey(
                        name: "FK__entrust_m__user___57DD0BE4",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_session_id",
                table: "chat_message",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_session_user_id",
                table: "chat_session",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_category_id",
                table: "content",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_parent_content_id",
                table: "content",
                column: "parent_content_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_user_id",
                table: "content",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_category_parent_category_id",
                table: "content_category",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_history_content_id",
                table: "content_history",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_history_user_id",
                table: "content_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_interaction_category_id",
                table: "content_interaction",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_interaction_content_id",
                table: "content_interaction",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_interaction_user_id",
                table: "content_interaction",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_notification_category_id",
                table: "content_notification",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_notification_content_id",
                table: "content_notification",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_notification_user_id",
                table: "content_notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_content_id",
                table: "entrust_mission",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_user_id",
                table: "entrust_mission",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_point_reserve_entrust_mission_id",
                table: "entrust_mission_point_reserve",
                column: "entrust_mission_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_point_reserve_user_id",
                table: "entrust_mission_point_reserve",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_progress_entrust_mission_id",
                table: "entrust_mission_progress",
                column: "entrust_mission_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_progress_user_id",
                table: "entrust_mission_progress",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_follow_followee_id",
                table: "follow",
                column: "followee_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_progress_mission_id",
                table: "mission_progress",
                column: "mission_id");

            migrationBuilder.CreateIndex(
                name: "IX_mission_progress_user_id",
                table: "mission_progress",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_target_user_id",
                table: "notifications",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_orderdetail_order_id",
                table: "orderdetail",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_orderdetail_product_id",
                table: "orderdetail",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_log_point_type_definition_id",
                table: "point_log",
                column: "point_type_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_log_user_id",
                table: "point_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "point_recharge_index_0",
                table: "point_recharge",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "UQ__point_re__465C81B833313BAA",
                table: "point_recharge",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_point_tip_content_id",
                table: "point_tip",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_tip_from_user_id",
                table: "point_tip",
                column: "from_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_tip_to_user_id",
                table: "point_tip",
                column: "to_user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__point_ty__543C4FD9008944C5",
                table: "point_type_definition",
                column: "type_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pointchange_product_id",
                table: "pointchange",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_productimage_product_id",
                table: "productimage",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipaddress_user_id",
                table: "shipaddress",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipped_detail_order_id",
                table: "shipped_detail",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_id",
                table: "user",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UQ__user__AB6E616418ABB504",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__user__F3DBC57254661CC7",
                table: "user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_image_user_id",
                table: "user_image",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_inventory_user_id",
                table: "user_inventory",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_loginlog_user_id",
                table: "user_loginlog",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_user_id",
                table: "user_notification",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message");

            migrationBuilder.DropTable(
                name: "content_history");

            migrationBuilder.DropTable(
                name: "content_interaction");

            migrationBuilder.DropTable(
                name: "content_notification");

            migrationBuilder.DropTable(
                name: "entrust_mission_point_reserve");

            migrationBuilder.DropTable(
                name: "entrust_mission_progress");

            migrationBuilder.DropTable(
                name: "follow");

            migrationBuilder.DropTable(
                name: "mission_progress");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "orderdetail");

            migrationBuilder.DropTable(
                name: "point_log");

            migrationBuilder.DropTable(
                name: "point_recharge");

            migrationBuilder.DropTable(
                name: "point_tip");

            migrationBuilder.DropTable(
                name: "pointchange");

            migrationBuilder.DropTable(
                name: "productimage");

            migrationBuilder.DropTable(
                name: "shipaddress");

            migrationBuilder.DropTable(
                name: "shipped_detail");

            migrationBuilder.DropTable(
                name: "user_image");

            migrationBuilder.DropTable(
                name: "user_inventory");

            migrationBuilder.DropTable(
                name: "user_loginlog");

            migrationBuilder.DropTable(
                name: "user_notification");

            migrationBuilder.DropTable(
                name: "user_status");

            migrationBuilder.DropTable(
                name: "chat_session");

            migrationBuilder.DropTable(
                name: "interaction_category");

            migrationBuilder.DropTable(
                name: "entrust_mission");

            migrationBuilder.DropTable(
                name: "mission");

            migrationBuilder.DropTable(
                name: "point_type_definition");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "content");

            migrationBuilder.DropTable(
                name: "content_category");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "role");
        }
    }
}
