
namespace incode.MVC.Services.Contents
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
    /// 新增或移除互動（Toggle）：已有該互動 -> 刪除（Data 為 null）；沒有 -> 新增（Data 為新增的互動）。
    /// 只接受 InteractionCategoryIds.MemberToggleable 的分類，其他回 400；內容不存在回 404
    /// </summary>
    Task<ResultDTO<ContentInteractionDto?>> AddOrUpdateInteraction(CreateContentInteractionDto dto);

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

    /// <summary>可以公開查詢的互動分類（目前只有最佳解答：本來就公開顯示在討論串上）。按讚、隱藏、收藏、檢舉等只有本人與管理員能查</summary>
    Task<bool> IsPublicInteractionCategoryAsync(int interactionCategoryId);

    Task<List<ContentInteractionDto>> GetInteractions(
      int? contentId,
      int? userId,
      int? interactionCategoryId,
      int? entrustMissionId);
  }
}
