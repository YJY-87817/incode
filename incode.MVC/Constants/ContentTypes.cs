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

        /// <summary>後台顯示用的名稱</summary>
        public static string DisplayName(int type) => type switch
        {
            Post => "主題",
            Reply => "回文",
            Comment => "留言",
            CommentReply => "留言回覆",
            _ => type.ToString(),
        };
    }

    /// <summary>論壇分類狀態（content_category.is_active，見 docs/database/seed.sql）。</summary>
    public static class ContentCategoryStatus
    {
        public const int MemberCreated = 1; // 會員新增
        public const int Official = 2;      // 官方提供
        public const int Disabled = 3;      // 停用

        /// <summary>後台可以設定的狀態（下拉選單順序）</summary>
        public static readonly int[] Selectable = { Official, MemberCreated, Disabled };

        public static string DisplayName(int status) => status switch
        {
            MemberCreated => "會員新增",
            Official => "官方",
            Disabled => "停用",
            _ => $"未設定（{status}）",
        };

        // 名稱 / 說明長度（content_category.category_name nvarchar(100)、description nvarchar(200)；名稱規則同 ContentCategoryMetadata）
        public const int NameMinLength = 2;
        public const int NameMaxLength = 50;
        public const int DescriptionMaxLength = 200;
    }

    /// <summary>論壇內容狀態（content.is_active，見 docs/TECHNICAL.md）。只有 Active 會顯示在前台。</summary>
    public static class ContentStatus
    {
        public const int Active = 1;          // 顯示
        public const int DeletedByAuthor = 2; // 本人刪除
        public const int RemovedByAdmin = 3;  // 官方移除（後台下架，可以恢復）

        public static string DisplayName(int status) => status switch
        {
            Active => "公開",
            DeletedByAuthor => "作者已刪除",
            RemovedByAdmin => "已下架",
            _ => status.ToString(),
        };
    }

    /// <summary>後台處理檢舉的動作（content_report_review.action）。</summary>
    public static class ContentReportActions
    {
        public const string Dismissed = "dismissed"; // 駁回：檢舉不成立，文章維持公開
        public const string Removed = "removed";     // 下架
        public const string Restored = "restored";   // 恢復上架

        public const int MaxNoteLength = 500;

        public static string DisplayName(string action) => action switch
        {
            Dismissed => "駁回",
            Removed => "下架",
            Restored => "恢復上架",
            _ => action,
        };
    }
}
