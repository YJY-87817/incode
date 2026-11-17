using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace incode.MVC.Tests;

/// <summary>
/// 啟動整個網站做 API 測試：資料庫換成 SQLite 記憶體資料庫，並提供 <c>/__test/login</c> 讓測試直接以真正的
/// Cookie 驗證方案（MemberAuth / AdminAuth）登入，避開密碼雜湊與登入限流，其餘驗證 / 授權 / 中介層都是正式程式。
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public TestDb Db { get; } = new();

    // 整個測試程序共用一個 UploadRoot：設定是透過環境變數（整個程序共用）傳入，若每個 ApiFactory 各用一個資料夾，
    // 平行執行的測試類別會互相覆蓋，網站可能讀到另一個 factory 的資料夾（CI 上 UploadSecurityHeadersTests 因此 404）
    private static readonly string SharedUploadRoot = CreateSharedUploadRoot();

    private static string CreateSharedUploadRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "incode-test-" + Guid.NewGuid().ToString("N"));
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); } catch { /* 暫存資料夾，清不掉也無妨 */ }
        };
        return root;
    }

    private readonly string _uploadRoot = SharedUploadRoot;

    /// <summary>測試用的 App:UploadRoot（會員上傳檔案的存放處，所有 ApiFactory 共用）。</summary>
    public string UploadRoot => _uploadRoot;

    public ApiFactory()
    {
        // Program.cs 在 Build 之前就讀取設定，所以用環境變數（ConfigureAppConfiguration 來不及）
        Environment.SetEnvironmentVariable("Authentication__Google__ClientSecret", "test-secret"); // Google 方案缺少密鑰時，每個請求都會丟例外
        Environment.SetEnvironmentVariable("App__UploadRoot", _uploadRoot);
    }

    /// <summary>子類別可以覆寫設定（例如綠界商店），會加在網站設定的最後面。</summary>
    protected virtual Dictionary<string, string?> ExtraConfiguration => new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(ExtraConfiguration));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<incodedatabaseContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<incodedatabaseContext>));
            services.RemoveAll<incodedatabaseContext>();
            services.AddScoped<incodedatabaseContext>(_ => Db.NewContext());

            services.AddSingleton<IStartupFilter, TestLoginStartupFilter>();

            // 不實際寄信（SMTP），記錄下來讓測試取出驗證 / 重設連結
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

            // 背景的過期委託處理會在測試進行中改動資料，API 測試不執行（服務本身由 EntrustMissionServiceTests 測試）
            foreach (var worker in services.Where(s => s.ImplementationType == typeof(EntrustMissionExpiryWorker)).ToList())
                services.Remove(worker);
        });
    }

    /// <summary>網站寄出的信（取代 SMTP 的 EmailService）。</summary>
    public RecordingEmailService Emails { get; } = new();

    /// <summary>建立一個已登入的用戶端（每個用戶端有自己的 Cookie）。</summary>
    public async Task<HttpClient> LoginAsync(int userId, bool admin = false)
    {
        // 後台 Cookie 每個請求都會確認帳號仍是管理員（MemberCookieValidator.ValidateAdminAsync），資料庫要一致
        if (admin) await MakeAdminAsync(userId);

        var client = CreateAnonymousClient();
        var response = await client.GetAsync($"/__test/login?userId={userId}&admin={(admin ? 1 : 0)}");
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>把會員設為管理員（RoleId = Admin），角色資料不存在時一併建立（與 docs/database/seed.sql 相同）。</summary>
    public async Task MakeAdminAsync(int userId)
    {
        await using var context = Db.NewContext();
        if (!await context.Role.AnyAsync())
        {
            context.Role.AddRange(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Member" });
            await context.SaveChangesAsync();
        }
        var user = await context.User.SingleAsync(u => u.UserId == userId);
        user.RoleId = (int)RoleType.Admin;
        await context.SaveChangesAsync();
    }

    public const string TestClientIpHeader = "X-Test-Client-Ip";

    /// <param name="ownIp">true：這個用戶端使用自己的來源 IP（依 IP 的速率限制不與其他用戶端共用額度）</param>
    public HttpClient CreateAnonymousClient(bool ownIp = false)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), // 登入 Cookie 是 Secure
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        if (ownIp)
        {
            var bytes = Guid.NewGuid().ToByteArray();
            client.DefaultRequestHeaders.Add(TestClientIpHeader, $"10.{bytes[0]}.{bytes[1]}.{bytes[2]}");
        }
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        Db.Dispose();
    }

    private sealed class TestLoginStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            // 測試用的來源 IP（TestServer 沒有 RemoteIpAddress，依 IP 的速率限制會讓同一個測試類別的所有請求共用額度）
            app.Use(async (context, nextMiddleware) =>
            {
                if (System.Net.IPAddress.TryParse(context.Request.Headers[TestClientIpHeader], out var ip))
                    context.Connection.RemoteIpAddress = ip;
                await nextMiddleware();
            });

            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Path != "/__test/login")
                {
                    await nextMiddleware();
                    return;
                }

                var userId = context.Request.Query["userId"].ToString();
                var admin = context.Request.Query["admin"] == "1";
                var scheme = admin ? "AdminAuth" : "MemberAuth";
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, "test-" + userId),
                    new Claim(ClaimTypes.Role, admin ? "Admin" : "Member"),
                }, scheme);

                await context.SignInAsync(scheme, new ClaimsPrincipal(identity));
            });

            next(app);
        };
    }
}

/// <summary>測試用的寄信服務：只記錄，不寄出。</summary>
public sealed class RecordingEmailService : IEmailService
{
    private readonly List<(string To, string Subject, string Content)> _sent = new();

    public Task SendEmailAsync(string toEmail, string subject, string content)
    {
        lock (_sent) _sent.Add((toEmail, subject, content));
        return Task.CompletedTask;
    }

    /// <summary>寄給該信箱的最後一封信（沒有則為 null）。</summary>
    public (string To, string Subject, string Content)? LastTo(string email)
    {
        lock (_sent) return _sent.LastOrDefault(m => m.To == email) is { To: not null } m ? m : null;
    }
}
