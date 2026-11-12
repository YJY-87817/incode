namespace incode.MVC.Tests;

/// <summary>在真正的 SQL Server 上執行 docs/database/migrations 的一次性腳本（沒有設定 INCODE_TEST_SQLSERVER 時略過）。</summary>
[Trait("Category", "Concurrency")] // 與併發測試一起在 CI 的 SQL Server job 執行
public class MigrationScriptTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerDb _db;

    public MigrationScriptTests(SqlServerFixture fixture) => _db = fixture.Db;

    private static string ReadScript(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "incode.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "docs", "database", "migrations", name));
    }

    [SqlServerFact]
    public async Task Tip_log_migration_replaces_emails_with_nicknames()
    {
        var sender = _db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var author = _db.SeedUser($"{Guid.NewGuid():N}@example.com");
        string senderEmail, authorEmail;
        await using (var seed = _db.NewContext())
        {
            var s = await seed.User.SingleAsync(u => u.UserId == sender);
            var a = await seed.User.SingleAsync(u => u.UserId == author);
            s.Username = "打賞者" + sender;
            a.Username = "作者" + author;
            senderEmail = s.Email;
            authorEmail = a.Email;
            // 改版前寫入的格式
            seed.PointLog.AddRange(
                new PointLog { UserId = sender, SourceType = "point_tip", PointTypeDefinitionId = (int)PointType.ExpenseTip_out, ChangeAmount = -1, BalanceAfter = 0, Status = "success", DisplaySource = "打賞", DisplaySourceSubTitle = "給 " + authorEmail, CreatedAt = DateTime.Now },
                new PointLog { UserId = author, SourceType = "point_tip", PointTypeDefinitionId = (int)PointType.IncomeTip_in, ChangeAmount = 1, BalanceAfter = 1, Status = "success", DisplaySource = "收到打賞", DisplaySourceSubTitle = "來自 " + senderEmail, CreatedAt = DateTime.Now },
                // 其他類型的紀錄剛好長得一樣也不能動
                new PointLog { UserId = author, SourceType = "orders", PointTypeDefinitionId = (int)PointType.ExpenseOrders, ChangeAmount = 0, BalanceAfter = 1, Status = "success", DisplaySource = "x", DisplaySourceSubTitle = "給 " + authorEmail, CreatedAt = DateTime.Now });
            await seed.SaveChangesAsync();
        }

        await using (var context = _db.NewContext())
        {
            var script = ReadScript("2026-11-12-tip-log-remove-email.sql");
            await context.Database.ExecuteSqlRawAsync(script);
            await context.Database.ExecuteSqlRawAsync(script); // 可重複執行
        }

        await using var verify = _db.NewContext();
        var logs = await verify.PointLog.AsNoTracking().Where(l => l.UserId == sender || l.UserId == author).ToListAsync();
        Assert.Equal("給 作者" + author, logs.Single(l => l.SourceType == "point_tip" && l.UserId == sender).DisplaySourceSubTitle);
        Assert.Equal("來自 打賞者" + sender, logs.Single(l => l.SourceType == "point_tip" && l.UserId == author).DisplaySourceSubTitle);
        Assert.Equal("給 " + authorEmail, logs.Single(l => l.SourceType == "orders").DisplaySourceSubTitle);
    }
}
