using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace incode.MVC.Tests;

/// <summary>
/// 第二次全專案檢查 41：發文時檢查類型、父文章、分類（POST api/ContentApi 與「發文 + 委託」），
/// 以及關鍵字只搜尋標題。
/// </summary>
public class ContentValidationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ContentValidationTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private async Task<int> NewCategory(int isActive = ContentCategoryStatus.Official)
    {
        await using var context = _factory.Db.NewContext();
        var category = new ContentCategory { CategoryName = "c" + Guid.NewGuid().ToString("N")[..8], Description = "d", IsActive = isActive };
        context.ContentCategory.Add(category);
        await context.SaveChangesAsync();
        return category.ContentCategoryId;
    }

    private async Task<int> AddContent(int userId, int type, int? parentId = null, int? categoryId = null, int isActive = 1, string title = "t")
    {
        await using var context = _factory.Db.NewContext();
        var content = new Content
        {
            UserId = userId, Type = type, ParentContentId = parentId, CategoryId = categoryId, Title = title, Body = "b",
            IsActive = isActive, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    private async Task<HttpResponseMessage> Post(HttpClient client, int type, int? parentContentId = null, int? categoryId = null)
        => await client.PostAsJsonAsync("/api/ContentApi", new { title = "標題", body = "<p>內文</p>", type, parentContentId, categoryId });

    private async Task<int> ContentCount(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.Content.CountAsync(c => c.UserId == userId);
    }

    // ---------- 主題 ----------

    [Fact]
    public async Task Topic_in_an_active_category_is_created()
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        var response = await Post(client, ContentTypes.Post, categoryId: await NewCategory());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Topic_without_category_or_with_disabled_or_missing_category_is_rejected()
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Post)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Post, categoryId: 999999)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Post(client, ContentTypes.Post, categoryId: await NewCategory(ContentCategoryStatus.Disabled))).StatusCode);
        Assert.Equal(0, await ContentCount(userId)); // 也沒有留下內文檔以外的資料
    }

    [Fact]
    public async Task Topic_with_parent_is_rejected()
    {
        var userId = NewUser();
        var topic = await AddContent(NewUser(), ContentTypes.Post, categoryId: await NewCategory());
        var client = await _factory.LoginAsync(userId);

        var response = await Post(client, ContentTypes.Post, parentContentId: topic, categoryId: await NewCategory());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public async Task Unknown_type_is_rejected(int type)
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        var response = await Post(client, type, categoryId: await NewCategory());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ContentCount(userId));
    }

    // ---------- 回覆類 ----------

    [Fact]
    public async Task Replies_follow_the_frontend_hierarchy()
    {
        var author = NewUser();
        var categoryId = await NewCategory();
        var topic = await AddContent(author, ContentTypes.Post, categoryId: categoryId);
        var reply = await AddContent(author, ContentTypes.Reply, parentId: topic, categoryId: categoryId);
        var comment = await AddContent(author, ContentTypes.Comment, parentId: reply, categoryId: categoryId);
        var client = await _factory.LoginAsync(NewUser());

        // 允許：回文 → 主題、留言 → 回文、簡易留言 → 主題、留言回覆 → 留言
        Assert.Equal(HttpStatusCode.Created, (await Post(client, ContentTypes.Reply, topic)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, ContentTypes.Comment, reply)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, ContentTypes.Comment, topic)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, ContentTypes.CommentReply, comment)).StatusCode);

        // 不允許的組合
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Reply, reply)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Comment, comment)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.CommentReply, topic)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.CommentReply, reply)).StatusCode);
    }

    [Fact]
    public async Task Reply_without_parent_or_to_missing_parent_is_rejected()
    {
        var client = await _factory.LoginAsync(NewUser());

        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Reply)).StatusCode);
        // 原本：不存在的父文章撞外鍵 → 500
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Reply, 999999)).StatusCode);
    }

    [Fact]
    public async Task Cannot_reply_under_deleted_content_or_deleted_topic()
    {
        var author = NewUser();
        var categoryId = await NewCategory();
        var deletedTopic = await AddContent(author, ContentTypes.Post, categoryId: categoryId, isActive: 2);
        var replyUnderDeletedTopic = await AddContent(author, ContentTypes.Reply, parentId: deletedTopic, categoryId: categoryId);
        var topic = await AddContent(author, ContentTypes.Post, categoryId: categoryId);
        var deletedReply = await AddContent(author, ContentTypes.Reply, parentId: topic, categoryId: categoryId, isActive: 2);
        var client = await _factory.LoginAsync(NewUser());

        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Reply, deletedTopic)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Comment, deletedReply)).StatusCode);
        // 父文章還在、但所屬主題已刪除
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, ContentTypes.Comment, replyUnderDeletedTopic)).StatusCode);
    }

    [Fact]
    public async Task Reply_inherits_category_of_parent()
    {
        var author = NewUser();
        var categoryId = await NewCategory();
        var topic = await AddContent(author, ContentTypes.Post, categoryId: categoryId);
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        // 回覆送來的分類（甚至是不存在的）不採用，以父文章為準
        var response = await Post(client, ContentTypes.Reply, topic, categoryId: 999999);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var context = _factory.Db.NewContext();
        Assert.Equal(categoryId, (await context.Content.SingleAsync(c => c.UserId == userId)).CategoryId);
    }

    [Fact]
    public async Task Post_with_entrust_validates_before_writing_anything()
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        var response = await client.PostAsJsonAsync("/api/ContentApi/with-entrust-mission", new
        {
            title = "沒有分類的委託", body = "<p>b</p>", type = ContentTypes.Post,
            entrustMission = new { title = "委託", detail = "d", rewardPoints = 10, category = "其他" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("分類", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await ContentCount(userId));
    }

    // ---------- 搜尋 ----------

    [Fact]
    public async Task Keyword_search_matches_titles()
    {
        var author = NewUser();
        var categoryId = await NewCategory();
        var marker = Guid.NewGuid().ToString("N")[..10];
        var matched = await AddContent(author, ContentTypes.Post, categoryId: categoryId, title: "標題含 " + marker);
        await AddContent(author, ContentTypes.Post, categoryId: categoryId, title: "其他標題");

        var json = await _factory.CreateAnonymousClient().GetStringAsync($"/api/ContentApi?keyword={marker}&categoryId={categoryId}");

        using var doc = JsonDocument.Parse(json);
        var ids = doc.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();
        Assert.Equal(new[] { matched }, ids);
    }
}
