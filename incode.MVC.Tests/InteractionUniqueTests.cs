using incode.MVC.Dtos.Contents;
using incode.MVC.Services.Contents;

namespace incode.MVC.Tests;

/// <summary>
/// 第二次全專案檢查 42：論壇互動與追蹤不能因為連點兩下寫入兩筆。
/// 唯一索引的建立在 SQLite 驗證；同時送出的請求需要真正的 SQL Server（SQLite 的寫入會整個資料庫排隊，重現不了）。
/// </summary>
public class InteractionUniqueTests
{
    [Fact]
    public async Task Duplicate_interaction_is_rejected_by_unique_index()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        int contentId;
        await using (var context = db.NewContext())
        {
            await InteractionSeed.EnsureCategoriesAsync(context);
            var content = new Content { UserId = userId, Title = "t", Body = "b", Type = 1, IsActive = 1 };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            contentId = content.ContentId;
            context.ContentInteraction.Add(new ContentInteraction { ContentId = contentId, UserId = userId, CategoryId = InteractionCategoryIds.Like });
            await context.SaveChangesAsync();
        }

        await using var second = db.NewContext();
        second.ContentInteraction.Add(new ContentInteraction { ContentId = contentId, UserId = userId, CategoryId = InteractionCategoryIds.Like });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());

        // 不同種類的互動可以並存
        second.ChangeTracker.Clear();
        second.ContentInteraction.Add(new ContentInteraction { ContentId = contentId, UserId = userId, CategoryId = InteractionCategoryIds.Bookmark });
        await second.SaveChangesAsync();
    }
}

/// <summary>同時送出的互動 / 追蹤請求（SQL Server）。</summary>
[Trait("Category", "Concurrency")]
public class SqlServerInteractionUniqueTests : IClassFixture<SqlServerFixture>
{
    private const int Parallel = 10;
    private readonly SqlServerDb _db;

    public SqlServerInteractionUniqueTests(SqlServerFixture fixture) => _db = fixture.Db;

    private int NewUser() => _db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static async Task<T[]> RunTogether<T>(Func<Task<T>> work)
    {
        var gate = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, Parallel).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            return await work();
        })).ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    [SqlServerFact]
    public async Task Concurrent_interaction_toggles_never_create_two_rows()
    {
        var author = NewUser();
        var member = NewUser();
        int contentId;
        await using (var context = _db.NewContext())
        {
            await InteractionSeed.EnsureCategoriesAsync(context);
            var content = new Content { UserId = author, Title = "t", Body = "b", Type = 1, IsActive = 1 };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            contentId = content.ContentId;
        }

        var results = await RunTogether(async () =>
        {
            await using var context = _db.NewContext();
            var points = new PointLogService(context);
            var service = new ContentInteractionService(context, points, new PointReserveService(context, points), new FakeMissionService());
            return await service.AddOrUpdateInteraction(new CreateContentInteractionDto
            {
                // 收藏：與按讚同一條新增路徑（按讚另外會同步任務進度，FakeMissionService 不支援）
                ContentId = contentId, UserId = member, InteractionCategoryId = InteractionCategoryIds.Bookmark,
            });
        });

        // 原本沒有唯一索引：同時新增會寫入多筆；有索引但沒處理時，第二筆會 500
        Assert.All(results, r => Assert.True(r.Ok, r.Message));
        await using var verify = _db.NewContext();
        Assert.True(await verify.ContentInteraction.CountAsync(ci => ci.ContentId == contentId && ci.UserId == member) <= 1);
    }

    [SqlServerFact]
    public async Task Concurrent_follows_create_one_row_and_never_fail()
    {
        var follower = NewUser();
        var target = NewUser();

        var results = await RunTogether(async () =>
        {
            await using var context = _db.NewContext();
            return await new FollowService(context).FollowUserAsync(follower, target);
        });

        // 原本撞到主鍵（follower_id + followee_id）的請求丟例外 → 500
        Assert.Equal(1, results.Count(r => r == FollowResult.Success));
        Assert.All(results, r => Assert.Contains(r, new[] { FollowResult.Success, FollowResult.AlreadyFollowing }));
        await using var verify = _db.NewContext();
        Assert.Equal(1, await verify.Follow.CountAsync(f => f.FollowerId == follower && f.FolloweeId == target));
    }
}

internal static class InteractionSeed
{
    /// <summary>建立互動分類（與 docs/database/seed.sql 相同，編號 1～6）。主鍵是識別欄位，依序新增編號就會是 1、2、3…</summary>
    public static async Task EnsureCategoriesAsync(incodedatabaseContext context)
    {
        if (await context.InteractionCategory.AnyAsync()) return;
        foreach (var name in new[] { "like", "hide", "bookmark", "report", "best_answer", "tip" })
        {
            context.InteractionCategory.Add(new InteractionCategory { Name = name, Description = name, IsActive = 1 });
            await context.SaveChangesAsync();
        }
    }
}
