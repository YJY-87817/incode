using incode2.DTO;
using incode2.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace incode2.ApiControllers
{
    [EnableCors("VueApp")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public ProductApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/ProductApi
        [HttpGet]
        public async Task<IEnumerable<ProductDTO>> GetProduct()
        {
            return _context.Product.Where(e => e.OnSale == true).Select(e => new ProductDTO
            {
                ProductId = e.ProductId,
                Name = e.Name,
                Description = e.Description,
                ImageName = e.ImageName,
                Type = e.Type,
                IsPhysical = e.IsPhysical,
                Point = e.Point,
                Stock = e.Stock,
            });
        }
    }
}
