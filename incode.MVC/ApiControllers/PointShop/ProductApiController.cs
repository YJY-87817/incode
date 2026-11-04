using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.PointShop
{
    [EnableCors("VueApp")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductApiController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductApiController(IProductService productService)
        {
            _productService = productService;
        }

        // 取得全部上架商品
        // GET: api/ProductApi
        [HttpGet]
        public async Task<ActionResult<ResultDTO<IEnumerable<ProductDTO>>>> GetProduct()
            => this.ToActionResult(await _productService.GetProductAsync());

        // 搜尋商品
        // GET: api/ProductApi/search?keyword=xxx
        [HttpGet("search")]
        public async Task<ActionResult<ResultDTO<IEnumerable<ProductDTO>>>> SearchProduct([FromQuery] string? keyword)
            => this.ToActionResult(await _productService.SearchProductAsync(keyword ?? ""));
    }
}
