using Google.GenAI.Types;
using incode2.DTO;
using incode2.Models;
using incode2.ViewModels;
using Microsoft.EntityFrameworkCore;
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
            // 給 API前端用
            return await _context.PointLog
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new PointLogDTO
                {
                    PointLogId = l.PointLogId,
                    CreatedAt = l.CreatedAt ?? DateTime.MinValue,
                    ChangeAmount = l.ChangeAmount,
                    BalanceAfter = l.BalanceAfter,
                    SourceType = l.SourceType,
                    SourceId = l.SourceId,
                    DisplayName = l.SourceType == "mission"
                        ? (_context.Mission.Where(m => m.MissionId == l.SourceId).Select(m => m.MissionName).FirstOrDefault() ?? "平台任務獎勵")
                        : l.SourceType == "entrust_mission"
                        ? (_context.EntrustMission.Where(e => e.EntrustMissionId == l.SourceId).Select(e => e.Title).FirstOrDefault() ?? "委託任務獎勵")
                        : l.SourceType == "entrust_mission_point_reserve"
                        ? (_context.EntrustMission.Where(e => e.EntrustMissionId == l.SourceId).Select(e => "[預扣] " + e.Title).FirstOrDefault() ?? "委託發布預扣")
                        : l.SourceType == "order"
                        ? "商城商品兌換"
                        : l.SourceType == "point_recharge"
                        ? "點數儲值"
                        : l.PointTypeDefinition != null ? l.PointTypeDefinition.DisplayName : "點數異動"
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
            var orderIds = logs.Where(l => l.SourceType == "order").Select(l => l.SourceId).Distinct().ToList();
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

            return logs.Select(log => {
                // 抓取具體項目名稱（如：每日登入）
                string specificTitle = log.SourceType switch
                {
                    "mission" => missions.GetValueOrDefault(log.SourceId ?? 0, "平台任務獎勵"),
                    "entrust_mission" => entrusts.GetValueOrDefault(log.SourceId ?? 0, "委託任務獎勵"),
                    "entrust_mission_point_reserve" => entrusts.TryGetValue(log.SourceId ?? 0, out var t) ? $"[預扣] {t}" : "委託預扣",
                    "point_recharge" => "點數儲值",
                    "order" => orders.TryGetValue(log.SourceId ?? 0, out var o) ? GetOrderDisplayText(o) : "商城消費",
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
    }
}