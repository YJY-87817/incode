using System.Net;
using System.Text.Json;

namespace incode.MVC.Tests;

/// <summary>分頁參數檢查（第二次檢查 38）：page / pageSize 為 0、負數會 500，極大的 pageSize 會一次撈出全部資料。</summary>
public class PagingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PagingTests(ApiFactory factory) => _factory = factory;

    private async Task<int> SeedTopicWithReplies(int replies)
    {
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        await using var context = _factory.Db.NewContext();
        var topic = new Content { UserId = userId, Title = "主題", Body = "<p>b</p>", Type = 1, IsActive = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        context.Content.Add(topic);
        await context.SaveChangesAsync();
        for (var i = 0; i < replies; i++)
            context.Content.Add(new Content { UserId = userId, ParentContentId = topic.ContentId, Title = "回覆", Body = "<p>r</p>", Type = 2, IsActive = 1, CreatedAt = DateTime.Now.AddMinutes(i), UpdatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        return topic.ContentId;
    }

    public static TheoryData<string> BadPaging => new()
    {
        "page=0&pageSize=10",
        "page=-5&pageSize=10",
        "page=1&pageSize=0",
        "page=1&pageSize=-3",
    };

    [Theory]
    [MemberData(nameof(BadPaging))]
    public async Task Topic_list_with_bad_paging_does_not_fail(string query)
    {
        await SeedTopicWithReplies(0);

        var response = await _factory.CreateAnonymousClient().GetAsync($"/api/ContentApi?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(BadPaging))]
    public async Task Children_with_bad_paging_does_not_fail(string query)
    {
        var topicId = await SeedTopicWithReplies(3);

        var response = await _factory.CreateAnonymousClient().GetAsync($"/api/ContentApi/{topicId}/children?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Huge_page_size_is_capped()
    {
        var topicId = await SeedTopicWithReplies(120);

        var response = await _factory.CreateAnonymousClient().GetAsync($"/api/ContentApi/{topicId}/children?page=1&pageSize=1000000");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100, body.GetProperty("items").GetArrayLength());
        Assert.Equal(120, body.GetProperty("totalCount").GetInt32()); // 總數仍然正確，前端可以逐頁取得
    }
}
