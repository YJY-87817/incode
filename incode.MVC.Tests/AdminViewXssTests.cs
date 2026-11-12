using System.Net;

namespace incode.MVC.Tests;

/// <summary>後台頁面不能讓會員可控的內容（暱稱等）跳出 &lt;script&gt;（第二次檢查 27）。</summary>
public class AdminViewXssTests : IClassFixture<ApiFactory>
{
    private const string Payload = "</script><script>window.__xss=1</script>";

    private readonly ApiFactory _factory;

    public AdminViewXssTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_home_page_does_not_let_a_nickname_break_out_of_the_script_tag()
    {
        // 暱稱含 </script> 的會員發了文，會進到首頁的「發文數排行榜」
        var attackerId = _factory.Db.SeedUser("attacker@example.com");
        await using (var context = _factory.Db.NewContext())
        {
            var user = await context.User.SingleAsync(u => u.UserId == attackerId);
            user.Username = Payload;
            context.Content.Add(new Content { UserId = attackerId, Title = "t", Body = "b", Type = 1, IsActive = 1 });
            await context.SaveChangesAsync();
        }

        var admin = await _factory.LoginAsync(_factory.Db.SeedUser("admin@example.com"), admin: true);
        var response = await admin.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(Payload, html);
        Assert.DoesNotContain("<script>window.__xss", html);
        // 暱稱仍然要出現在圖表資料裡（只是被跳脫）
        Assert.Contains("window.__xss=1", html);
    }
}
