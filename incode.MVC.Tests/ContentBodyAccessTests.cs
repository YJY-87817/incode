using incode.MVC.Services.Contents;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace incode.MVC.Tests;

/// <summary>
/// 第三次全專案檢查 49：論壇內文檔（/contents/*.html）只在有公開中的文章指向它時提供。
/// 作者刪除、後台下架的文章，以及編輯後留下的舊版本，直接開網址要 404（後台管理員仍可開啟）。
/// </summary>
public class ContentBodyAccessTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ContentBodyAccessTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private async Task<int> NewCategory()
    {
        await using var context = _factory.Db.NewContext();
        var category = new ContentCategory
        {
            CategoryName = "c" + Guid.NewGuid().ToString("N")[..8], Description = "d", IsActive = ContentCategoryStatus.Official,
        };
        context.ContentCategory.Add(category);
        await context.SaveChangesAsync();
        return category.ContentCategoryId;
    }

    /// <summary>以發文 API 建立主題，回傳文章 ID 與內文檔網址。</summary>
    private async Task<(int ContentId, string BodyUrl)> CreateTopic(HttpClient author, string html = "<p>secret</p>")
    {
        var response = await author.PostAsJsonAsync("/api/ContentApi",
            new { title = "標題", body = html, type = ContentTypes.Post, categoryId = await NewCategory() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return (json.GetProperty("contentId").GetInt32(), json.GetProperty("body").GetString()!);
    }

    private Task<HttpResponseMessage> Anonymous(string url) => _factory.CreateAnonymousClient().GetAsync(url);

    private async Task<string> CurrentBody(int contentId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.Content.Where(c => c.ContentId == contentId).Select(c => c.Body).SingleAsync();
    }

    [Fact]
    public async Task Body_of_a_public_post_is_served()
    {
        var (_, url) = await CreateTopic(await _factory.LoginAsync(NewUser()));

        var response = await Anonymous(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<p>secret</p>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Body_of_a_post_deleted_by_its_author_is_not_served()
    {
        var author = await _factory.LoginAsync(NewUser());
        var (contentId, url) = await CreateTopic(author);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous(url)).StatusCode); // 先讀一次（結果會被快取）

        Assert.Equal(HttpStatusCode.OK, (await author.DeleteAsync($"/api/ContentApi/{contentId}")).StatusCode);

        // 修正前：刪除後仍是 200
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous(url)).StatusCode);
    }

    [Fact]
    public async Task Body_of_a_post_taken_down_by_admin_is_not_served_until_restored()
    {
        var (contentId, url) = await CreateTopic(await _factory.LoginAsync(NewUser()));
        var adminId = NewUser();
        Assert.Equal(HttpStatusCode.OK, (await Anonymous(url)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IContentReportService>().RemoveAsync(contentId, adminId, "違規");

        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous(url)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IContentReportService>().RestoreAsync(contentId, adminId, null);

        Assert.Equal(HttpStatusCode.OK, (await Anonymous(url)).StatusCode);
    }

    [Fact]
    public async Task Old_version_after_edit_is_not_served_but_new_version_is()
    {
        var author = await _factory.LoginAsync(NewUser());
        var (contentId, oldUrl) = await CreateTopic(author, "<p>old</p>");
        Assert.Equal(HttpStatusCode.OK, (await Anonymous(oldUrl)).StatusCode);

        var edit = await author.PutAsJsonAsync($"/api/ContentApi/{contentId}", new { title = "標題", body = "<p>new</p>" });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var newUrl = await CurrentBody(contentId);

        Assert.NotEqual(oldUrl, newUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous(oldUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous(newUrl)).StatusCode);
    }

    [Fact]
    public async Task Body_file_without_any_post_is_not_served()
    {
        var path = Path.Combine(_factory.UploadRoot, "contents", $"content-{Guid.NewGuid():N}.html");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "<p>orphan</p>");

        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous("/contents/" + Path.GetFileName(path))).StatusCode);
    }

    [Fact]
    public async Task Admin_can_still_open_body_of_a_deleted_post()
    {
        var author = await _factory.LoginAsync(NewUser());
        var (contentId, url) = await CreateTopic(author);
        Assert.Equal(HttpStatusCode.OK, (await author.DeleteAsync($"/api/ContentApi/{contentId}")).StatusCode);

        var admin = await _factory.LoginAsync(NewUser(), admin: true); // 後台 Cookie

        var response = await admin.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<p>secret</p>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Other_member_cannot_open_body_of_a_deleted_post()
    {
        var author = await _factory.LoginAsync(NewUser());
        var (contentId, url) = await CreateTopic(author);
        Assert.Equal(HttpStatusCode.OK, (await author.DeleteAsync($"/api/ContentApi/{contentId}")).StatusCode);

        var member = await _factory.LoginAsync(NewUser());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync(url)).StatusCode);
    }
}
