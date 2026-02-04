using incode2.Dtos;
using incode2.Models;

namespace incode2.Services.Contents
{
  public interface IContentService
  {
    Task<object> GetContents(int? categoryId, int page, int pageSize, int? userId);
    Task<ContentDto?> GetContent(int id);
    Task<Content> CreateContent(CreateContentDto dto);
    Task<UpdateContentDto?> UpdateContent(int id, UpdateContentDto dto, int currentUserId);
    Task<bool> DeleteContent(int id);
    Task<List<ContentDto>> GetCommentsByContentId(int contentId);
    Task<List<ContentDto>> GetUserContents(int userId);
    Task<List<ContentDto>> GetUserContentsByTypes(int userId, IEnumerable<int> types);
    Task<List<ContentDto>> GetUserInteractedContents(int userId, int? interactionCategoryId, IEnumerable<string> interactionCategoryNames);
  }
}
