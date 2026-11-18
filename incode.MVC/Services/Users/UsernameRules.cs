namespace incode.MVC.Services.Users
{
    /// <summary>
    /// 暱稱規則：2～30 字，不可包含 &lt; &gt; 與控制字元（換行、Tab 等）。
    /// 只檢查新設定的暱稱；改版前已存在、不符合規則的暱稱維持原樣（沒有修改就不檢查）。
    /// 前端輸入框使用同樣的長度（maxlength 30）。
    /// </summary>
    public static class UsernameRules
    {
        public const int MinLength = 2;
        public const int MaxLength = 30;
        public const string ErrorMessage = "暱稱需為 2～30 字，且不可包含 < > 或換行等特殊字元";

        public static bool IsValid(string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            if (username != username.Trim()) return false;
            int length = new System.Globalization.StringInfo(username).LengthInTextElements;
            if (length < MinLength || length > MaxLength) return false;
            return !username.Any(IsForbidden);
        }

        /// <summary>
        /// 把外部來源的名稱（Google 顯示名稱、Email 前綴）轉成符合規則的暱稱：移除禁用字元、截短到 maxLength；
        /// 處理後太短時回傳 fallback。
        /// </summary>
        public static string Sanitize(string? name, string fallback, int maxLength = MaxLength)
        {
            var cleaned = new string((name ?? "").Where(c => !IsForbidden(c)).ToArray()).Trim();
            cleaned = Truncate(cleaned, maxLength).Trim();
            return cleaned.Length >= MinLength ? cleaned : fallback;
        }

        /// <summary>依「字」截短（不會切斷 emoji 等由多個 char 組成的字）。</summary>
        public static string Truncate(string value, int maxLength)
        {
            var info = new System.Globalization.StringInfo(value);
            return info.LengthInTextElements <= maxLength ? value : info.SubstringByTextElements(0, maxLength);
        }

        // 控制字元與看不見的格式字元（零寬空白、文字方向控制等，可用來偽裝暱稱）
        private static bool IsForbidden(char c) => c == '<' || c == '>' || char.IsControl(c)
            || char.GetUnicodeCategory(c) is System.Globalization.UnicodeCategory.Format
                or System.Globalization.UnicodeCategory.LineSeparator or System.Globalization.UnicodeCategory.ParagraphSeparator;
    }
}
