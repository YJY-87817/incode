using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Users
{
    public class StatusService : IStatusService
    {
        private readonly incodedatabaseContext _context;

        public StatusService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<UserStatus> GetUserStatusAsync(int userId)
        {
            var status = await _context.UserStatus.FindAsync(userId);

            if (status == null)
            {
                // 若無狀態資料，回傳預設值 (不寫入資料庫，僅顯示用)
                return new UserStatus
                {
                    UserId = userId,
                    Experience = 0,
                    Rank = 1,
                    Points = 0
                };
            }

            return status;
        }
    }
}

