using incode2.Dtos;
using incode2.Models;

namespace incode2.Services.Contents
{
  public interface IContentService
  {
    Task<ContentListResponseDto> GetContents(int? categoryId, int page, int pageSize, int? userId, string? keyword, string? types, string? sortBy);
    Task<ContentDto?> GetContent(int id, int? userId);
    Task<Content> CreateContent(CreateContentDto dto);
    Task<UpdateContentDto?> UpdateContent(int id, UpdateContentDto dto, int currentUserId);
    Task<bool> DeleteContent(int id, int currentUserId);
    Task<List<ContentDto>> GetCommentsByContentId(int contentId);
    Task<PagedResultDto<ContentDto>> GetChildrenByContentId(int contentId, string? types, int page, int pageSize, int? userId);
    Task<object> GetUserContents(int userId, IEnumerable<int>? types, int page, int pageSize);
    Task<object> GetUserInteractedContents(int userId, int? interactionCategoryId, IEnumerable<string> interactionCategoryNames, int page, int pageSize);
  }
}
