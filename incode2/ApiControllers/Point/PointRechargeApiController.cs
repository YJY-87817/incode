using incode2.Models;
using incode2.Services.Point;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using incode2.DTO;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointRechargeApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly EcpayService _ecpay;
        private readonly IPointLogService _pointLogService;

        public PointRechargeApiController(incodedatabaseContext context, EcpayService ecpay, IPointLogService pointLogService   )
        {
            _context = context;
            _ecpay = ecpay;
            _pointLogService = pointLogService;
        }

        //AI 綠界儲值Api
        [HttpPost("recharge")]
        public async Task<IActionResult> CreateRecharge(
       [FromBody] PointRechargeDTO dto)
        {
            var userId = 1; // 之後從 JWT 取

            var order = new PointRecharge
            {
                UserId = userId,
                OrderNo = "PR" + DateTime.Now.Ticks,
                CashAmount = dto.CashAmount,
                PointsGranted = dto.CashAmount * 10,
                PaymentMethod = dto.PaymentMethod,
                Status = "pending"
            };

            _context.PointRecharge.Add(order);
            await _context.SaveChangesAsync();

            var ecpayParams = _ecpay.CreatePayment(order);

            return Ok(ecpayParams);
        }

        //AI 付款完成回呼 API（ECPay ReturnURL）
        [HttpPost("ecpay/return")]
        public async Task<IActionResult> EcpayReturn([FromForm] IFormCollection form)
        {
            // 1️⃣ 基本驗證
            if (form["RtnCode"] != "1")
                return Content("0|FAIL");

            var orderNo = form["MerchantTradeNo"].ToString();
            var tradeNo = form["TradeNo"].ToString();

            // 2️⃣ 找訂單
            var recharge = await _context.PointRecharge
       .FirstOrDefaultAsync(x => x.OrderNo == orderNo);

            if (recharge == null)
                return Content("0|ORDER_NOT_FOUND");

            // 3️⃣ 防止重複加點（超重要）
            if (recharge.Status == "success")
                return Content("1|OK");

            // 4️⃣ 更新訂單
            recharge.PaymentTradeNo = tradeNo;

            await _pointLogService.AddPointsFromRechargeAsync(recharge);

            return Content("1|OK");
        }


        // GET: api/PointRechargeApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointRecharge>>> GetPointRecharge()
        {
            return await _context.PointRecharge.ToListAsync();
        }

        // GET: api/PointRechargeApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PointRecharge>> GetPointRecharge(int id)
        {
            var pointRecharge = await _context.PointRecharge.FindAsync(id);

            if (pointRecharge == null)
            {
                return NotFound();
            }

            return pointRecharge;
        }

        // PUT: api/PointRechargeApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPointRecharge(int id, PointRecharge pointRecharge)
        {
            if (id != pointRecharge.PointRechargeId)
            {
                return BadRequest();
            }

            _context.Entry(pointRecharge).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PointRechargeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/PointRechargeApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<PointRecharge>> PostPointRecharge(PointRecharge pointRecharge)
        //{
        //    _context.PointRecharge.Add(pointRecharge);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetPointRecharge", new { id = pointRecharge.PointRechargeId }, pointRecharge);
        //}

        [HttpPost]
        [Route("api/payment/ecpay/return")]
        public async Task<IActionResult> ECPayReturn([FromForm] ECPayReturnModel model)
        {
            // 1. 驗證回傳碼
            if (model.RtnCode != 1)
                return BadRequest("付款失敗");

            // 2. 找到訂單
            var order = await _context.PointRecharge
                .FirstOrDefaultAsync(o => o.OrderNo == model.MerchantTradeNo);

            if (order == null)
                return NotFound("訂單不存在");

            if (order.Status == "success")
                return Ok("已入帳"); // 防重送

            // 3. 驗證金額
            if (order.CashAmount != model.TradeAmt)
                return BadRequest("金額不符");

            // 4. 計算新的餘額
            var lastLog = await _context.PointLog
                .Where(l => l.UserId == order.UserId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.PointLogId)
                .Select(l => (int?)l.BalanceAfter)
                .FirstOrDefaultAsync();

            var newBalance = (lastLog ?? 0) + order.PointsGranted;

            using var tx = await _context.Database.BeginTransactionAsync();

            // 5. 新增 PointLog
            _context.PointLog.Add(new PointLog
            {
                UserId = order.UserId,
                PointTypeDefinitionId = 4, // recharge
                SourceType = "point_recharge",
                SourceId = order.PointRechargeId,
                ChangeAmount = order.PointsGranted,
                BalanceAfter = newBalance,
                Status = "success",
                CreatedAt = DateTime.UtcNow
            });

            // 6. 更新訂單
            order.Status = "success";
            order.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // 7. 回傳綠界需要的字串
            return Content("1|OK");
        }

        // 回傳模型
        public class ECPayReturnModel
        {
            public string MerchantID { get; set; }
            public string MerchantTradeNo { get; set; }
            public int RtnCode { get; set; }
            public string RtnMsg { get; set; }
            public string TradeNo { get; set; }
            public decimal TradeAmt { get; set; }
            public string PaymentDate { get; set; }
            public string PaymentType { get; set; }
        }


        // DELETE: api/PointRechargeApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePointRecharge(int id)
        {
            var pointRecharge = await _context.PointRecharge.FindAsync(id);
            if (pointRecharge == null)
            {
                return NotFound();
            }

            _context.PointRecharge.Remove(pointRecharge);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PointRechargeExists(int id)
        {
            return _context.PointRecharge.Any(e => e.PointRechargeId == id);
        }
    }
}
