using Microsoft.EntityFrameworkCore;

namespace incode2.Models
{
  public partial class incodedatabaseContext : DbContext
  {
    public incodedatabaseContext() { }

    //以下提供新增物件New方法(不在controller, view時)
    //OnConfiguring是DbContext的虛擬方法，用來設定資料庫連線與提供者
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        IConfiguration Config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)//找到專案根目錄，所有的.NET程式都跑在AppDomain
            .AddJsonFile("appsettings.json") //設定檔是JSON格式
            .Build();
        optionsBuilder.UseSqlServer(Config.GetConnectionString("incodedatabase"));
      }
    }
  }
}

