namespace incode.MVC.Constants
{
    /// <summary>
    /// 論壇互動分類 ID（interaction_category，見 docs/database/seed.sql；前端 contentInteractionApi.js 也以 ID 呼叫）。
    /// 最佳解答（best_answer）由 ContentInteractionService 依名稱查詢。
    /// </summary>
    public static class InteractionCategoryIds
    {
        public const int Like = 1;
        public const int Hide = 2;
        public const int Bookmark = 3;
        public const int Report = 4;

        /// <summary>
        /// 會員可以透過 POST api/ContentInteractionApi 自己切換的分類。
        /// 最佳解答、打賞等系統分類只能由對應流程寫入（否則會員可以替自己的回覆加上假徽章）。
        /// </summary>
        public static readonly IReadOnlySet<int> MemberToggleable = new HashSet<int> { Like, Hide, Bookmark, Report };
    }
}
