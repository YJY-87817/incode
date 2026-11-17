using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using incode.MVC.Services.Contents;

namespace incode.MVC.Tests;

/// <summary>第二次全專案檢查 40～44（低優先清單）中不需要改資料表的項目。</summary>
public class LowPriorityFixesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public LowPriorityFixesTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private async Task UpdateUser(int userId, Action<User> change)
    {
        await using var context = _factory.Db.NewContext();
        var user = await context.User.SingleAsync(u => u.UserId == userId);
        change(user);
        await context.SaveChangesAsync();
    }

    private async Task GivePoints(int userId, int amount)
    {
        await using var context = _factory.Db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        Assert.True(await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment));
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ---------- 40 帳號 ----------

    [Fact]
    public void Password_verify_returns_false_for_non_bcrypt_value()
    {
        // 原本 BCrypt 對 "GOOGLE_AUTH" 丟 SaltParseException
        var service = new PasswordService();
        Assert.False(service.Verify("anything", UserService.GoogleAccountPassword));
        Assert.True(service.Verify("secret1", service.Hash("secret1")));
    }

    [Fact]
    public async Task Password_login_to_google_account_is_400_not_500()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var userId = _factory.Db.SeedUser(email);
        await UpdateUser(userId, u => u.Password = UserService.GoogleAccountPassword);

        var response = await _factory.CreateAnonymousClient()
            .PostAsJsonAsync("/api/AuthApi/login", new { email, password = "whatever1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Google", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Deactivated_admin_is_signed_out_of_the_back_office()
    {
        var adminId = NewUser();
        var admin = await _factory.LoginAsync(adminId, admin: true);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/ChatMessages/GetActiveSupportSessions")).StatusCode);

        // 停用（SetActiveAsync 會清除快取，與後台 / API 停用會員相同）
        using (var scope = _factory.Services.CreateScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IUserService>().SetActiveAsync(adminId, false));

        AssertSentToAdminLogin(await admin.GetAsync("/api/ChatMessages/GetActiveSupportSessions"));
    }

    [Fact]
    public async Task Admin_cookie_of_an_account_that_is_no_longer_admin_is_rejected()
    {
        var adminId = NewUser();
        var admin = await _factory.LoginAsync(adminId, admin: true);
        await UpdateUser(adminId, u => u.RoleId = (int)RoleType.Member); // 第一個請求之前改掉（還沒有快取）

        AssertSentToAdminLogin(await admin.GetAsync("/api/ChatMessages/GetActiveSupportSessions"));
    }

    // 後台 Cookie 被拒絕後視為未登入，AdminAuth 的挑戰是導向後台登入頁
    private static void AssertSentToAdminLogin(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public void Google_sign_in_uses_its_own_temporary_cookie()
    {
        // 原本寫入預設方案（SmartScheme），有後台 Cookie 時會轉給 AdminAuth，覆蓋後台登入
        var options = _factory.Services.GetRequiredService<IOptionsMonitor<GoogleOptions>>().Get(GoogleDefaults.AuthenticationScheme);
        Assert.Equal(AuthSchemes.External, options.SignInScheme);
    }

    // ---------- 41 發文 ----------

    [Fact]
    public async Task Category_counts_exclude_deleted_contents()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        int categoryId;
        await using (var context = db.NewContext())
        {
            var category = new ContentCategory { CategoryName = "c", Description = "d", IsActive = 1 };
            context.ContentCategory.Add(category);
            await context.SaveChangesAsync();
            categoryId = category.ContentCategoryId;
            context.Content.AddRange(
                new Content { UserId = userId, CategoryId = categoryId, Title = "公開", Body = "b", Type = 1, IsActive = 1 },
                new Content { UserId = userId, CategoryId = categoryId, Title = "已刪除", Body = "b", Type = 1, IsActive = 2 });
            await context.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        var stats = (await new CategoryService(read).GetCategoriesAsync()).Single(c => c.Id == categoryId);

        Assert.Equal(1, stats.TopicCount);
        Assert.Equal(1, stats.PostCount);
    }

    [Fact]
    public async Task Deleted_content_cannot_be_edited()
    {
        var userId = NewUser();
        int contentId;
        await using (var context = _factory.Db.NewContext())
        {
            var content = new Content { UserId = userId, Title = "已刪除", Body = "/contents/x.html", Type = 1, IsActive = 2 };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            contentId = content.ContentId;
        }

        var client = await _factory.LoginAsync(userId);
        var response = await client.PutAsJsonAsync($"/api/ContentApi/{contentId}", new { title = "改標題", body = "<p>x</p>" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verify = _factory.Db.NewContext();
        Assert.Equal("已刪除", (await verify.Content.AsNoTracking().SingleAsync(c => c.ContentId == contentId)).Title);
    }

    private async Task<int> NewCategory()
    {
        await using var context = _factory.Db.NewContext();
        var category = new ContentCategory { CategoryName = "c" + Guid.NewGuid().ToString("N")[..8], Description = "d", IsActive = 2 };
        context.ContentCategory.Add(category);
        await context.SaveChangesAsync();
        return category.ContentCategoryId;
    }

    private object PostWithEntrust(string marker, string missionTitle, int reward) => new
    {
        title = "發文含委託 " + marker,
        body = $"<p>{marker}</p>",
        type = 1,
        categoryId = NewCategory().GetAwaiter().GetResult(),
        entrustMission = new { title = missionTitle, detail = "<img src=x onerror=alert(1)>", rewardPoints = reward, category = "其他" },
    };

    private string[] ContentFilesContaining(string marker)
    {
        var folder = Path.Combine(_factory.UploadRoot, UploadFolders.ContentBodies);
        return Directory.Exists(folder)
            ? Directory.GetFiles(folder).Where(f => File.ReadAllText(f).Contains(marker)).ToArray()
            : [];
    }

    [Fact]
    public async Task Post_with_entrust_and_insufficient_points_leaves_no_post()
    {
        var userId = NewUser(); // 沒有點數
        var marker = Guid.NewGuid().ToString("N");
        var client = await _factory.LoginAsync(userId);

        var response = await client.PostAsJsonAsync("/api/ContentApi/with-entrust-mission", PostWithEntrust(marker, "委託", 100));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var verify = _factory.Db.NewContext();
        Assert.False(await verify.Content.AnyAsync(c => c.UserId == userId)); // 原本文章已建立、沒有委託
        Assert.False(await verify.EntrustMission.AnyAsync(m => m.UserId == userId));
        Assert.Empty(ContentFilesContaining(marker)); // 內文檔也刪掉
    }

    [Fact]
    public async Task Post_with_entrust_encodes_mission_text_into_the_body()
    {
        var userId = NewUser();
        await GivePoints(userId, 500);
        var marker = Guid.NewGuid().ToString("N");
        var client = await _factory.LoginAsync(userId);

        var response = await client.PostAsJsonAsync("/api/ContentApi/with-entrust-mission",
            PostWithEntrust(marker, "<b>標題</b>", 100));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var html = File.ReadAllText(Assert.Single(ContentFilesContaining(marker)));
        Assert.Contains("委託任務：&lt;b&gt;標題&lt;/b&gt;", html);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html);
        Assert.DoesNotContain("<img", html);

        await using var verify = _factory.Db.NewContext();
        Assert.True(await verify.EntrustMission.AnyAsync(m => m.UserId == userId && m.Status == "open"));
    }

    // ---------- 43 後台 ----------

    private static string AntiforgeryToken(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    [Fact]
    public async Task Admin_logout_requires_antiforgery_token()
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        // 沒有 token（例如其他網站送出的表單）：400，原本直接登出
        var forged = await admin.PostAsync("/Account/Logout", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);

        // 版面上的登出表單帶有 token
        var token = AntiforgeryToken(await admin.GetStringAsync("/"));
        Assert.NotEmpty(token);
        var logout = await admin.PostAsync("/Account/Logout",
            new FormUrlEncodedContent([new("__RequestVerificationToken", token)]));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
    }

    [Fact]
    public async Task Admin_edit_member_with_duplicate_email_shows_error_instead_of_500()
    {
        var takenEmail = $"{Guid.NewGuid():N}@example.com";
        _factory.Db.SeedUser(takenEmail);
        var memberId = NewUser();
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var token = AntiforgeryToken(await admin.GetStringAsync($"/Member/Edit/{memberId}"));
        var response = await admin.PostAsync("/Member/Edit", new FormUrlEncodedContent(
        [
            new("__RequestVerificationToken", token),
            new("UserId", memberId.ToString()),
            new("Username", "新暱稱" + memberId),
            new("Email", takenEmail),
            new("IsActive", "true"),
        ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 原本存檔撞唯一索引 → 500
        Assert.Contains("此 Email 已被其他會員使用", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        await using var verify = _factory.Db.NewContext();
        Assert.NotEqual(takenEmail, (await verify.User.AsNoTracking().SingleAsync(u => u.UserId == memberId)).Email);
    }

    [Fact]
    public async Task Recent_login_logs_for_all_users_keeps_latest_per_user()
    {
        using var db = new TestDb();
        var a = db.SeedUser("a@example.com");
        var b = db.SeedUser("b@example.com");
        var start = new DateTime(2026, 11, 1);
        await using (var context = db.NewContext())
        {
            for (int i = 0; i < 12; i++)
                context.UserLoginlog.Add(new UserLoginlog { UserId = a, IpAddress = $"10.0.0.{i}", Logintime = start.AddHours(i) });
            context.UserLoginlog.Add(new UserLoginlog { UserId = b, IpAddress = "10.1.0.1", Logintime = start });
            await context.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        var logs = await new UserService(read, new MemoryCache(new MemoryCacheOptions())).GetRecentLoginLogsForAllUsersAsync(10);

        Assert.Equal(10, logs[a].Count);
        Assert.Equal("10.0.0.11", logs[a][0].IpAddress); // 最新的在前
        Assert.Equal("10.0.0.2", logs[a][9].IpAddress);
        Assert.Equal("10.1.0.1", Assert.Single(logs[b]).IpAddress);
    }

    // ---------- 44 其他 ----------

    [Fact]
    public async Task Public_stats_hide_point_balance_from_others()
    {
        var owner = NewUser();
        await GivePoints(owner, 321);

        static async Task<JsonElement> Points(HttpClient client, int userId)
        {
            using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/User/{userId}/stats"));
            return json.RootElement.GetProperty("points").Clone();
        }

        Assert.Equal(JsonValueKind.Null, (await Points(_factory.CreateAnonymousClient(), owner)).ValueKind);
        Assert.Equal(JsonValueKind.Null, (await Points(await _factory.LoginAsync(NewUser()), owner)).ValueKind);
        Assert.Equal(321, (await Points(await _factory.LoginAsync(owner), owner)).GetInt32());
    }

    [Theory]
    [InlineData("https://tracker.example.com/pixel.png", HttpStatusCode.BadRequest)]
    [InlineData("/img/user/../../appsettings.json", HttpStatusCode.BadRequest)]
    [InlineData("/img/user/3f2a9c1e-1111-2222-3333-444455556666.png", HttpStatusCode.OK)]
    [InlineData(AvatarUrls.GoogleDefault, HttpStatusCode.OK)]
    public async Task Profile_avatar_must_be_an_uploaded_image(string imageUrl, HttpStatusCode expected)
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        var response = await client.PutAsJsonAsync("/api/User/profile", new { email = "x@example.com", imageUrl });

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Profile_keeps_legacy_avatar_when_sent_back_unchanged()
    {
        var userId = NewUser();
        const string legacy = "https://localhost:7190/img/user/舊的 大頭貼.jpg";
        await UpdateUser(userId, u => u.ImageUrl = legacy);
        var client = await _factory.LoginAsync(userId);

        var response = await client.PutAsJsonAsync("/api/User/profile", new { email = "x@example.com", imageUrl = legacy, phone = "0912" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_external_avatar_is_rejected()
    {
        var response = await _factory.CreateAnonymousClient().PostAsJsonAsync("/api/AuthApi/register", new
        {
            email = $"{Guid.NewGuid():N}@example.com",
            password = "secret123",
            imageUrl = "https://tracker.example.com/pixel.png",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("大頭貼", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Use_item_decrements_and_fails_when_empty()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await using (var context = db.NewContext())
        {
            context.UserInventory.Add(new UserInventory { UserId = userId, ItemId = 7, Quantity = 2 });
            await context.SaveChangesAsync();
        }

        async Task<int> Use()
        {
            await using var context = db.NewContext();
            return await new InventoryService(context).UseItemAsync(userId, 7);
        }

        Assert.Equal(1, await Use());
        Assert.Equal(0, await Use());
        Assert.Equal(-1, await Use());
    }
}
