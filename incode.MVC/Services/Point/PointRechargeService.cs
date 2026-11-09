using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point
{
    public class PointRechargeService : IPointRechargeService
    {
        private readonly incodedatabaseContext _context;

        public PointRechargeService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<PointRecharge> CreateOrderAsync(int userId, int cashAmount, string? paymentMethod)
        {
            var order = new PointRecharge
            {
                UserId = userId,
                OrderNo = "PR" + DateTime.Now.Ticks,
                CashAmount = cashAmount,
                PointsGranted = cashAmount * IPointRechargeService.PointsPerCash,
                PaymentMethod = paymentMethod!,
                Status = "pending"
            };

            _context.PointRecharge.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public Task<int> MarkFailedAsync(string orderNo, string? paymentTradeNo) =>
            _context.PointRecharge
                .Where(r => r.OrderNo == orderNo && r.Status != "success")
                .ExecuteUpdateAsync(x => x
                    .SetProperty(r => r.Status, "failed")
                    .SetProperty(r => r.PaymentTradeNo, paymentTradeNo!));

        public Task<RechargeOrderStatusDto?> GetUserOrderAsync(int userId, string orderNo) =>
            _context.PointRecharge
                .AsNoTracking()
                .Where(r => r.OrderNo == orderNo && r.UserId == userId)
                .Select(r => new RechargeOrderStatusDto
                {
                    OrderNo = r.OrderNo,
                    Status = r.Status,
                    CashAmount = r.CashAmount,
                    PointsGranted = r.PointsGranted,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();
    }
}
