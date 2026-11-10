using incode.MVC.Dtos.Point;

namespace incode.MVC.Tests;

public class MissionServiceTests
{
    private static async Task<int> AddMission(TestDb db, string resetType, string targetType = "post", int target = 3,
        int reward = 50, bool active = true)
    {
        await using var context = db.NewContext();
        var mission = new Mission
        {
            MissionName = "m", MissionDetail = "d", TargetType = targetType, TargetCount = target,
            RewardPoints = reward, Category = "c", Difficulty = "簡單", ResetType = resetType,
            IsActive = active, ParticipantCount = 0,
        };
        context.Mission.Add(mission);
        await context.SaveChangesAsync();
        return mission.MissionId;
    }

    private static async Task<List<MissionProgress>> ProgressOf(TestDb db, int userId)
    {
        await using var context = db.NewContext();
        return await context.MissionProgress.AsNoTracking().Where(p => p.UserId == userId).ToListAsync();
    }

    [Fact]
    public async Task Claim_one_time_mission_only_once()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "once");

        await using var context = db.NewContext();
        var service = new MissionService(context);

        Assert.True((await service.ClaimMissionAsync(missionId, userId)).Ok);
        var again = await service.ClaimMissionAsync(missionId, userId);

        Assert.False(again.Ok);
        Assert.Equal(409, again.Code);
        Assert.Single(await ProgressOf(db, userId));
        Assert.Equal(1, (await context.Mission.AsNoTracking().SingleAsync()).ParticipantCount);
    }

    [Fact]
    public async Task Claim_unknown_or_inactive_mission_is_404()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var inactive = await AddMission(db, "once", active: false);

        await using var context = db.NewContext();
        var service = new MissionService(context);

        Assert.Equal(404, (await service.ClaimMissionAsync(999, userId)).Code);
        Assert.Equal(404, (await service.ClaimMissionAsync(inactive, userId)).Code);
    }

    [Fact]
    public async Task Claim_check_in_mission_completes_immediately()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "daily", targetType: "check-in", target: 1);

        await using var context = db.NewContext();
        Assert.True((await new MissionService(context).ClaimMissionAsync(missionId, userId)).Ok);

        var progress = Assert.Single(await ProgressOf(db, userId));
        Assert.Equal("completed", progress.Status);
        Assert.Equal(1, progress.CurrentCount);
    }

    [Fact]
    public async Task Claim_one_time_mission_ignores_legacy_progress_with_other_cycle_start()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "once");
        await using (var seed = db.NewContext())
        {
            // 改版前的資料：週期起點是「建立當天」
            seed.MissionProgress.Add(new MissionProgress
            {
                UserId = userId, MissionId = missionId, Status = "ongoing",
                CycleStartAt = new DateTime(2025, 3, 3), CycleEndAt = new DateTime(2025, 3, 3, 23, 59, 59),
            });
            await seed.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var result = await new MissionService(context).ClaimMissionAsync(missionId, userId);

        Assert.Equal(409, result.Code);
        Assert.Single(await ProgressOf(db, userId));
    }

    [Fact]
    public async Task Trigger_accumulates_and_completes_at_target()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await AddMission(db, "daily", targetType: "post", target: 3);

        await using var context = db.NewContext();
        var service = new MissionService(context);
        await service.TriggerMissionAsync(userId, "post");
        await service.TriggerMissionAsync(userId, "post");
        Assert.Equal("ongoing", Assert.Single(await ProgressOf(db, userId)).Status);

        await service.TriggerMissionAsync(userId, "post");

        var progress = Assert.Single(await ProgressOf(db, userId));
        Assert.Equal(3, progress.CurrentCount);
        Assert.Equal("completed", progress.Status);
        Assert.NotNull(progress.CompletedAt);
    }

    [Fact]
    public async Task Trigger_ignores_other_action_types_and_inactive_missions()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await AddMission(db, "daily", targetType: "like");
        await AddMission(db, "daily", targetType: "post", active: false);

        await using var context = db.NewContext();
        await new MissionService(context).TriggerMissionAsync(userId, "post");

        Assert.Empty(await ProgressOf(db, userId));
    }

    [Fact]
    public async Task Daily_login_mission_counts_only_once_per_day()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await AddMission(db, "daily", targetType: "login", target: 5);

        await using var context = db.NewContext();
        var service = new MissionService(context);
        for (var i = 0; i < 4; i++)
            await service.TriggerMissionAsync(userId, "login");

        Assert.Equal(1, Assert.Single(await ProgressOf(db, userId)).CurrentCount);
    }

    [Fact]
    public async Task Trigger_one_time_mission_reuses_single_progress_row()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await AddMission(db, "once", targetType: "purchase", target: 5);

        await using var context = db.NewContext();
        var service = new MissionService(context);
        await service.TriggerMissionAsync(userId, "purchase");
        await service.TriggerMissionAsync(userId, "purchase");

        var progress = Assert.Single(await ProgressOf(db, userId));
        Assert.Equal(2, progress.CurrentCount);
        Assert.Equal(MissionResetTypes.OneTimeCycleStart, progress.CycleStartAt);
    }

    [Fact]
    public async Task Sync_only_increases_progress()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        await AddMission(db, "daily", targetType: "like", target: 5);

        await using var context = db.NewContext();
        var service = new MissionService(context);

        await service.SyncMissionCountAsync(userId, "like", (_, _) => Task.FromResult(3));
        Assert.Equal(3, Assert.Single(await ProgressOf(db, userId)).CurrentCount);

        // 取消按讚使實際數量變少：進度不會倒退
        await service.SyncMissionCountAsync(userId, "like", (_, _) => Task.FromResult(1));
        Assert.Equal(3, Assert.Single(await ProgressOf(db, userId)).CurrentCount);

        await service.SyncMissionCountAsync(userId, "like", (_, _) => Task.FromResult(5));
        var progress = Assert.Single(await ProgressOf(db, userId));
        Assert.Equal(5, progress.CurrentCount);
        Assert.Equal("completed", progress.Status);
    }

    [Fact]
    public async Task Sync_does_not_turn_a_rewarded_progress_back_to_completed()
    {
        using var db = new TestDb();
        var userId = db.SeedUser();
        var missionId = await AddMission(db, "daily", targetType: "like", target: 1);
        await using (var seed = db.NewContext())
        {
            var (start, end) = (DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1));
            seed.MissionProgress.Add(new MissionProgress
            {
                UserId = userId, MissionId = missionId, CurrentCount = 1, Status = "rewarded",
                RewardedAt = DateTime.Now, CycleStartAt = start, CycleEndAt = end,
            });
            await seed.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        await new MissionService(context).SyncMissionCountAsync(userId, "like", (_, _) => Task.FromResult(4));

        var progress = Assert.Single(await ProgressOf(db, userId));
        Assert.Equal("rewarded", progress.Status);
        Assert.Equal(4, progress.CurrentCount);
    }

    [Fact]
    public async Task CreateMission_requires_name_and_applies_defaults()
    {
        using var db = new TestDb();
        await using var context = db.NewContext();
        var service = new MissionService(context);

        Assert.Equal(400, (await service.CreateMissionAsync(new MissionDTO { MissionName = " " })).Code);

        var ok = await service.CreateMissionAsync(new MissionDTO { MissionName = "新任務", TargetCount = 0 });
        Assert.True(ok.Ok);

        var mission = await context.Mission.AsNoTracking().SingleAsync();
        Assert.Equal(1, mission.TargetCount);   // 目標次數至少 1
        Assert.Equal("daily", mission.ResetType);
        Assert.True(mission.IsActive);
    }
}
