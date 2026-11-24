using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace incode.MVC.Tests;

/// <summary>每位會員每小時的寫入量設成 1 MB，方便測試上限。</summary>
public class SmallUploadQuotaApiFactory : ApiFactory
{
    protected override Dictionary<string, string?> ExtraConfiguration => new()
    {
        ["App:UploadQuotaMegabytesPerHour"] = "1",
    };
}

/// <summary>
/// 第三次全專案檢查 50：已登入會員可以塞滿磁碟。論壇圖片上傳（每次最多 100 MB）、發文 / 編輯（每次寫一個最多 500 萬字元的內文檔）、
/// 聊天圖片都沒有寫入量限制。改為每位會員每小時的寫入量上限（UploadQuota），超過回 429。
/// </summary>
public class UploadQuotaTests : IClassFixture<SmallUploadQuotaApiFactory>
{
    private const int KB = 1024;
    private readonly SmallUploadQuotaApiFactory _factory;

    public UploadQuotaTests(SmallUploadQuotaApiFactory factory) => _factory = factory;

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

    private static MultipartFormDataContent PngForm(int bytes)
    {
        var data = new byte[bytes];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(data, 0);
        var file = new ByteArrayContent(data);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new MultipartFormDataContent { { file, "files", "a.png" } };
    }

    private async Task<HttpResponseMessage> Post(HttpClient client, int categoryId, int bodyChars)
        => await client.PostAsJsonAsync("/api/ContentApi",
            new { title = "標題", body = "<p>" + new string('a', bodyChars) + "</p>", type = ContentTypes.Post, categoryId });

    private async Task<int> ContentCount(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.Content.CountAsync(c => c.UserId == userId);
    }

    private static async Task AssertQuotaMessage(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("上傳量", json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Forum_image_uploads_stop_at_the_hourly_quota()
    {
        var client = await _factory.LoginAsync(NewUser());

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/images/upload", PngForm(700 * KB))).StatusCode);

        // 修正前：沒有上限，第二次照樣寫入
        await AssertQuotaMessage(await client.PostAsync("/api/images/upload", PngForm(700 * KB)));
    }

    [Fact]
    public async Task Posts_stop_at_the_hourly_quota_and_nothing_is_written()
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);
        var categoryId = await NewCategory();

        Assert.Equal(HttpStatusCode.Created, (await Post(client, categoryId, 700 * KB)).StatusCode);

        await AssertQuotaMessage(await Post(client, categoryId, 700 * KB));
        Assert.Equal(1, await ContentCount(userId));
    }

    [Fact]
    public async Task Edits_count_toward_the_quota()
    {
        var client = await _factory.LoginAsync(NewUser());
        var created = await Post(client, await NewCategory(), 100);
        var contentId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("contentId").GetInt32();
        var body = "<p>" + new string('b', 700 * KB) + "</p>";

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/ContentApi/{contentId}", new { title = "t", body })).StatusCode);

        await AssertQuotaMessage(await client.PutAsJsonAsync($"/api/ContentApi/{contentId}", new { title = "t", body }));
    }

    [Fact]
    public async Task Images_and_posts_share_one_quota()
    {
        var client = await _factory.LoginAsync(NewUser());

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/images/upload", PngForm(700 * KB))).StatusCode);

        await AssertQuotaMessage(await Post(client, await NewCategory(), 700 * KB));
    }

    [Fact]
    public async Task Chat_images_share_the_same_quota()
    {
        var client = await _factory.LoginAsync(NewUser());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/images/upload", PngForm(700 * KB))).StatusCode);

        var data = new byte[700 * KB];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(data, 0);
        var image = new ByteArrayContent(data);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { image, "Image", "a.png" },
            { new StringContent("conn-1"), "ConnectionId" },
        };

        // 額度已用完：在存檔與呼叫 AI 之前就拒絕
        await AssertQuotaMessage(await client.PostAsync("/api/ChatMessages/UploadImage", form));
    }

    [Fact]
    public async Task Avatar_uploads_by_logged_in_members_share_the_quota()
    {
        var client = await _factory.LoginAsync(NewUser());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/images/upload", PngForm(700 * KB))).StatusCode);

        await AssertQuotaMessage(await client.PostAsync("/api/User/images", PngForm(700 * KB)));
    }

    [Fact]
    public async Task Quota_is_per_member()
    {
        var first = await _factory.LoginAsync(NewUser());
        var second = await _factory.LoginAsync(NewUser());

        Assert.Equal(HttpStatusCode.OK, (await first.PostAsync("/api/images/upload", PngForm(900 * KB))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await second.PostAsync("/api/images/upload", PngForm(900 * KB))).StatusCode);
    }

    [Fact]
    public async Task Small_posts_are_not_affected()
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);
        var categoryId = await NewCategory();

        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Created, (await Post(client, categoryId, 1000)).StatusCode);

        Assert.Equal(5, await ContentCount(userId));
    }
}

/// <summary>發文 / 編輯 / 論壇圖片上傳的請求次數限制（RateLimitPolicies.ContentWrite，預設的 ApiFactory 設定）。</summary>
public class ContentWriteRateLimitTests : IClassFixture<ApiFactory>
{
    private const int Limit = 30; // RateLimitPolicies.ContentWrite：每位會員每分鐘 30 次
    private readonly ApiFactory _factory;

    public ContentWriteRateLimitTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Too_many_posts_in_a_minute_are_rejected_per_member()
    {
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var client = await _factory.LoginAsync(userId);
        int categoryId;
        await using (var context = _factory.Db.NewContext())
        {
            var category = new ContentCategory { CategoryName = "c" + Guid.NewGuid().ToString("N")[..8], Description = "d", IsActive = ContentCategoryStatus.Official };
            context.ContentCategory.Add(category);
            await context.SaveChangesAsync();
            categoryId = category.ContentCategoryId;
        }

        HttpResponseMessage Post() => client.PostAsJsonAsync("/api/ContentApi",
            new { title = "t", body = "<p>b</p>", type = ContentTypes.Post, categoryId }).Result;

        for (int i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.Created, Post().StatusCode);

        // 修正前：沒有次數限制
        Assert.Equal(HttpStatusCode.TooManyRequests, Post().StatusCode);

        // 其他會員不受影響
        var other = await _factory.LoginAsync(_factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com"));
        Assert.Equal(HttpStatusCode.Created, (await other.PostAsJsonAsync("/api/ContentApi",
            new { title = "t", body = "<p>b</p>", type = ContentTypes.Post, categoryId })).StatusCode);
    }
}
