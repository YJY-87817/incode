using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using incode.MVC.Dtos.Contents;

namespace incode.MVC.Tests;

/// <summary>
/// 請求大小與欄位長度限制（第二次檢查 33）：全站原本允許 500 MB 的請求，標題、內文、聊天訊息沒有長度限制，
/// 匿名的大頭貼上傳沒有速率限制，可以塞滿磁碟。
/// </summary>
public class RequestLimitTests : IClassFixture<ApiFactory>
{
    private const long MB = 1024 * 1024;

    private readonly ApiFactory _factory;

    public RequestLimitTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    [Fact]
    public async Task Content_title_longer_than_the_column_is_rejected()
    {
        var client = await _factory.LoginAsync(NewUser());

        var response = await client.PostAsJsonAsync("/api/ContentApi",
            new { Title = new string('標', 201), Body = "<p>內文</p>", Type = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Content_body_has_a_maximum_length()
    {
        var client = await _factory.LoginAsync(NewUser());

        var response = await client.PostAsJsonAsync("/api/ContentApi",
            new { Title = "標題", Body = new string('a', ContentLimits.MaxBodyLength + 1), Type = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Updating_content_checks_the_same_lengths()
    {
        var client = await _factory.LoginAsync(NewUser());

        var response = await client.PutAsJsonAsync("/api/ContentApi/1",
            new { Title = new string('標', 201), Body = "<p>內文</p>" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_message_longer_than_the_limit_is_rejected()
    {
        var client = await _factory.LoginAsync(NewUser());

        var response = await client.PostAsJsonAsync("/api/ChatMessages/SendMessage",
            new { SessionId = 0, UserContent = new string('a', 2001), ConnectionId = "conn" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_image_caption_longer_than_the_limit_is_rejected()
    {
        var client = await _factory.LoginAsync(NewUser());
        using var form = new MultipartFormDataContent
        {
            { new StringContent("0"), "SessionId" },
            { new StringContent("conn"), "ConnectionId" },
            { new StringContent(new string('a', 2001)), "UserContent" },
            { new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }), "Image", "a.png" },
        };

        var response = await client.PostAsync("/api/ChatMessages/UploadImage", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_avatar_upload_is_rate_limited()
    {
        // 匿名請求依 IP 計算（TestServer 沒有 IP，全部算同一個來源）
        var client = _factory.CreateAnonymousClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            using var form = new MultipartFormDataContent(); // 沒有檔案也沒關係：速率限制在進到 Controller 之前計算
            last = await client.PostAsync("/api/User/images", form);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public void Default_request_body_limits_are_small()
    {
        using var scope = _factory.Services.CreateScope();
        var kestrel = scope.ServiceProvider.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
        var form = scope.ServiceProvider.GetRequiredService<IOptions<FormOptions>>().Value;

        Assert.True(kestrel.Limits.MaxRequestBodySize <= 30 * MB, $"Kestrel MaxRequestBodySize = {kestrel.Limits.MaxRequestBodySize}");
        Assert.True(form.MultipartBodyLengthLimit <= 30 * MB, $"MultipartBodyLengthLimit = {form.MultipartBodyLengthLimit}");
    }

    [Fact]
    public void Forum_image_upload_limit_is_bounded()
    {
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.TrimStart('/') == "api/images/upload")
            .ToList();

        var endpoint = Assert.Single(endpoints);
        var sizeLimit = endpoint.Metadata.GetMetadata<IRequestSizeLimitMetadata>();
        Assert.NotNull(sizeLimit);
        Assert.True(sizeLimit!.MaxRequestBodySize <= 100 * MB, $"RequestSizeLimit = {sizeLimit.MaxRequestBodySize}");
    }
}
