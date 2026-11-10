using Microsoft.Data.SqlClient;

namespace incode.MVC.Tests;

/// <summary>
/// 真正的 SQL Server 測試資料庫（併發測試用，SQLite 沒有 <c>sp_getapplock</c> 與列鎖）。
/// 連線字串從環境變數 <see cref="EnvVar"/> 取得（指向伺服器，不含資料庫名稱）；每個實例建立一個獨立的暫時資料庫，結束時刪除。
/// 例：<c>Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True</c>
/// </summary>
public sealed class SqlServerDb : TestDbBase
{
    public const string EnvVar = "INCODE_TEST_SQLSERVER";

    private readonly string _serverConnectionString;
    private readonly string _databaseName = "incode_test_" + Guid.NewGuid().ToString("N");
    private readonly string _connectionString;

    public SqlServerDb()
    {
        _serverConnectionString = Environment.GetEnvironmentVariable(EnvVar)
            ?? throw new InvalidOperationException($"需要環境變數 {EnvVar}");
        _connectionString = new SqlConnectionStringBuilder(_serverConnectionString) { InitialCatalog = _databaseName }.ConnectionString;

        using var context = NewContext();
        context.Database.EnsureCreated(); // 用真正的模型建立資料表（含 getdate() 預設值與索引）
    }

    public override incodedatabaseContext NewContext()
        => new(new DbContextOptionsBuilder<incodedatabaseContext>().UseSqlServer(_connectionString).Options);

    public override void Dispose()
    {
        SqlConnection.ClearAllPools();
        try
        {
            using var connection = new SqlConnection(_serverConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
            command.ExecuteNonQuery();
        }
        catch
        {
            // 暫時資料庫刪不掉不影響測試結果
        }
    }
}

/// <summary>需要真正的 SQL Server：沒有設定 <see cref="SqlServerDb.EnvVar"/> 時略過（本機與一般 CI job 不會執行）。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerDb.EnvVar)))
            Skip = $"需要 SQL Server：設定環境變數 {SqlServerDb.EnvVar}（見 docs/TESTING.md）";
    }
}

/// <summary>測試類別共用的 SQL Server 資料庫，第一次使用時才建立（全部略過時不會連線）。</summary>
public sealed class SqlServerFixture : IDisposable
{
    private readonly object _sync = new();
    private SqlServerDb? _db;

    public SqlServerDb Db
    {
        get
        {
            lock (_sync) return _db ??= new SqlServerDb();
        }
    }

    public void Dispose() => _db?.Dispose();
}
