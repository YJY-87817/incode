using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;
using System.Linq;
using incode2.ViewModel;

namespace incode2.Controllers
{
    public class AVMController : Controller
    {
        private readonly incodedatabaseContext _context;

        public AVMController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: AdminVM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var AdminViewModels = await _context.User.Select(u => new AdminViewModel
            {
                UserId = u.UserId,
                Account = u.Account,
                Username = u.Username,
                IsActive = u.IsActive,
               
                Logintime = _context.UserLogin.Where(s => s.UserId == u.UserId)
                .Select(s => s.Logintime).FirstOrDefault(),
                Location = _context.UserLogin.Where(s => s.UserId == u.UserId)
                .Select(s => s.Location).FirstOrDefault(),
            })
            .ToListAsync();

            return View(AdminViewModels);
        }

        // GET: CVM/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var AVM = await _context.User
                .Where(m => m.UserId == id) // 先篩選
                .Select(u => new AdminViewModel
                {
                    UserId = u.UserId,
                    Account = u.Account,
                    Username = u.Username,                   
                })
                .FirstOrDefaultAsync(); ;
            if (AVM == null)
            {
                return NotFound();
            }

            return View(AVM);
        }

    }
}
