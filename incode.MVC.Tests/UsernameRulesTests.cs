using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Tests;

/// <summary>
/// 第二次全專案檢查 27 後半：暱稱限制 2～30 字、不可包含 &lt; &gt; 與控制字元（註冊、修改個人資料、Google 登入、後台編輯）。
/// 改版前已存在的暱稱沒有修改時不檢查。
/// </summary>
public class UsernameRulesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UsernameRulesTests(ApiFactory factory)
    {
        _factory = factory;
        // 註冊會寫入 RoleId = 2（與 docs/database/seed.sql 相同），測試資料庫要先有角色
        using var context = _factory.Db.NewContext();
        if (!context.Role.Any())
        {
            context.Role.AddRange(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Member" });
            context.SaveChanges();
        }
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..6];

    [Theory]
    [InlineData("小明", true)]
    [InlineData("Tom Lee", true)]
    [InlineData("a", false)]
    [InlineData(" 小明", false)]
    [InlineData("小明\n", false)]
    [InlineData("a<b", false)]
    [InlineData("</script><script>alert(1)</script>", false)]
    [InlineData("tab\there", false)]
    [InlineData("右至左\u202Eabc", false)]
    public void IsValid(string username, bool expected) => Assert.Equal(expected, UsernameRules.IsValid(username));

    [Fact]
    public void Length_counts_characters_not_utf16_units()
    {
        Assert.True(UsernameRules.IsValid(new string('名', 30)));
        Assert.False(UsernameRules.IsValid(new string('名', 31)));
        Assert.True(UsernameRules.IsValid(string.Concat(Enumerable.Repeat("😀", 30)))); // emoji 是 2 個 char
    }

    [Theory]
    [InlineData("</script><script>x")]
    [InlineData("a")]
    [InlineData("1234567890123456789012345678901")] // 31 字
    public async Task Register_rejects_invalid_username(string username)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var response = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/register",
            new { email, password = "secret123", username });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("暱稱", await response.Content.ReadAsStringAsync());
        await using var context = _factory.Db.NewContext();
        Assert.False(await context.User.AnyAsync(u => u.Email == email));
    }

    [Fact]
    public async Task Register_accepts_valid_username_and_generates_valid_default()
    {
        var name = Unique("小明");
        var client = _factory.CreateAnonymousClient(ownIp: true);
        var ok = await client.PostAsJsonAsync("/api/AuthApi/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "secret123", username = name });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        // 沒填暱稱：由 Email 前綴產生，前綴很長或含特殊字元時仍符合規則
        var longEmail = $"{new string('x', 40)}{Guid.NewGuid():N}@example.com";
        var generated = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/register",
            new { email = longEmail, password = "secret123" });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);

        await using var context = _factory.Db.NewContext();
        Assert.True(await context.User.AnyAsync(u => u.Username == name));
        var user = await context.User.SingleAsync(u => u.Email == longEmail);
        Assert.True(UsernameRules.IsValid(user.Username), user.Username);
    }

    [Fact]
    public async Task Profile_update_rejects_invalid_new_username()
    {
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var client = await _factory.LoginAsync(userId);

        var response = await client.PutAsJsonAsync("/api/User/profile", new { email = "x@example.com", username = "<img src=x onerror=alert(1)>" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("暱稱", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Profile_update_keeps_legacy_username_that_breaks_the_rules()
    {
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var legacy = Unique(new string('舊', 40));
        await using (var context = _factory.Db.NewContext())
        {
            var user = await context.User.SingleAsync(u => u.UserId == userId);
            user.Username = legacy;
            await context.SaveChangesAsync();
        }
        var client = await _factory.LoginAsync(userId);

        // 前端每次都會把目前的暱稱一起送出：沒有改暱稱，只改電話 → 成功
        var response = await client.PutAsJsonAsync("/api/User/profile", new { email = "x@example.com", username = legacy, phone = "0912" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Google_name_is_sanitized()
    {
        using var db = new TestDb();
        await using var context = db.NewContext();
        context.Role.AddRange(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Member" });
        await context.SaveChangesAsync();
        var service = new UserService(context, new MemoryCache(new MemoryCacheOptions()));

        var user = await service.CreateGoogleUserAsync("g@example.com", "<b>王\n小明</b>");
        var fallback = await service.CreateGoogleUserAsync("h@example.com", "<>");

        Assert.Equal("b王小明/b", user.Username);
        Assert.Equal("Google User", fallback.Username);
    }

    [Fact]
    public async Task Admin_edit_rejects_invalid_new_username_but_keeps_legacy_one()
    {
        var memberId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var legacy = Unique(new string('舊', 40));
        string email;
        await using (var context = _factory.Db.NewContext())
        {
            var user = await context.User.SingleAsync(u => u.UserId == memberId);
            user.Username = legacy;
            email = user.Email;
            await context.SaveChangesAsync();
        }
        var admin = await _factory.LoginAsync(_factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com"), admin: true);

        async Task<string> Edit(string username)
        {
            var page = await admin.GetStringAsync($"/Member/Edit/{memberId}");
            var token = System.Text.RegularExpressions.Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
            var response = await admin.PostAsync("/Member/Edit", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserId"] = memberId.ToString(), ["Email"] = email, ["Username"] = username, ["IsActive"] = "true",
                ["__RequestVerificationToken"] = token,
            }));
            await using var context = _factory.Db.NewContext();
            return (await context.User.AsNoTracking().SingleAsync(u => u.UserId == memberId)).Username;
        }

        Assert.Equal(legacy, await Edit("a<script>"));       // 不合法：不儲存
        Assert.Equal(legacy, await Edit(legacy));            // 沒改暱稱：其他欄位照常儲存
        var renamed = Unique("新名");
        Assert.Equal(renamed, await Edit(renamed));
    }
}
