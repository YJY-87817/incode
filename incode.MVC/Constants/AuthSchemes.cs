namespace incode.MVC.Constants
{
    /// <summary>驗證方案名稱（Program.cs 註冊）。</summary>
    public static class AuthSchemes
    {
        public const string Member = "MemberAuth";
        public const string Admin = "AdminAuth";

        /// <summary>
        /// Google 登入的暫存 Cookie：Google 回呼先寫到這裡，<c>AuthApiController.GoogleResponse</c> 讀出 Email 後
        /// 以會員 Cookie 登入並清除。原本 Google 寫入預設方案（SmartScheme），瀏覽器有後台 Cookie 時會轉給 AdminAuth，
        /// 把後台登入覆蓋成 Google 的身分。
        /// </summary>
        public const string External = "ExternalAuth";
    }
}
