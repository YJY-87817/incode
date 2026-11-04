using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode.MVC.ApiControllers.Point
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
            if (!_ecpay.ValidateCheckMacValue(form))
            {
                Console.WriteLine("CheckMacValue 驗證失敗");
                return Content("0|Fail");
            }

            var rtnCode = form["RtnCode"].ToString();
            var orderNo = form["MerchantTradeNo"].ToString();
            var tradeNo = form["TradeNo"].ToString();

            // 2. 找到對應訂單
            var recharge = await _context.PointRecharge
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderNo == orderNo);

            if (recharge == null)
            {
                Console.WriteLine($"找不到訂單: {orderNo}");
                return Content("1|OK");  // 避免綠界一直重試
            }

            // 3. 防重複處理
            if (recharge.Status == "success")
            {
                Console.WriteLine($"訂單已處理過: {orderNo}");
                return Content("1|OK");
            }

            // 4. 根據 RtnCode 決定流程
            if (rtnCode == "1")
            {
                // 付款成功
                recharge.PaymentTradeNo = tradeNo;
                recharge.Status = "success";

                try
                {
                    await _pointLogService.AddPointsFromRechargeAsync(recharge);
                    _context.PointRecharge.Update(recharge);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"儲值成功，已加點 - OrderNo: {orderNo}");
                    return Content("1|OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加點失敗: {ex.Message}");
                    return Content("0|Fail");  // 讓綠界重試
                }
            }
            else
            {
                // 付款失敗、取消、ATM 尚未入帳等情況
                // 這裡建議記錄失敗狀態，方便之後對帳或通知使用者
                recharge.Status = "failed";  // 或 "cancelled"，看你定義
                recharge.PaymentTradeNo = tradeNo;  // 保留綠界交易編號

                try
                {
                    _context.PointRecharge.Update(recharge);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"付款失敗，已記錄 - OrderNo: {orderNo}, RtnCode: {rtnCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新失敗狀態失敗: {ex.Message}");
                    // 這裡可以選擇不拋錯，還是回 1|OK 給綠界
                }

                // 無論如何都要告訴綠界「我收到通知了」
                return Content("1|OK");
            }
        }

        // 儲值紀錄的查詢 / 修改 / 刪除只在後台 (PointRechargeController) 提供；
        // 原本這裡的 GET / PUT / DELETE 沒有登入檢查，任何人都能讀取或竄改所有人的儲值紀錄，已移除
    }
}
