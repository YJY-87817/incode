using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point
{
    public class EntrustMissionService : IEntrustMissionService
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;
        private readonly IPointReserveService _pointReserveService;

        public EntrustMissionService(
            incodedatabaseContext context,
            IPointLogService pointLogService,
            IPointReserveService pointReserveService)
        {
            _context = context;
            _pointLogService = pointLogService;
            _pointReserveService = pointReserveService;
        }

        // 公開列表：所有可見的委託任務（進行中 + 已結案）
        public async Task<IEnumerable<EntrustMissionDTO>> GetAllEntrustMissionAsync()
        {
            var now = DateTime.Now;

            return await _context.EntrustMission
                .Where(em => em.IsActive
                    && (em.Status == "open" || em.Status == "completed")
                    && (!em.Deadline.HasValue || em.Deadline.Value >= now || em.Status == "completed"))
                .Include(em => em.User)
                .Select(em => new EntrustMissionDTO
                {
                    EntrustMissionId = em.EntrustMissionId,
                    UserId = em.UserId,
                    ContentId = em.ContentId,
                    Title = em.Title,
                    Detail = em.Detail,
                    RewardPoints = em.RewardPoints,
                    Category = em.Category ?? "其他",
                    Deadline = em.Deadline,
                    Status = em.Status ?? "open",
                    IsUrgent = em.IsUrgent ?? false,
                    IsActive = em.IsActive,
                    CreatedAt = em.CreatedAt,
                    UpdatedAt = em.UpdatedAt,
                    Username = em.User.Username,
                    ImageUrl = em.User.ImageUrl,

                    BestContentId = em.BestContentId,
                    BestUserId = em.BestUserId,
                    SelectedAt = em.SelectedAt,

                    // 唯讀計算屬性不需要在這裡賦值，DTO 會自動計算
                    // HasBestAnswer = em.BestContentId.HasValue （自動）
                })
                .OrderByDescending(em => em.IsUrgent)
                .ThenByDescending(em => em.CreatedAt)
                .ToListAsync();
        }

        // 我的委託任務：我發起的 + 我獲得最佳解答的
        public async Task<IEnumerable<EntrustMissionDTO>> GetMyEntrustMissionsAsync(int userId)
        {
            return await _context.EntrustMission
                .Where(em => em.IsActive && (em.UserId == userId || em.BestUserId == userId))
                .Include(em => em.User)
                .Select(em => new EntrustMissionDTO
                {
                    EntrustMissionId = em.EntrustMissionId,
                    UserId = em.UserId,
                    ContentId = em.ContentId,
                    Title = em.Title,
                    Detail = em.Detail,
                    RewardPoints = em.RewardPoints,
                    Category = em.Category ?? "其他",
                    Deadline = em.Deadline,
                    Status = em.Status ?? "open",
                    IsUrgent = em.IsUrgent ?? false,
                    IsActive = em.IsActive,
                    CreatedAt = em.CreatedAt,
                    UpdatedAt = em.UpdatedAt,
                    Username = em.User.Username,
                    ImageUrl = em.User.ImageUrl,

                    BestContentId = em.BestContentId,
                    BestUserId = em.BestUserId,
                    SelectedAt = em.SelectedAt,

                    // 唯讀計算屬性自動計算，不需手動賦值
                    IsOwner = em.UserId == userId,
                    IsBestAnswerWinner = em.BestUserId == userId
                })
                .OrderByDescending(em => em.CreatedAt)
                .ToListAsync();
        }

        // 取得單一委託詳情（補充最佳解答資訊）
        public async Task<EntrustMissionDTO?> GetEntrustMissionByIdAsync(int id)
        {
            return await _context.EntrustMission
                .Where(em => em.EntrustMissionId == id && em.IsActive)
                .Include(em => em.User)
                .Include(em => em.Content)
                .Select(em => new EntrustMissionDTO
                {
                    EntrustMissionId = em.EntrustMissionId,
                    UserId = em.UserId,
                    ContentId = em.ContentId,
                    Title = em.Title,
                    Detail = em.Detail,
                    RewardPoints = em.RewardPoints,
                    Category = em.Category ?? "其他",
                    Deadline = em.Deadline,
                    Status = em.Status ?? "open",
                    IsUrgent = em.IsUrgent ?? false,
                    IsActive = em.IsActive,
                    CreatedAt = em.CreatedAt,
                    UpdatedAt = em.UpdatedAt,
                    Username = em.User.Username,
                    ImageUrl = em.User.ImageUrl,

                    BestContentId = em.BestContentId,
                    BestUserId = em.BestUserId,
                    SelectedAt = em.SelectedAt,
                })
                .FirstOrDefaultAsync();
        }

        // 發布新委託（基本維持，無需 progress 記錄）
        public async Task<ResultDTO> CreateEntrustMissionAsync(EntrustMissionDTO dto)
        {
            int userId = dto.UserId;
            DateTime now = DateTime.Now;

            // 基本驗證
            if (dto.RewardPoints <= 0)
                return new ResultDTO { Ok = false, Code = 400, Message = "賞金必須大於 0" };
            if (dto.ContentId <= 0)
                return new ResultDTO { Ok = false, Code = 400, Message = "必須選擇關聯內容" };
            if (dto.Deadline.HasValue && dto.Deadline < now)
                return new ResultDTO { Ok = false, Code = 400, Message = "截止時間不能是過去時間" };

            bool contentExists = await _context.Content.AnyAsync(c => c.ContentId == dto.ContentId);
            if (!contentExists)
                return new ResultDTO { Ok = false, Code = 404, Message = "指定的內容不存在" };

            bool userExists = await _context.User.AnyAsync(u => u.UserId == userId);
            if (!userExists)
                return new ResultDTO { Ok = false, Code = 404, Message = "使用者不存在" };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entrustMission = new EntrustMission
                {
                    UserId = userId,
                    ContentId = dto.ContentId,
                    Title = dto.Title,
                    Detail = dto.Detail ?? "無詳細說明",
                    RewardPoints = dto.RewardPoints,
                    Category = dto.Category,
                    Deadline = dto.Deadline,
                    Status = "open",
                    IsUrgent = dto.IsUrgent,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                    // Best* 欄位預設 null
                };

                _context.EntrustMission.Add(entrustMission);
                await _context.SaveChangesAsync(); // 先存取 ID

                // 預扣點數
                var reserveSuccess = await _pointReserveService.ReserveAsync(
                    userId,
                    dto.RewardPoints,
                    entrustMission.EntrustMissionId
                );

                if (!reserveSuccess)
                {
                    await tx.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "點數不足或預扣失敗" };
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 201,
                    Message = "委託任務建立成功",
                    Data = new { EntrustMissionId = entrustMission.EntrustMissionId }
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = $"建立失敗：{ex.Message}" };
            }
        }

        // 取消委託（退還預扣點數）
        public async Task<ResultDTO> CancelEntrustMissionAsync(int entrustMissionId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var mission = await _context.EntrustMission
                    .FirstOrDefaultAsync(em => em.EntrustMissionId == entrustMissionId && em.IsActive);

                if (mission == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

                if (mission.UserId != userId)
                    return new ResultDTO { Ok = false, Code = 403, Message = "只有任務發起者可以取消" };

                if (mission.Status == "completed")
                    return new ResultDTO { Ok = false, Code = 400, Message = "已結案的任務無法取消" };

                mission.Status = "cancelled";
                mission.UpdatedAt = DateTime.Now;

                // 退還預扣點數（如果還沒消耗）
                var releaseSuccess = await _pointReserveService.ReleaseAsync(mission.UserId, entrustMissionId);
                if (!releaseSuccess)
                {
                    await transaction.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "退還預扣點數失敗" };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO { Ok = true, Code = 200, Message = "委託任務已取消，預扣點數已退回" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = ex.Message };
            }
        }

        // 已移除的方法（可直接刪除或註解）
        // public async Task<ResultDTO> ClaimEntrustMissionAsync(...) { ... }          // 舊流程，移除
        // public async Task<ResultDTO> SubmitEntrustMissionAsync(...) { ... }        // 舊流程，移除
        // public async Task<ResultDTO> RejectEntrustMissionAsync(...) { ... }        // 舊流程，移除
    }
}