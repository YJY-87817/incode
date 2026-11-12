namespace incode.MVC.Services.Point
{
    public interface IPointReserveService
    {
        Task<bool> ReserveAsync(int userId, int points, int entrustMissionId);
        Task<bool> ConsumeAsync(int userId, int entrustMissionId);
        // 退回預扣點數；displaySource 為點數紀錄顯示的來源（取消 / 過期）
        Task<bool> ReleaseAsync(int userId, int entrustMissionId, string displaySource = "委託取消退款");
    }
}
