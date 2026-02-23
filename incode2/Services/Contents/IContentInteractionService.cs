using incode2.Dtos;
using incode2.Models;

namespace incode2.Services.Contents
{
  public interface IContentInteractionService
  {
    /// <summary>
    /// 取得特定內容的所有互動
    /// </summary>
    Task<List<ContentInteractionDto>> GetContentInteractions(int contentId);

    /// <summary>
    /// 取得使用者對特定內容的互動類型
    /// </summary>
    Task<List<ContentInteractionDto>> GetUserInteraction(int contentId, int userId);

    /// <summary>
    /// 新增或移除互動（Toggle）
    /// 如果使用者已經有該互動 -> 刪除（取消）
    /// 如果使用者沒有該互動 -> 新增
    /// 如果使用者有其他互動 -> 更新為新的互動類型
    /// </summary>
    Task<ContentInteractionDto> AddOrUpdateInteraction(CreateContentInteractionDto dto);

    /// <summary>
    /// 移除互動
    /// </summary>
    Task<bool> RemoveInteraction(int contentId, int userId, int interactionCategoryId);

    /// <summary>
    /// 取得互動分類列表
    /// </summary>
    Task<List<InteractionCategory>> GetInteractionCategory();

    /// <summary>
    /// 統計內容的互動數（按分類）
    /// </summary>
    Task<object> GetInteractionStats(int contentId);

    Task<Dictionary<int, HashSet<int>>> GetUserInteractionLookupAsync(IEnumerable<int> contentIds, int? userId);

    Task<Dictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> contentIds);

    Task<int> GetLikeCountAsync(int contentId);

    Task<ResultDTO> SelectBestAnswerAsync(int entrustMissionId, int contentId, int selectorUserId);

    /// <summary>
    /// 查詢委託任務的最佳解答 ContentId
    /// </summary>
    Task<int?> GetBestAnswerContentId(int entrustMissionId);

    Task<List<ContentInteractionDto>> GetInteractions(
      int? contentId,
      int? userId,
      int? interactionCategoryId,
      int? entrustMissionId);
  }
}
