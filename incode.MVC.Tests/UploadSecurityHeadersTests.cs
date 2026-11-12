using System.Net;

namespace incode.MVC.Tests;

/// <summary>
/// 會員上傳的檔案（論壇內文 HTML、圖片）直接開啟時不能在網站的來源下執行腳本（第二次檢查 26）。
/// 內文 HTML 是會員送來的原始內容，前端顯示前會經 DOMPurify，但直接開啟檔案網址時沒有過濾。
/// </summary>
public class UploadSecurityHeadersTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UploadSecurityHeadersTests(ApiFactory factory) => _factory = factory;

    private string WriteUpload(string relativePath, string content)
    {
        var path = Path.Combine(_factory.UploadRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return "/" + relativePath.Replace('\\', '/');
    }

    private static void AssertSandboxed(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var csp), "缺少 Content-Security-Policy");
        Assert.Contains("sandbox", string.Join(";", csp!));
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var nosniff), "缺少 X-Content-Type-Options");
        Assert.Contains("nosniff", nosniff!);
    }

    [Fact]
    public async Task Content_body_html_with_script_is_served_sandboxed()
    {
        var url = WriteUpload("contents/content-evil.html", "<p>hi</p><script>fetch('/api/User')</script>");

        var response = await _factory.CreateAnonymousClient().GetAsync(url);

        AssertSandboxed(response);
        // 內容本身不改（前端仍以 fetch 讀取後經 DOMPurify 顯示）
        Assert.Contains("<p>hi</p>", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("img/fromApi/a.png")]
    [InlineData("img/chat/a.png")]
    [InlineData("img/user/a.png")]
    [InlineData("img/product/a.png")]
    public async Task Uploaded_images_are_served_sandboxed(string relativePath)
    {
        var url = WriteUpload(relativePath, "not really a png");

        AssertSandboxed(await _factory.CreateAnonymousClient().GetAsync(url));
    }

    [Fact]
    public async Task Site_assets_are_not_sandboxed()
    {
        // 網站本身的靜態檔（後台的 JS / CSS 等）不能加 sandbox，否則後台頁面會壞
        var response = await _factory.CreateAnonymousClient().GetAsync("/img/okapi_icon.svg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }
}
