using incode2.Data;
using incode2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//給要去DI容器取物件的人使用
builder.Services.AddDbContext<incodedatabaseContext>(option =>
{
  option.UseSqlServer(builder.Configuration.GetConnectionString("incodedatabase"));
});


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.UseRouting();
//身分驗證 => 授予權限
app.UseAuthorization();

app.MapStaticAssets();
//網址列的route變化規則
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")//Controller命名最後一定要是Controller結尾
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
