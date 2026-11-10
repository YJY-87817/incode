using Microsoft.Extensions.Logging.Abstractions;
using incode.MVC.Dtos.PointShop;
using incode.MVC.Services.PointShop;

namespace incode.MVC.Tests;

public class OrderServiceTests
{
    private static OrderService Create(incodedatabaseContext context, FakeMissionService? missions = null)
        => new(context, new PointLogService(context), missions ?? new FakeMissionService(), NullLogger<OrderService>.Instance);

    private static async Task GivePoints(TestDb db, int userId, int amount)
    {
        await using var context = db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task<int> AddProduct(TestDb db, int point, int stock, bool onSale = true)
    {
        await using var context = db.NewContext();
        var product = new Product
        {
            Name = "p", Description = "d", ImageName = "i.png", Type = "t", Series = "s",
            IsPhysical = false, Point = point, Stock = stock, OnSale = onSale,
            CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Product.Add(product);
        await context.SaveChangesAsync();
        return product.ProductId;
    }

    private static OrderItemRequest Item(int productId, int quantity) => new() { ProductId = productId, Quantity = quantity };

    private static async Task<int> Balance(TestDb db, int userId)
    {
        await using var context = db.NewContext();
        return await new PointLogService(context).GetCurrentBalanceAsync(userId);
    }

    private static async Task<int> Stock(TestDb db, int productId)
    {
        await using var context = db.NewContext();
        return await context.Product.AsNoTracking().Where(p => p.ProductId == productId).Select(p => p.Stock).SingleAsync();
    }

    [Fact]
    public async Task Create_deducts_points_and_stock_using_database_price()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 100, stock: 5);
        var missions = new FakeMissionService();

        await using var context = db.NewContext();
        var result = await Create(context, missions).CreateOrderAsync(userId, new[] { Item(productId, 2) }, null, null, requireRecipient: false);

        Assert.True(result.Ok);
        Assert.Equal(800, await Balance(db, userId));
        Assert.Equal(3, await Stock(db, productId));
        Assert.Equal(1, missions.Triggered);

        await using var verify = db.NewContext();
        var order = await verify.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(200, order.TotalPoint);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task Create_with_insufficient_points_rolls_back_stock_and_order()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 100);
        var productId = await AddProduct(db, point: 100, stock: 5);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 2) }, null, null, requireRecipient: false);

        Assert.False(result.Ok);
        Assert.Equal(409, result.Code);
        Assert.Equal(5, await Stock(db, productId));
        Assert.Equal(100, await Balance(db, userId));
        await using var verify = db.NewContext();
        Assert.Equal(0, await verify.Orders.CountAsync());
    }

    [Fact]
    public async Task Create_with_insufficient_stock_fails_without_charging()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 10, stock: 1);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 2) }, null, null, requireRecipient: false);

        Assert.False(result.Ok);
        Assert.Equal(1, await Stock(db, productId));
        Assert.Equal(1000, await Balance(db, userId));
    }

    [Fact]
    public async Task Create_merges_duplicate_products_into_one_line()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 10, stock: 10);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 1), Item(productId, 2) }, null, null, requireRecipient: false);

        Assert.True(result.Ok);
        await using var verify = db.NewContext();
        var line = await verify.Orderdetail.AsNoTracking().SingleAsync();
        Assert.Equal(3, line.Quantity);
        Assert.Equal(970, await Balance(db, userId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_rejects_non_positive_quantity(int quantity)
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 10, stock: 10);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, quantity) }, null, null, requireRecipient: false);

        Assert.False(result.Ok);
        Assert.Equal(10, await Stock(db, productId));
        Assert.Equal(1000, await Balance(db, userId));
    }

    [Fact]
    public async Task Create_rejects_product_that_is_not_on_sale()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 10, stock: 10, onSale: false);

        await using var context = db.NewContext();
        var result = await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 1) }, null, null, requireRecipient: false);

        Assert.False(result.Ok);
        Assert.Equal(10, await Stock(db, productId));
    }

    [Fact]
    public async Task Cancel_refunds_points_and_stock_and_cannot_repeat()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 100, stock: 5);

        int orderId;
        await using (var context = db.NewContext())
            orderId = (await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 2) }, null, null, requireRecipient: false)).Data;

        await using (var context = db.NewContext())
        {
            var cancel = await Create(context).CancelOrderAsync(orderId);
            Assert.True(cancel.Ok);
        }

        Assert.Equal(1000, await Balance(db, userId));
        Assert.Equal(5, await Stock(db, productId));

        await using (var context = db.NewContext())
        {
            var again = await Create(context).CancelOrderAsync(orderId);
            Assert.False(again.Ok);
            Assert.Equal(409, again.Code);
        }
        Assert.Equal(1000, await Balance(db, userId)); // 沒有重複退點
        Assert.Equal(5, await Stock(db, productId));
    }

    [Fact]
    public async Task Cancel_unknown_order_is_404()
    {
        using var db = new TestDb();
        db.SeedUser();
        await using var context = db.NewContext();

        var result = await Create(context).CancelOrderAsync(999);

        Assert.Equal(404, result.Code);
    }

    [Fact]
    public async Task Update_items_charges_difference_and_adjusts_stock()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 100, stock: 5);

        int orderId;
        await using (var context = db.NewContext())
            orderId = (await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 2) }, null, null, requireRecipient: false)).Data;

        await using (var context = db.NewContext())
            Assert.True((await Create(context).UpdateOrderItemsAsync(orderId, new[] { Item(productId, 3) }, null)).Ok);
        Assert.Equal(700, await Balance(db, userId));
        Assert.Equal(2, await Stock(db, productId));

        await using (var context = db.NewContext())
            Assert.True((await Create(context).UpdateOrderItemsAsync(orderId, new[] { Item(productId, 1) }, null)).Ok);
        Assert.Equal(900, await Balance(db, userId));
        Assert.Equal(4, await Stock(db, productId));
    }

    [Fact]
    public async Task Update_canceled_order_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 1000);
        var productId = await AddProduct(db, point: 100, stock: 5);

        int orderId;
        await using (var context = db.NewContext())
            orderId = (await Create(context).CreateOrderAsync(userId, new[] { Item(productId, 1) }, null, null, requireRecipient: false)).Data;
        await using (var context = db.NewContext())
            await Create(context).CancelOrderAsync(orderId);

        await using var update = db.NewContext();
        var result = await Create(update).UpdateOrderItemsAsync(orderId, new[] { Item(productId, 2) }, null);

        Assert.Equal(409, result.Code);
        Assert.Equal(1000, await Balance(db, userId));
    }

    [Fact]
    public async Task GetOwnOrder_returns_null_for_other_users()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var other = db.SeedUser("other@example.com");
        await GivePoints(db, owner, 1000);
        var productId = await AddProduct(db, point: 10, stock: 5);

        await using var context = db.NewContext();
        var service = Create(context);
        var orderId = (await service.CreateOrderAsync(owner, new[] { Item(productId, 1) }, null, null, requireRecipient: false)).Data;

        Assert.NotNull(await service.GetOwnOrderAsync(orderId, owner));
        Assert.Null(await service.GetOwnOrderAsync(orderId, other));
    }
}
