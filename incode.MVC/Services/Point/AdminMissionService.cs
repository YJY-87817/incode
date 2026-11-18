using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point
{
    public class AdminMissionService : IAdminMissionService
    {
        private readonly incodedatabaseContext _context;

        public AdminMissionService(incodedatabaseContext context)
        {
            _context = context;
        }

        public Task<List<Mission>> GetMissionsAsync()
            => _context.Mission.AsNoTracking().ToListAsync();

        public Task<Mission?> GetMissionAsync(int missionId)
            => _context.Mission.AsNoTracking().FirstOrDefaultAsync(m => m.MissionId == missionId);

        public async Task<Mission> CreateMissionAsync(Mission form)
        {
            var mission = new Mission { CreatedAt = DateTime.Now };
            Apply(mission, form);
            _context.Mission.Add(mission);
            await _context.SaveChangesAsync();
            return mission;
        }

        public async Task<bool> UpdateMissionAsync(int missionId, Mission form)
        {
            var mission = await _context.Mission.FirstOrDefaultAsync(m => m.MissionId == missionId);
            if (mission == null) return false;

            Apply(mission, form);
            mission.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // 只複製表單可以設定的欄位；建立時間、參與人數、上次重置時間由系統維護（原本整個實體由表單綁定，可被覆蓋）
        private static void Apply(Mission mission, Mission form)
        {
            if (string.IsNullOrWhiteSpace(form.MissionName) || string.IsNullOrWhiteSpace(form.MissionDetail)
                || string.IsNullOrWhiteSpace(form.TargetType) || string.IsNullOrWhiteSpace(form.Category))
                throw new BusinessException("任務名稱、說明、目標類型、分類為必填");
            if (form.TargetCount < 1)
                throw new BusinessException("目標次數至少為 1");
            // 負的獎勵在領獎時會變成扣點
            if (form.RewardPoints < 0 || form.RewardExp < 0)
                throw new BusinessException("獎勵點數與經驗值不能是負數");

            mission.MissionName = form.MissionName.Trim();
            mission.MissionDetail = form.MissionDetail.Trim();
            mission.TargetType = form.TargetType.Trim();
            mission.TargetCount = form.TargetCount;
            mission.RewardPoints = form.RewardPoints;
            mission.RewardExp = form.RewardExp;
            mission.Category = form.Category.Trim();
            mission.Difficulty = string.IsNullOrWhiteSpace(form.Difficulty) ? "中等" : form.Difficulty.Trim();
            mission.EstimatedDuration = form.EstimatedDuration?.Trim();
            mission.ResetType = string.IsNullOrWhiteSpace(form.ResetType) ? "none" : form.ResetType.Trim();
            mission.IsFeatured = form.IsFeatured;
            mission.IsActive = form.IsActive;
        }

        public async Task DeleteMissionAsync(int missionId)
        {
            var mission = await _context.Mission.FirstOrDefaultAsync(m => m.MissionId == missionId);
            if (mission == null) return;

            // 原本直接刪除：有會員進度時撞外鍵 → 500；進度也是一次性任務「只能領一次」的依據，不能一起刪
            if (await _context.MissionProgress.AnyAsync(p => p.MissionId == missionId))
                throw new BusinessException("已有會員的任務進度，不能刪除；請改為停用（取消勾選「啟用」）");

            _context.Mission.Remove(mission);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<MissionProgress> Items, string Title)> GetProgressAsync(int? missionId, int? userId)
        {
            var query = _context.MissionProgress
                .Include(m => m.Mission)
                .Include(m => m.User)
                .AsNoTracking();

            if (missionId.HasValue)
                query = query.Where(m => m.MissionId == missionId.Value);
            else if (userId.HasValue) // 查會員時不篩任務
                query = query.Where(m => m.UserId == userId.Value);

            var items = await query.OrderBy(m => m.MissionId).ThenBy(m => m.UserId).ToListAsync();

            var first = items.FirstOrDefault();
            string title = missionId.HasValue ? $"任務進度 - {first?.Mission?.MissionName ?? "未找到任務"}"
                : userId.HasValue ? $"用戶任務進度 - {first?.User?.Email ?? "未找到用戶"}"
                : "所有任務進度記錄";
            return (items, title);
        }

        public Task<MissionProgress?> GetProgressByIdAsync(int missionProgressId)
            => _context.MissionProgress
                .Include(m => m.Mission)
                .Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MissionProgressId == missionProgressId);
    }
}
