using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace incode.MVC.Tests;

/// <summary>
/// 啟動整個網站做 API 測試：資料庫換成 SQLite 記憶體資料庫，並提供 <c>/__test/login</c> 讓測試直接以真正的
/// Cookie 驗證方案（MemberAuth / AdminAuth）登入，避開密碼雜湊與登入限流，其餘驗證 / 授權 / 中介層都是正式程式。
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public TestDb Db { get; } = new();

    private readonly string _uploadRoot = Path.Combine(Path.GetTempPath(), "incode-test-" + Guid.NewGuid().ToString("N"));

    /// <summary>測試用的 App:UploadRoot（會員上傳檔案的存放處）。</summary>
    public string UploadRoot => _uploadRoot;

    public ApiFactory()
    {
        // Program.cs 在 Build 之前就讀取設定，所以用環境變數（ConfigureAppConfiguration 來不及）
        Environment.SetEnvironmentVariable("Authentication__Google__ClientSecret", "test-secret"); // Google 方案缺少密鑰時，每個請求都會丟例外
        Environment.SetEnvironmentVariable("App__UploadRoot", _uploadRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<incodedatabaseContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<incodedatabaseContext>));
            services.RemoveAll<incodedatabaseContext>();
            services.AddScoped<incodedatabaseContext>(_ => Db.NewContext());

            services.AddSingleton<IStartupFilter, TestLoginStartupFilter>();
        });
    }

    /// <summary>建立一個已登入的用戶端（每個用戶端有自己的 Cookie）。</summary>
    public async Task<HttpClient> LoginAsync(int userId, bool admin = false)
    {
        var client = CreateAnonymousClient();
        var response = await client.GetAsync($"/__test/login?userId={userId}&admin={(admin ? 1 : 0)}");
        response.EnsureSuccessStatusCode();
        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), // 登入 Cookie 是 Secure
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        Db.Dispose();
        try { if (Directory.Exists(_uploadRoot)) Directory.Delete(_uploadRoot, recursive: true); } catch { /* 暫存資料夾，清不掉也無妨 */ }
    }

    private sealed class TestLoginStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
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
