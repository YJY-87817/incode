using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;
using System.Linq;

namespace incode2.Controllers
{
    public class CommunityController : Controller
    {
        private readonly incodedatabaseContext _context;

        public CommunityController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: CommunityVM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var CommunityViewModels = await _context.User.Select(u => new CommunityViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                
                FollowerId = u.FollowFollowee.Select(f => f.FolloweeId).FirstOrDefault(),
                FolloweeId = u.FollowFollower.Select(f => f.FollowerId).FirstOrDefault(),

                Experience = _context.UserStatus.Where(s => s.UserId == u.UserId)
                .Select(s => s.Experience).FirstOrDefault(), // 如果沒資料會回傳 0
                Rank = _context.UserStatus.Where(s => s.UserId == u.UserId)
                .Select(s => s.Rank).FirstOrDefault()
            })
            .ToListAsync();

            return View(CommunityViewModels);
        }

        // GET: CVM/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

         var CommunityViewModels = await _context.User
        .Where(u => u.UserId == id)
        .Select(u => new CommunityViewModel
        {
            Username = u.Username,
            Experience = _context.UserStatus.Where(s => s.UserId == u.UserId).Select(s => s.Experience).FirstOrDefault(),
            Rank = _context.UserStatus.Where(s => s.UserId == u.UserId).Select(s => s.Rank).FirstOrDefault()
            }).FirstOrDefaultAsync();
                
            if (CommunityViewModels == null)
            {
                return NotFound();
            }

            CommunityViewModels.SocialLinks = await _context.Follow
        .Where(f => f.FollowerId == id) // 假設這是找他追蹤的人
        .Select(f => new CommunityViewModel
        {
            FolloweeId = f.FolloweeId,
            FollowerId = f.FollowerId,
            Username = _context.User.Where(u => u.UserId == f.FolloweeId).Select(u => u.Username).FirstOrDefault()
            // 這裡可以再串接其他資料
        }).ToListAsync();

            return View(CommunityViewModels);
        }

    }
}
