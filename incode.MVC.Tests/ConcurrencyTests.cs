using Microsoft.Extensions.Logging.Abstractions;
using incode.MVC.Dtos.Point;
using incode.MVC.Dtos.PointShop;
using incode.MVC.Services.PointShop;

namespace incode.MVC.Tests;

/// <summary>
/// 併發測試：同時送出多個請求，檢查點數、庫存、領獎、退款只會成立應有的次數。
/// 需要真正的 SQL Server（<c>sp_getapplock</c> 與列鎖在 SQLite 不存在），沒有設定 INCODE_TEST_SQLSERVER 時略過。
/// 每個請求各自使用新的 DbContext 與服務實例，和正式環境一個請求一個 scope 相同。
/// </summary>
[Trait("Category", "Concurrency")]
public class ConcurrencyTests : IClassFixture<SqlServerFixture>
{
    private const int Parallel = 10;

    private readonly SqlServerDb _db;

    public ConcurrencyTests(SqlServerFixture fixture) => _db = fixture.Db;

    private int NewUser() => _db.SeedUser($"{Guid.NewGuid():N}@example.com");

    /// <summary>所有工作先等在閘門前，再同時放行。</summary>
    private static async Task<T[]> RunTogether<T>(int count, Func<int, Task<T>> work)
    {
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, count)
            .Select(i => Task.Run(async () =>
            {
                await gate.Task;
                return await work(i);
            }))
            .ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    private async Task GivePoints(int userId, int amount)
    {
        await using var context = _db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<int> Balance(int userId)
    {
        await using var context = _db.NewContext();
        return await new PointLogService(context).GetCurrentBalanceAsync(userId);
    }

    /// <summary>帳本是否連續：每一筆的 BalanceAfter = 前一筆 + 本筆異動，且沒有負數。</summary>
    private async Task AssertLedgerIsConsistent(int userId)
    {
        await using var context = _db.NewContext();
        var logs = await context.PointLog.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.PointLogId)
            .ToListAsync();

        var running = 0;
        foreach (var log in logs)
        {
            running += log.ChangeAmount;
            Assert.Equal(running, log.BalanceAfter);
            Assert.True(log.BalanceAfter >= 0, $"帳本出現負數餘額：{log.BalanceAfter}");
        }
    }

    private async Task<int> AddContent(int userId)
    {
        await using var context = _db.NewContext();
        var content = new Content { UserId = userId, Title = "t", Body = "b", Type = 0, IsActive = 1 };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    private async Task<int> AddProduct(int point, int stock)
    {
        await using var context = _db.NewContext();
        var product = new Product
        {
            Name = "p", Description = "d", ImageName = "i.png", Type = "t", Series = "s",
            IsPhysical = false, Point = point, Stock = stock, OnSale = true,
            CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Product.Add(product);
        await context.SaveChangesAsync();
        return product.ProductId;
    }

    private async Task<int> AddMission(string resetType, int reward = 50, int target = 1)
    {
        await using var context = _db.NewContext();
        var mission = new Mission
        {
            MissionName = "m", MissionDetail = "d", TargetType = "post", TargetCount = target, RewardPoints = reward,
            Category = "c", Difficulty = "簡單", ResetType = resetType, IsActive = true,
        };
        context.Mission.Add(mission);
        await context.SaveChangesAsync();
        return mission.MissionId;
    }

    private static OrderService CreateOrderService(incodedatabaseContext context)
        => new(context, new PointLogService(context), new FakeMissionService(), NullLogger<OrderService>.Instance);

    // ---------- 打賞：同時送出不會讓餘額變負數 ----------

    [SqlServerFact]
    public async Task Concurrent_tips_cannot_overdraw_the_balance()
    {
        var from = NewUser();
        var to = NewUser();
        await GivePoints(from, 100);
        var contentId = await AddContent(to);

        var results = await RunTogether(Parallel, async _ =>
        {
            await using var context = _db.NewContext();
            try
            {
                await new PointLogService(context).TipAsync(from, to, contentId, 30);
                return true;
            }
            catch (BusinessException)
            {
                return false; // 點數不足
            }
        });

        Assert.Equal(3, results.Count(ok => ok)); // 100 點最多打賞 3 次（每次 30）
        Assert.Equal(10, await Balance(from));
        Assert.Equal(90, await Balance(to));
        await AssertLedgerIsConsistent(from);
        await AssertLedgerIsConsistent(to);
    }

    // ---------- 綠界重複回呼：只加點一次 ----------

    [SqlServerFact]
    public async Task Duplicate_ecpay_callbacks_grant_points_once()
    {
        var userId = NewUser();
        await using (var seed = _db.NewContext())
        {
            seed.PointRecharge.Add(new PointRecharge
            {
                UserId = userId, OrderNo = "PR-" + Guid.NewGuid().ToString("N"), CashAmount = 50, PointsGranted = 500,
                PaymentMethod = "ecpay", Status = "pending",
            });
            await seed.SaveChangesAsync();
        }
        string orderNo;
        await using (var read = _db.NewContext())
            orderNo = (await read.PointRecharge.AsNoTracking().SingleAsync(r => r.UserId == userId)).OrderNo;

        var results = await RunTogether(Parallel, async _ =>
        {
            await using var context = _db.NewContext();
            return await new PointLogService(context).CompleteRechargeAsync(orderNo, "T1");
        });

        Assert.Equal(1, results.Count(r => r == RechargeResult.Completed));
        Assert.Equal(Parallel - 1, results.Count(r => r == RechargeResult.AlreadyProcessed));
        Assert.Equal(500, await Balance(userId));
        await AssertLedgerIsConsistent(userId);
    }

    // ---------- 任務領獎：同一筆進度只發一次 ----------

    [SqlServerFact]
    public async Task Concurrent_reward_claims_pay_once()
    {
        var userId = NewUser();
        var missionId = await AddMission("daily", reward: 50);
        int progressId;
        await using (var seed = _db.NewContext())
        {
            var progress = new MissionProgress
            {
                UserId = userId, MissionId = missionId, CurrentCount = 1, Status = "completed",
                CycleStartAt = DateTime.Now.Date, CycleEndAt = DateTime.Now.Date.AddDays(1),
            };
            seed.MissionProgress.Add(progress);
            await seed.SaveChangesAsync();
            progressId = progress.MissionProgressId;
        }

        var results = await RunTogether(Parallel, async _ =>
        {
            await using var context = _db.NewContext();
            var service = new MissionProgressService(context, new PointLogService(context), new FakeMissionService(),
                NullLogger<MissionProgressService>.Instance);
            return await service.ClaimRewardAsync(userId, progressId);
        });

        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.Equal(50, await Balance(userId));
        await AssertLedgerIsConsistent(userId);
    }

    // ---------- 任務領取：同一週期只建立一筆進度，其餘回 409（不是 500） ----------

    [SqlServerFact]
    public async Task Concurrent_mission_claims_create_one_progress_and_report_conflict()
    {
        var userId = NewUser();
        var missionId = await AddMission("once", target: 3);

        var results = await RunTogether(Parallel, async _ =>
        {
            await using var context = _db.NewContext();
            return await new MissionService(context).ClaimMissionAsync(missionId, userId);
        });

        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.All(results.Where(r => !r.Ok), r => Assert.Equal(409, r.Code));
        await using var verify = _db.NewContext();
        Assert.Equal(1, await verify.MissionProgress.CountAsync(p => p.UserId == userId && p.MissionId == missionId));
    }

    // ---------- 商城：同一會員同時下單不會花超過餘額 ----------

    [SqlServerFact]
    public async Task Concurrent_orders_by_one_member_cannot_overspend_points()
    {
        var userId = NewUser();
        await GivePoints(userId, 100);
        // 每個請求買不同的商品：扣庫存的 UPDATE 只會鎖住各自的商品，請求之間不會因商品列鎖而排隊，
        // 所以只有「會員點數鎖」（sp_getapplock）能防止每個請求都讀到 100 點的舊餘額
        var productIds = new List<int>();
        for (var i = 0; i < Parallel; i++) productIds.Add(await AddProduct(point: 30, stock: 100));

        var results = await RunTogether(Parallel, async i =>
        {
            await using var context = _db.NewContext();
            return await CreateOrderService(context).CreateOrderAsync(
                userId, new[] { new OrderItemRequest { ProductId = productIds[i], Quantity = 1 } }, null, null, requireRecipient: false);
        });

        Assert.Equal(3, results.Count(r => r.Ok)); // 100 點最多買 3 件
        Assert.All(results.Where(r => !r.Ok), r => Assert.Equal(409, r.Code));
        Assert.Equal(10, await Balance(userId));
        await AssertLedgerIsConsistent(userId);

        await using var verify = _db.NewContext();
        Assert.Equal(3, await verify.Orders.CountAsync(o => o.UserId == userId));
        var stocks = await verify.Product.AsNoTracking().Where(p => productIds.Contains(p.ProductId)).Select(p => p.Stock).ToListAsync();
        Assert.Equal(3, stocks.Count(stock => stock == 99)); // 成功的訂單扣了庫存，失敗的已回復
        Assert.Equal(7, stocks.Count(stock => stock == 100));
    }

    // ---------- 商城：多位會員搶最後的庫存不會超賣 ----------

    [SqlServerFact]
    public async Task Concurrent_orders_cannot_oversell_stock()
    {
        var productId = await AddProduct(point: 10, stock: 5);
        var users = Enumerable.Range(0, Parallel).Select(_ => NewUser()).ToArray();
        foreach (var user in users) await GivePoints(user, 100);

        var results = await RunTogether(Parallel, async i =>
        {
            await using var context = _db.NewContext();
            return await CreateOrderService(context).CreateOrderAsync(
                users[i], new[] { new OrderItemRequest { ProductId = productId, Quantity = 1 } }, null, null, requireRecipient: false);
        });

        Assert.Equal(5, results.Count(r => r.Ok));
        await using var verify = _db.NewContext();
        Assert.Equal(0, (await verify.Product.AsNoTracking().SingleAsync(p => p.ProductId == productId)).Stock);
        Assert.Equal(5, await verify.Orders.CountAsync(o => users.Contains(o.UserId)));

        // 成功的會員扣了 10 點，失敗的沒有被扣
        var balances = new List<int>();
        foreach (var user in users) balances.Add(await Balance(user));
        Assert.Equal(5, balances.Count(b => b == 90));
        Assert.Equal(5, balances.Count(b => b == 100));
    }

    // ---------- 委託取消：只退款一次 ----------

    [SqlServerFact]
    public async Task Concurrent_entrust_cancellations_refund_once()
    {
        var owner = NewUser();
        await GivePoints(owner, 500);
        var contentId = await AddContent(owner);

        int missionId;
        await using (var context = _db.NewContext())
        {
            var points = new PointLogService(context);
            var service = new EntrustMissionService(context, points, new PointReserveService(context, points),
                NullLogger<EntrustMissionService>.Instance);
            var created = await service.CreateEntrustMissionAsync(new EntrustMissionDTO
            {
                UserId = owner, ContentId = contentId, Title = "委託", Detail = "d", RewardPoints = 100, Category = "其他",
            });
            Assert.True(created.Ok, created.Message);
        }
        await using (var read = _db.NewContext())
            missionId = (await read.EntrustMission.AsNoTracking().SingleAsync(m => m.UserId == owner)).EntrustMissionId;
        Assert.Equal(400, await Balance(owner));

        var results = await RunTogether(Parallel, async _ =>
        {
            await using var context = _db.NewContext();
            var points = new PointLogService(context);
            var service = new EntrustMissionService(context, points, new PointReserveService(context, points),
                NullLogger<EntrustMissionService>.Instance);
            return await service.CancelEntrustMissionAsync(missionId, owner);
        });

        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.All(results.Where(r => !r.Ok), r => Assert.Equal(409, r.Code));
        Assert.Equal(500, await Balance(owner)); // 預扣的 100 點只退一次
        await AssertLedgerIsConsistent(owner);
    }
}
