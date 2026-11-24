using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace incode.MVC.Tests;

/// <summary>
/// 第二次全專案檢查 40（帳號）：重設 / 修改密碼後舊登入失效、會員修改密碼、Email 驗證、
/// Google 登入接管未驗證 Email 的密碼帳號。
/// </summary>
public class AccountSecurityTests : IClassFixture<ApiFactory>
{
    private const string Password = "secret123";
    private readonly ApiFactory _factory;

    public AccountSecurityTests(ApiFactory factory) => _factory = factory;

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    /// <summary>建立可以用密碼登入的會員。</summary>
    private async Task<(int UserId, string Email)> NewPasswordUser(bool emailVerified = false)
    {
        var email = NewEmail();
        var userId = _factory.Db.SeedUser(email);
        await UpdateUser(userId, u =>
        {
            u.Password = new PasswordService().Hash(Password);
            u.EmailVerified = emailVerified;
        });
        return (userId, email);
    }

    private async Task UpdateUser(int userId, Action<User> change)
    {
        await using var context = _factory.Db.NewContext();
        var user = await context.User.SingleAsync(u => u.UserId == userId);
        change(user);
        await context.SaveChangesAsync();
    }

    private async Task<User> GetUser(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.User.AsNoTracking().SingleAsync(u => u.UserId == userId);
    }

    /// <summary>以真正的登入 API 登入（Cookie 帶有登入戳記）。</summary>
    private async Task<HttpClient> PasswordLogin(string email, string password = Password)
    {
        var client = _factory.CreateAnonymousClient(ownIp: true);
        var response = await client.PostAsJsonAsync("/api/AuthApi/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static async Task<HttpStatusCode> ProfileStatus(HttpClient client)
        => (await client.GetAsync("/api/User/profile")).StatusCode;

    private string TokenFromLastMail(string email)
    {
        var mail = _factory.Emails.LastTo(email);
        Assert.NotNull(mail);
        return Uri.UnescapeDataString(Regex.Match(mail.Value.Content, "token=([0-9A-Za-z%]+)").Groups[1].Value);
    }

    // ---------- 重設密碼後舊登入失效 ----------

    [Fact]
    public async Task Reset_password_signs_out_every_device()
    {
        var (_, email) = await NewPasswordUser();
        var device = await PasswordLogin(email);
        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(device));

        var anonymous = _factory.CreateAnonymousClient(ownIp: true);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/AuthApi/forgot-password", new { email })).StatusCode);
        var reset = await anonymous.PostAsJsonAsync("/api/AuthApi/reset-password",
            new { email, token = TokenFromLastMail(email), newPassword = "newsecret1" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // 原本重設密碼後，舊的登入 Cookie（7 天、自動延長）仍然有效
        Assert.Equal(HttpStatusCode.Unauthorized, await ProfileStatus(device));
        await PasswordLogin(email, "newsecret1");
    }

    [Fact]
    public async Task Forgot_password_for_google_account_does_not_issue_a_reset_token()
    {
        var email = NewEmail();
        var userId = _factory.Db.SeedUser(email);
        await UpdateUser(userId, u => u.Password = UserService.GoogleAccountPassword);

        var response = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 與一般帳號相同的回應
        Assert.Null((await GetUser(userId)).ResetToken);       // 原本可以藉此替 Google 帳號設定密碼
        Assert.Contains("Google", _factory.Emails.LastTo(email)!.Value.Content);
    }

    // ---------- 修改密碼 ----------

    [Fact]
    public async Task Change_password_keeps_this_device_and_signs_out_others()
    {
        var (_, email) = await NewPasswordUser();
        var thisDevice = await PasswordLogin(email);
        var otherDevice = await PasswordLogin(email);

        var wrong = await thisDevice.PostAsJsonAsync("/api/AuthApi/change-password",
            new { currentPassword = "wrong-password", newPassword = "newsecret1" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(otherDevice)); // 失敗時不影響其他裝置

        var ok = await thisDevice.PostAsJsonAsync("/api/AuthApi/change-password",
            new { currentPassword = Password, newPassword = "newsecret1" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(thisDevice));
        Assert.Equal(HttpStatusCode.Unauthorized, await ProfileStatus(otherDevice));
        await PasswordLogin(email, "newsecret1");
        var old = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
    }

    [Fact]
    public async Task Change_password_validates_new_password_and_requires_login()
    {
        var (_, email) = await NewPasswordUser();
        var client = await PasswordLogin(email);

        var tooShort = await client.PostAsJsonAsync("/api/AuthApi/change-password", new { currentPassword = Password, newPassword = "12345" });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        var anonymous = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/change-password",
            new { currentPassword = Password, newPassword = "newsecret1" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Change_password_is_locked_after_repeated_wrong_passwords()
    {
        var (_, email) = await NewPasswordUser();
        var client = await PasswordLogin(email);

        for (int i = 0; i < LoginAttemptLimiter.MaxFailures; i++)
            await client.PostAsJsonAsync("/api/AuthApi/change-password", new { currentPassword = "wrong" + i, newPassword = "newsecret1" });

        var locked = await client.PostAsJsonAsync("/api/AuthApi/change-password", new { currentPassword = Password, newPassword = "newsecret1" });
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
    }

    [Fact]
    public async Task Google_account_cannot_change_password()
    {
        var email = NewEmail();
        var userId = _factory.Db.SeedUser(email);
        await UpdateUser(userId, u => u.Password = UserService.GoogleAccountPassword);
        var client = await _factory.LoginAsync(userId);

        var response = await client.PostAsJsonAsync("/api/AuthApi/change-password", new { currentPassword = "x", newPassword = "newsecret1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Google", await response.Content.ReadAsStringAsync());
        Assert.Equal(UserService.GoogleAccountPassword, (await GetUser(userId)).Password);
    }

    [Fact]
    public async Task Old_cookie_without_stamp_is_rejected_once_the_stamp_changes()
    {
        // 改版前簽發、沒有戳記的 Cookie：戳記從未換過時有效，換過之後失效（會員與後台都一樣）
        var member = _factory.Db.SeedUser(NewEmail());
        var admin = _factory.Db.SeedUser(NewEmail());
        var memberClient = await _factory.LoginAsync(member);
        var adminClient = await _factory.LoginAsync(admin, admin: true);
        await UpdateUser(member, u => u.SecurityStamp = "changed");
        await UpdateUser(admin, u => u.SecurityStamp = "changed");

        Assert.Equal(HttpStatusCode.Unauthorized, await ProfileStatus(memberClient));
        var adminResponse = await adminClient.GetAsync("/api/ChatMessages/GetActiveSupportSessions");
        Assert.Equal(HttpStatusCode.Redirect, adminResponse.StatusCode);
        Assert.Contains("/Account/Login", adminResponse.Headers.Location?.ToString());
    }

    // ---------- Email 驗證 ----------

    [Fact]
    public async Task Register_sends_verification_mail_and_link_verifies_email()
    {
        var email = NewEmail();
        var client = _factory.CreateAnonymousClient(ownIp: true);
        var register = await client.PostAsJsonAsync("/api/AuthApi/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode); // 不擋登入：註冊後仍自動登入

        using (var profile = JsonDocument.Parse(await client.GetStringAsync("/api/User/profile")))
        {
            Assert.False(profile.RootElement.GetProperty("emailVerified").GetBoolean());
            Assert.False(profile.RootElement.GetProperty("isGoogleAccount").GetBoolean());
        }

        var token = TokenFromLastMail(email);
        var anonymous = _factory.CreateAnonymousClient(ownIp: true);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await anonymous.PostAsJsonAsync("/api/AuthApi/verify-email", new { email, token = "WRONG" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await anonymous.PostAsJsonAsync("/api/AuthApi/verify-email", new { email, token })).StatusCode);

        using var after = JsonDocument.Parse(await client.GetStringAsync("/api/User/profile"));
        Assert.True(after.RootElement.GetProperty("emailVerified").GetBoolean());
    }

    [Fact]
    public async Task Expired_verification_link_is_rejected()
    {
        var (userId, email) = await NewPasswordUser();
        await UpdateUser(userId, u =>
        {
            u.EmailVerifyToken = "ABC";
            u.EmailVerifyTokenExpiry = DateTime.Now.AddMinutes(-1);
        });

        var response = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/verify-email", new { email, token = "ABC" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await GetUser(userId)).EmailVerified);
    }

    [Fact]
    public async Task Resend_verification_mail()
    {
        var (userId, email) = await NewPasswordUser();
        var client = await PasswordLogin(email);

        var response = await client.PostAsync("/api/AuthApi/resend-verification", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull((await GetUser(userId)).EmailVerifyToken);
        Assert.Equal("驗證您的 Email", _factory.Emails.LastTo(email)!.Value.Subject);

        await UpdateUser(userId, u => u.EmailVerified = true);
        var again = await client.PostAsync("/api/AuthApi/resend-verification", null);
        Assert.Contains("已經驗證", await again.Content.ReadAsStringAsync());
    }

    // ---------- Google 登入接管 ----------

    private async Task<(User User, bool TakenOver)> GoogleLogin(string email, string? name = "Google 名稱")
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAuthService>().GetOrCreateGoogleUserAsync(email, name);
    }

    [Fact]
    public async Task Google_login_takes_over_unverified_password_account()
    {
        // 有人先用這個 Email 註冊（沒有驗證），並且在自己的裝置上登入中
        var (userId, email) = await NewPasswordUser(emailVerified: false);
        var squatter = await PasswordLogin(email);

        var (user, takenOver) = await GoogleLogin(email);

        Assert.True(takenOver);
        Assert.Equal(userId, user.UserId);
        var stored = await GetUser(userId);
        Assert.Equal(UserService.GoogleAccountPassword, stored.Password); // 密碼清除，之後只能用 Google 登入
        Assert.True(stored.EmailVerified);
        Assert.Equal(HttpStatusCode.Unauthorized, await ProfileStatus(squatter)); // 先註冊的人被登出
        var login = await _factory.CreateAnonymousClient(ownIp: true).PostAsJsonAsync("/api/AuthApi/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [Fact]
    public async Task Google_login_keeps_verified_password_account()
    {
        var (userId, email) = await NewPasswordUser(emailVerified: true);
        var device = await PasswordLogin(email);

        var (_, takenOver) = await GoogleLogin(email);

        Assert.False(takenOver);
        Assert.NotEqual(UserService.GoogleAccountPassword, (await GetUser(userId)).Password);
        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(device));
        await PasswordLogin(email); // 密碼仍然可以登入
    }

    [Fact]
    public async Task Google_login_creates_verified_account()
    {
        var email = NewEmail();
        await using (var context = _factory.Db.NewContext())
        {
            if (!await context.Role.AnyAsync())
            {
                context.Role.AddRange(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Member" });
                await context.SaveChangesAsync();
            }
        }

        var (user, takenOver) = await GoogleLogin(email);

        Assert.False(takenOver);
        Assert.True((await GetUser(user.UserId)).EmailVerified);
    }

    // ---------- 第三次全專案檢查 46：會員 Cookie 的 Admin 角色 ----------

    private void EvictAccountState(int userId)
        => MemberCookieValidator.Evict(_factory.Services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(), userId);

    [Fact]
    public async Task Admin_logged_in_from_frontend_loses_admin_access_after_demotion()
    {
        var (adminId, email) = await NewPasswordUser();
        await _factory.MakeAdminAsync(adminId);
        var client = await PasswordLogin(email); // 會員 Cookie，帶 Admin 角色
        var victimId = _factory.Db.SeedUser(NewEmail());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/User")).StatusCode);

        await UpdateUser(adminId, u => u.RoleId = (int)RoleType.Member);
        EvictAccountState(adminId);

        // 修正前：降級後仍是 200（可以列出所有會員的 Email、停用其他會員）
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/User/{victimId}/deactivate", null)).StatusCode);
        Assert.True((await GetUser(victimId)).IsActive != false);

        // 被登出：Cookie 已清除，一般會員的 API 也要重新登入
        Assert.Equal(HttpStatusCode.Unauthorized, await ProfileStatus(client));

        // 以一般會員重新登入：可以使用會員功能，但沒有管理員權限
        var relogin = await PasswordLogin(email);
        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(relogin));
        Assert.Equal(HttpStatusCode.Forbidden, (await relogin.GetAsync("/api/User")).StatusCode);
    }

    [Fact]
    public async Task Admin_logged_in_from_frontend_keeps_admin_access_while_still_admin()
    {
        var (adminId, email) = await NewPasswordUser();
        await _factory.MakeAdminAsync(adminId);
        var client = await PasswordLogin(email);
        EvictAccountState(adminId); // 讓下一個請求重新查資料庫

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/User")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(client));
    }

    [Fact]
    public async Task Member_cookie_without_admin_role_is_not_affected_by_role_check()
    {
        var (_, email) = await NewPasswordUser();
        var client = await PasswordLogin(email);

        Assert.Equal(HttpStatusCode.OK, await ProfileStatus(client));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/User")).StatusCode);
    }
}
