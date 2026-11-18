using System.Net;
using System.Text.RegularExpressions;

namespace incode.MVC.Tests;

/// <summary>
/// 後台點數 / 任務模組改走服務層（PointLog、PointRecharge、Mission、MissionProgress、EntrustMission）：
/// 頁面仍可使用，並修正搬移時發現的問題：任務進度改為唯讀、有進度的任務不能刪除（原本 500）、
/// 任務表單只能設定可編輯的欄位、後台建立委託改走預扣流程（原本不扣點）。
/// </summary>
public class AdminPointMissionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AdminPointMissionTests(ApiFactory factory) => _factory = factory;

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

    private async Task<int> AddMission(string name, int reward = 10)
    {
        await using var context = _factory.Db.NewContext();
        var mission = new Mission
        {
            MissionName = name, MissionDetail = "d", TargetType = "post", TargetCount = 1, RewardPoints = reward,
            Category = "c", Difficulty = "簡單", ResetType = "none", IsActive = true, ParticipantCount = 7,
            CreatedAt = new DateTime(2026, 1, 1),
        };
        context.Mission.Add(mission);
        await context.SaveChangesAsync();
        return mission.MissionId;
    }

    private async Task<int> AddProgress(int userId, int missionId, string status = "rewarded")
    {
        await using var context = _factory.Db.NewContext();
        var progress = new MissionProgress
        {
            UserId = userId, MissionId = missionId, CurrentCount = 1, Status = status,
            CycleStartAt = MissionResetTypes.OneTimeCycleStart, CycleEndAt = MissionResetTypes.OneTimeCycleStart.AddYears(100),
        };
        context.MissionProgress.Add(progress);
        await context.SaveChangesAsync();
        return progress.MissionProgressId;
    }

    private async Task<Mission?> FindMission(int id)
    {
        await using var context = _factory.Db.NewContext();
        return await context.Mission.AsNoTracking().FirstOrDefaultAsync(m => m.MissionId == id);
    }

    private async Task GivePoints(int userId, int amount)
    {
        await using var context = _factory.Db.NewContext();
        context.PointLog.Add(new PointLog
        {
            UserId = userId, SourceType = "system", PointTypeDefinitionId = (int)PointType.SystemAdjustment,
            ChangeAmount = amount, BalanceAfter = amount, Status = "success", CreatedAt = DateTime.Now.AddMinutes(-1),
        });
        await context.SaveChangesAsync();
    }

    private async Task<int> AddTopic(int authorId, string title)
    {
        await using var context = _factory.Db.NewContext();
        var content = new Content
        {
            UserId = authorId, Type = ContentTypes.Post, Title = title, Body = "b",
            IsActive = ContentStatus.Active, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    // ---------- 頁面 ----------

    [Fact]
    public async Task Admin_pages_render()
    {
        var userId = NewUser();
        await GivePoints(userId, 100);
        var missionId = await AddMission("m" + Guid.NewGuid().ToString("N")[..6]);
        var progressId = await AddProgress(userId, missionId);
        int logId, rechargeId, entrustId;
        await using (var context = _factory.Db.NewContext())
        {
            logId = (await context.PointLog.FirstAsync(l => l.UserId == userId)).PointLogId;
            var recharge = new PointRecharge { UserId = userId, OrderNo = "P" + Guid.NewGuid().ToString("N")[..10], CashAmount = 10, PointsGranted = 100, PaymentMethod = "ecpay", Status = "pending", CreatedAt = DateTime.Now };
            context.PointRecharge.Add(recharge);
            await context.SaveChangesAsync();
            rechargeId = recharge.PointRechargeId;
            var topic = new Content { UserId = userId, Type = 1, Title = "t", Body = "b", IsActive = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
            context.Content.Add(topic);
            await context.SaveChangesAsync();
            var entrust = new EntrustMission { UserId = userId, ContentId = topic.ContentId, Title = "e", Detail = "d", RewardPoints = 5, Category = "其他", Status = "closed", IsActive = true, CreatedAt = DateTime.Now };
            context.EntrustMission.Add(entrust);
            await context.SaveChangesAsync();
            entrustId = entrust.EntrustMissionId;
        }
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        foreach (var url in new[]
        {
            "/PointLog", $"/PointLog/Details/{logId}", $"/PointLog/PointLogsDetail?UserId={userId}", "/PointLog/PointLogsDetail", "/PointLog/Adjust",
            "/PointRecharge", $"/PointRecharge/Details/{rechargeId}",
            "/Mission", "/Mission/Create", $"/Mission/Details/{missionId}", $"/Mission/Edit/{missionId}", $"/Mission/Delete/{missionId}",
            "/MissionProgress", $"/MissionProgress?MissionId={missionId}", $"/MissionProgress?UserId={userId}", $"/MissionProgress/Details/{progressId}",
            "/EntrustMission", "/EntrustMission/Create", $"/EntrustMission/Details/{entrustId}", $"/EntrustMission/Edit/{entrustId}", $"/EntrustMission/Delete/{entrustId}",
        })
        {
            var response = await admin.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} → {(int)response.StatusCode}");
        }
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Mission/Edit/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/EntrustMission/Details/999999")).StatusCode);
    }

    // ---------- 平台任務 ----------

    [Fact]
    public async Task Mission_can_be_created_and_edited_but_system_fields_are_kept()
    {
        var name = "新任務" + Guid.NewGuid().ToString("N")[..6];
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var create = await PostForm(admin, "/Mission/Create", "/Mission/Create",
            ("MissionName", name), ("MissionDetail", "說明"), ("TargetType", "post"), ("TargetCount", "3"),
            ("RewardPoints", "20"), ("RewardExp", "5"), ("Category", "論壇"), ("ResetType", "daily"),
            ("IsActive", "true"), ("IsActive", "false"));
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Mission created;
        await using (var context = _factory.Db.NewContext())
            created = await context.Mission.AsNoTracking().SingleAsync(m => m.MissionName == name);
        Assert.True(created.IsActive);
        Assert.Equal("daily", created.ResetType);
        Assert.NotNull(created.CreatedAt); // 原本新增表單沒有建立時間，寫入 null

        var missionId = await AddMission("舊任務" + Guid.NewGuid().ToString("N")[..6]);
        var edit = await PostForm(admin, $"/Mission/Edit/{missionId}", $"/Mission/Edit/{missionId}",
            ("MissionName", "改名"), ("MissionDetail", "d"), ("TargetType", "post"), ("TargetCount", "1"),
            ("RewardPoints", "30"), ("RewardExp", "0"), ("Category", "c"), ("ResetType", "none"),
            ("IsActive", "true"), ("ParticipantCount", "999"), ("CreatedAt", "2030-01-01"));
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        var edited = (await FindMission(missionId))!;
        Assert.Equal("改名", edited.MissionName);
        Assert.Equal(30, edited.RewardPoints);
        Assert.Equal(7, edited.ParticipantCount);                 // 參與人數由系統維護
        Assert.Equal(new DateTime(2026, 1, 1), edited.CreatedAt); // 建立時間不會被表單覆蓋
    }

    [Fact]
    public async Task Negative_reward_is_rejected()
    {
        var missionId = await AddMission("m" + Guid.NewGuid().ToString("N")[..6], reward: 10);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, $"/Mission/Edit/{missionId}", $"/Mission/Edit/{missionId}",
            ("MissionName", "m"), ("MissionDetail", "d"), ("TargetType", "post"), ("TargetCount", "1"),
            ("RewardPoints", "-50"), ("RewardExp", "0"), ("Category", "c"), ("ResetType", "none"), ("IsActive", "true"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 留在表單顯示錯誤
        Assert.Contains("負數", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(10, (await FindMission(missionId))!.RewardPoints);
    }

    [Fact]
    public async Task Mission_with_progress_cannot_be_deleted()
    {
        var used = await AddMission("有進度" + Guid.NewGuid().ToString("N")[..6]);
        await AddProgress(NewUser(), used);
        var unused = await AddMission("沒進度" + Guid.NewGuid().ToString("N")[..6]);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var blocked = await PostForm(admin, $"/Mission/Delete/{used}", $"/Mission/Delete/{used}");
        var deleted = await PostForm(admin, $"/Mission/Delete/{unused}", $"/Mission/Delete/{unused}");

        // 原本有進度時撞外鍵 → 500
        Assert.Equal(HttpStatusCode.Redirect, blocked.StatusCode);
        Assert.NotNull(await FindMission(used));
        Assert.Contains("改為停用", WebUtility.HtmlDecode(await admin.GetStringAsync("/Mission")));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Null(await FindMission(unused));
    }

    // ---------- 任務進度唯讀 ----------

    [Theory]
    [InlineData("GET", "/MissionProgress/Create")]
    [InlineData("POST", "/MissionProgress/Create")]
    [InlineData("GET", "/MissionProgress/Edit/1")]
    [InlineData("POST", "/MissionProgress/Edit/1")]
    [InlineData("GET", "/MissionProgress/Delete/1")]
    [InlineData("POST", "/MissionProgress/Delete/1")]
    public async Task Mission_progress_is_read_only(string method, string url)
    {
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? new FormUrlEncodedContent([]) : null,
        });

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    // ---------- 後台建立委託 ----------

    private async Task<List<PointLog>> Logs(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return await context.PointLog.AsNoTracking().Where(l => l.UserId == userId).OrderBy(l => l.PointLogId).ToListAsync();
    }

    [Fact]
    public async Task Admin_created_entrust_mission_reserves_points_from_the_author()
    {
        var author = NewUser();
        await GivePoints(author, 100);
        var topic = await AddTopic(author, "需要協助" + Guid.NewGuid().ToString("N")[..6]);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, "/EntrustMission/Create", "/EntrustMission/Create",
            ("ContentId", topic.ToString()), ("Title", "代建委託"), ("Detail", "說明"), ("RewardPoints", "40"), ("Category", "其他"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var context = _factory.Db.NewContext();
        var mission = await context.EntrustMission.AsNoTracking().SingleAsync(e => e.ContentId == topic);
        Assert.Equal(author, mission.UserId);
        Assert.Equal("open", mission.Status);
        Assert.Equal(new[] { 100, 60 }, (await Logs(author)).Select(l => l.BalanceAfter)); // 原本不會預扣
    }

    [Fact]
    public async Task Admin_created_entrust_mission_fails_without_enough_points()
    {
        var author = NewUser();
        await GivePoints(author, 10);
        var topic = await AddTopic(author, "點數不足" + Guid.NewGuid().ToString("N")[..6]);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await PostForm(admin, "/EntrustMission/Create", "/EntrustMission/Create",
            ("ContentId", topic.ToString()), ("Title", "代建委託"), ("Detail", "說明"), ("RewardPoints", "40"), ("Category", "其他"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("點數不足", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        await using var context = _factory.Db.NewContext();
        Assert.False(await context.EntrustMission.AnyAsync(e => e.ContentId == topic));
        Assert.Single(await Logs(author));
    }

    [Fact]
    public async Task Open_entrust_mission_cannot_be_deleted()
    {
        var author = NewUser();
        await GivePoints(author, 100);
        var topic = await AddTopic(author, "t" + Guid.NewGuid().ToString("N")[..6]);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);
        await PostForm(admin, "/EntrustMission/Create", "/EntrustMission/Create",
            ("ContentId", topic.ToString()), ("Title", "e"), ("Detail", "d"), ("RewardPoints", "10"), ("Category", "其他"));
        int id;
        await using (var context = _factory.Db.NewContext())
            id = (await context.EntrustMission.SingleAsync(e => e.ContentId == topic)).EntrustMissionId;

        await PostForm(admin, $"/EntrustMission/Delete/{id}", $"/EntrustMission/Delete/{id}");

        await using var verify = _factory.Db.NewContext();
        Assert.True(await verify.EntrustMission.AnyAsync(e => e.EntrustMissionId == id));
    }
}
