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

    incodedatabaseContext _context; //宣告一個變數用來接收資料庫物件
    IMemoryCache _cache;
    public HomeController(ILogger<HomeController> logger, incodedatabaseContext context, IMemoryCache cache) //請系統注入1個物件給我用
    {
      _logger = logger;
      _context = context;
      _cache = cache; //建構函示取物件
    }

    //處理user操作view的函式，叫做action函式
    //()內沒有值，預設抓Index.cshtml(同名函式的.cshtml)
    public async Task<IActionResult> Index()
    {
      ViewBag.Fuen = "這是從HomeController用ViewBag.Fuen傳過來的"; //傳資料到view用
      Content c = await _context.Content.FirstOrDefaultAsync(x => x.UserId ==1);
      ViewBag.content = c;
      ViewBag.user = new SelectList(_context.Content.Select(x => x.Body).Distinct());
      ViewBag.JS = $"alert('貼文總數:{_context.Content.Count()}篇')";
      HttpContext.Session.SetString("Session鍵", "Session");

      //Index裡面做寫入
      MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions();
      cacheOptions.SetSlidingExpiration(TimeSpan.FromDays(1));
      cacheOptions.SetPriority(CacheItemPriority.Normal);
      _cache.Set<Content>("Cache鍵", c);

      CookieOptions cookieOptions = new CookieOptions();
      cookieOptions.Expires = DateTime.Now.AddYears(30);
      cookieOptions.Secure = true;
      cookieOptions.HttpOnly = true;
      Response.Cookies.Append("CookieKey", "Cookie值", cookieOptions);

      return View();
      //return View("Privacy"); //指定要回傳的view名稱改為Privacy
    }

    public IActionResult Privacy()
    {
      string? SessionValue = HttpContext.Session.GetString("Session鍵");
      if (SessionValue != null)
      {

      }
      //在Privacy做取出
      Content c = _cache.Get<Content>("Cookie鍵");
      if (c!=null)
      {

      }
      string CookieValue = Request.Cookies["CookieKey"];
      if(CookieValue != null)
      {
      }

      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    //GET是生畫面，必須存在，不能只有POST
    [HttpGet]
    public IActionResult Contact()
    {
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    //public IActionResult Contact(IFormCollection col)
    public IActionResult Contact([Bind("Name,Email,Phone")] ContactViewModel cvm)
    {
      if (ModelState.IsValid) //Server端驗證通過
      {
        //處理==>寫入資料庫
        return RedirectToAction("Index", "Home");
      }
      //return View(null);
      return View(cvm);
    }

  }
}
