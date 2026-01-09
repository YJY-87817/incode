using incode2.Data;
using incode2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

//���UDI�e�����a��
var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
//�������ҥΪ�
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//���n�hDI�e�������󪺤H�ϥ�
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

//claude���ڡA�n��Api�Ϊ�
//�|�v�T�쥻��View��ܡA�����ѱ�
// �]�w JSON �ǦC�ƿﶵ
//builder.Services.AddControllers()
//.AddJsonOptions(options =>
//{
//  //options.JsonSerializerOptions.PropertyNamingPolicy = null; // �O����l�R�W
//  options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // �B�z�`���ѷ�
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

var app = builder.Build();

// Configure the HTTP request pipeline.
//���~�B�z
// ��ܦb�}�o���Ү���ܸԲӪ�error��T�A�p�G�O"�A��"�h�n�˹L�Ӥ~�i�H�ݨ�ԲӸ�T
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
//���ɡC�s��HTTP����--�۰ʭ��ɦ�HTTPS�����A�hs�O���[�K��w
app.UseHttpsRedirection();

// ���s�����i�H�U�� wwwroot �̭����ɮ� (���M Vue ���e���|�]���X��)
app.UseStaticFiles();

//�ҥ�Url Routing(�S�٬�Url Rewriting)�����}���n���S�Ӧh��T
app.UseRouting();

//�ҥ�CORS
app.UseCors("VueApp");

//�������� => �¤��v��
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

//���}�C��route�ܤƳW�h
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    //�p�G���S�񪺹w�]
    //Controller�R�W�̫�@�w�n�OController�����A���঳s
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();
app.Run();