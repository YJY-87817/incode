using incode2.Data;
using incode2.Models;
using incode2.Service.user;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<incodedatabaseContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("incodedatabase"));
});

// ==========================================
// AddIdentity 內部會自動註冊 "Identity.Application" 這個 Cookie Scheme。
// 用AddAuthentication().AddCookie(...)，會發生 "Scheme already exists" 錯誤導致崩潰。
// ==========================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // 密碼設定
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = false; //測試用 之後可以改嚴
    options.Password.RequireUppercase = false; //測試用
    options.Password.RequireNonAlphanumeric = false;//測試用

    // 鎖定設定
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // 使用者設定
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;//測試用
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ==========================================
// 使用此方法來「修改」上面 AddIdentity 自動產生的 Cookie 設定
// ==========================================
builder.Services.ConfigureApplicationCookie(options => {
    // 自訂 Cookie 名稱
    options.Cookie.Name = "MyCookieAuth";

    // 安全性設定
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None; // 允許跨域 (Vue 必備)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 強制 HTTPS
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
    
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

// ==========================================
// 4. CORS 設定
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueApp", policy =>
    {
        policy.WithOrigins(
          "http://localhost:7190",
          "https://localhost:5173",
          "http://localhost:5173",
          "http://localhost:8080",
          "http://localhost:3000"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials(); // 允許帶 Cookie
    });
});

// ==========================================
// 5. 其他服務註冊
// ==========================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".incode2.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 註冊你的自訂 Service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialService, SocialService>();

var app = builder.Build();

// ==========================================
// 6. Middleware Pipeline (請求處理流程)
// ==========================================

if (app.Environment.IsDevelopment())
{
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

app.UseCors("VueApp"); // 必須放在 Auth 之前

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();