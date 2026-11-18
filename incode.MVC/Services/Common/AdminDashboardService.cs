using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Common
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly incodedatabaseContext _context;

        public AdminDashboardService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<HomeViewModel> GetDashboardAsync()
        {
            var model = new HomeViewModel { displaycounts = new List<int>() };

            // 上方數量顯示用
            model.displaycounts.Add(await _context.User.CountAsync());
            model.displaycounts.Add(await _context.Content.CountAsync());
            model.displaycounts.Add((int)await _context.PointRecharge
                .Where(e => e.Status == "success").SumAsync(e => e.CashAmount));
            model.displaycounts.Add(await _context.Orders
                .Where(e => e.Status == OrderStatus.Pending).CountAsync());

            // 會員人數圖表
            model.userdisplay = await _context.User.AsNoTracking()
                .GroupBy(u => new { u.CreatedAt!.Value.Year, u.CreatedAt.Value.Month })
                .Select(g => new HomeUser
                {
                    createyear = g.Key.Year.ToString(),
                    createmonth = g.Key.Month.ToString(),
                    usercount = g.Count()
                })
                .OrderBy(x => x.createyear)
                .ThenBy(x => x.createmonth)
                .ToListAsync();

            // 發文數量排行榜
            model.userContentCounts = await _context.User.AsNoTracking()
                .Select(u => new UserContentCount
                {
                    UserName = u.Username,
                    ContentCount = _context.Content.Count(c => c.UserId == u.UserId)
                })
                .OrderByDescending(x => x.ContentCount)
                .Take(10)
                .ToListAsync();

            return model;
        }
    }
}
