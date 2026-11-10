namespace incode.MVC.Tests;

public class PointRechargeServiceTests
{
    [Fact]
    public async Task CreateOrder_computes_points_on_server_from_cash()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await using var context = db.NewContext();

        var order = await new PointRechargeService(context).CreateOrderAsync(userId, 95, "ecpay");

        Assert.Equal(950, order.PointsGranted);
        Assert.Equal("pending", order.Status);
        Assert.StartsWith("PR", order.OrderNo);
    }

    [Fact]
    public async Task MarkFailed_does_not_overwrite_a_successful_order()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await using (var seed = db.NewContext())
        {
            seed.PointRecharge.AddRange(
                new PointRecharge { UserId = userId, OrderNo = "OK", CashAmount = 10, PointsGranted = 100, PaymentMethod = "ecpay", Status = "success" },
                new PointRecharge { UserId = userId, OrderNo = "PENDING", CashAmount = 10, PointsGranted = 100, PaymentMethod = "ecpay", Status = "pending" });
            await seed.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var service = new PointRechargeService(context);

        Assert.Equal(0, await service.MarkFailedAsync("OK", "T"));
        Assert.Equal(1, await service.MarkFailedAsync("PENDING", "T"));

        Assert.Equal("success", (await context.PointRecharge.AsNoTracking().SingleAsync(r => r.OrderNo == "OK")).Status);
        Assert.Equal("failed", (await context.PointRecharge.AsNoTracking().SingleAsync(r => r.OrderNo == "PENDING")).Status);
    }

    [Fact]
    public async Task GetUserOrder_returns_null_for_someone_elses_order()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var other = db.SeedUser("other@example.com");
        await using var context = db.NewContext();
        var service = new PointRechargeService(context);
        var order = await service.CreateOrderAsync(owner, 100, "ecpay");

        Assert.NotNull(await service.GetUserOrderAsync(owner, order.OrderNo));
        Assert.Null(await service.GetUserOrderAsync(other, order.OrderNo));
        Assert.Null(await service.GetUserOrderAsync(owner, "NOPE"));
    }
}
