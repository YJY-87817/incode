using incode2.Hubs;
using incode2.Models;
using incode2.Services;
using incode2.Services.user;
using incode2.Services.Notification;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using incode2.Services.Chat;
using incode2.Services.Users;
using incode2.Services.Contents;
using incode2.Services.Point;
using Microsoft.AspNetCore.Http.Features;
using incode2.Services.PointShop;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<incodedatabaseContext>(options =>
{
  options.UseSqlServer(builder.Configuration.GetConnectionString("incodedatabase"));
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. CORS
builder.Services.AddCors(options =>
{
  options.AddPolicy("VueApp", policy =>
  {
    policy.WithOrigins(
        "https://localhost:5173",
        "https://localhost:5174",
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:8080",
        "http://localhost:3000",
        "http://localhost:7190",
        "http://localhost:3001",
        "https://hexabasic-yair-causally.ngrok-free.dev",
        "https://groomishly-undeft-ettie.ngrok-free.dev"
      )
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials(); // 允許帶 Cookie (前後端分離驗證必備)
  });
});

// 2.5 Configure request size limits for file uploads
// tim 預設不能上傳限制為約28.6 MB，改設定成 500 MB
// Program.cs設定後，在ImageApiController.cs內的上傳api要用[屬性類別]來解除預設限制
builder.Services.Configure<FormOptions>(options =>
{
  options.MultipartBodyLengthLimit = 524288000; // 500 MB (允許上傳多張圖片)
  options.ValueLengthLimit = int.MaxValue;
  options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure Kestrel server limits
builder.WebHost.ConfigureKestrel(serverOptions =>
{
  serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 MB
});

// Gemini服務
builder.Services.AddHttpClient<GeminiService>();
// SignalR服務
builder.Services.AddSignalR();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 3. Cookie
// owen以下
const string MemberScheme = "MemberAuth";
const string AdminScheme = "AdminAuth";
builder.Services.AddAuthentication(options =>
{
  options.DefaultScheme = MemberScheme;
  options.DefaultChallengeScheme = MemberScheme;
})
    .AddCookie(MemberScheme, options =>
    {
      options.Cookie.Name = "Incode.Member"; // 給予不同的 Cookie 名稱
      options.LoginPath = "/Auth/Login";     // 前台登入路徑 (如果是純 API 可設為 null 或回傳 401)
      options.AccessDeniedPath = "/Auth/AccessDenied";
      options.ExpireTimeSpan = TimeSpan.FromDays(7);
      options.Cookie.Path = "/";// 關鍵：這張 Cookie 只在根目錄有效 (通常全站)
                                // 安全性設定
      options.Cookie.HttpOnly = true;
      options.Cookie.SameSite = SameSiteMode.None; // 配合跨域/第三方登入
      options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      options.SlidingExpiration = true;

      options.Events.OnRedirectToLogin = context =>
      {
        // 如果是 API 請求，回傳 401 狀態碼，不要跳轉 HTML 頁面
        if (context.Request.Path.StartsWithSegments("/api"))
        {
          context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          return Task.CompletedTask;
        }
        // 如果是普通網頁請求，走預設跳轉
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
      };

      options.Events.OnRedirectToAccessDenied = context =>
      {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
      };
    })

// 2. 後台管理員的 Cookie 設定
.AddCookie(AdminScheme, options =>
{
  options.Cookie.Name = "Incode.Admin";  // 給予不同的 Cookie 名稱
  options.LoginPath = "/Admin/Account/Login"; // 後台專用的登入頁面
  options.AccessDeniedPath = "/Admin/Account/AccessDenied";
  options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // 後台通常時效較短，較安全

  // 關鍵安全性設定：這張 Cookie 只在 /Admin 路徑下才會被瀏覽器送出！
  // 這樣前台 Vue 怎麼樣都拿不到這張 Cookie
  options.Cookie.Path = "/";

  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
  options.Cookie.SameSite = SameSiteMode.Strict;
})
    .AddGoogle(googleOptions =>
    {
      googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
      googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
      googleOptions.SaveTokens = true; // 讓 Google 登入後，將 Token 暫存，方便我們在 Controller 取出 Email
    });
builder.Services.AddAuthorization();
// owen以上
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddRazorPages();
//owen
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStatusService, StatusService>();
//tim
builder.Services.AddScoped<IContentInteractionService, ContentInteractionService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentEntrustMissionService, ContentEntrustMissionService>();
//yjy
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
//qiu
builder.Services.AddScoped<IPointLogService, PointLogService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IMissionProgressService, MissionProgressService>();
builder.Services.AddScoped<IEntrustMissionService, EntrustMissionService>();
builder.Services.AddScoped<IEntrustMissionProgressService, EntrustMissionProgressService>();
builder.Services.AddScoped<EcpayService>();

var app = builder.Build();

// 5. HTTP

if (app.Environment.IsDevelopment())
{
  //  Model Migration
  // AddDatabaseDeveloperPageExceptionFilte
  app.UseMigrationsEndPoint();
}
else
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("VueApp"); // 必須在 UseStaticFiles 之前
app.MapControllers();
//tim以下
// 在 app.UseStaticFiles(); 之前添加
app.UseStaticFiles(new StaticFileOptions
{
  OnPrepareResponse = ctx =>
  {
    var origin = ctx.Context.Request.Headers["Origin"].ToString();
    var allowed = new[]
    {
      "https://localhost:5173",
      "https://localhost:5174",
      "http://localhost:5173",
      "http://localhost:5174",
      "http://localhost:8080",
      "http://localhost:3000",
      "http://localhost:7190",
      "http://localhost:3001"
    };

    var ext = Path.GetExtension(ctx.File.Name);
    var isAllowed = allowed.Contains(origin);
    var isStatic = ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".html";

    if (isAllowed && isStatic)
    {
      ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = origin;
      ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
      ctx.Context.Response.Headers["Vary"] = "Origin";
    }
  }
});
//tim以上
app.UseStaticFiles();
app.UseRouting();
app.UseCors("VueApp");
app.UseAuthentication(); //  Cookie)
app.UseAuthorization();  //  Role)
// https://localhost:xxxx/img/
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//lin
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

//SignalR的路由 設定聊天室的連線端點
app.MapHub<ChatHub>("/chatHub");

app.Run();