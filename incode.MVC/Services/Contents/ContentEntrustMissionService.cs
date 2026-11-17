using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Contents
{
  public class ContentEntrustMissionService : IContentEntrustMissionService
  {
    private readonly IContentService _contentService;
    private readonly IEntrustMissionService _entrustMissionService;
    private readonly incodedatabaseContext _context;
    private readonly UploadStorage _storage;

    public ContentEntrustMissionService(IContentService contentService, IEntrustMissionService entrustMissionService,
      incodedatabaseContext context, UploadStorage storage)
    {
      _contentService = contentService;
      _entrustMissionService = entrustMissionService;
      _context = context;
      _storage = storage;
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

      // 有委託時，文章與委託在同一個交易：委託建立失敗（例如點數不足）時文章也不會留下。
      // 原本先建立文章再建立委託，點數不足時文章已經發出、卻沒有委託
      await using var transaction = dto.EntrustMission != null
        ? await _context.Database.BeginTransactionAsync()
        : null;

      Content content;
      try
      {
        content = await _contentService.CreateContent(contentDto);
      }
      catch (BusinessException ex)
      {
        // 類型、父文章、分類不合法（檢查在寫入任何資料之前）
        return new ResultDTO<CreateContentWithEntrustMissionResultDto> { Ok = false, Code = 400, Message = ex.Message };
      }

      ResultDTO? entrustResult = null;
      if (dto.EntrustMission != null)
      {
        // 建立委託任務交由 EntrustMissionService 處理
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
          await transaction!.RollbackAsync();
          _context.ChangeTracker.Clear();
          _storage.Delete(UploadFolders.ContentBodies, content.Body); // 內文檔已寫入，文章 rollback 後刪除

          return new ResultDTO<CreateContentWithEntrustMissionResultDto>
          {
            Ok = false,
            Code = entrustResult.Code,
            Message = entrustResult.Message,
          };
        }

        await transaction!.CommitAsync();
      }

      // 回查完整 DTO
      var contentResult = await _contentService.GetContent(content.ContentId, userId);
      if (contentResult == null)
      {
        return new ResultDTO<CreateContentWithEntrustMissionResultDto>
        {
          Ok = false,
          Code = 500,
          Message = "無法取得內容資料"
        };
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

      // 委託標題 / 詳情是純文字：HTML 編碼後才接進內文（原本原樣接入，可以夾帶 HTML）
      builder.AppendLine("<hr />");
      builder.AppendLine($"<p>委託任務：{WebUtility.HtmlEncode(entrustMission.Title)}</p>");
      builder.AppendLine($"<p>{WebUtility.HtmlEncode(entrustMission.Detail)}</p>");

      return builder.ToString();
    }
  }
}
