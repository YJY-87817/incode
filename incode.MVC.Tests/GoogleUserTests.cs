using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Tests;

/// <summary>第二次全專案檢查 35：Google 登入建立帳號時 username（唯一索引）重複。</summary>
public class GoogleUserTests
{
    private static UserService Create(incodedatabaseContext context)
        => new(context, new MemoryCache(new MemoryCacheOptions()));

    private static async Task<User> CreateGoogleUser(TestDb db, string email, string? name)
    {
        await using var context = db.NewContext();
        if (!await context.Role.AnyAsync())
        {
            // Google 會員的 RoleId = 2（與 docs/database/seed.sql 相同）
            context.Role.AddRange(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Member" });
            await context.SaveChangesAsync();
        }
        return await Create(context).CreateGoogleUserAsync(email, name);
    }

    [Fact]
    public async Task Same_google_name_gets_a_unique_username()
    {
        using var db = new TestDb();

        var first = await CreateGoogleUser(db, "a@example.com", "王小明");
        var second = await CreateGoogleUser(db, "b@example.com", "王小明");
        var third = await CreateGoogleUser(db, "c@example.com", "王小明");

        Assert.Equal("王小明", first.Username);
        Assert.Equal("王小明_2", second.Username);
        Assert.Equal("王小明_3", third.Username);
    }

    [Fact]
    public async Task Users_without_google_name_can_all_register()
    {
        using var db = new TestDb();

        var first = await CreateGoogleUser(db, "a@example.com", null);
        var second = await CreateGoogleUser(db, "b@example.com", "  ");

        Assert.Equal("Google User", first.Username);
        Assert.Equal("Google User_2", second.Username);
    }

    [Fact]
    public async Task Name_already_used_by_password_member_gets_suffix()
    {
        using var db = new TestDb();
        db.SeedUser("taken@example.com"); // SeedUser 的 username = email

        var user = await CreateGoogleUser(db, "g@example.com", "taken@example.com");

        Assert.Equal("taken@example.com_2", user.Username);
    }

    [Fact]
    public async Task Long_name_with_suffix_stays_within_column_length()
    {
        using var db = new TestDb();
        var longName = new string('名', 200);

        var first = await CreateGoogleUser(db, "a@example.com", longName);
        var second = await CreateGoogleUser(db, "b@example.com", longName);

        Assert.Equal(150, first.Username.Length);
        Assert.Equal(150, second.Username.Length);
        Assert.EndsWith("_2", second.Username);
    }
}
