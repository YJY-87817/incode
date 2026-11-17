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
            var service = CreateService(context, uploadRoot);

            var result = await service.CreateContentWithEntrustMissionAsync(new CreateContentWithEntrustMissionDto
            {
                Title = "發文含委託", Body = "<p>b</p>", Type = 1,
                EntrustMission = new CreateEntrustMissionDto { Title = "委託", Detail = "d", RewardPoints = 100 },
            }, userId);

            Assert.False(result.Ok);
            Assert.Equal(400, result.Code);
            await using var verify = _db.NewContext();
            Assert.False(await verify.Content.AnyAsync(c => c.UserId == userId));
            Assert.Empty(Directory.GetFiles(Path.Combine(uploadRoot, UploadFolders.ContentBodies)));
        }
        finally
        {
            if (Directory.Exists(uploadRoot)) Directory.Delete(uploadRoot, recursive: true);
        }
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
