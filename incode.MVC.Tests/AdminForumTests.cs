using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace incode.MVC.Tests;

/// <summary>
/// 後台論壇模組改走服務層（Content、ContentEdit、ContentHistory、ContentCategory）：
/// 修改紀錄改為唯讀、分類表單加上狀態（原本編輯會把狀態蓋成 0）、不能形成父分類循環、
/// 有文章 / 子分類的分類不能刪除（原本 500）、沒有分類的文章詳情不再 500。
/// </summary>
public class AdminForumTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AdminForumTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static async Task<HttpResponseMessage> PostForm(HttpClient admin, string pageUrl, string postUrl, params (string Key, string Value)[] fields)
    {
        var token = Token(await admin.GetStringAsync(pageUrl));
        Assert.NotEmpty(token);
        var form = fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Append(new("__RequestVerificationToken", token));
        return await admin.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    private async Task<int> AddCategory(string name, int status = ContentCategoryStatus.Official, int? parentId = null)
    {
        await using var context = _factory.Db.NewContext();
        var category = new ContentCategory { CategoryName = name, Description = "d", IsActive = status, ParentCategoryId = parentId };
        context.ContentCategory.Add(category);
        await context.SaveChangesAsync();
        return category.ContentCategoryId;
    }

    private async Task<int> AddContent(int userId, int? categoryId, int? parentId = null)
    {
        await using var context = _factory.Db.NewContext();
        var content = new Content
        {
            UserId = userId, Type = parentId == null ? ContentTypes.Post : ContentTypes.Reply, ParentContentId = parentId,
            CategoryId = categoryId, Title = "t", Body = "b", IsActive = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    private async Task<ContentCategory?> Category(int id)
    {
        await using var context = _factory.Db.NewContext();
        return await context.ContentCategory.AsNoTracking().FirstOrDefaultAsync(c => c.ContentCategoryId == id);
    }

    private static (string, string)[] CategoryForm(int id, string name, int status, int? parentId = null) =>
        new[] { ("ContentCategoryId", id.ToString()), ("CategoryName", name), ("Description", "說明"), ("IsActive", status.ToString()), ("ParentCategoryId", parentId?.ToString() ?? "") };

    [Fact]
    public async Task Admin_pages_render()
    {
        var userId = NewUser();
        var categoryId = await AddCategory("c" + Guid.NewGuid().ToString("N")[..6]);
        var topic = await AddContent(userId, categoryId);
        var noCategory = await AddContent(userId, null); // 舊資料：沒有分類
        int historyId;
        await using (var context = _factory.Db.NewContext())
        {
            var history = new ContentHistory { ContentId = topic, UserId = userId, OldTitle = "舊標題", OldBody = "舊內文", CreatedAt = DateTime.Now };
            context.ContentHistory.Add(history);
            await context.SaveChangesAsync();
            historyId = history.ContentHistoryId;
        }
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        foreach (var url in new[]
        {
            "/Content", $"/Content/Details/{topic}", $"/Content/Details/{noCategory}", "/ContentEdit",
            "/ContentHistory", $"/ContentHistory/Details/{historyId}",
            "/ContentCategory", "/ContentCategory/Create", $"/ContentCategory/Details/{categoryId}", $"/ContentCategory/Edit/{categoryId}", $"/ContentCategory/Delete/{categoryId}",
        })
        {
            var response = await admin.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} → {(int)response.StatusCode}"); // 沒有分類的文章詳情原本 500
        }
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Content/Details/999999")).StatusCode);

        var json = await admin.GetStringAsync("/ContentCategory/IndexJson"); // 唯讀資料改用 GET
        using var doc = JsonDocument.Parse(json);
        var row = doc.RootElement.EnumerateArray().Single(r => r.GetProperty("contentCategoryId").GetInt32() == categoryId);
        Assert.Equal("官方", row.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Category_list_renders_every_column_as_text_by_default()
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        // 分類名稱可以由會員在前台新增：DataTables 所有欄位預設以純文字輸出，只有「操作」欄自訂 render
        var html = Regex.Replace(await admin.GetStringAsync("/ContentCategory"), @"\s+", "");

        Assert.Contains("consttext=$.fn.dataTable.render.text();", html);
        Assert.Contains("columnDefs:[{targets:\"_all\",render:text}]", html);
        var columns = Regex.Match(html, @"columns:\[(.*?)\],columnDefs").Groups[1].Value;
        Assert.Single(Regex.Matches(columns, "render:")); // 只有操作欄
        Assert.Contains("render:function(id){id=parseInt(id,10);", columns);
    }

    [Theory]
    [InlineData("a", "")]                            // 名稱少於 2 字
    [InlineData("名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名稱名", "")] // 51 字
    [InlineData("正常名稱", "x")]                     // 說明 201 字（下面補足）
    public async Task Category_name_and_description_length_is_checked(string name, string description)
    {
        if (description == "x") description = new string('說', 201);
        var id = await AddCategory("原名" + Guid.NewGuid().ToString("N")[..6]);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, $"/ContentCategory/Edit/{id}", $"/ContentCategory/Edit/{id}",
            ("ContentCategoryId", id.ToString()), ("CategoryName", name), ("Description", description),
            ("IsActive", "2"), ("ParentCategoryId", ""));

        // 原本：Controller 檢查 ModelState（2～50 字）；搬移後漏掉，超過資料庫欄位長度會 500
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(description.Length > 200 ? "說明最多" : "分類名稱需為", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.StartsWith("原名", (await Category(id))!.CategoryName);
    }

    [Fact]
    public async Task Legacy_category_status_is_kept_when_editing()
    {
        var id = await AddCategory("舊分類" + Guid.NewGuid().ToString("N")[..6], status: 0); // 舊後台建立的分類
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var page = WebUtility.HtmlDecode(await admin.GetStringAsync($"/ContentCategory/Edit/{id}"));
        Assert.Contains("<option selected=\"selected\" value=\"0\">未設定（0）</option>", page);

        await PostForm(admin, $"/ContentCategory/Edit/{id}", $"/ContentCategory/Edit/{id}", CategoryForm(id, "改名舊分類", 0));

        var saved = (await Category(id))!;
        Assert.Equal("改名舊分類", saved.CategoryName);
        Assert.Equal(0, saved.IsActive); // 只改名稱，狀態不會被改成「官方」
    }

    [Fact]
    public async Task Content_without_created_at_shows_empty_date()
    {
        int id;
        await using (var context = _factory.Db.NewContext())
        {
            var content = new Content { UserId = NewUser(), Type = 1, Title = "沒有建立時間", Body = "b", IsActive = 1 };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            id = content.ContentId;
        }
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        Assert.DoesNotContain("0001", await admin.GetStringAsync($"/Content/Details/{id}"));
        Assert.DoesNotContain("0001/01/01", await admin.GetStringAsync("/Content"));
    }

    [Theory]
    [InlineData("GET", "/ContentHistory/Create")]
    [InlineData("POST", "/ContentHistory/Create")]
    [InlineData("GET", "/ContentHistory/Edit/1")]
    [InlineData("POST", "/ContentHistory/Edit/1")]
    [InlineData("GET", "/ContentHistory/Delete/1")]
    [InlineData("POST", "/ContentHistory/Delete/1")]
    public async Task Content_history_is_read_only(string method, string url)
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? new FormUrlEncodedContent([]) : null,
        });

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    public async Task Category_status_is_kept_and_can_be_changed()
    {
        var disabled = await AddCategory("停用分類" + Guid.NewGuid().ToString("N")[..6], ContentCategoryStatus.Disabled);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        // 表單送出目前的狀態：原本沒有狀態欄位，存檔後一律變成 0（停用的分類被重新啟用）
        await PostForm(admin, $"/ContentCategory/Edit/{disabled}", $"/ContentCategory/Edit/{disabled}",
            CategoryForm(disabled, "改名", ContentCategoryStatus.Disabled));
        Assert.Equal(ContentCategoryStatus.Disabled, (await Category(disabled))!.IsActive);
        Assert.Equal("改名", (await Category(disabled))!.CategoryName);

        await PostForm(admin, $"/ContentCategory/Edit/{disabled}", $"/ContentCategory/Edit/{disabled}",
            CategoryForm(disabled, "改名", ContentCategoryStatus.Official));
        Assert.Equal(ContentCategoryStatus.Official, (await Category(disabled))!.IsActive);

        var invalid = await PostForm(admin, $"/ContentCategory/Edit/{disabled}", $"/ContentCategory/Edit/{disabled}",
            CategoryForm(disabled, "改名", 0));
        Assert.Contains("狀態", WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync()));
        Assert.Equal(ContentCategoryStatus.Official, (await Category(disabled))!.IsActive);
    }

    [Fact]
    public async Task New_category_defaults_to_official()
    {
        var name = "新分類" + Guid.NewGuid().ToString("N")[..6];
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        Assert.Matches("<option selected=\"selected\" value=\"2\">官方</option>", WebUtility.HtmlDecode(await admin.GetStringAsync("/ContentCategory/Create")));
        var response = await PostForm(admin, "/ContentCategory/Create", "/ContentCategory/Create",
            ("CategoryName", name), ("Description", ""), ("IsActive", "2"), ("ParentCategoryId", ""));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var context = _factory.Db.NewContext();
        Assert.Equal(ContentCategoryStatus.Official, (await context.ContentCategory.SingleAsync(c => c.CategoryName == name)).IsActive);
    }

    [Fact]
    public async Task Category_cannot_become_its_own_ancestor()
    {
        var parent = await AddCategory("父" + Guid.NewGuid().ToString("N")[..6]);
        var child = await AddCategory("子" + Guid.NewGuid().ToString("N")[..6], parentId: parent);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var self = await PostForm(admin, $"/ContentCategory/Edit/{parent}", $"/ContentCategory/Edit/{parent}",
            CategoryForm(parent, "父分類", ContentCategoryStatus.Official, parent));
        var cycle = await PostForm(admin, $"/ContentCategory/Edit/{parent}", $"/ContentCategory/Edit/{parent}",
            CategoryForm(parent, "父分類", ContentCategoryStatus.Official, child));

        Assert.Contains("父分類", WebUtility.HtmlDecode(await self.Content.ReadAsStringAsync()));
        Assert.Contains("父分類", WebUtility.HtmlDecode(await cycle.Content.ReadAsStringAsync()));
        Assert.Null((await Category(parent))!.ParentCategoryId);
    }

    [Fact]
    public async Task Category_with_contents_or_children_cannot_be_deleted()
    {
        var withContent = await AddCategory("有文章" + Guid.NewGuid().ToString("N")[..6]);
        await AddContent(NewUser(), withContent);
        var withChild = await AddCategory("有子分類" + Guid.NewGuid().ToString("N")[..6]);
        await AddCategory("子" + Guid.NewGuid().ToString("N")[..6], parentId: withChild);
        var empty = await AddCategory("空" + Guid.NewGuid().ToString("N")[..6]);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        foreach (var id in new[] { withContent, withChild, empty })
        {
            var response = await PostForm(admin, $"/ContentCategory/Delete/{id}", $"/ContentCategory/Delete/{id}");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); // 原本撞外鍵 → 500
        }

        Assert.NotNull(await Category(withContent));
        Assert.NotNull(await Category(withChild));
        Assert.Null(await Category(empty));
    }
}
