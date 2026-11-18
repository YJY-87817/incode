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

    // 用 ADO.NET 直接送出整段 SQL（與 sqlcmd 相同）。不用 EF 的 ExecuteSqlRaw：它會把註解裡的 { } 當成參數佔位符而丟 FormatException
    private async Task RunScriptAsync(string script)
    {
        await using var context = _db.NewContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = script;
        await command.ExecuteNonQueryAsync();
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

        var script = ReadScript("2026-11-12-tip-log-remove-email.sql");
        await RunScriptAsync(script);
        await RunScriptAsync(script); // 可重複執行

        await using var verify = _db.NewContext();
        var logs = await verify.PointLog.AsNoTracking().Where(l => l.UserId == sender || l.UserId == author).ToListAsync();
        Assert.Equal("給 作者" + author, logs.Single(l => l.SourceType == "point_tip" && l.UserId == sender).DisplaySourceSubTitle);
        Assert.Equal("來自 打賞者" + sender, logs.Single(l => l.SourceType == "point_tip" && l.UserId == author).DisplaySourceSubTitle);
        Assert.Equal("給 " + authorEmail, logs.Single(l => l.SourceType == "orders").DisplaySourceSubTitle);
    }
    [SqlServerFact]
    public async Task Content_interaction_migration_removes_duplicates_and_adds_unique_index()
    {
        // 模擬改版前的資料庫：還沒有唯一索引（測試資料庫由目前的模型建立，先把索引拿掉）
        await RunScriptAsync($"DROP INDEX [{incodedatabaseContext.ContentInteractionUniqueIndex}] ON dbo.content_interaction");

        var author = _db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var member = _db.SeedUser($"{Guid.NewGuid():N}@example.com");
        int contentId;
        await using (var seed = _db.NewContext())
        {
            await InteractionSeed.EnsureCategoriesAsync(seed);
            var content = new Content { UserId = author, Title = "t", Body = "b", Type = 1, IsActive = 1 };
            seed.Content.Add(content);
            await seed.SaveChangesAsync();
            contentId = content.ContentId;

            // 按讚 3 筆（重複）、收藏 1 筆、作者自己的按讚 1 筆
            foreach (var (userId, category) in new[]
            {
                (member, InteractionCategoryIds.Like), (member, InteractionCategoryIds.Like), (member, InteractionCategoryIds.Like),
                (member, InteractionCategoryIds.Bookmark), (author, InteractionCategoryIds.Like),
            })
            {
                seed.ContentInteraction.Add(new ContentInteraction { ContentId = contentId, UserId = userId, CategoryId = category, CreatedAt = DateTime.Now });
                await seed.SaveChangesAsync(); // 一筆一筆存，ID 依序遞增
            }
        }

        int firstLikeId;
        await using (var read = _db.NewContext())
            firstLikeId = await read.ContentInteraction
                .Where(ci => ci.UserId == member && ci.CategoryId == InteractionCategoryIds.Like)
                .MinAsync(ci => ci.ContentInteractionId);

        var script = ReadScript("2026-11-17-content-interaction-unique.sql");
        await RunScriptAsync(script);
        await RunScriptAsync(script); // 可重複執行（索引已存在時略過）

        await using var verify = _db.NewContext();
        var rows = await verify.ContentInteraction.AsNoTracking().Where(ci => ci.ContentId == contentId).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Equal(firstLikeId, rows.Single(r => r.UserId == member && r.CategoryId == InteractionCategoryIds.Like).ContentInteractionId); // 保留最早的
        Assert.Single(rows, r => r.UserId == member && r.CategoryId == InteractionCategoryIds.Bookmark);
        Assert.Single(rows, r => r.UserId == author);

        // 索引已建立：再寫入重複的會失敗
        verify.ContentInteraction.Add(new ContentInteraction { ContentId = contentId, UserId = member, CategoryId = InteractionCategoryIds.Like });
        await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync());
    }
    [SqlServerFact]
    public async Task User_email_verify_migration_adds_columns_and_marks_google_accounts()
    {
        // 模擬改版前的資料庫：還沒有這 4 個欄位（測試資料庫由目前的模型建立，先拿掉；email_verified 有預設值條件約束要先刪）
        await RunScriptAsync("""
            DECLARE @df sysname = (SELECT d.name FROM sys.default_constraints d
                JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
                WHERE d.parent_object_id = OBJECT_ID(N'dbo.[user]') AND c.name = N'email_verified');
            IF @df IS NOT NULL EXEC (N'ALTER TABLE dbo.[user] DROP CONSTRAINT [' + @df + N']');
            ALTER TABLE dbo.[user] DROP COLUMN email_verified, email_verify_token, email_verify_token_expiry, security_stamp;
            """);
        await RunScriptAsync($"""
            INSERT INTO dbo.[user] (email, username, password) VALUES
                (N'google-{Guid.NewGuid():N}@example.com', N'g-{Guid.NewGuid():N}', N'GOOGLE_AUTH'),
                (N'pw-{Guid.NewGuid():N}@example.com', N'p-{Guid.NewGuid():N}', N'$2a$11$hash');
            """);

        var script = ReadScript("2026-11-17-user-email-verify-security-stamp.sql");
        await RunScriptAsync(script);
        await RunScriptAsync(script); // 可重複執行（欄位已存在就略過）

        await using var verify = _db.NewContext();
        var users = await verify.User.AsNoTracking().ToListAsync(); // 模型的 4 個欄位都要查得到
        Assert.All(users.Where(u => u.Password == "GOOGLE_AUTH"), u => Assert.True(u.EmailVerified));
        Assert.All(users.Where(u => u.Password != "GOOGLE_AUTH"), u => Assert.False(u.EmailVerified)); // 既有密碼會員為未驗證
        Assert.All(users, u => Assert.Null(u.SecurityStamp));
    }

    [SqlServerFact]
    public async Task Content_report_review_migration_creates_table()
    {
        // 模擬改版前的資料庫：還沒有這張表（測試資料庫由目前的模型建立，先刪除）
        await RunScriptAsync("DROP TABLE dbo.content_report_review;");

        var script = ReadScript("2026-11-18-content-report-review.sql");
        await RunScriptAsync(script);
        await RunScriptAsync(script); // 可重複執行（資料表已存在就略過）

        // 模型可以讀寫這張表
        var adminId = _db.SeedUser($"{Guid.NewGuid():N}@example.com");
        int contentId;
        await using (var context = _db.NewContext())
        {
            var content = new Content { UserId = adminId, Type = 1, Title = "t", Body = "b", IsActive = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            contentId = content.ContentId;
            context.ContentReportReview.Add(new ContentReportReview
            {
                ContentId = contentId, AdminUserId = adminId, Action = ContentReportActions.Dismissed, Note = "備註", CreatedAt = DateTime.Now,
            });
            await context.SaveChangesAsync();
        }
        await using var verify = _db.NewContext();
        Assert.Equal("備註", (await verify.ContentReportReview.AsNoTracking().SingleAsync(r => r.ContentId == contentId)).Note);
    }
}
