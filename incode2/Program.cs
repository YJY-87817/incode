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

//加入 CORS 服務設定
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowVueApp", 
    policy =>
    {
        policy.WithOrigins("http://localhost:3000") 
            .AllowAnyHeader()
            .AllowAnyMethod(); 
    });
});


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

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
app.UseCors("AllowVueApp");


//身分驗證 => 授予權限
app.UseAuthorization();

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
