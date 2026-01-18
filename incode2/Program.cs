using incode2.Models;
using incode2.Service.user;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. 資料庫連線 & Migration 支援
// ==========================================
// 註冊你的資料庫 Context
builder.Services.AddDbContext<incodedatabaseContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("incodedatabase"));
});

// 補上這個服務，才能讓下面的 app.UseMigrationsEndPoint() 正常運作
// 它會捕捉資料庫錯誤並顯示「Apply Migrations」的藍色按鈕頁面
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==========================================
// 2. CORS 設定 (指定 Vue 前端來源)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueApp", policy =>
    {
        policy.WithOrigins(          
          "https://localhost:5173",
          "http://localhost:5173",
          "http://localhost:8080",
          "http://localhost:3000"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials(); // 允許帶 Cookie (前後端分離驗證必備)
    });
});

// ==========================================
// 3. Cookie 驗證 (混合模式：API vs 後台)
// ==========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Cookie 安全設定
        options.Cookie.HttpOnly = true; // 防止 XSS       
        options.Cookie.SameSite = SameSiteMode.None; // 前後端不同網域(Cross-Site)可能需設為 None + Secure
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //瀏覽器規定：只要 SameSite=None，Secure 屬性必須為 true，否則 Cookie 會被拒收！
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

        // 設定後台 MVC 用的導向路徑
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        // 區分 API 還是 網頁

        // 1. 當未登入 (401)
        options.Events.OnRedirectToLogin = context =>
        {
            // 如果請求網址是以 /api 開頭 -> 回傳 401 (給前端 Vue 看，不跳轉)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            // 如果是後台網頁 -> 執行預設行為 (跳轉到 LoginPath)
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        // 2. 當權限不足 (403)
        options.Events.OnRedirectToAccessDenied = context =>
        {
            // 如果請求網址是以 /api 開頭 -> 回傳 403 (給前端 Vue 看，不跳轉)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            // 如果是後台網頁 -> 執行預設行為 (跳轉到 AccessDeniedPath)
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// ==========================================
// 4. 註冊 MVC 服務 (包含 API 與 View)
// ==========================================
builder.Services.AddControllersWithViews();

// ==========================================
// 5. 註冊 Service
// ==========================================
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialService, SocialService>();

var app = builder.Build();

// ==========================================
// 5. HTTP 請求管線 (Middleware)
// ==========================================

// 環境判斷
if (app.Environment.IsDevelopment())
{
    // 當資料庫與 Model 不一致時，顯示 Migration 錯誤頁面
    // 需搭配上面的 AddDatabaseDeveloperPageExceptionFilter 使用
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

//  順序很重要：CORS -> 驗證 -> 授權
app.UseCors("VueApp");
app.UseAuthentication(); // 檢查你是誰 (讀取 Cookie)
app.UseAuthorization();  // 檢查你能做什麼 (讀取 Role)

// 路由設定
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();