namespace incode.MVC.Services.Point
{
    /// <summary>
    /// 定期處理過期的委託（<see cref="IEntrustMissionService.ExpireOverdueMissionsAsync"/>）：
    /// 啟動後先執行一次，之後每隔 <c>App:EntrustExpiryIntervalMinutes</c> 分鐘（預設 60）執行；設為 0 則停用。
    /// 多台伺服器同時執行也安全：每筆委託都在交易內鎖定委託人後重新讀取狀態，只會結案、退款一次。
    /// </summary>
    public class EntrustMissionExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EntrustMissionExpiryWorker> _logger;
        private readonly TimeSpan _interval;

        public EntrustMissionExpiryWorker(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<EntrustMissionExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(configuration.GetValue("App:EntrustExpiryIntervalMinutes", 60));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_interval <= TimeSpan.Zero) return;

            using var timer = new PeriodicTimer(_interval);
            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IEntrustMissionService>();
                    int count = await service.ExpireOverdueMissionsAsync(DateTime.Now);
                    if (count > 0)
                        _logger.LogInformation("已處理 {Count} 筆過期委託", count);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // 資料庫暫時無法連線等：記錄後等下一輪
                    _logger.LogError(ex, "過期委託處理失敗");
                }
            }
            while (await WaitAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
        {
            try { return await timer.WaitForNextTickAsync(token); }
            catch (OperationCanceledException) { return false; }
        }
    }
}
