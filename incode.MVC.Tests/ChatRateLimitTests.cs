using System.Net;
using System.Net.Http.Json;

namespace incode.MVC.Tests;

/// <summary>會呼叫 Gemini 的聊天端點要有速率限制（第二次檢查 34），避免被刷 AI / 生圖 API 費用。</summary>
public class ChatRateLimitTests : IClassFixture<ApiFactory>
{
    private const int Limit = 10; // RateLimitPolicies.AiChat：每位會員每分鐘 10 次

    private readonly ApiFactory _factory;

    public ChatRateLimitTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    // 內容不合法（沒有 SessionId）也沒關係：速率限制在進到 Controller 之前就計算
    private static Task<HttpResponseMessage> Send(HttpClient client)
        => client.PostAsJsonAsync("/api/ChatMessages/SendMessage", new { SessionId = 0, Message = "hi" });

    [Fact]
    public async Task Chat_messages_are_limited_per_member()
    {
        var client = await _factory.LoginAsync(NewUser());

        for (var i = 0; i < Limit; i++)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await Send(client)).StatusCode);

        var blocked = await Send(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        // 另一位會員有自己的額度
        var other = await _factory.LoginAsync(NewUser());
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await Send(other)).StatusCode);
    }

    [Fact]
    public async Task Auto_title_shares_the_same_limit()
    {
        var client = await _factory.LoginAsync(NewUser());

        for (var i = 0; i < Limit; i++)
            await Send(client);

        var response = await client.PostAsync("/api/ChatSessionsApi/AutoTitle/1", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
