using incode.MVC.Services.Contents;
using incode.MVC.ViewModels.Contents;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text.RegularExpressions;

namespace incode.MVC.Tests;

/// <summary>
/// 第二次全專案檢查 43：後台點數紀錄 / 儲值單改為唯讀（原本可以直接新增、修改、刪除，手填餘額、改成 success 不加點），
/// 手動調整點數走 ChangePointsAsync、儲值人工補單走 CompleteRechargeAsync；檢舉清單的駁回、下架、恢復上架。
/// </summary>
public class AdminModerationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AdminModerationTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    // 從 GET 頁面取防偽 token 後送出表單
    private static async Task<HttpResponseMessage> PostForm(HttpClient admin, string pageUrl, string postUrl, params (string Key, string Value)[] fields)
    {
        var token = Token(await admin.GetStringAsync(pageUrl));
        Assert.NotEmpty(token);
        var form = fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Append(new("__RequestVerificationToken", token));
        return await admin.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    private async Task<List<PointLog>> Logs(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.PointLog.AsNoTracking().Where(l => l.UserId == userId).OrderBy(l => l.PointLogId).ToListAsync();
    }

    private async Task GivePoints(int userId, int amount)
    {
        await using var context = _factory.Db.NewContext();
        context.PointLog.Add(new PointLog
        {
            UserId = userId, SourceType = "system", PointTypeDefinitionId = (int)PointType.SystemAdjustment,
            ChangeAmount = amount, BalanceAfter = amount, Status = "success", CreatedAt = DateTime.Now.AddMinutes(-1),
        });
        await context.SaveChangesAsync();
    }

    // ---------- 點數紀錄 / 儲值單唯讀 ----------

    [Theory]
    [InlineData("GET", "/PointLog/Create")]
    [InlineData("GET", "/PointLog/Edit/1")]
    [InlineData("GET", "/PointLog/Delete/1")]
    [InlineData("POST", "/PointLog/Create")]
    [InlineData("POST", "/PointLog/Edit/1")]
    [InlineData("POST", "/PointLog/Delete/1")]
    [InlineData("GET", "/PointRecharge/Create")]
    [InlineData("GET", "/PointRecharge/Edit/1")]
    [InlineData("GET", "/PointRecharge/Delete/1")]
    [InlineData("POST", "/PointRecharge/Create")]
    [InlineData("POST", "/PointRecharge/Edit/1")]
    [InlineData("POST", "/PointRecharge/Delete/1")]
    public async Task Ledger_crud_pages_are_gone(string method, string url)
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? new FormUrlEncodedContent([]) : null,
        });

        // GET 404；POST 405（沒有任何動作接受）。原本 GET 會顯示可手填餘額 / 狀態的表單
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    public async Task Ledger_pages_still_render()
    {
        var userId = NewUser();
        await GivePoints(userId, 10);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        foreach (var url in new[] { "/PointLog", "/PointLog/Adjust", $"/PointLog/PointLogsDetail?UserId={userId}", "/PointRecharge", "/ContentReport" })
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Admin_adjustment_appends_to_the_ledger()
    {
        var userId = NewUser();
        await GivePoints(userId, 50);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var add = await PostForm(admin, "/PointLog/Adjust", "/PointLog/Adjust",
            ("UserId", userId.ToString()), ("ChangeAmount", "120"), ("Reason", "活動獎勵"));
        var deduct = await PostForm(admin, "/PointLog/Adjust", "/PointLog/Adjust",
            ("UserId", userId.ToString()), ("ChangeAmount", "-70"), ("Reason", "客服更正"));

        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, deduct.StatusCode);
        var logs = await Logs(userId);
        Assert.Equal(new[] { 50, 170, 100 }, logs.Select(l => l.BalanceAfter)); // 餘額由系統接續計算
        Assert.All(logs.Skip(1), l =>
        {
            Assert.Equal((int)PointType.SystemAdjustment, l.PointTypeDefinitionId);
            Assert.Equal("系統調整", l.DisplaySource);
        });
        Assert.Equal("客服更正", logs[2].DisplaySourceSubTitle);
    }

    [Theory]
    [InlineData("-51", "活動", "點數不足")]   // 不能扣到負數
    [InlineData("0", "活動", "調整點數")]
    [InlineData("100001", "活動", "調整點數")]
    [InlineData("10", "", "原因")]
    public async Task Invalid_adjustment_is_rejected(string amount, string reason, string message)
    {
        var userId = NewUser();
        await GivePoints(userId, 50);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, "/PointLog/Adjust", "/PointLog/Adjust",
            ("UserId", userId.ToString()), ("ChangeAmount", amount), ("Reason", reason));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 留在表單頁顯示錯誤
        Assert.Contains(message, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Single(await Logs(userId));
    }

    private async Task<int> AddRecharge(int userId, string status, int points = 500)
    {
        await using var context = _factory.Db.NewContext();
        var recharge = new PointRecharge
        {
            UserId = userId, OrderNo = "T" + Guid.NewGuid().ToString("N")[..18], CashAmount = points / 10,
            PointsGranted = points, PaymentMethod = "ecpay", Status = status, CreatedAt = DateTime.Now,
        };
        context.PointRecharge.Add(recharge);
        await context.SaveChangesAsync();
        return recharge.PointRechargeId;
    }

    private async Task<string?> RechargeStatus(int id)
    {
        await using var context = _factory.Db.NewContext();
        return (await context.PointRecharge.AsNoTracking().SingleAsync(r => r.PointRechargeId == id)).Status;
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("failed")]
    public async Task Manual_completion_grants_points_once(string status)
    {
        var userId = NewUser();
        await GivePoints(userId, 30);
        var rechargeId = await AddRecharge(userId, status);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var first = await PostForm(admin, "/PointRecharge", $"/PointRecharge/Complete/{rechargeId}", ("paymentTradeNo", "2411180000123"));
        var second = await PostForm(admin, "/PointRecharge", $"/PointRecharge/Complete/{rechargeId}", ("paymentTradeNo", "2411180000123"));

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, second.StatusCode);
        Assert.Equal("success", await RechargeStatus(rechargeId));
        var logs = await Logs(userId);
        Assert.Equal(new[] { 30, 530 }, logs.Select(l => l.BalanceAfter)); // 只加一次
        Assert.Equal("point_recharge", logs[1].SourceType);
        Assert.Equal(rechargeId, logs[1].SourceId);
        Assert.Contains("不能重複加點", WebUtility.HtmlDecode(await admin.GetStringAsync("/PointRecharge")));
    }

    [Theory]
    [InlineData("success", "2411180000123")]
    [InlineData("cancelled", "2411180000123")]
    [InlineData("pending", "")] // 交易編號 / 備註必填
    public async Task Manual_completion_is_rejected(string status, string tradeNo)
    {
        var userId = NewUser();
        var rechargeId = await AddRecharge(userId, status);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        await PostForm(admin, "/PointRecharge", $"/PointRecharge/Complete/{rechargeId}", ("paymentTradeNo", tradeNo));

        Assert.Equal(status, await RechargeStatus(rechargeId));
        Assert.Empty(await Logs(userId));
    }

    // ---------- 檢舉清單 ----------

    private async Task<int> AddTopic(int authorId, string title)
    {
        await using var context = _factory.Db.NewContext();
        var category = new ContentCategory { CategoryName = "c" + Guid.NewGuid().ToString("N")[..8], Description = "d", IsActive = ContentCategoryStatus.Official };
        context.ContentCategory.Add(category);
        await context.SaveChangesAsync();
        var content = new Content
        {
            UserId = authorId, Type = ContentTypes.Post, CategoryId = category.ContentCategoryId, Title = title, Body = "b",
            IsActive = ContentStatus.Active, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    private async Task Report(int contentId, int reporterId, DateTime? at = null)
    {
        await using var context = _factory.Db.NewContext();
        if (!await context.InteractionCategory.AnyAsync())
        {
            // 與 docs/database/seed.sql 相同
            foreach (var (id, name) in new[] { (1, "like"), (2, "hide"), (3, "bookmark"), (4, "report"), (5, "best_answer"), (6, "tip") })
                context.InteractionCategory.Add(new InteractionCategory { InteractionCategoryId = id, Name = name, Description = name, IsActive = 1 });
            await context.SaveChangesAsync();
        }
        context.ContentInteraction.Add(new ContentInteraction
        {
            ContentId = contentId, UserId = reporterId, CategoryId = InteractionCategoryIds.Report, CreatedAt = at ?? DateTime.Now,
        });
        await context.SaveChangesAsync();
    }

    private async Task<ContentReportIndexViewModel> Index()
    {
        await using var context = _factory.Db.NewContext();
        return await new ContentReportService(context, NullLogger<ContentReportService>.Instance).GetIndexAsync();
    }

    private async Task<int> Status(int contentId)
    {
        await using var context = _factory.Db.NewContext();
        return (await context.Content.AsNoTracking().SingleAsync(c => c.ContentId == contentId)).IsActive;
    }

    [Fact]
    public async Task Reported_content_is_listed_and_dismiss_clears_it_until_reported_again()
    {
        var title = "被檢舉 " + Guid.NewGuid().ToString("N")[..8];
        var topic = await AddTopic(NewUser(), title);
        await Report(topic, NewUser(), DateTime.Now.AddMinutes(-5));
        await Report(topic, NewUser(), DateTime.Now.AddMinutes(-4));
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var item = Assert.Single((await Index()).Pending, i => i.ContentId == topic);
        Assert.Equal(2, item.PendingReportCount);
        Assert.Contains(title, WebUtility.HtmlDecode(await admin.GetStringAsync("/ContentReport")));

        var dismiss = await PostForm(admin, "/ContentReport", $"/ContentReport/Dismiss/{topic}", ("note", "沒有違規"));
        Assert.Equal(HttpStatusCode.Redirect, dismiss.StatusCode);
        Assert.DoesNotContain((await Index()).Pending, i => i.ContentId == topic);
        Assert.Equal(ContentStatus.Active, await Status(topic));
        var review = Assert.Single((await Index()).RecentReviews, r => r.ContentId == topic);
        Assert.Equal(ContentReportActions.Dismissed, review.Action);
        Assert.Equal("沒有違規", review.Note);

        // 駁回之後的新檢舉：再列入待處理，只算新的那一筆
        await Report(topic, NewUser(), DateTime.Now.AddMinutes(1));
        Assert.Equal(1, Assert.Single((await Index()).Pending, i => i.ContentId == topic).PendingReportCount);
    }

    [Fact]
    public async Task Remove_hides_content_and_restore_brings_it_back()
    {
        var topic = await AddTopic(NewUser(), "要下架的主題");
        await Report(topic, NewUser(), DateTime.Now.AddMinutes(-1));
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        var visitor = _factory.CreateAnonymousClient();
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync($"/api/ContentApi/{topic}")).StatusCode);

        await PostForm(admin, "/ContentReport", $"/ContentReport/Remove/{topic}", ("note", "廣告"));

        Assert.Equal(ContentStatus.RemovedByAdmin, await Status(topic));
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/ContentApi/{topic}")).StatusCode);
        var index = await Index();
        Assert.DoesNotContain(index.Pending, i => i.ContentId == topic);
        Assert.Contains(index.Removed, i => i.ContentId == topic);

        // 已下架：再下架一次失敗，不會多一筆紀錄
        await PostForm(admin, "/ContentReport", $"/ContentReport/Remove/{topic}");
        Assert.Contains("已經下架", WebUtility.HtmlDecode(await admin.GetStringAsync("/ContentReport")));

        await PostForm(admin, "/ContentReport", $"/ContentReport/Restore/{topic}");
        Assert.Equal(ContentStatus.Active, await Status(topic));
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync($"/api/ContentApi/{topic}")).StatusCode);
        Assert.Equal(new[] { ContentReportActions.Restored, ContentReportActions.Removed },
            (await Index()).RecentReviews.Where(r => r.ContentId == topic).Select(r => r.Action));
    }

    [Fact]
    public async Task Content_deleted_by_author_cannot_be_removed_or_restored()
    {
        var topic = await AddTopic(NewUser(), "作者刪除");
        await using (var context = _factory.Db.NewContext())
        {
            await context.Content.Where(c => c.ContentId == topic).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, ContentStatus.DeletedByAuthor));
        }
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        await PostForm(admin, "/ContentReport", $"/ContentReport/Remove/{topic}");
        await PostForm(admin, "/ContentReport", $"/ContentReport/Restore/{topic}");

        Assert.Equal(ContentStatus.DeletedByAuthor, await Status(topic)); // 會員自己刪除的不能被後台「恢復」
        Assert.DoesNotContain((await Index()).RecentReviews, r => r.ContentId == topic);
    }

    [Fact]
    public async Task Reported_comment_reply_shows_its_topic_title()
    {
        var author = NewUser();
        var title = "主題 " + Guid.NewGuid().ToString("N")[..8];
        var topic = await AddTopic(author, title);
        int commentReply;
        await using (var context = _factory.Db.NewContext())
        {
            int parent = topic;
            Content? last = null;
            foreach (var type in new[] { ContentTypes.Reply, ContentTypes.Comment, ContentTypes.CommentReply })
            {
                last = new Content { UserId = author, Type = type, ParentContentId = parent, Title = "", Body = "b", IsActive = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
                context.Content.Add(last);
                await context.SaveChangesAsync();
                parent = last.ContentId;
            }
            commentReply = last!.ContentId;
        }
        await Report(commentReply, NewUser(), DateTime.Now.AddMinutes(-1));

        var item = Assert.Single((await Index()).Pending, i => i.ContentId == commentReply);
        Assert.Equal(title, item.TopicTitle);
        Assert.Equal(topic, item.TopicId);

        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        await PostForm(admin, "/ContentReport", $"/ContentReport/Dismiss/{commentReply}");
        Assert.Equal(title, Assert.Single((await Index()).RecentReviews, r => r.ContentId == commentReply).TopicTitle);
    }

    [Fact]
    public async Task Report_actions_require_admin()
    {
        var topic = await AddTopic(NewUser(), "t");
        var member = await _factory.LoginAsync(NewUser());

        var page = await member.GetAsync("/ContentReport");
        var remove = await member.PostAsync($"/ContentReport/Remove/{topic}", new FormUrlEncodedContent([]));

        Assert.NotEqual(HttpStatusCode.OK, page.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, remove.StatusCode);
        Assert.Equal(ContentStatus.Active, await Status(topic));
    }
}
