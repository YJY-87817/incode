using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using incode.MVC.Dtos.PointShop;
using incode.MVC.Services.PointShop;
using incode.MVC.Services.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace incode.MVC.Tests;

/// <summary>第三次全專案檢查 51：商城數量上限、總點數溢位、收件資料長度（服務層，SQLite）。</summary>
public class ShopLimitTests
{
    private static OrderService Create(incodedatabaseContext context)
        => new(context, new PointLogService(context), new FakeMissionService(), NullLogger<OrderService>.Instance);

    private static async Task GivePoints(TestDb db, int userId, int amount)
    {
        await using var context = db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task<int> AddProduct(TestDb db, int point, int stock, bool physical = false)
    {
        await using var context = db.NewContext();
        var product = new Product
        {
            Name = "p", Description = "d", ImageName = "i.png", Type = "t", Series = "s",
            IsPhysical = physical, Point = point, Stock = stock, OnSale = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Product.Add(product);
        await context.SaveChangesAsync();
        return product.ProductId;
    }

    private static OrderItemRequest Item(int productId, int quantity) => new() { ProductId = productId, Quantity = quantity };

    private static async Task<int> Stock(TestDb db, int productId)
    {
        await using var context = db.NewContext();
        return await context.Product.AsNoTracking().Where(p => p.ProductId == productId).Select(p => p.Stock).SingleAsync();
    }

    [Fact]
    public async Task Order_whose_total_points_overflow_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser(); // 0 點
        var ids = new[]
        {
            await AddProduct(db, point: 1_000_000, stock: 1000),
            await AddProduct(db, point: 1_000_000, stock: 1000),
            await AddProduct(db, point: 1_000_000, stock: 1000),
        };

        await using var context = db.NewContext();
        // 3 × 1000 × 1,000,000 = 30 億，超過 int：修正前總點數溢位成負數，不扣點就成立訂單
        var result = await Create(context).CreateOrderAsync(userId, ids.Select(id => Item(id, 1000)), null, null, requireRecipient: false);

        Assert.False(result.Ok);
        foreach (var id in ids) Assert.Equal(1000, await Stock(db, id));
        await using var verify = db.NewContext();
        Assert.False(await verify.Orders.AnyAsync());
    }

    [Fact]
    public async Task Quantity_above_the_per_item_maximum_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 10_000);
        var productId = await AddProduct(db, point: 1, stock: 5000);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, OrderLimits.MaxQuantityPerItem + 1) }, null, null, requireRecipient: false);

        Assert.False(result.Ok);
        Assert.Equal(400, result.Code);
        Assert.Equal(5000, await Stock(db, productId));
    }

    [Fact]
    public async Task Same_product_split_into_lines_cannot_bypass_the_maximum()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 10_000);
        var productId = await AddProduct(db, point: 1, stock: 5000);

        await using var context = db.NewContext();
        var half = OrderLimits.MaxQuantityPerItem / 2 + 1;
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, half), Item(productId, half) }, null, null, requireRecipient: false);

        Assert.False(result.Ok);
    }

    [Fact]
    public async Task Recipient_fields_longer_than_the_columns_are_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 100);
        var productId = await AddProduct(db, point: 1, stock: 5, physical: true);
        var recipient = new NewShippedRequest { RecipientName = new string('名', 51), RecipientPhone = "0912345678", RecipientAddress = "地址" };

        await using var context = db.NewContext();
        // 修正前：SQL Server 上超過 nvarchar(50) → 500（SQLite 不檢查長度，會直接寫入）
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 1) }, recipient, null, requireRecipient: true);

        Assert.False(result.Ok);
        Assert.Equal(400, result.Code);
        await using var verify = db.NewContext();
        Assert.False(await verify.Orders.AnyAsync());
    }

    [Fact]
    public async Task Order_remark_longer_than_the_column_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 100);
        var productId = await AddProduct(db, point: 1, stock: 5);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 1) }, null, new string('備', 301), requireRecipient: false);

        Assert.False(result.Ok);
        Assert.Equal(400, result.Code);
    }

    [Fact]
    public async Task Editing_shipping_data_checks_lengths()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 100);
        var productId = await AddProduct(db, point: 1, stock: 5, physical: true);
        var ok = new NewShippedRequest { RecipientName = "王小明", RecipientPhone = "0912345678", RecipientAddress = "地址" };
        await using (var context = db.NewContext())
            Assert.True((await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 1) }, ok, null, requireRecipient: true)).Ok);
        int shippedId;
        await using (var context = db.NewContext())
            shippedId = await context.ShippedDetail.Select(s => s.ShippedDetailId).SingleAsync();

        await using (var context = db.NewContext())
        {
            var result = await Create(context).EditShippedAsync(shippedId,
                new NewShippedRequest { RecipientName = "王小明", RecipientPhone = "0912345678", RecipientAddress = new string('址', 51) });
            Assert.False(result.Ok);
            Assert.Equal(400, result.Code);
        }
        await using var verify = db.NewContext();
        Assert.Equal("地址", (await verify.ShippedDetail.AsNoTracking().SingleAsync()).RecipientAddress);
    }
}

/// <summary>改版前已超過數量上限的舊訂單：仍可修改狀態 / 減少數量，但不能再增加。</summary>
public class LegacyOrderQuantityTests
{
    [Fact]
    public async Task Legacy_order_above_the_maximum_can_still_be_updated_but_not_increased()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        int productId, orderId;
        await using (var context = db.NewContext())
        {
            var product = new Product
            {
                Name = "p", Description = "d", ImageName = "i.png", Type = "t", Series = "s", IsPhysical = false,
                Point = 0, Stock = 5000, OnSale = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            };
            context.Product.Add(product);
            await context.SaveChangesAsync();
            productId = product.ProductId;
            var order = new Orders { UserId = userId, Status = OrderStatus.Pending, OrderDate = DateTime.Now, Remark = "", TotalPoint = 0 };
            order.Orderdetail.Add(new Orderdetail { ProductId = productId, Quantity = 1500, UnitPointAtPurchase = 0 });
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            orderId = order.OrderId;
        }

        OrderService Create(incodedatabaseContext c) => new(c, new PointLogService(c), new FakeMissionService(), NullLogger<OrderService>.Instance);

        await using (var context = db.NewContext())
            Assert.True((await Create(context).UpdateOrderItemsAsync(orderId,
                new[] { new OrderItemRequest { ProductId = productId, Quantity = 1500 } }, null, newStatus: OrderStatus.Shipped)).Ok);
        await using (var context = db.NewContext())
            Assert.False((await Create(context).UpdateOrderItemsAsync(orderId,
                new[] { new OrderItemRequest { ProductId = productId, Quantity = 1501 } }, null)).Ok);
        await using (var context = db.NewContext())
            Assert.True((await Create(context).UpdateOrderItemsAsync(orderId,
                new[] { new OrderItemRequest { ProductId = productId, Quantity = 1200 } }, null)).Ok);
    }
}

/// <summary>第三次全專案檢查 52～56 中需要啟動網站的項目。</summary>
public class ThirdAuditLowPriorityTests : IClassFixture<ApiFactory>
{
    private const string Password = "secret123";
    private readonly ApiFactory _factory;

    public ThirdAuditLowPriorityTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task UpdateUser(int userId, Action<User> change)
    {
        await using var context = _factory.Db.NewContext();
        var user = await context.User.SingleAsync(u => u.UserId == userId);
        change(user);
        await context.SaveChangesAsync();
    }

    private async Task<User> GetUser(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.User.AsNoTracking().SingleAsync(u => u.UserId == userId);
    }

    private async Task<HttpResponseMessage> EditMember(HttpClient admin, int memberId, string? password = null, string? email = null, bool isActive = true)
    {
        var member = await GetUser(memberId);
        var token = Token(await admin.GetStringAsync($"/Member/Edit/{memberId}"));
        var fields = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("UserId", memberId.ToString()),
            new("Username", member.Username),
            new("Email", email ?? member.Email),
            new("IsActive", isActive ? "true" : "false"),
        };
        if (password != null) fields.Add(new("Password", password));
        return await admin.PostAsync("/Member/Edit", new FormUrlEncodedContent(fields));
    }

    private async Task<HttpResponseMessage> Login(string email, string password)
        => await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/login", new { email, password });

    private static async Task<string> Message(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!;

    // ---------- 52 後台會員編輯 ----------

    [Fact]
    public async Task Admin_password_reset_on_member_edit_actually_changes_the_password()
    {
        var memberId = NewUser();
        var email = (await GetUser(memberId)).Email;
        await UpdateUser(memberId, u => u.Password = new PasswordService().Hash("oldpass1"));
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await EditMember(admin, memberId, password: "newpass1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        // 修正前：寫入雜湊的那行被註解，仍顯示成功但密碼沒變
        Assert.Equal(HttpStatusCode.OK, (await Login(email, "newpass1")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(email, "oldpass1")).StatusCode);
        Assert.False(string.IsNullOrEmpty((await GetUser(memberId)).SecurityStamp)); // 其他裝置的登入失效
    }

    [Fact]
    public async Task Admin_cannot_set_a_password_on_a_google_account()
    {
        var memberId = NewUser();
        await UpdateUser(memberId, u => u.Password = UserService.GoogleAccountPassword);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await EditMember(admin, memberId, password: "newpass1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 回到表單顯示錯誤
        Assert.Equal(UserService.GoogleAccountPassword, (await GetUser(memberId)).Password);
    }

    [Fact]
    public async Task Admin_cannot_reset_own_password_from_member_edit()
    {
        var adminId = NewUser();
        await UpdateUser(adminId, u => u.Password = new PasswordService().Hash("oldpass1"));
        var admin = await _factory.LoginAsync(adminId, admin: true);
        var before = (await GetUser(adminId)).Password;

        var response = await EditMember(admin, adminId, password: "newpass1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 回到表單顯示錯誤，不會把自己登出
        Assert.Equal(before, (await GetUser(adminId)).Password);
    }

    [Fact]
    public async Task Changing_a_member_email_marks_it_unverified()
    {
        var memberId = NewUser();
        await UpdateUser(memberId, u => u.EmailVerified = true);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await EditMember(admin, memberId, email: $"{Guid.NewGuid():N}@example.com");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False((await GetUser(memberId)).EmailVerified);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_own_account_from_member_edit()
    {
        var adminId = NewUser();
        var admin = await _factory.LoginAsync(adminId, admin: true);

        var response = await EditMember(admin, adminId, isActive: false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(false, (await GetUser(adminId)).IsActive);
    }

    // ---------- 53 點數紀錄的顯示文字 ----------

    [Fact]
    public async Task Tip_on_a_post_with_a_maximum_length_title_keeps_display_text_within_the_columns()
    {
        var authorId = NewUser();
        var tipperId = NewUser();
        int contentId;
        await using (var context = _factory.Db.NewContext())
        {
            var content = new Content
            {
                UserId = authorId, Type = ContentTypes.Post, Title = new string('標', 200), Body = "b",
                IsActive = ContentStatus.Active, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            contentId = content.ContentId;
            await using var tx = await context.Database.BeginTransactionAsync();
            await new PointLogService(context).ChangePointsAsync(tipperId, 10, "test", null, PointType.SystemAdjustment);
            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        await using (var context = _factory.Db.NewContext())
            await new PointLogService(context).TipAsync(tipperId, authorId, contentId, 1);

        await using var verify = _factory.Db.NewContext();
        var logs = await verify.PointLog.AsNoTracking().Where(l => l.SourceType == "point_tip" && l.SourceId == contentId).ToListAsync();
        Assert.Equal(2, logs.Count);
        // display_source nvarchar(200)、display_source_sub_title nvarchar(100)：修正前「收到打賞〈標題〉」206 字，SQL Server 上打賞失敗
        Assert.All(logs, l => Assert.True(l.DisplaySource!.Length <= PointLogService.MaxDisplaySourceLength, $"{l.DisplaySource!.Length} 字"));
        Assert.All(logs, l => Assert.True((l.DisplaySourceSubTitle?.Length ?? 0) <= PointLogService.MaxDisplaySubTitleLength));
    }

    [Fact]
    public async Task Entrust_with_a_maximum_length_title_keeps_reserve_subtitle_within_the_column()
    {
        var userId = NewUser();
        int contentId;
        await using (var context = _factory.Db.NewContext())
        {
            var content = new Content
            {
                UserId = userId, Type = ContentTypes.Post, Title = "t", Body = "b",
                IsActive = ContentStatus.Active, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            contentId = content.ContentId;
            await using var tx = await context.Database.BeginTransactionAsync();
            await new PointLogService(context).ChangePointsAsync(userId, 100, "test", null, PointType.SystemAdjustment);
            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        var client = await _factory.LoginAsync(userId);
        var response = await client.PostAsJsonAsync("/api/EntrustMissionApi",
            new { contentId, title = new string('委', 150), detail = "d", rewardPoints = 10, category = "其他" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var verify = _factory.Db.NewContext();
        var log = await verify.PointLog.AsNoTracking().SingleAsync(l => l.UserId == userId && l.SourceType == "entrust_mission_reserve");
        // 「任務 #id - {150 字標題}」原本超過 nvarchar(100)
        Assert.True(log.DisplaySourceSubTitle!.Length <= PointLogService.MaxDisplaySubTitleLength);
    }

    // ---------- 54 分頁 / 沒有使用的端點 ----------

    [Fact]
    public async Task Huge_page_number_does_not_fail()
    {
        var response = await _factory.CreateAnonymousClient().GetAsync($"/api/ContentApi?page={int.MaxValue}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unused_comments_endpoint_is_removed()
    {
        var response = await _factory.CreateAnonymousClient().GetAsync("/api/ContentApi/1/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- 55 登入不透露帳號狀態 ----------

    [Fact]
    public async Task Login_failures_look_the_same_for_unknown_google_and_deactivated_accounts()
    {
        var googleEmail = $"{Guid.NewGuid():N}@example.com";
        var googleId = _factory.Db.SeedUser(googleEmail);
        await UpdateUser(googleId, u => u.Password = UserService.GoogleAccountPassword);

        var inactiveEmail = $"{Guid.NewGuid():N}@example.com";
        var inactiveId = _factory.Db.SeedUser(inactiveEmail);
        await UpdateUser(inactiveId, u => { u.Password = new PasswordService().Hash(Password); u.IsActive = false; });

        var unknown = await Message(await Login($"{Guid.NewGuid():N}@example.com", "wrongpass"));
        // 修正前：Google 帳號與停用帳號在比對密碼前就回傳專屬訊息，可以查出哪些 Email 是這兩種帳號
        Assert.Equal(unknown, await Message(await Login(googleEmail, "wrongpass")));
        Assert.Equal(unknown, await Message(await Login(inactiveEmail, "wrongpass")));
    }

    [Fact]
    public async Task Deactivated_account_with_correct_password_still_gets_the_specific_message()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var userId = _factory.Db.SeedUser(email);
        await UpdateUser(userId, u => { u.Password = new PasswordService().Hash(Password); u.IsActive = false; });

        var response = await Login(email, Password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("鎖定", await Message(response));
    }

    // ---------- 56 其他 ----------

    [Fact]
    public async Task Admin_login_is_recorded_in_the_login_log()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var adminId = _factory.Db.SeedUser(email);
        await UpdateUser(adminId, u => u.Password = new PasswordService().Hash(Password));
        await _factory.MakeAdminAsync(adminId);
        var client = _factory.CreateAnonymousClient(ownIp: true);

        var token = Token(await client.GetStringAsync("/Account/Login"));
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Email"] = email, ["Password"] = Password,
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verify = _factory.Db.NewContext();
        Assert.True(await verify.UserLoginlog.AnyAsync(l => l.UserId == adminId));
    }

    [Fact]
    public async Task Member_category_name_follows_the_admin_length_rule()
    {
        var client = await _factory.LoginAsync(NewUser());

        var tooLong = await client.PostAsJsonAsync("/api/CategoryApi", new { name = new string('分', ContentCategoryStatus.NameMaxLength + 1) });
        var tooShort = await client.PostAsJsonAsync("/api/CategoryApi", new { name = "分" });
        var ok = await client.PostAsJsonAsync("/api/CategoryApi", new { name = "分類" + Guid.NewGuid().ToString("N")[..6] });

        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public void Chat_connection_registry_only_accepts_the_owners_connection()
    {
        var registry = new ChatConnectionRegistry();
        registry.Register("conn-a", 1);

        Assert.True(registry.IsOwnedBy("conn-a", 1));
        Assert.False(registry.IsOwnedBy("conn-a", 2)); // 別人的連線
        Assert.False(registry.IsOwnedBy("conn-x", 1)); // 不存在的連線

        registry.Unregister("conn-a");
        Assert.False(registry.IsOwnedBy("conn-a", 1));
    }
}

/// <summary>第三次全專案檢查 56：綠界通知的商店代號與金額（正式商店設定）。</summary>
public class EcpayAmountCheckTests : IClassFixture<RealMerchantApiFactory>
{
    private readonly RealMerchantApiFactory _factory;

    public EcpayAmountCheckTests(RealMerchantApiFactory factory) => _factory = factory;

    private async Task<HttpResponseMessage> Notify(string orderNo, string amount, string? merchantId = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["MerchantID"] = merchantId ?? RealMerchantApiFactory.Ecpay["ECPay:MerchantID"]!,
            ["MerchantTradeNo"] = orderNo,
            ["RtnCode"] = "1",
            ["RtnMsg"] = "交易成功",
            ["TradeNo"] = "T" + orderNo,
            ["TradeAmt"] = amount,
            ["PaymentType"] = "Credit_CreditCard",
            ["SimulatePaid"] = "0",
        };
        fields["CheckMacValue"] = new EcpayService(new ConfigurationBuilder().AddInMemoryCollection(RealMerchantApiFactory.Ecpay).Build())
            .GenerateCheckMacValue(fields);
        return await _factory.CreateAnonymousClient().PostAsync("/api/PointRechargeApi/ecpay/return", new FormUrlEncodedContent(fields));
    }

    private async Task<(int UserId, string OrderNo)> NewOrder()
    {
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        await using var context = _factory.Db.NewContext();
        return (userId, (await new PointRechargeService(context).CreateOrderAsync(userId, 100, "ecpay")).OrderNo);
    }

    private async Task<(string Status, int Balance)> State(int userId, string orderNo)
    {
        await using var context = _factory.Db.NewContext();
        var status = (await context.PointRecharge.AsNoTracking().SingleAsync(r => r.OrderNo == orderNo)).Status;
        return (status, await new PointLogService(context).GetCurrentBalanceAsync(userId));
    }

    [Fact]
    public async Task Notification_with_a_different_amount_does_not_grant_points()
    {
        var (userId, orderNo) = await NewOrder();

        await Notify(orderNo, amount: "10");

        Assert.Equal(("pending", 0), await State(userId, orderNo));
    }

    [Fact]
    public async Task Notification_for_another_merchant_does_not_grant_points()
    {
        var (userId, orderNo) = await NewOrder();

        await Notify(orderNo, amount: "100", merchantId: "9999999");

        Assert.Equal(("pending", 0), await State(userId, orderNo));
    }

    [Fact]
    public async Task Matching_notification_still_grants_points()
    {
        var (userId, orderNo) = await NewOrder();

        var response = await Notify(orderNo, amount: "100");

        Assert.Equal("1|OK", await response.Content.ReadAsStringAsync());
        Assert.Equal(("success", 1000), await State(userId, orderNo));
    }
}

/// <summary>第三次全專案檢查 56：Gemini API Key 放在標頭，不放在網址。</summary>
public class GeminiApiKeyTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"hi\"}]}}]}"),
            });
        }
    }

    [Fact]
    public async Task Api_key_is_sent_in_a_header_not_in_the_url()
    {
        var handler = new CapturingHandler();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GeminiApiKey"] = "secret-key" }).Build();
        var service = new GeminiService(new HttpClient(handler), config, null!, NullLogger<GeminiService>.Instance);

        Assert.Equal("hi", await service.GetGeminiReplyAsync("hello"));

        Assert.NotNull(handler.Request);
        Assert.DoesNotContain("secret-key", handler.Request!.RequestUri!.ToString());
        Assert.Equal("secret-key", Assert.Single(handler.Request.Headers.GetValues("x-goog-api-key")));
    }
}
