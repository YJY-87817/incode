using System.Text.RegularExpressions;

namespace incode.MVC.Services.Users
{
    /// <summary>
    /// 會員大頭貼網址（User.ImageUrl）允許的值。原本可以填任意網址（例如外部追蹤圖片），其他人看個人頁、
    /// 論壇時瀏覽器就會向該網址送出請求（洩漏 IP / 瀏覽紀錄）。
    /// </summary>
    public static partial class AvatarUrls
    {
        /// <summary>Google 登入建立帳號時的預設大頭貼。</summary>
        public const string GoogleDefault = "https://cdn-icons-png.flaticon.com/512/847/847969.png";

        // 大頭貼上傳 API（POST api/User/images）回傳的網址：/img/user/{Guid}.{副檔名}
        [GeneratedRegex(@"^/img/user/[A-Za-z0-9_-]+\.(jpg|jpeg|png|gif|webp)$", RegexOptions.IgnoreCase)]
        private static partial Regex UploadedAvatar();

        /// <summary>
        /// 空白（不設定）、站內上傳的大頭貼、Google 預設圖，或與目前的值相同（改版前的舊資料，編輯其他欄位時會原樣送回）。
        /// </summary>
        public static bool IsAllowed(string? url, string? currentValue = null)
        {
            if (string.IsNullOrEmpty(url)) return true;
            if (currentValue != null && url == currentValue) return true;
            return url == GoogleDefault || UploadedAvatar().IsMatch(url);
        }
    }
}
