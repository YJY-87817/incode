using incode2.DTO;
using incode2.DTO.PointShop;
using incode2.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // 取得UserId的訂單資料
        // GET: api/OrderApi/2
        [HttpGet("{UserId}")]
        public IEnumerable<OrderDTO> GetOrder(int UserId)
        {
            return _context.Orders.Where(e => e.UserId == UserId).Select(e => new OrderDTO
            {
                UserId = e.UserId,
                TotalPoint = e.TotalPoint,
                Remark = e.Remark,
                Status = e.Status,
                OrderId = e.OrderId,
            });
        }

        // 取得OrderId的訂單資料
        // GET: api/Orderdetail/2
        [Route("/api/OrderDetail/{OrderId}")]
        [HttpGet("{OrderId}")]
        public IEnumerable<OrderDetailDTO> GetOrderdetail(int OrderId)
        {
            return _context.Orderdetail.Include(p => p.Product).Where(e => e.OrderId == OrderId).Select(e => new OrderDetailDTO
            {
                OrderdetailId = e.OrderdetailId,
                OrderId = e.OrderId,
                ProductId = e.ProductId,
                ProductName = e.Product.Name,
                ImageName = e.Product.ImageName,
                Quantity = e.Quantity,
                UnitPointAtPurchase = e.UnitPointAtPurchase,
                RequiresShipping = e.RequiresShipping,
            });
        }

        // 取得ShippedDetailId的訂單資料
        // GET: api/ShippedDetail/2
        [Route("/api/ShippedDetail/{OrderdetailId}")]
        [HttpGet("{OrderdetailId}")]
        public IEnumerable<ShippedDTO> ShippedDetail(int OrderdetailId)
        {
            return _context.ShippedDetail.Include(p => p.Orderdetail).ThenInclude(o => o.Product).Where(e => e.OrderdetailId == OrderdetailId).Select(e => new ShippedDTO
            {
                ShippedDate = e.ShippedDate,
                ArrivalDate = e.ArrivalDate,
                IsCompleted = e.IsCompleted,
                IsFailed = e.IsFailed,
                Remark = e.Remark,
                OrderdetailId = e.OrderdetailId,
                ProductId = e.Orderdetail.ProductId,
                ProductName = e.Orderdetail.Product.Name,
            });
        }

        // 新增訂單
        //  POST: api/OrderApi/2
        [Route("/api/OrderApi/{UserId}")]
        [HttpPost("{UserId}")]
        public async Task<ResultDTO> PutNewOrder(int UserId, CreateOrderRequest NewOrder)
        {
            if (UserId != NewOrder.UserId)
            {
                return new ResultDTO { ok = false, code = 400 }; // 400
            }

            return new ResultDTO { ok = false, code = 404 };

        }

    }
}
