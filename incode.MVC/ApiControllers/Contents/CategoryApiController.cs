using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Contents
{
  [ApiController]
  [Route("api/[controller]")]
  public class CategoryApiController : ControllerBase
  {
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryApiController> _logger;

    public CategoryApiController(ICategoryService categoryService, ILogger<CategoryApiController> logger)
    {
      _categoryService = categoryService;
      _logger = logger;
    }

    // 取得所有分類（包含分類統計信息）
    // GET api/CategoryApi
    // Response: [{ id, parentCategoryId, name, description, topicCount, postCount }]
    //   - topicCount: 該分類下的主文數量；postCount: 主文 + 留言數量（不含已刪除的內容）
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
      try
      {
        return Ok(await _categoryService.GetCategoriesAsync());
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "取得論壇分類失敗");
        return StatusCode(500, new { message = "伺服器錯誤，請稍後再試" });
      }
    }

    // 新增分類
    // POST api/CategoryApi
    // Body: { "parentCategoryId": null（可選）, "name": "分類名稱（必填，100 字內）", "description": "可選，200 字內" }
    // 指定 parentCategoryId 時會驗證父分類是否存在
    [HttpPost]
    [Authorize] // 會員可新增分類，但需登入
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto request)
    {
      var result = await _categoryService.CreateCategoryAsync(request);
      if (result == null)
        return BadRequest(new { message = "父分類不存在" });

      return Ok(result);
    }
  }
}
