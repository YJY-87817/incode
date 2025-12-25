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
                name: "admin",
                columns: table => new
                {
                    admin_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    autority = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__admin__43AA414154A3117D", x => x.admin_id);
                });

            migrationBuilder.CreateTable(
                name: "content_category",
                columns: table => new
                {
                    content_category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_category_id = table.Column<int>(type: "int", nullable: true),
                    category_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___0FFD1C56B7C06147", x => x.content_category_id);
                    table.ForeignKey(
                        name: "FK__content_c__paren__0E6E26BF",
                        column: x => x.parent_category_id,
                        principalTable: "content_category",
                        principalColumn: "content_category_id");
                });

            migrationBuilder.CreateTable(
                name: "image_usage_location",
                columns: table => new
                {
                    image_usage_location_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__image_us__CA06DFFEC35A4BAF", x => x.image_usage_location_id);
                });

            migrationBuilder.CreateTable(
                name: "interaction_category",
                columns: table => new
                {
                    interaction_category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__interact__45DCC996A9F658A6", x => x.interaction_category_id);
                });

            migrationBuilder.CreateTable(
                name: "mission",
                columns: table => new
                {
                    mission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mission_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mission_detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    target_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    target_count = table.Column<int>(type: "int", nullable: false),
                    reward_points = table.Column<int>(type: "int", nullable: false),
                    reward_exp = table.Column<int>(type: "int", nullable: false),
                    reset_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "none"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__mission__B5419AB2DCB0D9D6", x => x.mission_id);
                });

            migrationBuilder.CreateTable(
                name: "point_type_definition",
                columns: table => new
                {
                    point_type_definition_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_ty__E69D541F5577381D", x => x.point_type_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    image_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    isphysical = table.Column<bool>(type: "bit", nullable: false),
                    point = table.Column<int>(type: "int", nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    onsale = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__product__47027DF5253D6146", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    password = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    phone = table.Column<int>(type: "int", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user__B9BE370F38FC00B1", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "admin_image",
                columns: table => new
                {
                    admin_image_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admin_id = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    usage_location = table.Column<int>(type: "int", nullable: false),
                    caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__admin_im__66FA4961597598A5", x => x.admin_image_id);
                    table.ForeignKey(
                        name: "FK__admin_ima__admin__18EBB532",
                        column: x => x.admin_id,
                        principalTable: "admin",
                        principalColumn: "admin_id");
                    table.ForeignKey(
                        name: "FK__admin_ima__usage__19DFD96B",
                        column: x => x.usage_location,
                        principalTable: "image_usage_location",
                        principalColumn: "image_usage_location_id");
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
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ishidden = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content__655FE510DF34C17B", x => x.content_id);
                    table.ForeignKey(
                        name: "FK__content__categor__0C85DE4D",
                        column: x => x.category_id,
                        principalTable: "content_category",
                        principalColumn: "content_category_id");
                    table.ForeignKey(
                        name: "FK__content__parent___0B91BA14",
                        column: x => x.parent_content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content__user_id__0D7A0286",
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
                    title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    reward_points = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "open"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__entrust___19471E95A503AE34", x => x.entrust_mission_id);
                    table.ForeignKey(
                        name: "FK__entrust_m__user___236943A5",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    follower_id = table.Column<int>(type: "int", nullable: false),
                    followee_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__follows__710D19E62D4780D2", x => new { x.follower_id, x.followee_id });
                    table.ForeignKey(
                        name: "FK__follows__followe__1AD3FDA4",
                        column: x => x.follower_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__follows__followe__1BC821DD",
                        column: x => x.followee_id,
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
                    is_completed = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "('0')"),
                    accepted_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    completed_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__mission___F43E603D79FB31A2", x => x.mission_progress_id);
                    table.ForeignKey(
                        name: "FK__mission_p__missi__1F98B2C1",
                        column: x => x.mission_id,
                        principalTable: "mission",
                        principalColumn: "mission_id");
                    table.ForeignKey(
                        name: "FK__mission_p__user___22751F6C",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "order",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    total_point = table.Column<int>(type: "int", nullable: false),
                    order_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    shipping = table.Column<bool>(type: "bit", nullable: false),
                    note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    cancel_order = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__order__465962294BFFDBBA", x => x.order_id);
                    table.ForeignKey(
                        name: "FK__order__user_id__2645B050",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "point_deposits",
                columns: table => new
                {
                    point_deposit_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    order_no = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    cash_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    points_granted = table.Column<int>(type: "int", nullable: false),
                    payment_method = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    related_order_id = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_de__6DCA5DF6BBD15F90", x => x.point_deposit_id);
                    table.ForeignKey(
                        name: "FK__point_dep__user___25518C17",
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
                    source_id = table.Column<int>(type: "int", nullable: false),
                    point_type_definition_id = table.Column<int>(type: "int", nullable: false),
                    change_amount = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__point_lo__E2EBFE7C22A26ECA", x => x.point_log_id);
                    table.ForeignKey(
                        name: "FK__point_log__point__208CD6FA",
                        column: x => x.point_type_definition_id,
                        principalTable: "point_type_definition",
                        principalColumn: "point_type_definition_id");
                    table.ForeignKey(
                        name: "FK__point_log__user___1EA48E88",
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
                    usage_location = table.Column<int>(type: "int", nullable: false),
                    caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__user_ima__A9249C9D973907F0", x => x.user_image_id);
                    table.ForeignKey(
                        name: "FK__user_imag__usage__17F790F9",
                        column: x => x.usage_location,
                        principalTable: "image_usage_location",
                        principalColumn: "image_usage_location_id");
                    table.ForeignKey(
                        name: "FK__user_imag__user___17036CC0",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_login",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: true),
                    logintime = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK__user_logi__user___1DB06A4F",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_status",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: true),
                    experience = table.Column<int>(type: "int", nullable: true),
                    rank = table.Column<int>(type: "int", nullable: true),
                    item_id = table.Column<int>(type: "int", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK__user_stat__user___1CBC4616",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "content_history",
                columns: table => new
                {
                    content_history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    content_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    old_title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    old_body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ishidden = table.Column<bool>(type: "bit", nullable: true),
                    edited_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___B7830094376D982F", x => x.content_history_id);
                    table.ForeignKey(
                        name: "FK__content_h__conte__123EB7A3",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content_h__user___1332DBDC",
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
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___AA89BFE57938C2EE", x => x.content_interaction_id);
                    table.ForeignKey(
                        name: "FK__content_i__categ__0F624AF8",
                        column: x => x.category_id,
                        principalTable: "interaction_category",
                        principalColumn: "interaction_category_id");
                    table.ForeignKey(
                        name: "FK__content_i__conte__10566F31",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content_i__user___114A936A",
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
                    category = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    isread = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__content___42595B28CC767AB1", x => x.content_notification_id);
                    table.ForeignKey(
                        name: "FK__content_n__categ__160F4887",
                        column: x => x.category,
                        principalTable: "interaction_category",
                        principalColumn: "interaction_category_id");
                    table.ForeignKey(
                        name: "FK__content_n__conte__151B244E",
                        column: x => x.content_id,
                        principalTable: "content",
                        principalColumn: "content_id");
                    table.ForeignKey(
                        name: "FK__content_n__user___14270015",
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
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "accepted"),
                    is_completed = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "('0')"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__entrust___8F68D7F4D8A32147", x => x.entrust_mission_progress_id);
                    table.ForeignKey(
                        name: "FK__entrust_m__entru__2180FB33",
                        column: x => x.entrust_mission_id,
                        principalTable: "entrust_mission",
                        principalColumn: "entrust_mission_id");
                    table.ForeignKey(
                        name: "FK__entrust_m__user___245D67DE",
                        column: x => x.user_id,
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
                    shipped = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__orderdet__59AE74110880848F", x => x.orderdetail_id);
                    table.ForeignKey(
                        name: "FK__orderdeta__order__2739D489",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__orderdeta__produ__282DF8C2",
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
                    orderdetail_id = table.Column<int>(type: "int", nullable: false),
                    recipient_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recipient_address = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    recipient_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    shipped_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    arrival_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    completed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__shipped___26C93C93812ED2F2", x => x.shipped_detail_id);
                    table.ForeignKey(
                        name: "FK__shipped_d__order__29221CFB",
                        column: x => x.orderdetail_id,
                        principalTable: "orderdetail",
                        principalColumn: "orderdetail_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_image_admin_id",
                table: "admin_image",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_image_usage_location",
                table: "admin_image",
                column: "usage_location");

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
                name: "IX_content_notification_category",
                table: "content_notification",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_content_notification_content_id",
                table: "content_notification",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_notification_user_id",
                table: "content_notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_entrust_mission_user_id",
                table: "entrust_mission",
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
                name: "IX_follows_followee_id",
                table: "follows",
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
                name: "IX_order_user_id",
                table: "order",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_orderdetail_order_id",
                table: "orderdetail",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_orderdetail_product_id",
                table: "orderdetail",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_deposits_user_id",
                table: "point_deposits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__point_de__465C81B8D1564B10",
                table: "point_deposits",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_point_log_point_type_definition_id",
                table: "point_log",
                column: "point_type_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_log_user_id",
                table: "point_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__point_ty__543C4FD9F754B5D6",
                table: "point_type_definition",
                column: "type_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipped_detail_orderdetail_id",
                table: "shipped_detail",
                column: "orderdetail_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_image_usage_location",
                table: "user_image",
                column: "usage_location");

            migrationBuilder.CreateIndex(
                name: "IX_user_image_user_id",
                table: "user_image",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_login_user_id",
                table: "user_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_user_id",
                table: "user_status",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_image");

            migrationBuilder.DropTable(
                name: "content_history");

            migrationBuilder.DropTable(
                name: "content_interaction");

            migrationBuilder.DropTable(
                name: "content_notification");

            migrationBuilder.DropTable(
                name: "entrust_mission_progress");

            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "mission_progress");

            migrationBuilder.DropTable(
                name: "point_deposits");

            migrationBuilder.DropTable(
                name: "point_log");

            migrationBuilder.DropTable(
                name: "shipped_detail");

            migrationBuilder.DropTable(
                name: "user_image");

            migrationBuilder.DropTable(
                name: "user_login");

            migrationBuilder.DropTable(
                name: "user_status");

            migrationBuilder.DropTable(
                name: "admin");

            migrationBuilder.DropTable(
                name: "interaction_category");

            migrationBuilder.DropTable(
                name: "content");

            migrationBuilder.DropTable(
                name: "entrust_mission");

            migrationBuilder.DropTable(
                name: "mission");

            migrationBuilder.DropTable(
                name: "point_type_definition");

            migrationBuilder.DropTable(
                name: "orderdetail");

            migrationBuilder.DropTable(
                name: "image_usage_location");

            migrationBuilder.DropTable(
                name: "content_category");

            migrationBuilder.DropTable(
                name: "order");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
