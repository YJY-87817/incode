using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
{
    public class EntrustMissionService : IEntrustMissionService
    {
        private readonly incodedatabaseContext _context;
        public EntrustMissionService(incodedatabaseContext context) => _context = context;

        public async Task<IEnumerable<EntrustMissionDTO>> GetAllEntrustMissionAsync()
        {
            return await _context.EntrustMission
                .Where(em=>em.IsActive && em.Status == "open")
                .Include(em=>em.User)
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
                MaxParticipants = em.MaxParticipants,
                CurrentParticipants = em.CurrentParticipants ?? 0,
                Status = em.Status ?? "open",
                IsUrgent = em.IsUrgent ?? false,
                IsActive = em.IsActive,
                CreatedAt = em.CreatedAt,
                UpdatedAt = em.UpdatedAt,
                RequesterName = em.User.Username ?? "匿名影友",
                })
                .OrderByDescending(em => em.IsUrgent)      // 急件優先
                .ThenByDescending(em => em.CreatedAt)      // 新發布優先
                .ToListAsync();
        }

        public async Task<EntrustMissionDTO?> GetEntrustMissionByIdAsync(int id)
        {
            return await _context.EntrustMission
                .Where(em => em.EntrustMissionId == id && em.IsActive)  // 建議加上 IsActive 過濾
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
                    MaxParticipants = em.MaxParticipants,
                    CurrentParticipants = em.CurrentParticipants ?? 0,
                    Status = em.Status ?? "其他",
                    IsUrgent = em.IsUrgent ?? false,
                    IsActive = em.IsActive,
                    CreatedAt = em.CreatedAt,
                    UpdatedAt = em.UpdatedAt,
                    RequesterName =  em.User.Username ?? "匿名影友"
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ResultDTO> CreateEntrustMissionAsync(EntrustMissionDTO dto)
        {
            // 驗證必要欄位
            if (dto.ContentId <= 0)
            {
                return new ResultDTO { Ok = false, Code = 400, Message = "必須選擇關聯內容（ContentId）" };
            }

            if (dto.RewardPoints <= 0)
            {
                return new ResultDTO { Ok = false, Code = 400, Message = "賞金必須大於 0" };
            }

            // 查詢關聯內容（用 AsNoTracking 提升效能）
            var content = await _context.Content
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContentId == dto.ContentId);

            if (content == null)
            {
                return new ResultDTO { Ok = false, Code = 404, Message = "指定的內容（Content）不存在" };
            }

            // 強制 Title = Content.Title（忽略前端傳入的 Title）
            string finalTitle = content.Title;

            var em = new EntrustMission
            {
                UserId = dto.UserId,
                ContentId = dto.ContentId,
                Title = finalTitle,                         // ← 強制同步
                Detail = dto.Detail ?? "無詳細說明",
                RewardPoints = dto.RewardPoints,
                Category = dto.Category ?? "其他",
                Deadline = dto.Deadline,
                MaxParticipants = dto.MaxParticipants > 0 ? dto.MaxParticipants : 1,
                CurrentParticipants = 0,
                Status = "open",
                IsUrgent = dto.IsUrgent ?? false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.EntrustMission.Add(em);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 201,
                Message = "委託任務建立成功",
                Data = new
                {
                    EntrustMissionId = em.EntrustMissionId,
                    Title = em.Title  // 回傳最終標題（即 Content.Title）
                }
            };
        }

        public async Task<ResultDTO> UpdateEntrustMissionAsync(int id, EntrustMissionDTO dto)
        {
            var em = await _context.EntrustMission.FindAsync(id);
            if (em == null) return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

            // 只更新允許修改的欄位
            em.Detail = dto.Detail ?? em.Detail;
            em.RewardPoints = dto.RewardPoints;
            em.Category = dto.Category ?? em.Category;
            em.Deadline = dto.Deadline ?? em.Deadline;
            em.MaxParticipants = dto.MaxParticipants > 0 ? dto.MaxParticipants : em.MaxParticipants;
            em.IsUrgent = dto.IsUrgent ?? em.IsUrgent;
            em.Status = dto.Status ?? em.Status;
            em.IsActive = true;

            // 禁止修改的核心欄位
            // em.UserId = ... 不可改
            // em.CreatedAt = ... 不可改
            em.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204, Message = "更新成功" };
        }

        public async Task<ResultDTO> DeleteEntrustMissionAsync(int id)
        {
            var em = await _context.EntrustMission.FindAsync(id);
            if (em == null)
            {
                return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };
            }

            // 建議改成軟刪除（更安全）
            em.IsActive = false;
            em.UpdatedAt = DateTime.UtcNow;

            //_context.EntrustMission.Remove(em);  // 如果堅持硬刪除
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204, Message = "委託任務已下架" };
        }
    }
}
