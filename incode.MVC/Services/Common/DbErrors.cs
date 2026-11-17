using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Common
{
    /// <summary>資料庫錯誤的判斷。</summary>
    public static class DbErrors
    {
        /// <summary>
        /// 寫入違反唯一索引 / 主鍵（SQL Server 2601、2627；測試用的 SQLite 為 "UNIQUE constraint failed"）。
        /// 用來分辨「同時送出的另一個請求先寫入了」與其他錯誤。
        /// </summary>
        public static bool IsUniqueViolation(DbUpdateException ex) => ex.InnerException switch
        {
            SqlException sql => sql.Number is 2601 or 2627,
            { } inner => inner.Message.Contains("UNIQUE constraint failed", StringComparison.Ordinal),
            _ => false,
        };
    }
}
