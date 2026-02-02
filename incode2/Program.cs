using incode2.Hubs;
using incode2.Models;
using incode2.Services;
using incode2.Services.user;
using incode2.Services.Notification;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using incode2.Services.Chat;
using incode2.Services.Users;
using incode2.Services.Contents;
using incode2.Services.Point;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<incodedatabaseContext>(options =>
{
  options.UseSqlServer(builder.Configuration.GetConnectionString("incodedatabase"));
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==========================================
// 2. CORS
// ==========================================
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
        "http://localhost:3001"
      )
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials(); // 允許帶 Cookie (前後端分離驗證必備)
  });
});

// ==========================================
// 2.5 Configure request size limits for file uploads
// ==========================================
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

// ==========================================
// 3. Cookie
// ==========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      // Cookie 
      options.Cookie.HttpOnly = true; //      
      options.Cookie.SameSite = SameSiteMode.None; // 
      options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //
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
builder.Services.AddAuthorization();

// ==========================================
// 4.
// ==========================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
//owen
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStatusService, StatusService>();
//tim
builder.Services.AddScoped<IContentInteractionService, ContentInteractionService>();
builder.Services.AddScoped<IContentService, ContentService>();
//yjy
builder.Services.AddScoped<IImageService, ImageService>();
//qiu
builder.Services.AddScoped<IPointLogService, PointLogService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IMissionProgressService, MissionProgressService>();
builder.Services.AddScoped<IEntrustMissionService, EntrustMissionService>();
builder.Services.AddScoped<IEntrustMissionProgressService, EntrustMissionProgressService>();

var app = builder.Build();

// ==========================================
// 5. HTTP
// ==========================================

// 
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

//以下tim的
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
//以上tim的
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
app.MapRazorPages()
   .WithStaticAssets();

//SignalR的路由 設定聊天室的連線端點
app.MapHub<ChatHub>("/chatHub");

app.Run();