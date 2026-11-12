namespace incode.MVC.Tests;

public class PointLogServiceTests
{
    private static async Task<bool> ChangeAndCommit(TestDb db, int userId, int amount)
    {
        await using var context = db.NewContext();
        var service = new PointLogService(context);
        await using var tx = await context.Database.BeginTransactionAsync();
        var ok = await service.ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return ok;
    }

    [Fact]
    public async Task ChangePoints_without_transaction_throws()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await using var context = db.NewContext();
        var service = new PointLogService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePointsAsync(userId, 10, "test", null, PointType.SystemAdjustment));
    }

    [Fact]
    public async Task ChangePoints_keeps_running_balance()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();

        Assert.True(await ChangeAndCommit(db, userId, 100));
        Assert.True(await ChangeAndCommit(db, userId, -30));

        await using var context = db.NewContext();
        Assert.Equal(70, await new PointLogService(context).GetCurrentBalanceAsync(userId));
    }

    [Fact]
    public async Task ChangePoints_rejects_deduction_below_zero_and_writes_nothing()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await ChangeAndCommit(db, userId, 50);

        Assert.False(await ChangeAndCommit(db, userId, -51));

        await using var context = db.NewContext();
        Assert.Equal(50, await new PointLogService(context).GetCurrentBalanceAsync(userId));
        Assert.Equal(1, await context.PointLog.CountAsync());
    }

    [Fact]
    public async Task ChangePoints_counts_unsaved_changes_in_same_transaction()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();

        await using var context = db.NewContext();
        var service = new PointLogService(context);
        await using var tx = await context.Database.BeginTransactionAsync();

        Assert.True(await service.ChangePointsAsync(userId, 100, "test", null, PointType.SystemAdjustment));
        // 第一筆尚未 SaveChanges：餘額仍要算進去，否則這筆 -150 會被誤判為可扣
        Assert.False(await service.ChangePointsAsync(userId, -150, "test", null, PointType.SystemAdjustment));
        Assert.True(await service.ChangePointsAsync(userId, -40, "test", null, PointType.SystemAdjustment));
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        Assert.Equal(60, await service.GetCurrentBalanceAsync(userId));
    }

    [Fact]
    public async Task CompleteRecharge_grants_points_only_once()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await using (var seed = db.NewContext())
        {
            seed.PointRecharge.Add(new PointRecharge
            {
                UserId = userId, OrderNo = "PR1", CashAmount = 50, PointsGranted = 500,
                PaymentMethod = "ecpay", Status = "pending",
            });
            await seed.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var service = new PointLogService(context);

        Assert.Equal(RechargeResult.Completed, await service.CompleteRechargeAsync("PR1", "T1"));
        Assert.Equal(RechargeResult.AlreadyProcessed, await service.CompleteRechargeAsync("PR1", "T1"));
        Assert.Equal(RechargeResult.AlreadyProcessed, await service.CompleteRechargeAsync("PR1", "T2"));

        Assert.Equal(500, await service.GetCurrentBalanceAsync(userId));
        Assert.Equal(1, await context.PointLog.CountAsync());
    }

    [Fact]
    public async Task CompleteRecharge_unknown_order_is_not_found()
    {
        using var db = new TestDb();
        db.SeedUser();
        await using var context = db.NewContext();

        Assert.Equal(RechargeResult.NotFound, await new PointLogService(context).CompleteRechargeAsync("NOPE", null));
    }

    [Fact]
    public async Task Tip_log_shows_nicknames_not_emails()
    {
        // 第二次檢查 29：點數紀錄原本寫入對方的 Email（「給 xxx@…」「來自 xxx@…」），打賞 1 點就能拿到對方 Email
        using var db = new TestDb();
        var from = db.SeedUser("sender@example.com");
        var to = db.SeedUser("author@example.com");
        await using (var seed = db.NewContext())
        {
            (await seed.User.SingleAsync(u => u.UserId == from)).Username = "打賞的人";
            (await seed.User.SingleAsync(u => u.UserId == to)).Username = "發文的人";
            seed.Content.Add(new Content { ContentId = 77, UserId = to, Title = "文章", Body = "b", Type = 1, IsActive = 1 });
            await seed.SaveChangesAsync();
        }
        await ChangeAndCommit(db, from, 100);

        await using (var context = db.NewContext())
            await new PointLogService(context).TipAsync(from, to, 77, 10);

        await using var verify = db.NewContext();
        var subtitles = await verify.PointLog.AsNoTracking().Where(l => l.SourceType == "point_tip").Select(l => l.DisplaySourceSubTitle).ToListAsync();
        Assert.Equal(2, subtitles.Count);
        Assert.All(subtitles, s => Assert.DoesNotContain("@", s));
        Assert.Contains("給 發文的人", subtitles);
        Assert.Contains("來自 打賞的人", subtitles);
    }

    [Fact]
    public async Task Tip_to_self_or_non_positive_amount_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await using var context = db.NewContext();
        var service = new PointLogService(context);

        await Assert.ThrowsAsync<BusinessException>(() => service.TipAsync(userId, userId, 1, 10));
        await Assert.ThrowsAsync<BusinessException>(() => service.TipAsync(userId, userId + 1, 1, 0));
    }

    [Fact]
    public async Task Tip_with_insufficient_balance_is_rejected_and_writes_nothing()
    {
        using var db = new TestDb();
        var from = db.SeedUser("from@example.com");
        var to = db.SeedUser("to@example.com");
        await ChangeAndCommit(db, from, 20);

        await using var context = db.NewContext();
        var ex = await Assert.ThrowsAsync<BusinessException>(() => new PointLogService(context).TipAsync(from, to, 1, 50));
        Assert.Equal("點數不足", ex.Message);

        await using var verify = db.NewContext();
        Assert.Equal(1, await verify.PointLog.CountAsync());
        Assert.Equal(0, await verify.PointTip.CountAsync());
    }
}
