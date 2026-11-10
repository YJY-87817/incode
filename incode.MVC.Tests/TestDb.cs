using Microsoft.Data.Sqlite;

namespace incode.MVC.Tests;

/// <summary>
/// 測試用資料庫：SQLite 記憶體資料庫（連線保持開啟，連線關閉資料就消失）。
/// 模型裡的 <c>HasDefaultValueSql("(getdate())")</c> 是 SQL Server 專用，SQLite 沒有 getdate()，所以在測試中移除。
/// SQLite 不會執行 sp_getapplock（<c>PointLogService.LockUsersAsync</c> 會略過），所以併發問題不在這裡驗證。
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    /// <summary>每次呼叫回傳新的 context（共用同一個資料庫），可模擬不同請求。</summary>
    public incodedatabaseContext NewContext()
    {
        var options = new DbContextOptionsBuilder<incodedatabaseContext>()
            .UseSqlite(_connection)
            .Options;
        return new SqliteTestContext(options);
    }

    /// <summary>建立會員與點數類型（點數流水帳需要）。</summary>
    public int SeedUser(string email = "user@example.com")
    {
        using var context = NewContext();

        if (!context.PointTypeDefinition.Any())
        {
            foreach (var type in Enum.GetValues<PointType>())
            {
                context.PointTypeDefinition.Add(new PointTypeDefinition
                {
                    PointTypeDefinitionId = (int)type,
                    TypeName = type.ToString(),
                    DisplayName = type.ToString(),
                    Category = "test",
                    IsActive = true,
                });
            }
        }

        var user = new User { Email = email, Username = email, Password = "x", IsActive = true };
        context.User.Add(user);
        context.SaveChanges();
        return user.UserId;
    }

    public void Dispose() => _connection.Dispose();
}

/// <summary>SQLite 測試用 context：移除模型中 SQL Server 專用的 getdate() 預設值。</summary>
internal sealed class SqliteTestContext : incodedatabaseContext
{
    public SqliteTestContext(DbContextOptions<incodedatabaseContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
        {
            if (property.GetDefaultValueSql() != null)
                property.SetDefaultValueSql(null);
        }
    }
}
