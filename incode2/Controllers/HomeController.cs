using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Threading.Tasks;

namespace incode2.Controllers
{    
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]   
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        incodedatabaseContext _context; 

        public HomeController(incodedatabaseContext context) 
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var displaydata = new HomeViewModel();
            displaydata.displaycounts = new List<int>();
            // 上方數量顯示用
            displaydata.displaycounts.Add(await _context.User.CountAsync());
            displaydata.displaycounts.Add(await _context.Content.CountAsync());
            displaydata.displaycounts.Add((int)await _context.PointRecharge.Where(e => e.Status == "success").SumAsync(e => e.CashAmount));
            displaydata.displaycounts.Add(await _context.Orders.Where(e =>e.Status == "待處理").CountAsync());
            // 會員人數圖表
            displaydata.userdisplay = await _context.User
                .GroupBy(u => new { u.CreatedAt.Value.Year, u.CreatedAt.Value.Month })
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
            displaydata.userContentCounts = await _context.User
                .Select(u => new UserContentCount
                {
                    UserName = u.Username,
                    ContentCount = _context.Content.Count(c => c.UserId == u.UserId)
                }).OrderByDescending(x => x.ContentCount)
                .Take(10)
                .ToListAsync();

            return View(displaydata);
        }
           

    }
}
