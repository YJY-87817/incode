namespace incode.MVC.Tests;

/// <summary>測試資料庫的共同部分：建立 context 與種入會員 / 點數類型。SQLite（<see cref="TestDb"/>）與真正的 SQL Server（<see cref="SqlServerDb"/>）共用。</summary>
public abstract class TestDbBase : IDisposable
{
    /// <summary>每次呼叫回傳新的 context（共用同一個資料庫），可模擬不同請求。</summary>
    public abstract incodedatabaseContext NewContext();

    public abstract void Dispose();

    /// <summary>建立會員與點數類型（點數流水帳需要）。</summary>
    public int SeedUser(string email = "user@example.com")
    {
        using var context = NewContext();

        if (!context.PointTypeDefinition.Any())
        {
            // 主鍵是識別欄位（SQL Server 不能直接指定值），依列舉值由小到大新增，編號就會是 1、2、3…
            foreach (var type in Enum.GetValues<PointType>().OrderBy(t => (int)t))
            {
                context.PointTypeDefinition.Add(new PointTypeDefinition
                {
                    TypeName = type.ToString(),
                    DisplayName = type.ToString(),
                    Category = "test",
                    IsActive = true,
                });
                context.SaveChanges();
            }

            var ids = context.PointTypeDefinition.OrderBy(t => t.PointTypeDefinitionId).Select(t => t.PointTypeDefinitionId).ToList();
            if (!ids.SequenceEqual(Enum.GetValues<PointType>().Select(t => (int)t).OrderBy(i => i)))
                throw new InvalidOperationException("點數類型編號與 PointType 列舉不一致：" + string.Join(",", ids));
        }

        var user = new User { Email = email, Username = email, Password = "x", IsActive = true };
        context.User.Add(user);
        context.SaveChanges();
        return user.UserId;
    }
}
