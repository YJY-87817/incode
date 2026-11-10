using Microsoft.Extensions.Logging.Abstractions;

namespace incode.MVC.Tests;

public class MissionProgressServiceTests
{
    private static MissionProgressService Create(incodedatabaseContext context, FakeMissionService? missions = null)
        => new(context, new PointLogService(context), missions ?? new FakeMissionService(), NullLogger<MissionProgressService>.Instance);

    private static async Task<int> AddMission(TestDb db, string resetType, int target = 1, int reward = 50)
    {
        await using var context = db.NewContext();
        var mission = new Mission
        {
            MissionName = "m", MissionDetail = "d", TargetType = "post", TargetCount = target,
            RewardPoints = reward, Category = "c", Difficulty = "簡單", ResetType = resetType, IsActive = true,
        };
        context.Mission.Add(mission);
        await context.SaveChangesAsync();
        return mission.MissionId;
    }

    private static async Task<int> AddProgress(TestDb db, int userId, int missionId, int count, DateTime cycleStart,
        string status = "completed")
    {
        await using var context = db.NewContext();
        var progress = new MissionProgress
        {
            UserId = userId, MissionId = missionId, CurrentCount = count, Status = status,
            CycleStartAt = cycleStart, CycleEndAt = cycleStart.AddDays(1),
        };
        context.MissionProgress.Add(progress);
        await context.SaveChangesAsync();
        return progress.MissionProgressId;
    }

    private static async Task<int> Balance(TestDb db, int userId)
    {
        await using var context = db.NewContext();
        return await new PointLogService(context).GetCurrentBalanceAsync(userId);
    }

    [Fact]
    public async Task ClaimReward_pays_once_and_marks_rewarded()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "daily", reward: 50);
        var progressId = await AddProgress(db, userId, missionId, 1, DateTime.Now.Date);
        var missions = new FakeMissionService();

        await using (var context = db.NewContext())
            Assert.True((await Create(context, missions).ClaimRewardAsync(userId, progressId)).Ok);

        Assert.Equal(50, await Balance(db, userId));
        Assert.Equal(1, missions.Triggered);

        await using (var context = db.NewContext())
        {
            var again = await Create(context, missions).ClaimRewardAsync(userId, progressId);
            Assert.False(again.Ok);
            Assert.Equal(400, again.Code);
        }
        Assert.Equal(50, await Balance(db, userId));

        await using var verify = db.NewContext();
        Assert.Equal("rewarded", (await verify.MissionProgress.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task ClaimReward_rejects_progress_below_target()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "daily", target: 3);
        var progressId = await AddProgress(db, userId, missionId, 2, DateTime.Now.Date, status: "ongoing");

        await using var context = db.NewContext();
        var result = await Create(context).ClaimRewardAsync(userId, progressId);

        Assert.Equal(400, result.Code);
        Assert.Equal(0, await Balance(db, userId));
    }

    [Fact]
    public async Task ClaimReward_for_someone_elses_progress_is_404()
    {
        using var db = new TestDb();
        var owner = db.SeedUser("owner@example.com");
        var other = db.SeedUser("other@example.com");
        var missionId = await AddMission(db, "daily");
        var progressId = await AddProgress(db, owner, missionId, 1, DateTime.Now.Date);

        await using var context = db.NewContext();
        var result = await Create(context).ClaimRewardAsync(other, progressId);

        Assert.Equal(404, result.Code);
        Assert.Equal(0, await Balance(db, other));
        Assert.Equal(0, await Balance(db, owner));
    }

    [Fact]
    public async Task ClaimReward_one_time_mission_pays_only_once_even_with_legacy_duplicate_progress()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "once", reward: 50);
        // 改版前每天都會建立新進度，同一會員同一任務可能有多筆
        var first = await AddProgress(db, userId, missionId, 1, new DateTime(2025, 3, 3));
        var second = await AddProgress(db, userId, missionId, 1, new DateTime(2025, 3, 4));

        await using (var context = db.NewContext())
            Assert.True((await Create(context).ClaimRewardAsync(userId, first)).Ok);
        await using (var context = db.NewContext())
        {
            var result = await Create(context).ClaimRewardAsync(userId, second);
            Assert.False(result.Ok);
        }

        Assert.Equal(50, await Balance(db, userId));
    }

    [Fact]
    public async Task GetUserMissions_does_not_offer_reward_again_for_rewarded_one_time_mission()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "once");
        var first = await AddProgress(db, userId, missionId, 1, new DateTime(2025, 3, 3));
        await AddProgress(db, userId, missionId, 1, new DateTime(2025, 3, 4));
        await using (var context = db.NewContext())
            await Create(context).ClaimRewardAsync(userId, first);

        await using var verify = db.NewContext();
        var views = (await Create(verify).GetUserMissionsAsync(userId)).ToList();

        Assert.Equal(2, views.Count);
        Assert.All(views, v => Assert.False(v.CanClaimReward));
    }
}
