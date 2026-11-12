using System.Text.Json;
using incode.MVC.Services.Chat;

namespace incode.MVC.Tests;

/// <summary>第二次全專案檢查 32：AI 客服的查詢工具原本回傳寫死的假資料、解析工具呼叫時沒有檢查格式。</summary>
public class ChatToolServiceTests
{
    private static ChatToolService Create(incodedatabaseContext context)
        => new(context, new PointLogService(context));

    // ---------- 解析工具呼叫 ----------

    [Theory]
    [InlineData("CALL_FUNCTION:GetProductInfo|電影票", ChatTool.GetProductInfo, "電影票")]
    [InlineData("好的，我幫你查詢。\nCALL_FUNCTION:SearchContents| 寄生上流 \n謝謝", ChatTool.SearchContents, "寄生上流")]
    [InlineData("CALL_FUNCTION:PointLogs|{{剩餘}}", ChatTool.PointLogs, "剩餘")]
    [InlineData("CALL_FUNCTION:PointLogs", ChatTool.PointLogs, "")] // 沒有「|」：原本 Split('|')[1] 丟例外 → 500
    public void ParseToolCall_reads_tool_and_keyword(string response, ChatTool tool, string keyword)
    {
        var call = ChatToolService.ParseToolCall(response);

        Assert.NotNull(call);
        Assert.Equal(tool, call.Value.Tool);
        Assert.Equal(keyword, call.Value.Keyword);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("這部電影很好看。")]
    [InlineData("CALL_FUNCTION:DeleteEverything|x")]
    [InlineData("CALL_FUNCTION:|x")]
    [InlineData("CALL_FUNCTION:0|x")]
    public void ParseToolCall_returns_null_for_normal_or_unknown_replies(string? response)
        => Assert.Null(ChatToolService.ParseToolCall(response));

    // ---------- 點數 ----------

    [Fact]
    public async Task Point_summary_is_the_members_own_balance()
    {
        using var db = new TestDb();
        var member = db.SeedUser("member@example.com");
        var other = db.SeedUser("other@example.com");
        await GivePoints(db, member, 320);
        await GivePoints(db, member, -20);
        await GivePoints(db, other, 9999);

        await using var context = db.NewContext();
        using var json = JsonDocument.Parse(await Create(context).GetPointSummaryAsync(member));

        Assert.Equal(300, json.RootElement.GetProperty("currentBalance").GetInt32()); // 原本一律 1500
        var logs = json.RootElement.GetProperty("recentLogs").EnumerateArray().ToList();
        Assert.Equal(2, logs.Count);
        Assert.Equal(-20, logs[0].GetProperty("changeAmount").GetInt32());
        Assert.Equal(300, logs[0].GetProperty("balanceAfter").GetInt32());
    }

    [Fact]
    public async Task Point_summary_for_member_without_logs_is_zero()
    {
        using var db = new TestDb();
        var member = db.SeedUser();

        await using var context = db.NewContext();
        using var json = JsonDocument.Parse(await Create(context).GetPointSummaryAsync(member));

        Assert.Equal(0, json.RootElement.GetProperty("currentBalance").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("recentLogs").EnumerateArray());
    }

    // ---------- 商品 ----------

    [Fact]
    public async Task Products_only_on_sale_and_matching_keyword()
    {
        using var db = new TestDb();
        await AddProduct(db, "威秀電影票", point: 300);
        await AddProduct(db, "下架的電影票", point: 100, onSale: false);
        await AddProduct(db, "馬克杯", point: 50);

        await using var context = db.NewContext();
        using var json = JsonDocument.Parse(await Create(context).SearchProductsAsync("電影票"));

        var product = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("威秀電影票", product.GetProperty("name").GetString());
        Assert.Equal(300, product.GetProperty("point").GetInt32());
        Assert.False(product.TryGetProperty("stock", out _)); // 不提供庫存數量
    }

    [Fact]
    public async Task Products_fall_back_to_single_words_and_report_no_match()
    {
        using var db = new TestDb();
        await AddProduct(db, "電影兌換券", point: 2000);

        await using var context = db.NewContext();
        var service = Create(context);

        using var json = JsonDocument.Parse(await service.SearchProductsAsync("想要 電影兌換券"));
        Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("查無相關商品資料。", await service.SearchProductsAsync("遊戲機"));
        Assert.Equal("查無相關商品資料。", await service.SearchProductsAsync(""));
    }

    // ---------- 文章 ----------

    [Fact]
    public async Task Contents_only_public_topics_matching_title()
    {
        using var db = new TestDb();
        var author = db.SeedUser("author@example.com");
        var topic = await AddContent(db, author, "寄生上流影評");
        await AddContent(db, author, "寄生上流（已刪除）", isActive: 2);
        await AddContent(db, author, "回覆：寄生上流", parentId: topic);
        await AddContent(db, author, "其他電影");

        await using var context = db.NewContext();
        using var json = JsonDocument.Parse(await Create(context).SearchContentsAsync("寄生上流"));

        var content = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(topic, content.GetProperty("contentId").GetInt32());
        Assert.Equal("author@example.com", content.GetProperty("username").GetString());
        Assert.Equal("查無相關文章。", await Create(context).SearchContentsAsync("不存在的片名"));
    }

    // ---------- 輔助 ----------

    private static async Task GivePoints(TestDb db, int userId, int amount)
    {
        await using var context = db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        Assert.True(await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment));
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task AddProduct(TestDb db, string name, int point, bool onSale = true)
    {
        await using var context = db.NewContext();
        context.Product.Add(new Product
        {
            Name = name, Description = name, ImageName = "x.jpg", Type = "票券", Series = "s",
            Point = point, Stock = 10, OnSale = onSale, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        });
        await context.SaveChangesAsync();
    }

    private static async Task<int> AddContent(TestDb db, int userId, string title, int isActive = 1, int? parentId = null)
    {
        await using var context = db.NewContext();
        var content = new Content
        {
            UserId = userId, Title = title, Body = "b", Type = 1, IsActive = isActive, ParentContentId = parentId,
            CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }
}
