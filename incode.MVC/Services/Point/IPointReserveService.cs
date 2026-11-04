namespace incode.MVC.Services.Point
{
    public interface IPointReserveService
    {
        Task<bool> ReserveAsync(int userId, int points, int entrustMissionId);
        Task<bool> ConsumeAsync(int userId, int entrustMissionId);
        Task<bool> ReleaseAsync(int userId, int entrustMissionId);
    }
}
