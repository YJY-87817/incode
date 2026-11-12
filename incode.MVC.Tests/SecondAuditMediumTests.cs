using System.Net;
using System.Text.Json;

namespace incode.MVC.Tests;

/// <summary>第二次全專案檢查 29～31 的回歸測試（API 層，整個網站以 WebApplicationFactory 啟動）。</summary>
public class SecondAuditMediumTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SecondAuditMediumTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private async Task EnsureInteractionCategoriesAsync()
    {
        await using var context = _factory.Db.NewContext();
        if (await context.InteractionCategory.AnyAsync()) return;
        // 與 docs/database/seed.sql 相同
        foreach (var (id, name) in new[] { (1, "like"), (2, "hide"), (3, "bookmark"), (4, "report"), (5, "best_answer"), (6, "tip") })
            context.InteractionCategory.Add(new InteractionCategory { InteractionCategoryId = id, Name = name, Description = name, IsActive = 1 });
        await context.SaveChangesAsync();
    }

    private async Task<int> AddContent(int userId, string title)
    {
        await using var context = _factory.Db.NewContext();
        var content = new Content { UserId = userId, Title = title, Body = "<p>b</p>", Type = 1, IsActive = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    private async Task AddInteraction(int userId, int contentId, int categoryId, string description = "")
    {
        await using var context = _factory.Db.NewContext();
        context.ContentInteraction.Add(new ContentInteraction
        {
            UserId = userId, ContentId = contentId, CategoryId = categoryId, Description = description, CreatedAt = DateTime.Now,
        });
        await context.SaveChangesAsync();
    }

    private static List<int> ContentIds(JsonElement items)
        => items.EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();

    // ---------- 30「我的收藏」 ----------

    [Fact]
    public async Task My_bookmarks_returns_bookmarked_contents_not_hidden_ones()
    {
        await EnsureInteractionCategoriesAsync();
        var author = NewUser();
        var member = NewUser();
        var bookmarked = await AddContent(author, "收藏的文章");
        var hidden = await AddContent(author, "隱藏的文章");
        await AddInteraction(member, bookmarked, InteractionCategoryIds.Bookmark);
        await AddInteraction(member, hidden, InteractionCategoryIds.Hide);

        var client = await _factory.LoginAsync(member);
        var response = await client.GetAsync("/api/ContentApi/me/bookmarked-contents");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ids = ContentIds(body.GetProperty("items"));
        Assert.Contains(bookmarked, ids);
        Assert.DoesNotContain(hidden, ids);
    }

    // ---------- 31 互動紀錄不能匿名 / 查別人 ----------

    [Fact]
    public async Task Anonymous_cannot_list_someone_elses_interactions()
    {
        await EnsureInteractionCategoriesAsync();
        var victim = NewUser();
        var contentId = await AddContent(NewUser(), "被檢舉的文章");
        await AddInteraction(victim, contentId, InteractionCategoryIds.Report);

        var anonymous = _factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.GetAsync($"/api/ContentInteractionApi?userId={victim}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.GetAsync($"/api/ContentInteractionApi?interactionCategoryId={InteractionCategoryIds.Report}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anonymous.GetAsync($"/api/ContentInteractionApi?contentId={contentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/ContentInteractionApi/content/{contentId}")).StatusCode);
    }

    [Fact]
    public async Task Members_can_list_only_their_own_interactions()
    {
        await EnsureInteractionCategoriesAsync();
        var me = NewUser();
        var other = NewUser();
        var contentId = await AddContent(NewUser(), "文章");
        await AddInteraction(me, contentId, InteractionCategoryIds.Bookmark);
        var client = await _factory.LoginAsync(me);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/ContentInteractionApi?userId={me}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/ContentInteractionApi?userId={other}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/ContentInteractionApi/content/{contentId}")).StatusCode);
    }

    [Fact]
    public async Task Best_answer_records_stay_public_for_the_forum_page()
    {
        // 前端在詳情頁以 interactionCategoryId = 最佳解答 查詢（訪客也會），最佳解答本來就公開顯示在討論串上
        await EnsureInteractionCategoriesAsync();
        var contentId = await AddContent(NewUser(), "解答");
        await AddInteraction(NewUser(), contentId, 5, "entrust_mission:1");

        var response = await _factory.CreateAnonymousClient().GetAsync("/api/ContentInteractionApi?interactionCategoryId=5&entrustMissionId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admins_can_list_all_interactions_of_a_content()
    {
        await EnsureInteractionCategoriesAsync();
        var contentId = await AddContent(NewUser(), "文章");
        await AddInteraction(NewUser(), contentId, InteractionCategoryIds.Report);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/ContentInteractionApi/content/{contentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/ContentInteractionApi?contentId={contentId}")).StatusCode);
    }
}
