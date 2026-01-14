using incode2.DTO;
using incode2.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [HttpGet("{UserId}")]
        public async Task<IEnumerable<OrderDTO>> GetOrder(int UserId)
        {
            var AllOrder = await _context.Orders
                .Include(o => o.Orderdetail) // 載入明細
                .Where(o => o.UserId == UserId)
                .ToListAsync();

            return AllOrder.Select(o => new OrderDTO
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                TotalPoint = o.TotalPoint,
                Remark = o.Remark,
                Status = o.Status,
                //orderdetails = o.Orderdetail.ToList()
            });
        }
    }
}
