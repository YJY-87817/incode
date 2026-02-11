using System.Text;
using incode2.DTO;
using incode2.Dtos;
using incode2.Services.Point;

namespace incode2.Services.Contents
{
  public class ContentEntrustMissionService : IContentEntrustMissionService
  {
    private readonly IContentService _contentService;
    private readonly IEntrustMissionService _entrustMissionService;

    public ContentEntrustMissionService(IContentService contentService, IEntrustMissionService entrustMissionService)
    {
      _contentService = contentService;
      _entrustMissionService = entrustMissionService;
    }

    public async Task<ResultDTO<CreateContentWithEntrustMissionResultDto>> CreateContentWithEntrustMissionAsync(CreateContentWithEntrustMissionDto dto, int userId)
    {
      var contentBody = dto.Body ?? string.Empty;
      if (dto.EntrustMission != null)
      {
        contentBody = AppendEntrustMissionToBody(contentBody, dto.EntrustMission);
      }

      var contentDto = new CreateContentDto
      {
        ParentContentId = dto.ParentContentId,
        CategoryId = dto.CategoryId,
        UserId = userId,
        Title = dto.Title,
        Body = contentBody,
        Type = dto.Type
      };

      var content = await _contentService.CreateContent(contentDto);
      var contentResult = await _contentService.GetContent(content.ContentId);
      if (contentResult == null)
      {
        return new ResultDTO<CreateContentWithEntrustMissionResultDto>
        {
          Ok = false,
          Code = 500,
          Message = "無法取得內容資料"
        };
      }

      ResultDTO? entrustResult = null;
      if (dto.EntrustMission != null)
      {
        var entrustDto = new EntrustMissionDTO
        {
          UserId = userId,
          ContentId = content.ContentId,
          Title = dto.EntrustMission.Title,
          Detail = dto.EntrustMission.Detail,
          RewardPoints = dto.EntrustMission.RewardPoints,
          Category = dto.EntrustMission.Category,
          Deadline = dto.EntrustMission.Deadline,
          IsUrgent = dto.EntrustMission.IsUrgent,
          Status = "open",
          IsActive = true
        };

        entrustResult = await _entrustMissionService.CreateEntrustMissionAsync(entrustDto);

        if (!entrustResult.Ok)
        {
          return new ResultDTO<CreateContentWithEntrustMissionResultDto>
          {
            Ok = false,
            Code = entrustResult.Code,
            Message = entrustResult.Message,
            Data = new CreateContentWithEntrustMissionResultDto
            {
              Content = contentResult,
              EntrustMission = entrustResult
            }
          };
        }
      }

      return new ResultDTO<CreateContentWithEntrustMissionResultDto>
      {
        Ok = true,
        Code = 201,
        Message = "發佈成功",
        Data = new CreateContentWithEntrustMissionResultDto
        {
          Content = contentResult,
          EntrustMission = entrustResult
        }
      };
    }

    private static string AppendEntrustMissionToBody(string body, CreateEntrustMissionDto entrustMission)
    {
      var builder = new StringBuilder(body ?? string.Empty);

      if (builder.Length > 0)
      {
        builder.AppendLine();
      }

      builder.AppendLine("<hr />");
      builder.AppendLine($"<p>委託任務：{entrustMission.Title}</p>");
      builder.AppendLine($"<p>{entrustMission.Detail}</p>");

      return builder.ToString();
    }
  }
}
