using Microsoft.Extensions.Logging.Abstractions;
using incode.MVC.Dtos.Point;

namespace incode.MVC.Tests;

public class EntrustMissionServiceTests
{
    private static EntrustMissionService Create(incodedatabaseContext context)
    {
        var points = new PointLogService(context);
        return new EntrustMissionService(context, points, new PointReserveService(context, points),
            NullLogger<EntrustMissionService>.Instance);
    }

    private static async Task GivePoints(TestDb db, int userId, int amount)
    {
        await using var context = db.NewContext();
        await using var tx = await context.Database.BeginTransactionAsync();
        await new PointLogService(context).ChangePointsAsync(userId, amount, "test", null, PointType.SystemAdjustment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task<int> Balance(TestDb db, int userId)
    {
        await using var context = db.NewContext();
        return await new PointLogService(context).GetCurrentBalanceAsync(userId);
    }

    private static async Task<int> AddContent(TestDb db, int userId, int? parentId = null, bool active = true)
    {
        await using var context = db.NewContext();
        var content = new Content
        {
            UserId = userId, ParentContentId = parentId, Title = "t", Body = "b",
            Type = 0, IsActive = active ? 1 : 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
        };
        context.Content.Add(content);
        await context.SaveChangesAsync();
        return content.ContentId;
    }

    private static EntrustMissionDTO Dto(int userId, int contentId, int reward = 100)
        => new() { UserId = userId, ContentId = contentId, Title = "委託", Detail = "d", RewardPoints = reward, Category = "其他" };

    private static async Task<int> CreateMission(TestDb db, int userId, int contentId, int reward = 100)
    {
        await using var context = db.NewContext();
        var result = await Create(context).CreateEntrustMissionAsync(Dto(userId, contentId, reward));
        Assert.True(result.Ok, result.Message);
        return (int)result.Data!.GetType().GetProperty("EntrustMissionId")!.GetValue(result.Data)!;
    }

    // ---------- 建立 ----------

    [Fact]
    public async Task Create_reserves_points_on_own_topic()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 500);
        var contentId = await AddContent(db, userId);

        var missionId = await CreateMission(db, userId, contentId, reward: 100);

        Assert.Equal(400, await Balance(db, userId));
        await using var verify = db.NewContext();
        var reserve = await verify.EntrustMissionPointReserve.AsNoTracking().SingleAsync();
        Assert.Equal(missionId, reserve.EntrustMissionId);
        Assert.Equal(100, reserve.ReservedPoints);
        Assert.Equal("reserved", reserve.Status);
        Assert.Equal("open", (await verify.EntrustMission.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Create_on_someone_elses_content_is_404_and_charges_nothing()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var attacker = db.SeedUser("attacker@example.com");
        await GivePoints(db, attacker, 500);
        var contentId = await AddContent(db, owner);

        await using var context = db.NewContext();
        var result = await Create(context).CreateEntrustMissionAsync(Dto(attacker, contentId));

        Assert.Equal(404, result.Code);
        Assert.Equal(500, await Balance(db, attacker));
    }

    [Fact]
    public async Task Create_on_a_reply_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 500);
        var topic = await AddContent(db, userId);
        var reply = await AddContent(db, userId, parentId: topic);

        await using var context = db.NewContext();
        var result = await Create(context).CreateEntrustMissionAsync(Dto(userId, reply));

        Assert.Equal(400, result.Code);
        Assert.Equal(500, await Balance(db, userId));
    }

    [Fact]
    public async Task Create_twice_on_same_topic_is_409()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 500);
        var contentId = await AddContent(db, userId);
        await CreateMission(db, userId, contentId);

        await using var context = db.NewContext();
        var result = await Create(context).CreateEntrustMissionAsync(Dto(userId, contentId));

        Assert.Equal(409, result.Code);
        Assert.Equal(400, await Balance(db, userId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_rejects_non_positive_reward(int reward)
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var contentId = await AddContent(db, userId);

        await using var context = db.NewContext();
        var result = await Create(context).CreateEntrustMissionAsync(Dto(userId, contentId, reward));

        Assert.Equal(400, result.Code);
    }

    [Fact]
    public async Task Create_with_past_deadline_is_rejected()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 500);
        var contentId = await AddContent(db, userId);
        var dto = Dto(userId, contentId);
        dto.Deadline = DateTime.Now.AddDays(-1);

        await using var context = db.NewContext();
        Assert.Equal(400, (await Create(context).CreateEntrustMissionAsync(dto)).Code);
    }

    [Fact]
    public async Task Create_with_insufficient_points_rolls_everything_back()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 50);
        var contentId = await AddContent(db, userId);

        await using var context = db.NewContext();
        var result = await Create(context).CreateEntrustMissionAsync(Dto(userId, contentId, reward: 100));

        Assert.False(result.Ok);
        Assert.Equal(50, await Balance(db, userId));
        await using var verify = db.NewContext();
        Assert.Equal(0, await verify.EntrustMission.CountAsync());
        Assert.Equal(0, await verify.EntrustMissionPointReserve.CountAsync());
    }

    // ---------- 會員取消 ----------

    [Fact]
    public async Task Cancel_refunds_reserved_points_once()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await GivePoints(db, userId, 500);
        var missionId = await CreateMission(db, userId, await AddContent(db, userId));

        await using (var context = db.NewContext())
            Assert.True((await Create(context).CancelEntrustMissionAsync(missionId, userId)).Ok);
        Assert.Equal(500, await Balance(db, userId));

        await using (var context = db.NewContext())
        {
            var again = await Create(context).CancelEntrustMissionAsync(missionId, userId);
            Assert.Equal(409, again.Code);
        }
        Assert.Equal(500, await Balance(db, userId)); // 沒有重複退點

        await using var verify = db.NewContext();
        Assert.Equal("cancelled", (await verify.EntrustMission.AsNoTracking().SingleAsync()).Status);
        Assert.Equal("released", (await verify.EntrustMissionPointReserve.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Cancel_by_someone_else_is_404()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var other = db.SeedUser("other@example.com");
        await GivePoints(db, owner, 500);
        var missionId = await CreateMission(db, owner, await AddContent(db, owner));

        await using var context = db.NewContext();
        var result = await Create(context).CancelEntrustMissionAsync(missionId, other);

        Assert.Equal(404, result.Code);
        Assert.Equal(400, await Balance(db, owner));
        Assert.Equal(0, await Balance(db, other));
    }

    [Fact]
    public async Task Cancel_is_blocked_once_another_member_has_replied()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var helper = db.SeedUser("helper@example.com");
        await GivePoints(db, owner, 500);
        var topic = await AddContent(db, owner);
        var missionId = await CreateMission(db, owner, topic);
        await AddContent(db, helper, parentId: topic);

        await using var context = db.NewContext();
        var result = await Create(context).CancelEntrustMissionAsync(missionId, owner);

        Assert.Equal(409, result.Code);
        Assert.Equal(400, await Balance(db, owner)); // 賞金沒有拿回來
    }

    [Fact]
    public async Task Cancel_detects_nested_replies_from_others()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var helper = db.SeedUser("helper@example.com");
        await GivePoints(db, owner, 500);
        var topic = await AddContent(db, owner);
        var missionId = await CreateMission(db, owner, topic);
        var ownReply = await AddContent(db, owner, parentId: topic);
        await AddContent(db, helper, parentId: ownReply); // 回覆自己的補充

        await using var context = db.NewContext();
        Assert.Equal(409, (await Create(context).CancelEntrustMissionAsync(missionId, owner)).Code);
    }

    [Fact]
    public async Task Cancel_ignores_own_and_deleted_replies()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var helper = db.SeedUser("helper@example.com");
        await GivePoints(db, owner, 500);
        var topic = await AddContent(db, owner);
        var missionId = await CreateMission(db, owner, topic);
        await AddContent(db, owner, parentId: topic);                 // 自己補充
        await AddContent(db, helper, parentId: topic, active: false); // 別人的回覆已刪除

        await using var context = db.NewContext();
        Assert.True((await Create(context).CancelEntrustMissionAsync(missionId, owner)).Ok);
        Assert.Equal(500, await Balance(db, owner));
    }

    [Fact]
    public async Task Cancel_closed_mission_is_rejected()
    {
        using var db = new TestDb();
        var owner = db.SeedUser();
        await GivePoints(db, owner, 500);
        var missionId = await CreateMission(db, owner, await AddContent(db, owner));
        await using (var seed = db.NewContext())
        {
            var mission = await seed.EntrustMission.SingleAsync();
            mission.Status = "closed";
            await seed.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var result = await Create(context).CancelEntrustMissionAsync(missionId, owner);

        Assert.Equal(409, result.Code);
        Assert.Equal(400, await Balance(db, owner));
    }

    // ---------- 後台取消 ----------

    [Fact]
    public async Task Admin_cancel_refunds_even_when_others_have_replied()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var helper = db.SeedUser("helper@example.com");
        await GivePoints(db, owner, 500);
        var topic = await AddContent(db, owner);
        var missionId = await CreateMission(db, owner, topic);
        await AddContent(db, helper, parentId: topic);

        await using (var context = db.NewContext())
            Assert.True((await Create(context).CancelEntrustMissionByAdminAsync(missionId)).Ok);

        Assert.Equal(500, await Balance(db, owner));
        await using var verify = db.NewContext();
        Assert.Equal("cancelled", (await verify.EntrustMission.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Admin_cancel_unknown_mission_is_404()
    {
        using var db = new TestDb();
        db.SeedUser();
        await using var context = db.NewContext();

        Assert.Equal(404, (await Create(context).CancelEntrustMissionByAdminAsync(999)).Code);
    }

    [Fact]
    public async Task Admin_cancel_twice_does_not_refund_twice()
    {
        using var db = new TestDb();
        var owner = db.SeedUser();
        await GivePoints(db, owner, 500);
        var missionId = await CreateMission(db, owner, await AddContent(db, owner));

        await using (var context = db.NewContext())
            Assert.True((await Create(context).CancelEntrustMissionByAdminAsync(missionId)).Ok);
        await using (var context = db.NewContext())
            Assert.Equal(409, (await Create(context).CancelEntrustMissionByAdminAsync(missionId)).Code);

        Assert.Equal(500, await Balance(db, owner));
    }
}
