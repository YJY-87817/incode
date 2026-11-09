using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

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
    // 允許來源由設定提供：App:FrontendUrl + App:CorsOrigins (見 appsettings.json)
    policy.WithOrigins(builder.Configuration.GetCorsOrigins())
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

// 3. Cookie
// owen以下
const string MemberScheme = "MemberAuth";
// API 與 SignalR 未登入 / 無權限時回 401 / 403，不轉址到登入頁
static bool IsApiOrHub(PathString path) => path.StartsWithSegments("/api") || path.StartsWithSegments("/chatHub");
const string AdminScheme = "AdminAuth";

// 使用 PolicyScheme 作為預設，根據 cookie 存在情況選擇要用哪個 scheme Authenticate
builder.Services.AddAuthentication(options =>
{
    // 將 DefaultScheme 指向我們的「智慧轉發」方案
    options.DefaultScheme = "SmartScheme";
    options.DefaultChallengeScheme = "SmartScheme";
})
// Policy scheme 會檢查 request cookie，若有 Incode.Admin 則使用 AdminAuth，否則使用 MemberAuth
.AddPolicyScheme("SmartScheme", "Smart auth scheme", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // 先檢查是否有管理員 cookie
        if (!string.IsNullOrEmpty(context.Request.Cookies["Incode.Admin"]))
        {
            return AdminScheme;
        }
        // 再檢查前台會員 cookie
        if (!string.IsNullOrEmpty(context.Request.Cookies["Incode.Member"]))
        {
            return MemberScheme;
        }
        // 預設回傳 MemberScheme（未登入時也使用此 scheme 作為挑戰）
        return MemberScheme;
    };
})
    .AddCookie(MemberScheme, options =>
    {
        options.Cookie.Name = "Incode.Member"; // 給予不同的 Cookie 名稱
        options.LoginPath = "/Auth/Login";     // 前台登入路徑 (如果是純 API 可設為 null 或回傳 401)
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);        
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        // SameSite=None：前後端可能部署在不同網域，API 請求才帶得到 Cookie。
        // 其他網站也因此能帶著 Cookie 送出請求，CSRF 由下方 UseApiOriginCheck 檢查 Origin 擋下
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;

      // 帳號被停用 / 刪除時，已登入的人下一個請求就登出（見 MemberCookieValidator）
      options.Events.OnValidatePrincipal = MemberCookieValidator.ValidateAsync;

      options.Events.OnRedirectToLogin = context =>
      {
        // 如果是 API 請求，回傳 401 狀態碼，不要跳轉 HTML 頁面
        if (IsApiOrHub(context.Request.Path))
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
        if (IsApiOrHub(context.Request.Path))
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
    options.LoginPath = "/Account/Login"; // 後台專用的登入頁面
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(10); // 後台通常時效較短，較安全

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
builder.Services.AddControllersWithViews(); // 後台 MVC + 前台 API Controller
//owen
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginAttemptLimiter>(); // 同一帳號登入失敗次數限制
builder.Services.AddAuthRateLimiting();               // 登入 / 註冊 / 忘記密碼的 IP 速率限制
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStatusService, StatusService>();
//tim
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPointRechargeService, PointRechargeService>();
builder.Services.AddScoped<IContentInteractionService, ContentInteractionService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentEntrustMissionService, ContentEntrustMissionService>();
//yjy
builder.Services.AddSingleton<UploadStorage>(); // 上傳檔案存放處（App:UploadRoot，網站目錄外）
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
//qiu
builder.Services.AddScoped<IPointLogService, PointLogService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IMissionProgressService, MissionProgressService>();
builder.Services.AddScoped<IEntrustMissionService, EntrustMissionService>();
builder.Services.AddScoped<IPointReserveService, PointReserveService>();
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
// CORS 必須在 UseRouting 之後 (Controller 上的 [EnableCors] 需要)，並放在靜態檔之前：
// 前端除了呼叫 API，也會以 fetch 讀取 /contents/*.html、/img/... 等靜態檔，共用同一份允許來源設定
app.UseCors("VueApp");
// CSRF：API 的寫入請求與 SignalR 只接受自己與 CORS 允許來源的 Origin（見 Extensions/OriginCheckExtensions.cs）
app.UseApiOriginCheck(builder.Configuration.GetCorsOrigins());
app.UseStaticFiles();
// 上傳檔案（App:UploadRoot）掛在網站根目錄：/img/chat/xxx、/contents/xxx.html 等網址不變。
// 改版前的上傳檔仍在 wwwroot，由上一行提供；找不到才會到這裡
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = app.Services.GetRequiredService<UploadStorage>().CreateFileProvider()
});

app.UseAuthentication(); // Cookie
app.UseAuthorization();  // Role
app.UseRateLimiter();    // 在 UseCors 之後，429 回應也帶 CORS 標頭，前端才讀得到訊息

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

//SignalR的路由 設定聊天室的連線端點
app.MapHub<ChatHub>("/chatHub");

app.Run();
