using incode2.DTO;
using incode2.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace incode2.ApiControllers
{
    [EnableCors("VueApp")]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public OrderApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/OrderApi
        [HttpGet]
        public async Task<IEnumerable<OrderDTO>> GetOrder(int OrderId)
        {

            return _context.Product.Select(e => new OrderDTO
            {
               
            });
        }
    }
}
