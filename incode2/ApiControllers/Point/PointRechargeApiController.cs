using incode2.DTO;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointRechargeApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly EcpayService _ecpay;
        private readonly IPointLogService _pointLogService;

        public PointRechargeApiController(
            incodedatabaseContext context, 
            EcpayService ecpay, 
            IPointLogService pointLogService   )
        {
            _context = context;
            _ecpay = ecpay;
            _pointLogService = pointLogService;
        }


        //AI 綠界儲值Api
        [HttpPost("recharge")]
        [Authorize]
        public async Task<IActionResult> CreateRecharge(
       [FromBody] PointRechargeDTO dto)
        {
            int userId = User.GetId();

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
        public async Task<IActionResult> EcpayReturn()
        {
            var form = Request.Form;

            // 1. 驗證 CheckMacValue
            bool isValid = _ecpay.ValidateCheckMacValue(form);

            if (!isValid)
            {
                // 記錄失敗原因（方便 debug）
                Console.WriteLine("CheckMacValue 驗證失敗");
                Console.WriteLine("收到的 MAC: " + form["CheckMacValue"]);
                // 為了不讓綠界一直重試，通常仍回 1|OK
                return Content("1|OK");
            }

            Console.WriteLine("CheckMacValue 驗證通過");

            var rtnCode = form["RtnCode"].ToString();
            var orderNo = form["MerchantTradeNo"].ToString();
            var tradeNo = form["TradeNo"].ToString();

            // 找訂單
            var recharge = await _context.PointRecharge
                .FirstOrDefaultAsync(x => x.OrderNo == orderNo);

            if (recharge == null)
            {
                Console.WriteLine($"找不到訂單: {orderNo}");
                return Content("1|OK");  // 綠界建議一律回 1|OK
            }

            // 防重複處理
            if (recharge.Status == "success")
            {
                // 額外檢查：是否真的有對應 point_log？
                var hasLog = await _context.PointLog
                    .AnyAsync(l => l.SourceType == "point_recharge" && l.SourceId == recharge.PointRechargeId);

                if (hasLog)
                {
                    Console.WriteLine($"訂單已處理過且有 log: {orderNo}");
                    return Content("1|OK");
                }
                else
                {
                    Console.WriteLine($"訂單顯示 success 但無 log，強制重新加點: {orderNo}");
                    // 強制重新加點
                    recharge.Status = "pending";  // 暫時還原
                }
            }

            if (recharge.Status == "failed")
            {
                Console.WriteLine($"訂單已 failed，不處理: {orderNo}");
                return Content("1|OK");
            }

            // 根據 RtnCode 處理
            if (rtnCode == "1")
            {
                recharge.PaymentTradeNo = tradeNo;
                recharge.Status = "success";

                try
                {
                    Console.WriteLine($"準備呼叫加點 - RechargeId: {recharge.PointRechargeId}");
                    await _pointLogService.AddPointsFromRechargeAsync(recharge);
                    return Content("1|OK");
                }
                catch (Exception ex)
                {
                    recharge.Status = "failed";
                    await _context.SaveChangesAsync(); // 還是存失敗狀態
                    Console.WriteLine($"加點失敗: {ex.Message}\n{ex.StackTrace}");
                    return Content("1|OK");
                }
            }
            else
            {
                recharge.Status = "failed";
                Console.WriteLine($"付款失敗，RtnCode = {rtnCode}");
            }

            // 更新訂單
            _context.PointRecharge.Update(recharge);
            await _context.SaveChangesAsync();

            // 記錄完整通知內容（開發時很有用）
            Console.WriteLine("===== 綠界回呼完成 =====");
            foreach (var key in form.Keys)
            {
                Console.WriteLine($"{key} = {form[key]}");
            }

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
