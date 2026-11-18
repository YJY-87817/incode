using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using incode.MVC.Dtos.PointShop;
using incode.MVC.Services.PointShop;

namespace incode.MVC.Tests;

/// <summary>
/// 後台點數商城改走服務層（Order、Shipped、Product）：頁面仍可使用，並修正搬移時發現的問題：
/// 寄送資料指定不存在的訂單 / 過長欄位原本 500、所屬訂單可被改掉、已出貨訂單的紀錄可被刪除；
/// 商品編輯用表單庫存覆蓋資料庫（蓋掉會員下單扣的庫存）；訂單頁把會員填的收件資料當 HTML 組出來（XSS）。
/// </summary>
public class AdminShopTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AdminShopTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static async Task<HttpResponseMessage> PostForm(HttpClient admin, string pageUrl, string postUrl, params (string Key, string Value)[] fields)
    {
        var token = Token(await admin.GetStringAsync(pageUrl));
        Assert.NotEmpty(token);
        var form = fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Append(new("__RequestVerificationToken", token));
        return await admin.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    private async Task<int> AddProduct(int point = 10, int stock = 10)
    {
        await using var context = _factory.Db.NewContext();
        var product = new Product
        {
            Name = "商品" + Guid.NewGuid().ToString("N")[..6], Description = "d", ImageName = "NoImage.png", Type = "t", Series = "s",
            IsPhysical = false, Point = point, Stock = stock, OnSale = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Product.Add(product);
        await context.SaveChangesAsync();
        return product.ProductId;
    }

    private async Task<Product> Product(int id)
    {
        await using var context = _factory.Db.NewContext();
        return await context.Product.AsNoTracking().SingleAsync(p => p.ProductId == id);
    }

    private async Task<Orders> Order(int id)
    {
        await using var context = _factory.Db.NewContext();
        return await context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == id);
    }

    private async Task GivePoints(int userId, int amount)
    {
        await using var context = _factory.Db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // 經由服務建立訂單（扣庫存、扣點）
    private async Task<(int OrderId, int UserId, int ProductId)> NewOrder(string status = OrderStatus.Pending)
    {
        var userId = NewUser();
        await GivePoints(userId, 100);
        var productId = await AddProduct(point: 30, stock: 10);
        await using var context = _factory.Db.NewContext();
        var service = new OrderService(context, new PointLogService(context), new FakeMissionService(), NullLogger<OrderService>.Instance);
        var result = await service.CreateOrderAsync(userId, new[] { new OrderItemRequest { ProductId = productId, Quantity = 1 } },
            null, null, requireRecipient: false, status: status);
        Assert.True(result.Ok, result.Message);
        return (result.Data, userId, productId);
    }

    private async Task<int> AddShipped(int orderId, bool failed = false, bool completed = false)
    {
        await using var context = _factory.Db.NewContext();
        var shipped = new ShippedDetail
        {
            OrderId = orderId, RecipientName = "王小明", RecipientAddress = "台北市", RecipientPhone = "0912",
            IsFailed = failed, IsCompleted = completed,
        };
        context.ShippedDetail.Add(shipped);
        await context.SaveChangesAsync();
        return shipped.ShippedDetailId;
    }

    private async Task<ShippedDetail?> Shipped(int id)
    {
        await using var context = _factory.Db.NewContext();
        return await context.ShippedDetail.AsNoTracking().FirstOrDefaultAsync(s => s.ShippedDetailId == id);
    }

    private static string Decoded(HttpResponseMessage response) => WebUtility.HtmlDecode(response.Content.ReadAsStringAsync().Result);

    // ---------- 頁面 ----------

    [Fact]
    public async Task Admin_shop_pages_render()
    {
        var (orderId, _, productId) = await NewOrder();
        var shippedId = await AddShipped(orderId);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        foreach (var url in new[]
        {
            "/Order", "/Order/Create", $"/Order/Edit/{orderId}", "/Order/GetShippedData",
            "/Shipped", "/Shipped/Create", $"/Shipped/Edit/{shippedId}", $"/Shipped/Delete/{shippedId}",
            "/Product", "/Product/Create", $"/Product/Edit/{productId}",
        })
        {
            var response = await admin.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} → {(int)response.StatusCode}");
        }
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Order/Edit/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Product/Edit/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Shipped/Edit/999999")).StatusCode);

        var rows = await admin.GetStringAsync("/Order/GetShippedData");
        Assert.Contains("\"recipientName\":\"王小明\"", System.Text.RegularExpressions.Regex.Unescape(rows));
        Assert.Contains("\"orderStatus\"", rows);
    }

    [Fact]
    public async Task Order_page_escapes_member_supplied_recipient_data()
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        // 收件人 / 地址 / 電話 / 備註是會員在前台填的：組 HTML 前要跳脫
        var script = Regex.Replace(await admin.GetStringAsync("/Order"), @"\s+", "");

        Assert.Contains("functionesc(value)", script);
        foreach (var field in new[] { "recipientName", "recipientAddress", "recipientPhone", "remark" })
        {
            Assert.Contains($"esc(item.{field})", script);
            Assert.DoesNotContain($"(item.{field}||'')", script); // 原本直接串進 HTML
        }
    }

    // ---------- 訂單 ----------

    [Fact]
    public async Task Order_edit_changes_status_together_with_items()
    {
        var (orderId, userId, productId) = await NewOrder();
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, $"/Order/Edit/{orderId}", $"/Order/Edit/{orderId}",
            ("OrderId", orderId.ToString()), ("UserId", userId.ToString()), ("Status", OrderStatus.Shipped), ("Remark", "已寄出"),
            ("orderdetails[0].ProductId", productId.ToString()), ("orderdetails[0].Quantity", "2"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var order = await Order(orderId);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("已寄出", order.Remark);
        Assert.Equal(60, order.TotalPoint);
        Assert.Equal(8, (await Product(productId)).Stock);
    }

    [Fact]
    public async Task Order_edit_cancel_refunds_and_canceled_order_cannot_be_restored()
    {
        var (orderId, userId, productId) = await NewOrder();
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var cancel = await PostForm(admin, $"/Order/Edit/{orderId}", $"/Order/Edit/{orderId}",
            ("OrderId", orderId.ToString()), ("UserId", userId.ToString()), ("Status", OrderStatus.Canceled), ("Remark", "客人取消"));
        Assert.Equal(HttpStatusCode.Redirect, cancel.StatusCode);
        Assert.Equal(OrderStatus.Canceled, (await Order(orderId)).Status);
        Assert.Equal(10, (await Product(productId)).Stock);

        var restore = await PostForm(admin, $"/Order/Edit/{orderId}", $"/Order/Edit/{orderId}",
            ("OrderId", orderId.ToString()), ("UserId", userId.ToString()), ("Status", OrderStatus.Pending), ("Remark", ""));
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.Contains("已取消的訂單不能恢復", Decoded(restore));
        Assert.Equal(OrderStatus.Canceled, (await Order(orderId)).Status);
    }

    [Fact]
    public async Task Order_create_checks_member_and_cannot_start_canceled()
    {
        var userId = NewUser();
        await GivePoints(userId, 100);
        var productId = await AddProduct(point: 10, stock: 5);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var noMember = await PostForm(admin, "/Order/Create", "/Order/Create",
            ("UserId", "999999"), ("Status", OrderStatus.Pending), ("orderdetails[0].ProductId", productId.ToString()), ("orderdetails[0].Quantity", "1"));
        Assert.Equal(HttpStatusCode.OK, noMember.StatusCode);
        Assert.Contains("找不到會員", Decoded(noMember));

        var canceled = await PostForm(admin, "/Order/Create", "/Order/Create",
            ("UserId", userId.ToString()), ("Status", OrderStatus.Canceled), ("orderdetails[0].ProductId", productId.ToString()), ("orderdetails[0].Quantity", "1"));
        Assert.Equal(HttpStatusCode.OK, canceled.StatusCode);
        Assert.Equal(5, (await Product(productId)).Stock);

        var ok = await PostForm(admin, "/Order/Create", "/Order/Create",
            ("UserId", userId.ToString()), ("Status", OrderStatus.Pending), ("orderdetails[0].ProductId", productId.ToString()), ("orderdetails[0].Quantity", "2"));
        Assert.Equal(HttpStatusCode.Redirect, ok.StatusCode);
        Assert.Equal(3, (await Product(productId)).Stock);
    }

    // ---------- 寄送紀錄 ----------

    private static (string, string)[] ShippedForm(int orderId, string name = "收件人", string address = "台北市信義區", string phone = "0912345678", params (string, string)[] extra)
        => new[] { ("OrderId", orderId.ToString()), ("RecipientName", name), ("RecipientAddress", address), ("RecipientPhone", phone), ("Remark", "") }.Concat(extra).ToArray();

    [Fact]
    public async Task Shipped_create_validates_order_and_lengths()
    {
        var (pending, _, _) = await NewOrder();
        var (canceledOrder, userId, _) = await NewOrder();
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        await PostForm(admin, $"/Order/Edit/{canceledOrder}", $"/Order/Edit/{canceledOrder}",
            ("OrderId", canceledOrder.ToString()), ("UserId", userId.ToString()), ("Status", OrderStatus.Canceled));

        var noOrder = await PostForm(admin, "/Shipped/Create", "/Shipped/Create", ShippedForm(999999));
        var canceled = await PostForm(admin, "/Shipped/Create", "/Shipped/Create", ShippedForm(canceledOrder));
        var tooLong = await PostForm(admin, "/Shipped/Create", "/Shipped/Create", ShippedForm(pending, address: new string('址', 51)));
        var empty = await PostForm(admin, "/Shipped/Create", "/Shipped/Create", ShippedForm(pending, name: " "));

        // 原本：不存在的訂單 / 過長欄位撞資料庫 → 500
        Assert.Equal(HttpStatusCode.OK, noOrder.StatusCode);
        Assert.Contains("找不到訂單", Decoded(noOrder));
        Assert.Contains("已取消的訂單不能新增", Decoded(canceled));
        Assert.Contains("收件地址最多", Decoded(tooLong));
        Assert.Contains("請填寫完整", Decoded(empty));
        await using (var context = _factory.Db.NewContext())
            Assert.False(await context.ShippedDetail.AnyAsync(s => s.OrderId == pending || s.OrderId == canceledOrder));

        var ok = await PostForm(admin, "/Shipped/Create", "/Shipped/Create", ShippedForm(pending));
        Assert.Equal(HttpStatusCode.Redirect, ok.StatusCode);
        await using var verify = _factory.Db.NewContext();
        var created = await verify.ShippedDetail.SingleAsync(s => s.OrderId == pending);
        Assert.False(created.IsFailed || created.IsCompleted);
    }

    [Fact]
    public async Task Shipped_edit_cannot_move_it_to_another_order_or_be_failed_and_completed()
    {
        var (orderA, _, _) = await NewOrder();
        var (orderB, _, _) = await NewOrder();
        var shippedId = await AddShipped(orderA);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var move = await PostForm(admin, $"/Shipped/Edit/{shippedId}", $"/Shipped/Edit/{shippedId}",
            ShippedForm(orderB, "新名字", extra: ("ShippedDetailId", shippedId.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, move.StatusCode);
        var saved = (await Shipped(shippedId))!;
        Assert.Equal("新名字", saved.RecipientName);
        Assert.Equal(orderA, saved.OrderId); // 所屬訂單不可變更

        var both = await PostForm(admin, $"/Shipped/Edit/{shippedId}", $"/Shipped/Edit/{shippedId}",
            ShippedForm(orderA, extra: new[] { ("ShippedDetailId", shippedId.ToString()), ("IsFailed", "true"), ("IsCompleted", "true") }));
        Assert.Equal(HttpStatusCode.OK, both.StatusCode);
        Assert.Contains("不能同時", Decoded(both));
        Assert.False((await Shipped(shippedId))!.IsCompleted);

        var done = await PostForm(admin, $"/Shipped/Edit/{shippedId}", $"/Shipped/Edit/{shippedId}",
            ShippedForm(orderA, extra: new[] { ("ShippedDetailId", shippedId.ToString()), ("IsCompleted", "true") }));
        Assert.Equal(HttpStatusCode.Redirect, done.StatusCode);
        Assert.True((await Shipped(shippedId))!.IsCompleted);
    }

    [Fact]
    public async Task Shipped_of_shipped_or_completed_orders_cannot_be_deleted()
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        var ids = new Dictionary<string, int>();
        foreach (var status in new[] { OrderStatus.Pending, OrderStatus.Shipped, OrderStatus.Completed })
        {
            var (orderId, _, _) = await NewOrder(status);
            ids[status] = await AddShipped(orderId);
        }

        foreach (var id in ids.Values)
            Assert.Equal(HttpStatusCode.Redirect, (await PostForm(admin, $"/Shipped/Delete/{id}", $"/Shipped/Delete/{id}")).StatusCode);

        Assert.Null(await Shipped(ids[OrderStatus.Pending]));           // 待處理：可以刪除
        Assert.NotNull(await Shipped(ids[OrderStatus.Shipped]));        // 已出貨 / 已完成：保留
        Assert.NotNull(await Shipped(ids[OrderStatus.Completed]));
        Assert.Contains("不能刪除", Decoded(await admin.GetAsync("/Order")));

        var list = await admin.GetStringAsync("/Shipped");
        Assert.Contains($"/Shipped/Edit/{ids[OrderStatus.Shipped]}", list);
        Assert.DoesNotContain($"/Shipped/Delete/{ids[OrderStatus.Shipped]}", list);
    }

    // ---------- 商品 ----------

    [Fact]
    public async Task Product_create_validates_and_ignores_overposted_fields()
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        var name = "新商品" + Guid.NewGuid().ToString("N")[..6];

        var invalid = await PostForm(admin, "/Product/Create", "/Product/Create",
            ("Name", " "), ("Point", "10"), ("Stock", "5"), ("OnSale", "true"));
        var negative = await PostForm(admin, "/Product/Create", "/Product/Create",
            ("Name", name), ("Point", "-1"), ("Stock", "5"), ("OnSale", "true"));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("請填寫商品名稱", Decoded(invalid));
        Assert.Contains("所需點數", Decoded(negative));

        var ok = await PostForm(admin, "/Product/Create", "/Product/Create",
            ("Name", name), ("Point", "10"), ("Stock", "5"), ("OnSale", "true"), ("ImageName", "../../appsettings.json"), ("CreatedAt", "2000-01-01"));
        Assert.Equal(HttpStatusCode.Redirect, ok.StatusCode);
        await using var context = _factory.Db.NewContext();
        var product = await context.Product.AsNoTracking().SingleAsync(p => p.Name == name);
        Assert.Null(product.ImageName);                              // 圖片檔名只由伺服器產生
        Assert.True(product.CreatedAt > new DateTime(2020, 1, 1));   // 建立時間由系統設定
    }

    [Fact]
    public async Task Product_edit_does_not_overwrite_stock_changed_by_orders()
    {
        var productId = await AddProduct(point: 10, stock: 10);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        var page = await admin.GetStringAsync($"/Product/Edit/{productId}"); // 管理員打開編輯頁（庫存 10）
        Assert.Contains("name=\"stockDelta\"", page);

        await using (var context = _factory.Db.NewContext()) // 期間會員下單，庫存被扣到 3
            await context.Product.Where(p => p.ProductId == productId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, 3));

        // 表單送來的 Stock 不再被採用（原本會把庫存蓋回 10）；以 stockDelta 增減
        var response = await PostForm(admin, $"/Product/Edit/{productId}", $"/Product/Edit/{productId}",
            ("ProductId", productId.ToString()), ("Name", "改名"), ("Point", "20"), ("Stock", "10"), ("OnSale", "true"), ("stockDelta", "5"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var product = await Product(productId);
        Assert.Equal(8, product.Stock);
        Assert.Equal("改名", product.Name);
        Assert.Equal(20, product.Point);
    }

    [Fact]
    public async Task Product_stock_cannot_go_negative_and_failure_saves_nothing()
    {
        var productId = await AddProduct(point: 10, stock: 3);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, $"/Product/Edit/{productId}", $"/Product/Edit/{productId}",
            ("ProductId", productId.ToString()), ("Name", "不該存進去"), ("Point", "10"), ("OnSale", "true"), ("stockDelta", "-5"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("庫存不足", Decoded(response));
        var product = await Product(productId);
        Assert.Equal(3, product.Stock);
        Assert.NotEqual("不該存進去", product.Name); // 庫存調整失敗，其他欄位也不儲存
        Assert.Contains("value=\"3\"", Decoded(response)); // 重新顯示的是資料庫目前的庫存
        Assert.DoesNotContain("0001/01/01", Decoded(response));  // 建立 / 最後修改時間也從資料庫帶出
    }

    [Fact]
    public async Task Product_with_invalid_image_is_not_created()
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        var token = Token(await admin.GetStringAsync("/Product/Create"));
        var name = "假圖片" + Guid.NewGuid().ToString("N")[..6];
        using var content = new MultipartFormDataContent
        {
            { new StringContent(name), "Name" }, { new StringContent("10"), "Point" }, { new StringContent("5"), "Stock" },
            { new StringContent("true"), "OnSale" }, { new StringContent(token), "__RequestVerificationToken" },
        };
        content.Add(new ByteArrayContent("<?php echo 1;"u8.ToArray()), "imageFile", "evil.png");

        var response = await admin.PostAsync("/Product/Create", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var context = _factory.Db.NewContext();
        Assert.False(await context.Product.AnyAsync(p => p.Name == name));
    }
}
