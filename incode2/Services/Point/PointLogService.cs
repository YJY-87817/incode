using Google.GenAI.Types;
using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using incode2.ViewModels;
using Microsoft.EntityFrameworkCore;
using incode2.Constants; // 引用PointType定義的 Enum
using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace incode2.Services.Point
{
    public class PointLogService : IPointLogService
    {
        private readonly incodedatabaseContext _context;

        public PointLogService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 儲值成功 → 加點 + 寫 point_log（含 transaction）
        public async Task AddPointsFromRechargeAsync(PointRecharge recharge)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ 計算目前餘額（以 log 為準）
                var currentBalance = await _context.PointLog
                    .Where(x => x.UserId == recharge.UserId
                             && x.Status == "success")
                    .SumAsync(x => (int?)x.ChangeAmount) ?? 0;

                // 2️⃣ 新餘額
                var newBalance = currentBalance + recharge.PointsGranted;

                // 3️⃣ 寫 point_log
                var log = new PointLog
                {
                    UserId = recharge.UserId,
                    SourceType = "recharge",
                    SourceId = recharge.PointRechargeId,
                    PointTypeDefinitionId = 1, // 🔥 儲值類型（依你定義）
                    ChangeAmount = recharge.PointsGranted,
                    BalanceAfter = newBalance,
                    Status = "success",
                    CreatedAt = DateTime.Now
                };

                _context.PointLog.Add(log);

                // 4️⃣ 更新儲值訂單
                recharge.Status = "success";
                recharge.CreatedAt = DateTime.Now; // 如果你有這欄
                _context.PointRecharge.Update(recharge);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // 加扣點
        public async Task<bool> ChangePointsAsync(int userId, int changeAmount, string sourceType, int sourceId, PointType type)
        {
            //using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int currentBalance = await GetCurrentBalanceAsync(userId);
                if (changeAmount < 0 && currentBalance + changeAmount < 0)
                    return false;
                int balanceAfter = currentBalance + changeAmount;

                var log = new PointLog
                {
                    UserId = userId,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    PointTypeDefinitionId = (int)type, //
                    ChangeAmount = changeAmount, // 正數
                    BalanceAfter = balanceAfter,
                    Status = "success",
                    CreatedAt = DateTime.Now
                };

                _context.PointLog.Add(log);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }  


        public async Task<ResultDTO> AddPointsAsync(int userId, int points, int missionId)
        {
            try
            {
                var user = await _context.User.FindAsync(userId);
                if (user == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "使用者不存在" };

                var lastLog = await _context.PointLog
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.CreatedAt)
                    .ThenByDescending(l => l.PointLogId)
                    .Select(l => (int?)l.BalanceAfter)
                    .FirstOrDefaultAsync();

                var currentBalance = lastLog ?? 0;
                var newBalance = currentBalance + points;

                var log = new PointLog
                {
                    UserId = userId,
                    ChangeAmount = points,
                    BalanceAfter = newBalance,
                    Status = "success",
                    CreatedAt = DateTime.UtcNow,
                    PointTypeDefinitionId = 1,
                    SourceType = "mission",
                    SourceId = missionId,
                };

                _context.PointLog.Add(log);
                // 這裡不要 SaveChangesAsync()，讓外層統一存檔

                return new ResultDTO { Ok = true, Code = 200, Message = "點數記錄成功" };
            }
            catch (Exception ex)
            {
                return new ResultDTO
                {
                    Ok = false,
                    Code = 500,
                    Message = $"點數記錄失敗：{ex.Message}"
                };
            }
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
                    UserId = l.UserId,
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
                        : l.SourceType == "orders"
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

        //儲值成功 => 入帳（綠界 ReturnURL 用）
        public async Task RechargeSuccessAsync(string orderNo)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var order = await _context.PointRecharge
                .FirstOrDefaultAsync(o => o.OrderNo == orderNo);

            if (order == null)
                throw new Exception("訂單不存在");

            if (order.Status == "success")
                return; // 防重送（超重要）

            // 取得最新餘額
            var lastLog = await _context.PointLog
                .Where(l => l.UserId == order.UserId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.PointLogId)
                .Select(l => (int?)l.BalanceAfter)
                .FirstOrDefaultAsync();

            var newBalance = (lastLog ?? 0) + order.PointsGranted;

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

            // 更新訂單狀態
            order.Status = "success";
            order.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }


        public async Task TipAsync(int fromUserId, int toUserId, int ContentId, int amount)
        {
            if (fromUserId == toUserId)
                throw new Exception("不能打賞自己");

            using var tx = await _context.Database.BeginTransactionAsync();

            // 取得雙方最新餘額
            var fromLast = await _context.PointLog
                .Where(l => l.UserId == fromUserId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.PointLogId)
                .Select(l => (int?)l.BalanceAfter)
                .FirstOrDefaultAsync();

            var toLast = await _context.PointLog
                .Where(l => l.UserId == toUserId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.PointLogId)
                .Select(l => (int?)l.BalanceAfter)
                .FirstOrDefaultAsync();

            var fromBalance = fromLast ?? 0;
            var toBalance = toLast ?? 0;

            if (fromBalance < amount)
                throw new Exception("點數不足");

            var fromNewBalance = fromBalance - amount;
            var toNewBalance = toBalance + amount;

            // 新增兩筆 PointLog
            _context.PointLog.AddRange(
                new PointLog
                {
                    UserId = fromUserId,
                    PointTypeDefinitionId = 6, // tip_out
                    SourceType = "content",
                    SourceId = ContentId,
                    ChangeAmount = -amount,
                    BalanceAfter = fromNewBalance,
                    Status = "success",
                    CreatedAt = DateTime.UtcNow
                },
                new PointLog
                {
                    UserId = toUserId,
                    PointTypeDefinitionId = 7, // tip_in
                    SourceType = "content",
                    SourceId = ContentId,
                    ChangeAmount = amount,
                    BalanceAfter = toNewBalance,
                    Status = "success",
                    CreatedAt = DateTime.UtcNow
                }
            );

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

        }
    }
}