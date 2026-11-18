using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point
{
    public class AdminPointQueryService : IAdminPointQueryService
    {
        private readonly incodedatabaseContext _context;

        public AdminPointQueryService(incodedatabaseContext context)
        {
            _context = context;
        }

        public Task<List<PointLog>> GetLogsAsync()
            => _context.PointLog.Include(p => p.PointTypeDefinition).Include(p => p.User).AsNoTracking().ToListAsync();

        public Task<PointLog?> GetLogAsync(int pointLogId)
            => _context.PointLog.Include(p => p.PointTypeDefinition).Include(p => p.User).AsNoTracking()
                .FirstOrDefaultAsync(m => m.PointLogId == pointLogId);

        public Task<List<PointRecharge>> GetRechargesAsync()
            => _context.PointRecharge.Include(p => p.User).AsNoTracking().ToListAsync();

        public Task<PointRecharge?> GetRechargeAsync(int pointRechargeId)
            => _context.PointRecharge.Include(p => p.User).AsNoTracking()
                .FirstOrDefaultAsync(m => m.PointRechargeId == pointRechargeId);

        public async Task<List<(int UserId, string Username, string Email)>> GetUserOptionsAsync()
        {
            var users = await _context.User.AsNoTracking()
                .OrderBy(u => u.UserId)
                .Select(u => new { u.UserId, u.Username, u.Email })
                .ToListAsync();
            return users.Select(u => (u.UserId, u.Username, u.Email)).ToList();
        }

        public async Task<(List<PointLogsViewModel> Logs, int CurrentBalance)> GetUserLedgerAsync(int userId)
        {
            var logs = await _context.PointLog
                .AsNoTracking()
                .Include(l => l.PointTypeDefinition)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // 一次查出相關的任務 / 委託名稱（避免在迴圈內反覆查詢）
            var missionIds = logs.Where(l => l.SourceType == "mission").Select(l => l.SourceId).Distinct().ToList();
            var entrustIds = logs.Where(l => l.SourceType == "entrust_mission" || l.SourceType == "entrust_mission_point_reserve")
                                 .Select(l => l.SourceId).Distinct().ToList();

            var missions = await _context.Mission.Where(m => missionIds.Contains(m.MissionId))
                .ToDictionaryAsync(m => m.MissionId, m => m.MissionName ?? "任務");
            var entrusts = await _context.EntrustMission.Where(e => entrustIds.Contains(e.EntrustMissionId))
                .ToDictionaryAsync(e => e.EntrustMissionId, e => e.Title ?? "委託");

            var items = logs.Select(log => new PointLogsViewModel
            {
                CreatedAt = log.CreatedAt ?? DateTime.Now,
                ChangeAmount = log.ChangeAmount,
                NewBalance = log.BalanceAfter,
                DisplayName = log.PointTypeDefinition?.Category ?? "點數異動",
                Description = log.SourceType switch
                {
                    "mission" => missions.GetValueOrDefault(log.SourceId ?? 0, "平台任務獎勵"),
                    "entrust_mission" => entrusts.GetValueOrDefault(log.SourceId ?? 0, "委託任務獎勵"),
                    "entrust_mission_point_reserve" => entrusts.TryGetValue(log.SourceId ?? 0, out var t) ? $"[預扣] {t}" : "委託預扣",
                    "point_recharge" => "點數儲值",
                    "orders" => $"訂單編號 #{log.SourceId}",
                    _ => log.DisplaySource ?? "系統調整",
                },
            }).ToList();

            int balance = logs.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.PointLogId)
                .Select(l => l.BalanceAfter).FirstOrDefault();
            return (items, balance);
        }
    }
}
