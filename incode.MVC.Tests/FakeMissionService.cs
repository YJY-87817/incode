using incode.MVC.Dtos.Point;

namespace incode.MVC.Tests;

/// <summary>只記錄 <see cref="TriggerMissionAsync"/> 被呼叫次數，其餘不使用。</summary>
internal sealed class FakeMissionService : IMissionService
{
    public int Triggered;
    public Task TriggerMissionAsync(int userId, string actionType, int increment = 1) { Triggered++; return Task.CompletedTask; }
    public Task<IEnumerable<MissionDTO>> GetAllMissionAsync() => throw new NotSupportedException();
    public Task<ResultDTO> ClaimMissionAsync(int missionId, int userId) => throw new NotSupportedException();
    public Task<MissionDTO> GetMissionByIdAsync(int id) => throw new NotSupportedException();
    public Task<ResultDTO> CreateMissionAsync(MissionDTO dto) => throw new NotSupportedException();
    public Task SyncMissionCountAsync(int userId, string actionType, Func<DateTime, DateTime, Task<int>> countInCycle) => throw new NotSupportedException();
}
