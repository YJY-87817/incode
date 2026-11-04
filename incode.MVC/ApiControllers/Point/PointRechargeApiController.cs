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
        private readonly ILogger<PointRechargeApiController> _logger;

        public PointRechargeApiController(
            incodedatabaseContext context,
            EcpayService ecpay,
            IPointLogService pointLogService,
            ILogger<PointRechargeApiController> logger)
        {
            _context = context;
            _ecpay = ecpay;
            _pointLogService = pointLogService;
            _logger = logger;
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
                _logger.LogWarning("綠界回呼 CheckMacValue 驗證失敗");
                return Content("0|Fail");
            }

            var rtnCode = form["RtnCode"].ToString();
            var orderNo = form["MerchantTradeNo"].ToString();
            var tradeNo = form["TradeNo"].ToString();

            if (rtnCode == "1")
            {
                // 付款成功：狀態更新與加點在同一個交易，並以條件更新防止重複加點
                try
                {
                    var result = await _pointLogService.CompleteRechargeAsync(orderNo, tradeNo);
                    switch (result)
                    {
                        case RechargeResult.Completed:
                            _logger.LogInformation("儲值成功，已加點 - OrderNo: {OrderNo}", orderNo);
                            break;
                        case RechargeResult.AlreadyProcessed:
                            _logger.LogInformation("訂單已處理過 - OrderNo: {OrderNo}", orderNo);
                            break;
                        default:
                            _logger.LogWarning("找不到儲值訂單 - OrderNo: {OrderNo}", orderNo);
                            break;
                    }
                    return Content("1|OK"); // 找不到訂單也回 OK，避免綠界一直重試
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "儲值加點失敗 - OrderNo: {OrderNo}", orderNo);
                    return Content("0|Fail"); // 交易已 rollback，讓綠界重試
                }
            }

            // 付款失敗、取消等：只更新尚未成功的訂單（已成功的不會被後到的失敗通知覆蓋）
            try
            {
                var updated = await _context.PointRecharge
                    .Where(r => r.OrderNo == orderNo && r.Status != "success")
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(r => r.Status, "failed")
                        .SetProperty(r => r.PaymentTradeNo, tradeNo));
                _logger.LogInformation("付款未成功 - OrderNo: {OrderNo}, RtnCode: {RtnCode}, 更新 {Count} 筆",
                    orderNo, rtnCode, updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新儲值失敗狀態失敗 - OrderNo: {OrderNo}", orderNo);
            }

            // 無論如何都要告訴綠界「我收到通知了」
            return Content("1|OK");
        }

        // 儲值紀錄的查詢 / 修改 / 刪除只在後台 (PointRechargeController) 提供；
        // 原本這裡的 GET / PUT / DELETE 沒有登入檢查，任何人都能讀取或竄改所有人的儲值紀錄，已移除
    }
}
