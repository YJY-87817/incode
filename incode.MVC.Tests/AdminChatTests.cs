using System.Net;
using System.Text.RegularExpressions;

namespace incode.MVC.Tests;

/// <summary>
/// 後台聊天（ChatMessages、ChatSessions）與首頁改走服務層：
/// 對話與訊息改為唯讀（原本可偽造 / 竄改對話紀錄、編輯對話會把真人模式重設為 false、
/// 刪除有訊息的對話撞外鍵 500），客服頁開啟時就顯示對話目前的真人 / AI 模式（原本固定顯示 AI）。
/// </summary>
public class AdminChatTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AdminChatTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private async Task<int> AddSession(int userId, bool humanMode, string title = "客服對話")
    {
        await using var context = _factory.Db.NewContext();
        var session = new ChatSession { UserId = userId, Title = title, Ishumanmode = humanMode, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        context.ChatSession.Add(session);
        await context.SaveChangesAsync();
        context.ChatMessage.Add(new ChatMessage { SessionId = session.ChatSessionId, Role = "user", Body = "你好", CreatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        return session.ChatSessionId;
    }

    // 沒有對應的 Action：404（或路由符合但方法不允許的 405）；重點是資料沒有被改動
    private static void AssertGone(HttpStatusCode status) =>
        Assert.True(status is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed, $"實際：{(int)status}");

    [Fact]
    public async Task Admin_pages_render()
    {
        var sessionId = await AddSession(NewUser(), false);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        foreach (var url in new[] { "/", "/ChatMessages", $"/ChatMessages/Index/{sessionId}", "/ChatSessions", $"/ChatSessions/Details/{sessionId}" })
        {
            var response = await admin.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} -> {(int)response.StatusCode}");
        }
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/ChatSessions/Details/999999")).StatusCode);
        // 沒有對話時客服頁也能開
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/ChatMessages/Index/999999")).StatusCode);
    }

    [Fact]
    public async Task Support_page_shows_the_current_mode_of_the_session()
    {
        var humanSession = await AddSession(NewUser(), true);
        var aiSession = await AddSession(NewUser(), false);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var human = await admin.GetStringAsync($"/ChatMessages/Index/{humanSession}");
        Assert.Contains("var isHumanMode = true;", human);
        var ai = await admin.GetStringAsync($"/ChatMessages/Index/{aiSession}");
        Assert.Contains("var isHumanMode = false;", ai);
    }

    [Theory]
    [InlineData("/ChatMessages/Create")]
    [InlineData("/ChatMessages/Edit/1")]
    [InlineData("/ChatMessages/Delete/1")]
    [InlineData("/ChatSessions/Create")]
    [InlineData("/ChatSessions/Edit/1")]
    [InlineData("/ChatSessions/Delete/1")]
    public async Task Chat_records_cannot_be_created_edited_or_deleted_from_the_back_office(string url)
    {
        var sessionId = await AddSession(NewUser(), true);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        var token = Token(await admin.GetStringAsync($"/ChatMessages/Index/{sessionId}")
            + await admin.GetStringAsync("/ChatSessions"));

        AssertGone((await admin.GetAsync(url)).StatusCode);

        var form = new Dictionary<string, string>
        {
            ["ChatSessionId"] = sessionId.ToString(), ["SessionId"] = sessionId.ToString(), ["ChatMessageId"] = "1",
            ["Role"] = "user", ["Body"] = "偽造", ["Title"] = "改掉", ["UserId"] = "1", ["__RequestVerificationToken"] = token,
        };
        var post = await admin.PostAsync(url, new FormUrlEncodedContent(form));
        AssertGone(post.StatusCode);

        await using var context = _factory.Db.NewContext();
        var session = await context.ChatSession.AsNoTracking().SingleAsync(s => s.ChatSessionId == sessionId);
        Assert.True(session.Ishumanmode);
        Assert.Equal("客服對話", session.Title);
        Assert.Equal("你好", (await context.ChatMessage.AsNoTracking().SingleAsync(m => m.SessionId == sessionId)).Body);
    }
}
