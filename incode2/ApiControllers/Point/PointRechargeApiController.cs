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
        public async Task<IActionResult> EcpayReturn([FromForm] IFormCollection form)
        {
            var rtnCode = form["RtnCode"].ToString();
            var orderNo = form["MerchantTradeNo"].ToString();
            var tradeNo = form["TradeNo"].ToString();

            // 找訂單
            var recharge = await _context.PointRecharge
                .FirstOrDefaultAsync(x => x.OrderNo == orderNo);

            if (recharge == null)
                return Content("0|ORDER_NOT_FOUND");

            // 防重複加點
            if (recharge.Status == "success" || recharge.Status == "failed")
                return Content("1|OK");

            // CheckMacValue 驗證
            var receivedMac = form["CheckMacValue"].ToString();
            var data = form.Keys
                           .Where(k => k != "CheckMacValue")
                           .ToDictionary(k => k, k => form[k].ToString());
            var calculatedMac = _ecpay.GenerateCheckMacValue(data);
            if (receivedMac != calculatedMac)
                return Content("0|CHECKMAC_FAIL");

            // 根據 RtnCode 更新訂單
            switch (rtnCode)
            {
                case "1":
                    recharge.PaymentTradeNo = tradeNo;

                    try
                    {
                        await _pointLogService.AddPointsFromRechargeAsync(recharge);
                    }
                    catch
                    {
                        recharge.Status = "failed";
                    }
                    break;

                case "2": // 付款失敗
                default:
                    recharge.Status = "failed";
                    break;
            }

            // 更新訂單
            _context.PointRecharge.Update(recharge);
            await _context.SaveChangesAsync();

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
