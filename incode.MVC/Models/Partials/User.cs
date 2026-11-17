using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Models
{
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {
        // 以下欄位不在 Database-First 產生的模型裡，欄位設定在 Partials/incodedatabaseContext.cs 的 OnModelCreatingPartial；
        // 已上線的資料庫執行 docs/database/migrations/2026-11-17-user-email-verify-security-stamp.sql 新增

        /// <summary>Email 已驗證（點了驗證信連結，或以 Google 登入過）。未驗證的密碼帳號被同 Email 的 Google 登入時會被接管</summary>
        public bool EmailVerified { get; set; }

        /// <summary>Email 驗證信的 token 與期限</summary>
        public string? EmailVerifyToken { get; set; }
        public DateTime? EmailVerifyTokenExpiry { get; set; }

        /// <summary>
        /// 登入時寫進 Cookie 的戳記；重設 / 修改密碼、Google 接管帳號時換新，舊 Cookie 的戳記對不上就登出（MemberCookieValidator）。
        /// null 表示從未換過，此時沒有戳記的舊 Cookie 仍然有效
        /// </summary>
        public string? SecurityStamp { get; set; }
    }
    public partial class UserLogin
    {
    }
}
