namespace incode.MVC.Constants
{
    /// <summary>
    /// 論壇內容類型（content.type，見 docs/dbdiagram.md；前端 stores/forum/constants.js 的 CONTENT_TYPES）。
    /// </summary>
    public static class ContentTypes
    {
        public const int Post = 1;          // 主題（貼文版面）
        public const int Reply = 2;         // 回文
        public const int Comment = 3;       // 留言
        public const int CommentReply = 4;  // 留言回覆

        /// <summary>
        /// 各類型允許的父文章類型（依前端實際的用法）：回文掛在主題下；留言掛在回文下，或以「簡易留言」直接掛在主題下；
        /// 留言回覆掛在留言下。主題沒有父文章
        /// </summary>
        public static readonly IReadOnlyDictionary<int, IReadOnlySet<int>> AllowedParentTypes = new Dictionary<int, IReadOnlySet<int>>
        {
            [Reply] = new HashSet<int> { Post },
            [Comment] = new HashSet<int> { Reply, Post },
            [CommentReply] = new HashSet<int> { Comment },
        };

        public static bool IsValid(int type) => type is Post or Reply or Comment or CommentReply;
    }

    /// <summary>論壇分類狀態（content_category.is_active，見 docs/database/seed.sql）。</summary>
    public static class ContentCategoryStatus
    {
        public const int MemberCreated = 1; // 會員新增
        public const int Official = 2;      // 官方提供
        public const int Disabled = 3;      // 停用
    }
}
