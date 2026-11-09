using Microsoft.EntityFrameworkCore;
using incode.MVC.Constants; // 引用PointType定義的 Enum
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace incode.MVC.Services.Point
{
    public class PointLogService : IPointLogService
    {
        private readonly incodedatabaseContext _context;

        public PointLogService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 綠界付款成功回呼
        public async Task<RechargeResult> CompleteRechargeAsync(string orderNo, string? paymentTradeNo)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 條件更新會鎖住這筆儲值單到交易結束：同時到達的重複回呼會在這裡等待，之後更新 0 筆
                var updated = await _context.PointRecharge
                    .Where(r => r.OrderNo == orderNo && r.Status != "success")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Status, "success")
                        .SetProperty(r => r.PaymentTradeNo, paymentTradeNo));

                if (updated == 0)
                {
                    await tx.CommitAsync();
                    return await _context.PointRecharge.AnyAsync(r => r.OrderNo == orderNo)
                        ? RechargeResult.AlreadyProcessed
                        : RechargeResult.NotFound;
                }

                var recharge = await _context.PointRecharge
                    .AsNoTracking()
                    .FirstAsync(r => r.OrderNo == orderNo);

                await ChangePointsAsync(
                    userId: recharge.UserId,
                    changeAmount: recharge.PointsGranted,
                    sourceType: "point_recharge",
                    sourceId: recharge.PointRechargeId,
                    type: PointType.IncomePointRecharge,
                    displaySource: "點數儲值",
                    displaySubTitle: $"訂單 #{recharge.OrderNo ?? recharge.PointRechargeId.ToString()}");

                // 狀態更新與加點在同一個交易：任一失敗都會 rollback，綠界重試時會重新處理
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return RechargeResult.Completed;
            }
            catch
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task LockUsersAsync(params int[] userIds)
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("點數異動必須在交易內執行");

            if (!_context.Database.IsSqlServer()) return; // sp_getapplock 為 SQL Server 專用

            // 固定依 ID 由小到大取得鎖，避免兩個請求互相等待對方（例如 A 打賞 B 的同時 B 打賞 A）。
            // 同一交易內重複鎖定同一會員不會卡住（applock 可重入）
            foreach (var userId in userIds.Distinct().Order())
            {
                var resource = $"point-user-{userId}";
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @result int;
                    EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction', @LockTimeout = 10000;
                    IF @result < 0 THROW 50001, 'point lock timeout', 1;");
            }
        }

        // 加扣點：呼叫端負責交易與 SaveChanges
        public async Task<bool> ChangePointsAsync(
            int userId,
            int changeAmount,
            string sourceType,
            int? sourceId,
            PointType type,
            string? displaySource = null,
            string? displaySubTitle = null)
        {
            // 鎖定後才讀餘額：同一會員的其他異動必須等這個交易結束，不會兩邊讀到同一個舊餘額
            await LockUsersAsync(userId);

            // 餘額 = 資料庫最後一筆 + 這個交易中尚未儲存的異動（同一交易對同一會員多次加扣點時才會正確）
            var pending = _context.ChangeTracker.Entries<PointLog>()
                .Where(e => e.State == EntityState.Added && e.Entity.UserId == userId)
                .Sum(e => e.Entity.ChangeAmount);
            var currentBalance = await GetCurrentBalanceAsync(userId) + pending;
            var newBalance = currentBalance + changeAmount;

            if (changeAmount < 0 && newBalance < 0)
                return false;

            var log = new PointLog
            {
                UserId = userId,
                SourceType = sourceType,
                SourceId = sourceId,
                PointTypeDefinitionId = (int)type,
                ChangeAmount = changeAmount,
                BalanceAfter = newBalance,
                Status = "success",
                CreatedAt = DateTime.Now,
                DisplaySource = displaySource,
                DisplaySourceSubTitle = displaySubTitle
            };

            _context.PointLog.Add(log);

            // 不呼叫 SaveChanges，讓呼叫端決定何時 commit
            return true;
        }

        public async Task<IEnumerable<PointLogDTO>> GetUserLogsAsync(int userId)
        {
            return await _context.PointLog
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .Include(l => l.PointTypeDefinition)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new PointLogDTO
                {
                    PointLogId = l.PointLogId,
                    UserId = l.UserId,
                    CreatedAt = l.CreatedAt ?? DateTime.MinValue,
                    ChangeAmount = l.ChangeAmount,
                    BalanceAfter = l.BalanceAfter,
                    SourceType = l.SourceType,
                    SourceId = l.SourceId,

                    // 主顯示名稱（已包含 subTitle）
                    DisplayName = !string.IsNullOrWhiteSpace(l.DisplaySource)
                ? l.DisplaySource
                  + (string.IsNullOrWhiteSpace(l.DisplaySourceSubTitle)
                     ? ""
                     : $" - {l.DisplaySourceSubTitle}")
                : l.SourceType == "mission"
                    ? (_context.Mission.Where(m => m.MissionId == l.SourceId).Select(m => m.MissionName).FirstOrDefault() ?? "平台任務獎勵")
                    : l.SourceType == "entrust_mission"
                        ? (_context.EntrustMission.Where(e => e.EntrustMissionId == l.SourceId).Select(e => e.Title).FirstOrDefault() ?? "委託任務獎勵")
                        : l.SourceType == "entrust_mission_point_reserve"
                            ? (_context.EntrustMission.Where(e => e.EntrustMissionId == l.SourceId).Select(e => "[預扣] " + e.Title).FirstOrDefault() ?? "委託發布預扣")
                            : l.SourceType == "orders"
                               ? (l.ChangeAmount < 0 ? "商城商品購買" : "商城商品退款")
                                : l.SourceType == "point_recharge"
                                    ? "點數儲值"
                                    : l.SourceType == "point_tip"
                                        ? (l.ChangeAmount < 0 ? "打賞內容" : "收到打賞")
                                        : l.PointTypeDefinition != null
                                            ? l.PointTypeDefinition.DisplayName
                                            : "點數異動",

                    // 關鍵：一定要回傳這個欄位給前端！
                    DisplaySourceSubTitle = l.DisplaySourceSubTitle
                })
                .ToListAsync();
        }

        //獲取當前餘額：直接抓取最新一筆紀錄的 BalanceAfter，效能最高 $O(1)$
        public async Task<int> GetCurrentBalanceAsync(int userId)
        {
            var lastLog = await _context.PointLog
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.PointLogId)
                .Select(l => (int?)l.BalanceAfter)
                .FirstOrDefaultAsync();

            return lastLog ?? 0;
        }

        // 提供給後台或詳細對帳頁面，使用批量載入技術 (Bulk Loading) 處理大量關聯資料
        public async Task<List<PointLogsViewModel>> GetDetailedLogsAsync(int userId)
        {
            var logs = await _context.PointLog
                .AsNoTracking()
                .Include(l => l.PointTypeDefinition)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            if (!logs.Any()) return new List<PointLogsViewModel>();

            // 1. 收集所有關聯 ID 以進行批量查詢
            var missionIds = logs.Where(l => l.SourceType == "mission").Select(l => l.SourceId).Distinct().ToList();
            var entrustIds = logs.Where(l => l.SourceType == "entrust_mission" || l.SourceType == "entrust_mission_point_reserve")
                                 .Select(l => l.SourceId).Distinct().ToList();
            var orderIds = logs.Where(l => l.SourceType == "orders").Select(l => l.SourceId).Distinct().ToList();
            var rechargeIds = logs.Where(l => l.SourceType == "point_recharge").Select(l => l.SourceId).Distinct().ToList();

            // 2. 執行批量查詢並轉換為 Dictionary
            var missions = await _context.Mission.Where(m => missionIds.Contains(m.MissionId))
                .ToDictionaryAsync(m => m.MissionId, m => m.MissionName ?? "任務");

            var entrusts = await _context.EntrustMission.Where(e => entrustIds.Contains(e.EntrustMissionId))
                .ToDictionaryAsync(e => e.EntrustMissionId, e => e.Title ?? "委託");

            var recharges = await _context.PointRecharge.Where(r => rechargeIds.Contains(r.PointRechargeId))
                .ToDictionaryAsync(r => r.PointRechargeId, r => r.PaymentMethod ?? "線上支付");

            var orders = await _context.Orders.AsNoTracking()
                .Include(o => o.Orderdetail!).ThenInclude(od => od.Product)
                .Where(o => orderIds.Contains(o.OrderId))
                .ToDictionaryAsync(o => o.OrderId, o => o);

            return logs.Select(log =>
            {
                // 抓取具體項目名稱（如：每日登入）
                string specificTitle = log.SourceType switch
                {
                    "mission" => missions.GetValueOrDefault(log.SourceId ?? 0, "平台任務獎勵"),
                    "entrust_mission" => entrusts.GetValueOrDefault(log.SourceId ?? 0, "委託任務獎勵"),
                    "entrust_mission_point_reserve" => entrusts.TryGetValue(log.SourceId ?? 0, out var t) ? $"[預扣] {t}" : "委託預扣",
                    "point_recharge" => "點數儲值",
                    "orders" => orders.TryGetValue(log.SourceId ?? 0, out var o) ? GetOrderDisplayText(o) : "商城消費",
                    _ => "系統調整" // 只有完全對不到類型時才顯示這個
                };

                return new PointLogsViewModel
                {
                    CreatedAt = log.CreatedAt ?? DateTime.Now,
                    ChangeAmount = log.ChangeAmount,
                    NewBalance = log.BalanceAfter,
                    DisplayName = log.PointTypeDefinition?.DisplayName ?? "點數異動", // 這是藍/綠色標籤用的
                    Description = specificTitle // 這是左側大標題用的
                };
            }).ToList();
        }

        // 保持你原本的美化邏輯
        private static string GetOrderDisplayText(Orders order)
        {
            if (order?.Orderdetail == null || !order.Orderdetail.Any())
                return $"點數商店消費 (#{order?.OrderId})";

            var firstProduct = order.Orderdetail.First().Product?.Name ?? "商品";
            int totalItems = order.Orderdetail.Sum(d => d.Quantity);

            return totalItems > 1
                ? $"購買 {firstProduct} 等 {totalItems} 件商品"
                : $"購買 {firstProduct}";
        }


        public async Task TipAsync(int fromUserId, int toUserId, int contentId, int amount)
        {
            if (fromUserId == toUserId)
                throw new BusinessException("不能打賞自己");

            if (amount <= 0)
                throw new BusinessException("打賞金額必須大於 0");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 先鎖定雙方（依 ID 排序），之後的餘額檢查與扣款才不會被同時送出的打賞穿越
                await LockUsersAsync(fromUserId, toUserId);

                var fromBalance = await GetCurrentBalanceAsync(fromUserId);
                if (fromBalance < amount)
                    throw new BusinessException("點數不足");

                var contentTitle = await _context.Content
                    .Where(c => c.ContentId == contentId)
                    .Select(c => c.Title ?? "無標題內容")
                    .FirstOrDefaultAsync() ?? "內容";

                var fromNickname = await _context.User
                    .Where(u => u.UserId == fromUserId)
                    .Select(u => u.Email ?? $"用戶{u.UserId}")
                    .FirstOrDefaultAsync();

                var toNickname = await _context.User
                    .Where(u => u.UserId == toUserId)
                    .Select(u => u.Email ?? $"用戶{u.UserId}")
                    .FirstOrDefaultAsync();

                // ================================
                // ⭐ 新增 point_tip 真實紀錄
                // ================================
                var tipRecord = new PointTip
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    ContentId = contentId,
                    Amount = amount,
                    CreatedAt = DateTime.Now
                };

                _context.PointTip.Add(tipRecord);

                // 扣款
                bool deductSuccess = await ChangePointsAsync(
                    userId: fromUserId,
                    changeAmount: -amount,
                    sourceType: "point_tip",
                    sourceId: contentId,
                    type: PointType.ExpenseTip_out,
                    displaySource: $"打賞〈{contentTitle}〉",
                    displaySubTitle: $"給 {toNickname}"
                );

                if (!deductSuccess)
                    throw new Exception("扣款失敗");

                // 加款
                bool addSuccess = await ChangePointsAsync(
                    userId: toUserId,
                    changeAmount: amount,
                    sourceType: "point_tip",
                    sourceId: contentId,
                    type: PointType.IncomeTip_in,
                    displaySource: $"收到打賞〈{contentTitle}〉",
                    displaySubTitle: $"來自 {fromNickname}"
                );

                if (!addSuccess)
                    throw new Exception("加款失敗");

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }
    }
}
