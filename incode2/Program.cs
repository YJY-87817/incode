using incode2.Data;
using incode2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

//claude給我，要給Api用的
// 設定 CORS - 允許 Vue 前端存取
builder.Services.AddCors(options =>
{
  options.AddPolicy("VueApp", policy =>
  {
    policy.WithOrigins(
      "http://localhost:7190",
      "http://localhost:5173",  // Vite 預設開發埠
      "http://localhost:8080",  // Vue CLI 預設開發埠
      "http://localhost:3000"   // 其他可能的開發埠
    )
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
  });
});

//claude給我，要給Api用的
//會影響原本的View顯示，先註解掉
// 設定 JSON 序列化選項

//builder.Services.AddControllers()
//.AddJsonOptions(options =>
//{
//  //options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持原始命名
//  options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // 處理循環參照
//});

builder.Services.AddSession(options =>
{
  options.Cookie.Name = ".incode2.Session";
  options.IdleTimeout = TimeSpan.FromMinutes(5);
  options.Cookie.IsEssential = true;
  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

//新增密碼驗證規則(下面)
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

// 讓瀏覽器可以下載 wwwroot 裡面的檔案 (不然 Vue 的畫面會跑不出來)
app.UseStaticFiles();

//啟用Url Routing(又稱為Url Rewriting)讓網址不要暴露太多資訊
app.UseRouting();

//啟用CORS
app.UseCors("VueApp");

//身分驗證 => 授予權限
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