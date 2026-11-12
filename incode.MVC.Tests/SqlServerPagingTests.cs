namespace incode.MVC.Tests;

/// <summary>
/// 分頁參數在 SQL Server 上的行為（第二次檢查 38）。SQLite 允許負的 OFFSET 與 LIMIT 0，
/// SQL Server 的 OFFSET … FETCH 遇到負數或 0 會丟錯，所以只能在真正的 SQL Server 上重現。
/// </summary>
[Trait("Category", "Concurrency")] // 在 CI 的 SQL Server job 執行
public class SqlServerPagingTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerDb _db;

    public SqlServerPagingTests(SqlServerFixture fixture) => _db = fixture.Db;

    private static ContentService CreateService(incodedatabaseContext context)
    {
        var points = new PointLogService(context);
        var interactions = new ContentInteractionService(context, points, new PointReserveService(context, points), new FakeMissionService());
        return new ContentService(context, storage: null!, interactions, new FakeMissionService()); // 查詢不會用到檔案存放
    }

    private async Task<int> SeedTopic()
    {
        var userId = _db.SeedUser($"{Guid.NewGuid():N}@example.com");
        await using var context = _db.NewContext();
        var topic = new Content { UserId = userId, Title = "主題", Body = "<p>b</p>", Type = 1, IsActive = 1 };
        context.Content.Add(topic);
        await context.SaveChangesAsync();
        context.Content.Add(new Content { UserId = userId, ParentContentId = topic.ContentId, Title = "回覆", Body = "<p>r</p>", Type = 2, IsActive = 1 });
        await context.SaveChangesAsync();
        return topic.ContentId;
    }

    [SqlServerTheory]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -3)]
    public async Task Bad_paging_values_do_not_throw(int page, int pageSize)
    {
        var topicId = await SeedTopic();
        await using var context = _db.NewContext();
        var service = CreateService(context);

        var list = await service.GetContents(null, page, pageSize, null, null, null, null);
        var children = await service.GetChildrenByContentId(topicId, null, page, pageSize, null);

        Assert.NotNull(list);
        Assert.NotEmpty(children.Items);
    }
}
