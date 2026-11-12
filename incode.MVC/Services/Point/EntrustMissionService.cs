using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point
{
    public class EntrustMissionService : IEntrustMissionService
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;
        private readonly IPointReserveService _pointReserveService;
        private readonly ILogger<EntrustMissionService> _logger;

        public EntrustMissionService(
            incodedatabaseContext context,
            IPointLogService pointLogService,
            IPointReserveService pointReserveService,
            ILogger<EntrustMissionService> logger)
        {
            _context = context;
            _pointLogService = pointLogService;
            _pointReserveService = pointReserveService;
            _logger = logger;
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

            // 只能掛在自己的主題文章（不是回覆）上：不是自己的文章一律 404。
            // 選最佳解答時以「回覆所屬的主題」比對委託的 ContentId，掛在回覆上的委託永遠無法結案
            var content = await _context.Content
                .AsNoTracking()
                .Where(c => c.ContentId == dto.ContentId && c.IsActive == 1)
                .Select(c => new { c.UserId, c.ParentContentId })
                .FirstOrDefaultAsync();
            if (content == null || content.UserId != userId)
                return new ResultDTO { Ok = false, Code = 404, Message = "指定的內容不存在" };
            if (content.ParentContentId.HasValue)
                return new ResultDTO { Ok = false, Code = 400, Message = "委託只能建立在自己的主題文章上" };

            bool hasMission = await _context.EntrustMission
                .AnyAsync(em => em.ContentId == dto.ContentId && em.IsActive && em.Status != "cancelled" && em.Status != "expired");
            if (hasMission)
                return new ResultDTO { Ok = false, Code = 409, Message = "這篇文章已經有委託" };

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
                _logger.LogError(ex, "建立委託失敗 - UserId: {UserId}", userId);
                return new ResultDTO { Ok = false, Code = 500, Message = "建立委託失敗，請稍後再試" };
            }
        }

        // 取消委託（退還預扣點數）
        // 委託人取消委託：狀態改為 cancelled 並退還預扣點數（同一交易）。
        // 只有「徵求中」且還沒有其他會員回覆的委託可以取消，避免拿到解答後取消、拿回賞金
        public Task<ResultDTO> CancelEntrustMissionAsync(int entrustMissionId, int userId)
            => CancelAsync(entrustMissionId, userId, byAdmin: false);

        // 後台管理員取消委託：不限發起人、已有回覆或已下架也可以取消（例如違規內容），同樣退還預扣點數
        public async Task<ResultDTO> CancelEntrustMissionByAdminAsync(int entrustMissionId)
        {
            var ownerId = await _context.EntrustMission
                .Where(em => em.EntrustMissionId == entrustMissionId)
                .Select(em => (int?)em.UserId)
                .FirstOrDefaultAsync();
            if (ownerId == null)
                return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

            return await CancelAsync(entrustMissionId, ownerId.Value, byAdmin: true);
        }

        // userId：委託人（會員取消時為登入者，後台取消時為委託的發起人）
        private async Task<ResultDTO> CancelAsync(int entrustMissionId, int userId, bool byAdmin)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 與選擇最佳解答鎖定同一位委託人，避免同時取消與結案造成退款又發獎勵
                await _pointLogService.LockUsersAsync(userId);

                var mission = await _context.EntrustMission
                    .FirstOrDefaultAsync(em => em.EntrustMissionId == entrustMissionId && (em.IsActive || byAdmin));

                // 不存在或不是自己發起的委託一律 404
                if (mission == null || mission.UserId != userId)
                    return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

                if (mission.Status == "cancelled")
                    return new ResultDTO { Ok = false, Code = 409, Message = "此委託已經取消" };

                if (mission.Status != "open" || mission.BestContentId.HasValue)
                    return new ResultDTO { Ok = false, Code = 409, Message = "已結案的委託無法取消" };

                if (!byAdmin && await HasRepliesFromOthersAsync(mission.ContentId, userId))
                    return new ResultDTO { Ok = false, Code = 409, Message = "已有其他會員回覆，無法取消，請從回覆中選出最佳解答" };

                mission.Status = "cancelled";
                mission.UpdatedAt = DateTime.Now;

                // 退還預扣點數
                var releaseSuccess = await _pointReserveService.ReleaseAsync(mission.UserId, entrustMissionId);
                if (!releaseSuccess)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("取消委託時找不到可退還的預扣點數 - EntrustMissionId: {Id}", entrustMissionId);
                    return new ResultDTO { Ok = false, Code = 409, Message = "找不到可退還的預扣點數，請聯繫客服" };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = $"委託已取消，{mission.RewardPoints} 點已退回",
                    Data = new { mission.EntrustMissionId, mission.Status, RefundedPoints = mission.RewardPoints }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "取消委託失敗 - EntrustMissionId: {Id}", entrustMissionId);
                return new ResultDTO { Ok = false, Code = 500, Message = "取消委託失敗，請稍後再試" };
            }
        }

        // 討論串（委託的主文 rootContentId 底下所有層的回覆）中是否有其他會員仍存在的回覆
        private async Task<bool> HasRepliesFromOthersAsync(int rootContentId, int ownerId)
        {
            var frontier = new List<int> { rootContentId };
            var visited = new HashSet<int> { rootContentId };

            while (frontier.Count > 0)
            {
                var children = await _context.Content
                    .AsNoTracking()
                    .Where(c => c.ParentContentId.HasValue && frontier.Contains(c.ParentContentId.Value))
                    .Select(c => new { c.ContentId, c.UserId, c.IsActive })
                    .ToListAsync();

                if (children.Any(c => c.IsActive == 1 && c.UserId != ownerId))
                    return true;

                frontier = children.Select(c => c.ContentId).Where(visited.Add).ToList();
            }

            return false;
        }

        // 截止後還有回覆的委託，留給委託人選最佳解答的時間；過了就退款並結案
        public static readonly TimeSpan ExpiryGracePeriod = TimeSpan.FromDays(7);

        // 過期處理（由 EntrustMissionExpiryWorker 定期呼叫）：徵求中、有截止日期且已過期的委託
        //   - 討論串沒有其他會員回覆 → 截止後立即結案
        //   - 已有回覆 → 截止後再等 ExpiryGracePeriod，委託人仍沒選最佳解答才結案
        // 結案：狀態改為 expired、退還預扣點數。沒有截止日期的委託不會過期
        public async Task<int> ExpireOverdueMissionsAsync(DateTime now)
        {
            var candidates = await _context.EntrustMission
                .AsNoTracking()
                .Where(em => em.Status == "open" && em.BestContentId == null
                    && em.Deadline.HasValue && em.Deadline.Value < now)
                .Select(em => new { em.EntrustMissionId, em.UserId })
                .ToListAsync();

            int expired = 0;
            foreach (var candidate in candidates)
            {
                try
                {
                    if (await ExpireAsync(candidate.EntrustMissionId, candidate.UserId, now))
                        expired++;
                }
                catch (Exception ex)
                {
                    // 一筆失敗不影響其他委託，下次執行會再處理
                    _logger.LogError(ex, "委託過期處理失敗 - EntrustMissionId: {Id}", candidate.EntrustMissionId);
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }
            }

            return expired;
        }

        private async Task<bool> ExpireAsync(int entrustMissionId, int userId, DateTime now)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // 與選擇最佳解答 / 取消鎖定同一位委託人，鎖定後重新讀取，避免委託人同時選了最佳解答又被退款
            await _pointLogService.LockUsersAsync(userId);

            var mission = await _context.EntrustMission
                .FirstOrDefaultAsync(em => em.EntrustMissionId == entrustMissionId);
            if (mission == null || mission.UserId != userId || mission.Status != "open"
                || mission.BestContentId.HasValue || !mission.Deadline.HasValue || mission.Deadline.Value >= now)
                return false;

            bool inGracePeriod = mission.Deadline.Value + ExpiryGracePeriod > now;
            if (inGracePeriod && await HasRepliesFromOthersAsync(mission.ContentId, userId))
                return false;

            mission.Status = "expired";
            mission.UpdatedAt = now;

            if (!await _pointReserveService.ReleaseAsync(mission.UserId, entrustMissionId, "委託過期退款"))
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("委託過期時找不到可退還的預扣點數 - EntrustMissionId: {Id}", entrustMissionId);
                return false;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("委託已過期並退款 - EntrustMissionId: {Id}, Points: {Points}", entrustMissionId, mission.RewardPoints);
            return true;
        }

        // 已移除的方法（可直接刪除或註解）
        // public async Task<ResultDTO> ClaimEntrustMissionAsync(...) { ... }          // 舊流程，移除
        // public async Task<ResultDTO> SubmitEntrustMissionAsync(...) { ... }        // 舊流程，移除
        // public async Task<ResultDTO> RejectEntrustMissionAsync(...) { ... }        // 舊流程，移除
    }
}