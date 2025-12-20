using Microsoft.EntityFrameworkCore;

namespace incode2.Models
{
  public partial class incodedatabaseContext : DbContext
  {
    public incodedatabaseContext() { }

    //以下提供新增物件New方法(不在controller, view時)
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        IConfiguration Config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        optionsBuilder.UseSqlServer(Config.GetConnectionString("incodedatabase"));
      }
    }
  }
}

