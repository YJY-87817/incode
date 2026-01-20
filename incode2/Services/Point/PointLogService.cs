using Microsoft.EntityFrameworkCore;
using incode2.DTO;
using incode2.ViewModels;
using incode2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.Services.Point
{
    public class PointLogService : IPointLogService
    {
        private readonly incodedatabaseContext _context;

        public PointLogService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PointLogDTO>> GetUserLogsAsync(int userId)
        {
            // 簡單版：只給 API 或未來前端用，欄位精簡
            return await _context.PointLog
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new PointLogDTO
                {
                    PointLogId = l.PointLogId,
                    CreatedAt = l.CreatedAt ?? DateTime.MinValue,
                    ChangeAmount = l.ChangeAmount,
                    SourceType = l.SourceType,
                    SourceId = l.SourceId,
                    DisplayName = l.PointTypeDefinition != null
                        ? l.PointTypeDefinition.DisplayName
                        : "未知類型"
                })
                .ToListAsync();
        }

        public async Task<int> GetCurrentBalanceAsync(int userId)
        {
            // 最有效率的寫法：直接 SUM，不需要載全部紀錄
            var balance = await _context.PointLog
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .SumAsync(l => (int?)l.ChangeAmount) ?? 0;

            return balance;
        }

        public async Task<List<PointLogsViewModel>> GetDetailedLogsAsync(int userId)
        {
            // 取出所有該使用者的點數紀錄（舊→新）
            var logs = await _context.PointLog
                .AsNoTracking()
                .Include(l => l.PointTypeDefinition)
                .Where(l => l.UserId == userId)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            if (!logs.Any())
            {
                return new List<PointLogsViewModel>();
            }

            // 收集各種來源的 ID
            var missionIds = logs.Where(l => l.SourceType == "mission")
                                .Select(l => l.SourceId).Distinct().ToList();

            var entrustMissionIds = logs.Where(l => l.SourceType == "entrust_mission")
                                       .Select(l => l.SourceId).Distinct().ToList();

            var depositIds = logs.Where(l => l.SourceType == "deposit" || l.SourceType == "point_deposits")
                                .Select(l => l.SourceId).Distinct().ToList();

            var orderIds = logs.Where(l => l.SourceType == "order")
                              .Select(l => l.SourceId).Distinct().ToList();

            // 批量載入關聯資料 → 字典加速查找
            var missions = await _context.Mission
                .Where(m => missionIds.Contains(m.MissionId))
                .ToDictionaryAsync(m => m.MissionId, m => m.MissionName ?? "未知任務");

            var entrustMissions = await _context.EntrustMission
                .Where(em => entrustMissionIds.Contains(em.EntrustMissionId))
                .ToDictionaryAsync(em => em.EntrustMissionId, em => em.Title ?? "未知委託");

            var deposits = await _context.PointRecharge
                .Where(pd => depositIds.Contains(pd.PointRechargeId))
                .ToDictionaryAsync(pd => pd.PointRechargeId, pd => pd.PaymentMethod ?? "未知儲值");

            var orders = new Dictionary<int, Orders>();
            if (orderIds.Any())
            {
                orders = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Orderdetail!)
                        .ThenInclude(od => od.Product)
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToDictionaryAsync(o => o.OrderId, o => o);
            }

            // 開始建構 ViewModel + 跑馬燈式餘額
            var viewModels = new List<PointLogsViewModel>(logs.Count);
            int runningBalance = 0;

            foreach (var log in logs)
            {
                runningBalance += log.ChangeAmount;

                string description = log.SourceType switch
                {
                    "mission" => missions.TryGetValue(log.SourceId ?? 0, out var name)
                        ? name
                        : $"任務 #{log.SourceId ?? 0}（找不到）",

                    "entrust_mission" => entrustMissions.TryGetValue(log.SourceId ?? 0, out var title)
                        ? title
                        : $"委託任務 #{log.SourceId ?? 0}（找不到）",

                    "order" => orders.TryGetValue(log.SourceId ?? 0, out var order)
                        ? GetOrderDisplayText(order)
                        : $"訂單 #{log.SourceId ?? 0}（資料遺失）",

                    "deposit" or "point_deposits" => deposits.TryGetValue(log.SourceId ?? 0, out var method)
                        ? $"儲值紀錄 #{log.SourceId ?? 0} - {method}"
                        : $"儲值紀錄 #{log.SourceId ?? 0}",

                    _ => string.IsNullOrEmpty(log.SourceType)
                        ? "系統調整/管理員操作"
                        : $"其他來源：{log.SourceType} #{log.SourceId ?? 0}"
                };

                viewModels.Add(new PointLogsViewModel
                {
                    CreatedAt = log.CreatedAt ?? DateTime.Now,
                    ChangeAmount = log.ChangeAmount,
                    Description = description,
                    NewBalance = runningBalance,
                    DisplayName = log.PointTypeDefinition?.DisplayName ?? "未知點數類型"
                });
            }

            // 最新在上（前端比較直覺）
            viewModels.Reverse();

            return viewModels;
        }

        // 保持你原本的美化邏輯
        private static string GetOrderDisplayText(Orders order)
        {
            if (order?.Orderdetail == null || !order.Orderdetail.Any())
                return $"點數商店消費（訂單 #{order?.OrderId})";

            var details = order.Orderdetail;
            var firstProductName = details.First().Product?.Name ?? "商品";

            if (details.Count == 1)
            {
                var qty = details.First().Quantity;
                return qty == 1
                    ? $"購買 {firstProductName}（訂單 #{order.OrderId}）"
                    : $"購買 {firstProductName} × {qty}（訂單 #{order.OrderId}）";
            }

            var totalItems = details.Sum(d => d.Quantity);
            return $"購買 {firstProductName} 等 {totalItems} 件（訂單 #{order.OrderId}）";
        }
    }
}