using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using incode.MVC.Dtos.Contents;
using incode.MVC.Services.Contents;

namespace incode.MVC.Tests;

/// <summary>
/// 第二次全專案檢查 40～44 在真正 SQL Server 上的行為：道具併發扣減、後台會員列表的批次查詢（SQL 轉譯）、
/// 「發文 + 委託」共用交易（委託服務的點數鎖 sp_getapplock 只在 SQL Server 執行）。
/// </summary>
[Trait("Category", "Concurrency")] // 在 CI 的 SQL Server job 執行
public class SqlServerLowPriorityTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerDb _db;

    public SqlServerLowPriorityTests(SqlServerFixture fixture) => _db = fixture.Db;

    private int NewUser() => _db.SeedUser($"{Guid.NewGuid():N}@example.com");

    [SqlServerFact]
    public async Task Concurrent_item_use_never_goes_below_zero()
    {
        var userId = NewUser();
        await using (var context = _db.NewContext())
        {
            context.UserInventory.Add(new UserInventory { UserId = userId, ItemId = 1, Quantity = 3 });
            await context.SaveChangesAsync();
        }

        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            await using var context = _db.NewContext();
            return await new InventoryService(context).UseItemAsync(userId, 1);
        })).ToArray();
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        // 原本讀出數量再存回：同時使用 10 次會有多次「成功」，數量卻只扣一次
        Assert.Equal(3, results.Count(r => r >= 0));
        await using var verify = _db.NewContext();
        Assert.Equal(0, (await verify.UserInventory.AsNoTracking().SingleAsync(i => i.UserId == userId)).Quantity);
    }

    [SqlServerFact]
    public async Task Recent_login_logs_query_translates_on_sql_server()
    {
        var userId = NewUser();
        await using (var context = _db.NewContext())
        {
            for (int i = 0; i < 12; i++)
                context.UserLoginlog.Add(new UserLoginlog { UserId = userId, IpAddress = $"10.0.0.{i}", Logintime = new DateTime(2026, 11, 1).AddHours(i) });
            await context.SaveChangesAsync();
        }

        await using var read = _db.NewContext();
        var logs = await new UserService(read, new MemoryCache(new MemoryCacheOptions())).GetRecentLoginLogsForAllUsersAsync(10);

        Assert.Equal(10, logs[userId].Count);
        Assert.Equal("10.0.0.11", logs[userId][0].IpAddress);
    }

    [SqlServerFact]
    public async Task Post_with_entrust_rolls_back_the_post_when_points_are_insufficient()
    {
        var userId = NewUser(); // 沒有點數
        var uploadRoot = Path.Combine(Path.GetTempPath(), "incode-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var context = _db.NewContext();
            var category = new ContentCategory { CategoryName = "c" + Guid.NewGuid().ToString("N")[..8], Description = "d", IsActive = 2 };
            context.ContentCategory.Add(category);
            await context.SaveChangesAsync();
            var service = CreateService(context, uploadRoot);

            var result = await service.CreateContentWithEntrustMissionAsync(new CreateContentWithEntrustMissionDto
            {
                Title = "發文含委託", Body = "<p>b</p>", Type = 1, CategoryId = category.ContentCategoryId,
                EntrustMission = new CreateEntrustMissionDto { Title = "委託", Detail = "d", RewardPoints = 100 },
            }, userId);

            Assert.False(result.Ok);
            Assert.Equal(400, result.Code);
            Assert.Contains("點數", result.Message); // 是委託失敗（點數不足）而 rollback，不是發文檢查擋下
            await using var verify = _db.NewContext();
            Assert.False(await verify.Content.AnyAsync(c => c.UserId == userId));
            Assert.Empty(Directory.GetFiles(Path.Combine(uploadRoot, UploadFolders.ContentBodies)));
        }
        finally
        {
            if (Directory.Exists(uploadRoot)) Directory.Delete(uploadRoot, recursive: true);
        }
    }

    [SqlServerFact]
    public async Task Concurrent_manual_completion_and_ecpay_callback_grant_points_once()
    {
        var userId = NewUser();
        string orderNo = "T" + Guid.NewGuid().ToString("N")[..18];
        int rechargeId;
        await using (var context = _db.NewContext())
        {
            var recharge = new PointRecharge { UserId = userId, OrderNo = orderNo, CashAmount = 50, PointsGranted = 500, PaymentMethod = "ecpay", Status = "pending", CreatedAt = DateTime.Now };
            context.PointRecharge.Add(recharge);
            await context.SaveChangesAsync();
            rechargeId = recharge.PointRechargeId;
        }

        // 後台人工補單 5 次 + 綠界付款成功通知 5 次同時進來
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            await gate.Task;
            await using var context = _db.NewContext();
            var service = new PointLogService(context);
            try
            {
                if (i % 2 == 0) await service.AdminCompleteRechargeAsync(rechargeId, "manual");
                else await service.CompleteRechargeAsync(orderNo, "ecpay-trade");
            }
            catch (BusinessException) { } // 已經成功：補單回「不能重複加點」
        })).ToArray();
        gate.SetResult();
        await Task.WhenAll(tasks);

        await using var verify = _db.NewContext();
        var logs = await verify.PointLog.AsNoTracking().Where(l => l.UserId == userId).ToListAsync();
        Assert.Equal(500, Assert.Single(logs).ChangeAmount);
        Assert.Equal("success", (await verify.PointRecharge.AsNoTracking().SingleAsync(r => r.PointRechargeId == rechargeId)).Status);
    }

    [SqlServerFact]
    public async Task Concurrent_admin_adjustments_keep_the_ledger_continuous()
    {
        var userId = NewUser();
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            await using var context = _db.NewContext();
            await new PointLogService(context).AdminAdjustPointsAsync(userId, 10, "併發測試");
        })).ToArray();
        gate.SetResult();
        await Task.WhenAll(tasks);

        await using var verify = _db.NewContext();
        var balances = await verify.PointLog.AsNoTracking().Where(l => l.UserId == userId)
            .OrderBy(l => l.PointLogId).Select(l => l.BalanceAfter).ToListAsync();
        Assert.Equal(Enumerable.Range(1, 10).Select(i => i * 10), balances); // 每筆接續上一筆，沒有讀到舊餘額
    }

    private static ContentEntrustMissionService CreateService(incodedatabaseContext context, string uploadRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:UploadRoot"] = uploadRoot })
            .Build();
        var storage = new UploadStorage(configuration, new TestEnvironment(uploadRoot));
        var points = new PointLogService(context);
        var reserve = new PointReserveService(context, points);
        var interactions = new ContentInteractionService(context, points, reserve, new FakeMissionService());
        var contents = new ContentService(context, storage, interactions, new FakeMissionService());
        var missions = new EntrustMissionService(context, points, reserve, NullLogger<EntrustMissionService>.Instance);
        return new ContentEntrustMissionService(contents, missions, context, storage);
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "incode.MVC.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
    }
}
