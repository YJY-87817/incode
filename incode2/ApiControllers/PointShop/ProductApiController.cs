using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Models;
using incode2.Services.PointShop;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static NuGet.Packaging.PackagingConstants;

namespace incode2.ApiControllers
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

        // GET: api/ProductApi
        [HttpGet]
        public async Task<ResultDTO<IEnumerable<ProductDTO>>> GetProduct()
        {
            return await _productService.GetProduct();
        }
    }
}
