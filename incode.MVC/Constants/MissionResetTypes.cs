namespace incode.MVC.Constants
{
    /// <summary>平台任務的重置週期（mission.reset_type）</summary>
    public static class MissionResetTypes
    {
        /// <summary>
        /// 一次性任務（once / none / 空值或未知值）的固定週期起點。
        /// 一次性任務每位會員只有一筆進度、只能領一次獎勵；週期起點不可以用「今天」，否則每天都會建立新進度、再領一次。
        /// </summary>
        public static readonly DateTime OneTimeCycleStart = new(2000, 1, 1);

        public static bool IsOneTime(string? resetType)
            => (resetType ?? "").Trim().ToLowerInvariant() is not ("daily" or "weekly" or "monthly" or "yearly");
    }
}
