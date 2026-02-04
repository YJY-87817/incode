using incode2.Models;
using incode2.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Threading.Tasks;

namespace incode2.Controllers
{
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
            displaydata.displaycounts.Add(await _context.User.CountAsync());
            displaydata.displaycounts.Add(await _context.Content.CountAsync());
            displaydata.displaycounts.Add(await _context.EntrustMission.CountAsync());
            displaydata.displaycounts.Add(await _context.Orders.Where(e =>e.Status == "«Ý³B²z").CountAsync());
            
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

            return View(displaydata);
        }
           

    }
}
