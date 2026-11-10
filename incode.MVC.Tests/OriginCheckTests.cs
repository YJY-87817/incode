using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace incode.MVC.Tests;

public class OriginCheckTests
{
    private static readonly string[] Allowed = { "https://front.example.com/" };

    private static async Task<(int Status, bool ReachedNext)> Send(
        string method, string path, string? origin = null, string? referer = null)
    {
        var reached = false;
        var app = new ApplicationBuilder(new TestServiceProvider());
        app.UseApiOriginCheck(Allowed);
        app.Run(_ => { reached = true; return Task.CompletedTask; });
        var pipeline = app.Build();

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.example.com");
        context.Request.Path = path;
        if (origin != null) context.Request.Headers.Origin = origin;
        if (referer != null) context.Request.Headers.Referer = referer;
        context.Response.Body = new MemoryStream();

        await pipeline(context);
        return (context.Response.StatusCode, reached);
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public async Task Post_from_unknown_origin_is_forbidden()
    {
        var (status, reached) = await Send("POST", "/api/Order", origin: "https://evil.example.com");
        Assert.Equal(403, status);
        Assert.False(reached);
    }

    [Theory]
    [InlineData("https://front.example.com")]
    [InlineData("https://api.example.com")]
    public async Task Post_from_allowed_or_same_origin_passes(string origin)
    {
        var (_, reached) = await Send("POST", "/api/Order", origin: origin);
        Assert.True(reached);
    }

    [Fact]
    public async Task Origin_null_is_forbidden()
    {
        var (status, _) = await Send("DELETE", "/api/Order/1", origin: "null");
        Assert.Equal(403, status);
    }

    [Fact]
    public async Task Request_without_origin_or_referer_passes()
    {
        var (_, reached) = await Send("POST", "/api/Order");
        Assert.True(reached);
    }

    [Fact]
    public async Task Referer_is_used_when_origin_is_missing()
    {
        var (status, _) = await Send("POST", "/api/Order", referer: "https://evil.example.com/page?x=1");
        Assert.Equal(403, status);

        var (_, reached) = await Send("POST", "/api/Order", referer: "https://front.example.com/page");
        Assert.True(reached);
    }

    [Fact]
    public async Task Get_requests_to_api_are_not_checked()
    {
        var (_, reached) = await Send("GET", "/api/Order", origin: "https://evil.example.com");
        Assert.True(reached);
    }

    [Fact]
    public async Task Chat_hub_is_checked_even_for_get()
    {
        var (status, _) = await Send("GET", "/chatHub", origin: "https://evil.example.com");
        Assert.Equal(403, status);
    }

    [Fact]
    public async Task Ecpay_callback_is_exempt()
    {
        var (_, reached) = await Send("POST", "/api/PointRechargeApi/ecpay/return", origin: "https://evil.example.com");
        Assert.True(reached);
    }
}
