using incode2.Dtos;
using incode2.Models;

namespace incode2.Services.Contents
{
  public interface IContentService
  {
    Task<object> GetContents(int? categoryId, int page, int pageSize);
    Task<ContentDto?> GetContent(int id);
    Task<Content> CreateContent(CreateContentDto dto);
    Task<UpdateContentDto?> UpdateContent(int id, UpdateContentDto dto);
    Task<bool> DeleteContent(int id);
  }
}
