
namespace incode.MVC.Services.Contents
{
  public interface IContentEntrustMissionService
  {
    Task<ResultDTO<CreateContentWithEntrustMissionResultDto>> CreateContentWithEntrustMissionAsync(CreateContentWithEntrustMissionDto dto, int userId);
  }
}
