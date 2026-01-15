using incode2.Data;
using incode2.Models;
using incode2.Service.user;
using incode2.Service.user.@interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

//註冊DI容器的地方
var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
//身份驗證用的
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//給要去DI容器取物件的人使用
builder.Services.AddDbContext<incodedatabaseContext>(option =>
{
  option.UseSqlServer(builder.Configuration.GetConnectionString("incodedatabase"));
});

//claude���ڡA�n��Api�Ϊ�
// �]�w CORS - ���\ Vue �e�ݦs��
builder.Services.AddCors(options =>
{
  options.AddPolicy("VueApp", policy =>
  {
    policy.WithOrigins(
      "http://localhost:7190",
      "http://localhost:5173",  // Vite �w�]�}�o��
      "http://localhost:8080",  // Vue CLI �w�]�}�o��
      "http://localhost:3000"   // ��L�i�઺�}�o��
    )
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
  });
});

builder.Services.AddDistributedMemoryCache(); // 使用記憶體暫存 (正式環境建議用 Redis)

builder.Services.AddSession(options =>
{
  options.Cookie.Name = ".incode2.Session"; //暫存資料 像購物車
  options.IdleTimeout = TimeSpan.FromMinutes(60);
  options.Cookie.IsEssential = true;
  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.Name = ".ReviewPlatform.Auth"; // 身分驗證的 Cookie 名稱
        options.ExpireTimeSpan = TimeSpan.FromDays(1); // 登入狀態可以維持 1 天
        //options.SlidingExpiration = true;  如果有活動，自動延長過期時間
        options.Cookie.HttpOnly = true;   // 前端 JS 讀不到，安全

        // 如果未登入被攔截，回應 401 而不是跳轉頁面 (因為是前後端分離)
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        // 如果權限不足 (例如 Member 想進 Admin)，回應 403
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// 在這裡註冊你的 Service
builder.Services.AddScoped<IUserService, AuthService>();
builder.Services.AddScoped<ISocialService, SocialService>();

//新增密碼驗證規則
builder.Services.Configure<IdentityOptions>(options => {
  options.Password.RequireDigit = true;
  options.Password.RequireLowercase = true;
  options.Password.RequireNonAlphanumeric = true;
  options.Password.RequireUppercase = true;
  options.Password.RequiredLength = 8;
  options.Password.RequiredUniqueChars = 1;

  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
  options.Lockout.MaxFailedAccessAttempts = 3;
  options.Lockout.AllowedForNewUsers = true;  //帳號創立後要去email收驗證信

  options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
  options.User.RequireUniqueEmail = true;
  options.SignIn.RequireConfirmedEmail = true;
});
builder.Services.ConfigureApplicationCookie(options => {
  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
  options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
  options.LoginPath = "/Identity/Account/Login";
  options.AccessDeniedPath = "/Identity/Account/AccessDenied";
  options.SlidingExpiration = true;
});
//新增密碼驗證規則(上面)

var app = builder.Build();

// Configure the HTTP request pipeline.
//錯誤處理
// 選擇在開發環境時顯示詳細的error資訊，如果是"服務"則要倒過來才可以看到詳細資訊
if (app.Environment.IsDevelopment())
{
  app.UseMigrationsEndPoint();
}
else
{
  app.UseExceptionHandler("/Home/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

// HTTP request pipeline configuration
//重導。瀏覽HTTP網頁--自動重導至HTTPS網頁，多s是有加密協定
app.UseHttpsRedirection();

//啟用Url Routing(又稱為Url Rewriting)讓網址不要暴露太多資訊
app.UseStaticFiles();

//�ҥ�Url Routing(�S�٬�Url Rewriting)�����}���n���S�Ӧh��T
app.UseRouting();

app.UseCors("VueApp"); //CORS

app.UseAuthentication();//先驗證身分
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

//網址列的route變化規則
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    //如果都沒填的預設
    //Controller命名最後一定要是Controller結尾，不能有s
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();
app.Run();