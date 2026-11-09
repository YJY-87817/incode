using System;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point;

public class PointReserveService : IPointReserveService
{
    private readonly incodedatabaseContext _context;
    private readonly IPointLogService _pointLogService;

    public PointReserveService(
        incodedatabaseContext context,
        IPointLogService pointLogService)
    {
        _context = context;
        _pointLogService = pointLogService;
    }

    // 發佈任務時呼叫 → 扣點 + 建立預扣
    public async Task<bool> ReserveAsync(int userId, int points, int entrustMissionId)
    {
        // 先取得任務標題（如果不存在就給預設值）
        var missionTitle = await _context.EntrustMission
            .Where(m => m.EntrustMissionId == entrustMissionId)
            .Select(m => m.Title ?? "未命名委託")
            .FirstOrDefaultAsync();

        // 1️先扣點
        var deduct = await _pointLogService.ChangePointsAsync(
            userId,
            -points,
            "entrust_mission_reserve",
            entrustMissionId,
            PointType.ExpenseEntrustMissionPointReserve,
            displaySource: "委託發布預扣",
            displaySubTitle: $"任務 #{entrustMissionId} - {missionTitle}"
            );

        if (!deduct) return false;

        // 2️建立預扣紀錄
        _context.EntrustMissionPointReserve.Add(new EntrustMissionPointReserve
        {
            UserId = userId,
            EntrustMissionId = entrustMissionId,
            ReservedPoints = points,
            Status = "reserved",
            CreatedAt = DateTime.Now
        });

        return true;
    }

    // 選最佳解答時呼叫 → 消耗預扣
    public async Task<bool> ConsumeAsync(int userId, int entrustMissionId)
    {
        var reserve = await _context.EntrustMissionPointReserve
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.EntrustMissionId == entrustMissionId &&
                r.Status == "reserved");

        if (reserve == null) return false;

        reserve.Status = "consumed";
        reserve.ReleasedAt = DateTime.Now;

        return true;
    }

    // 任務取消時呼叫 → 退回發起人
    public async Task<bool> ReleaseAsync(int userId, int entrustMissionId)
    {
        var reserve = await _context.EntrustMissionPointReserve
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.EntrustMissionId == entrustMissionId &&
                r.Status == "reserved");

        if (reserve == null) return false;

        reserve.Status = "released";
        reserve.ReleasedAt = DateTime.Now;

        // 先取得任務標題
        var missionTitle = await _context.EntrustMission
            .Where(m => m.EntrustMissionId == entrustMissionId)
            .Select(m => m.Title ?? "未命名委託")
            .FirstOrDefaultAsync();

        // 退點（失敗時回傳 false，由呼叫端 rollback）
        return await _pointLogService.ChangePointsAsync(
            userId,
            reserve.ReservedPoints,
            "entrust_mission_refund",
            entrustMissionId,
            PointType.IncomeEntrustMissionRefund,
            displaySource: "委託取消退款",
            displaySubTitle: $"任務 #{entrustMissionId} - {missionTitle}"
        );
    }
}
