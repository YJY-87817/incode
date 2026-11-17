using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Models
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

    // 產生的 OnModelCreating 之後執行：補上 Database-First 原本沒有的設定（重新產生模型時不會被覆蓋）
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
      // 同一會員對同一內容的同一種互動只能有一筆（連點兩下原本會寫入兩筆，按讚數虛增）。
      // 已上線的資料庫執行 docs/database/migrations/2026-11-17-content-interaction-unique.sql 建立
      modelBuilder.Entity<ContentInteraction>()
        .HasIndex(e => new { e.ContentId, e.UserId, e.CategoryId }, ContentInteractionUniqueIndex)
        .IsUnique();

      // 會員的 Email 驗證與登入戳記（Models/Partials/User.cs）
      modelBuilder.Entity<User>(entity =>
      {
        entity.Property(e => e.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        entity.Property(e => e.EmailVerifyToken).HasColumnName("email_verify_token").HasMaxLength(100);
        entity.Property(e => e.EmailVerifyTokenExpiry).HasColumnName("email_verify_token_expiry").HasColumnType("datetime");
        entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(64);
      });
    }

    public const string ContentInteractionUniqueIndex = "UX_content_interaction_content_user_category";
  }
}

