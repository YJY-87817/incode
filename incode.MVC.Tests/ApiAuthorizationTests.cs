using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace incode.MVC.Tests;

/// <summary>整個網站的 API 權限測試：登入檢查、管理員限定、擁有者檢查、停用帳號、Origin 檢查。</summary>
public class ApiAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiAuthorizationTests(ApiFactory factory) => _factory = factory;

    private int NewUser() => _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");

    private static HttpRequestMessage Request(HttpMethod method, string url, string? origin = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (origin != null) request.Headers.Add("Origin", origin);
        return request;
    }

    // ---------- 未登入 ----------

    public static TheoryData<string, string> MemberOnlyEndpoints => new()
    {
        { "GET", "/api/OrderApi/order" },
        { "GET", "/api/OrderApi/orderdetail/1" },
        { "PUT", "/api/OrderApi/CancelOrder/1" },
        { "POST", "/api/OrderApi/OrderPost" },
        { "GET", "/api/PointLogApi/user/my-logs" },
        { "GET", "/api/MissionProgressApi/my-missions" },
        { "POST", "/api/MissionProgressApi/claim-reward" },
        { "GET", "/api/EntrustMissionApi/my-entrusts" },
        { "POST", "/api/EntrustMissionApi" },
        { "POST", "/api/EntrustMissionApi/1/cancel" },
        { "POST", "/api/PointRechargeApi/recharge" },
        { "GET", "/api/PointRechargeApi/order/ABC" },
        { "GET", "/api/ChatSessionsApi/User/1" },
        { "DELETE", "/api/ChatSessionsApi/Delete/1" },
        { "PUT", "/api/User/profile" },
    };

    [Theory]
    [MemberData(nameof(MemberOnlyEndpoints))]
    public async Task Anonymous_requests_to_member_endpoints_get_401(string method, string url)
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.SendAsync(Request(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_chat_hub_negotiate_gets_401()
    {
        var response = await _factory.CreateAnonymousClient()
            .SendAsync(Request(HttpMethod.Post, "/chatHub/negotiate?negotiateVersion=1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/User/profile?username=nobody")]
    [InlineData("/api/User/1/stats")]
    [InlineData("/api/EntrustMissionApi")]
    public async Task Public_endpoints_do_not_require_login(string url)
    {
        var response = await _factory.CreateAnonymousClient().GetAsync(url);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- 管理員限定 ----------

    public static TheoryData<string, string> AdminOnlyEndpoints => new()
    {
        { "GET", "/api/User" },
        { "POST", "/api/User/1/deactivate" },
        { "POST", "/api/User/1/activate" },
        { "GET", "/api/ChatMessages/GetActiveSupportSessions" },
        { "POST", "/api/ChatMessages/AdminReply" },
    };

    [Theory]
    [MemberData(nameof(AdminOnlyEndpoints))]
    public async Task Members_cannot_use_admin_endpoints(string method, string url)
    {
        var client = await _factory.LoginAsync(NewUser());

        var response = await client.SendAsync(Request(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/User")]
    [InlineData("/api/ChatMessages/GetActiveSupportSessions")]
    public async Task Admins_can_use_admin_endpoints(string url)
    {
        var client = await _factory.LoginAsync(NewUser(), admin: true);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_own_account()
    {
        var adminId = NewUser();
        var client = await _factory.LoginAsync(adminId, admin: true);

        var response = await client.PostAsync($"/api/User/{adminId}/deactivate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_deactivating_unknown_user_is_404()
    {
        var client = await _factory.LoginAsync(NewUser(), admin: true);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/User/999999/deactivate", null)).StatusCode);
    }

    // ---------- 停用帳號 ----------

    [Fact]
    public async Task Deactivated_member_is_logged_out_on_next_request_and_can_return_after_reactivation()
    {
        var memberId = NewUser();
        var member = await _factory.LoginAsync(memberId);
        var admin = await _factory.LoginAsync(NewUser(), admin: true);

        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/PointLogApi/user/my-logs")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/User/{memberId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/PointLogApi/user/my-logs")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/User/{memberId}/activate", null)).StatusCode);
        var again = await _factory.LoginAsync(memberId);
        Assert.Equal(HttpStatusCode.OK, (await again.GetAsync("/api/PointLogApi/user/my-logs")).StatusCode);
    }

    // ---------- 擁有者檢查（非本人一律 404） ----------

    private async Task<int> SeedOrder(int userId)
    {
        await using var context = _factory.Db.NewContext();
        var order = new Orders { UserId = userId, TotalPoint = 0, Remark = "", Status = OrderStatus.Pending, OrderDate = DateTime.Now };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order.OrderId;
    }

    [Fact]
    public async Task Order_details_and_cancel_are_limited_to_the_owner()
    {
        var owner = NewUser();
        var other = NewUser();
        var orderId = await SeedOrder(owner);
        var ownerClient = await _factory.LoginAsync(owner);
        var otherClient = await _factory.LoginAsync(other);

        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/OrderApi/orderdetail/{orderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/OrderApi/shippeddetail/{orderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PutAsync($"/api/OrderApi/CancelOrder/{orderId}", null)).StatusCode);

        // 別人的取消沒有生效
        await using (var context = _factory.Db.NewContext())
            Assert.Equal(OrderStatus.Pending, (await context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == orderId)).Status);

        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/OrderApi/orderdetail/{orderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.PutAsync($"/api/OrderApi/CancelOrder/{orderId}", null)).StatusCode);
    }

    [Fact]
    public async Task Recharge_order_status_is_limited_to_the_owner()
    {
        var owner = NewUser();
        var other = NewUser();
        string orderNo;
        await using (var context = _factory.Db.NewContext())
            orderNo = (await new PointRechargeService(context).CreateOrderAsync(owner, 100, "ecpay")).OrderNo;

        Assert.Equal(HttpStatusCode.OK, (await (await _factory.LoginAsync(owner)).GetAsync($"/api/PointRechargeApi/order/{orderNo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await _factory.LoginAsync(other)).GetAsync($"/api/PointRechargeApi/order/{orderNo}")).StatusCode);
    }

    [Fact]
    public async Task Cancelling_someone_elses_entrust_mission_is_404_and_changes_nothing()
    {
        var owner = NewUser();
        var other = NewUser();
        int missionId;
        await using (var context = _factory.Db.NewContext())
        {
            var content = new Content { UserId = owner, Title = "t", Body = "b", Type = 0, IsActive = 1 };
            context.Content.Add(content);
            await context.SaveChangesAsync();
            var mission = new EntrustMission
            {
                UserId = owner, ContentId = content.ContentId, Title = "t", Detail = "d", RewardPoints = 10,
                Category = "其他", Status = "open", IsActive = true,
            };
            context.EntrustMission.Add(mission);
            await context.SaveChangesAsync();
            missionId = mission.EntrustMissionId;
        }

        var response = await (await _factory.LoginAsync(other)).PostAsync($"/api/EntrustMissionApi/{missionId}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verify = _factory.Db.NewContext();
        Assert.Equal("open", (await verify.EntrustMission.AsNoTracking().SingleAsync(m => m.EntrustMissionId == missionId)).Status);
    }

    [Fact]
    public async Task Claiming_someone_elses_mission_reward_is_404_and_pays_nothing()
    {
        var owner = NewUser();
        var other = NewUser();
        int progressId;
        await using (var context = _factory.Db.NewContext())
        {
            var mission = new Mission
            {
                MissionName = "m", MissionDetail = "d", TargetType = "post", TargetCount = 1, RewardPoints = 999,
                Category = "c", Difficulty = "簡單", ResetType = "daily", IsActive = true,
            };
            context.Mission.Add(mission);
            await context.SaveChangesAsync();
            var progress = new MissionProgress
            {
                UserId = owner, MissionId = mission.MissionId, CurrentCount = 1, Status = "completed",
                CycleStartAt = DateTime.Now.Date, CycleEndAt = DateTime.Now.Date.AddDays(1),
            };
            context.MissionProgress.Add(progress);
            await context.SaveChangesAsync();
            progressId = progress.MissionProgressId;
        }

        var response = await (await _factory.LoginAsync(other))
            .PostAsJsonAsync("/api/MissionProgressApi/claim-reward", new { ProgressId = progressId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verify = _factory.Db.NewContext();
        Assert.Equal(0, await verify.PointLog.CountAsync(l => l.UserId == other));
    }

    [Fact]
    public async Task Chat_session_lists_are_limited_to_the_owner_but_admins_can_read_any()
    {
        var owner = NewUser();

        Assert.Equal(HttpStatusCode.OK, (await (await _factory.LoginAsync(owner)).GetAsync($"/api/ChatSessionsApi/User/{owner}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await _factory.LoginAsync(NewUser())).GetAsync($"/api/ChatSessionsApi/User/{owner}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await _factory.LoginAsync(NewUser(), admin: true)).GetAsync($"/api/ChatSessionsApi/User/{owner}")).StatusCode);
    }

    [Fact]
    public async Task Profile_update_never_returns_password_hash()
    {
        var userId = NewUser();
        var client = await _factory.LoginAsync(userId);

        var response = await client.PutAsJsonAsync("/api/User/profile", new { Email = "new@example.com", Username = "新暱稱" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("resetToken", body, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Origin 檢查（CSRF） ----------

    [Fact]
    public async Task Write_requests_from_foreign_origin_are_rejected_even_when_logged_in()
    {
        var owner = NewUser();
        var orderId = await SeedOrder(owner);
        var client = await _factory.LoginAsync(owner);

        var response = await client.SendAsync(Request(HttpMethod.Put, $"/api/OrderApi/CancelOrder/{orderId}", "https://evil.example.com"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = _factory.Db.NewContext();
        Assert.Equal(OrderStatus.Pending, (await context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == orderId)).Status);
    }

    [Fact]
    public async Task Write_requests_from_the_configured_frontend_origin_are_allowed()
    {
        var owner = NewUser();
        var orderId = await SeedOrder(owner);
        var client = await _factory.LoginAsync(owner);

        var response = await client.SendAsync(Request(HttpMethod.Put, $"/api/OrderApi/CancelOrder/{orderId}", "https://localhost:5173"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ecpay_callback_is_not_blocked_by_origin_check()
    {
        var response = await _factory.CreateAnonymousClient()
            .SendAsync(Request(HttpMethod.Post, "/api/PointRechargeApi/ecpay/return", "https://evil.example.com"));

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
