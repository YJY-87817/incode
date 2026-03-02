using incode2.Models;
using System.Security.Cryptography;
using System.Text;

namespace incode2.Services.Point
{
    public class EcpayService
    {
        private readonly string MerchantID;
        private readonly string HashKey;
        private readonly string HashIV;
        private readonly string ReturnUrl;
        private readonly IConfiguration _config;

        public EcpayService(IConfiguration config)
        {
            MerchantID = config["ECPay:MerchantID"] ?? throw new InvalidOperationException("ECPay:MerchantID 未設定");
            HashKey = config["ECPay:HashKey"] ?? throw new InvalidOperationException("ECPay:HashKey 未設定");
            HashIV = config["ECPay:HashIV"] ?? throw new InvalidOperationException("ECPay:HashIV 未設定");
            ReturnUrl = config["ECPay:ReturnUrl"] ?? throw new InvalidOperationException("ECPay:ReturnUrl 未設定");
        }

        public Dictionary<string, string> CreatePayment(
            PointRecharge order)
        {
            var data = new Dictionary<string, string>
        {
            { "MerchantID", MerchantID },
            { "MerchantTradeNo", order.OrderNo },
            { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
            { "PaymentType", "aio" },
            { "TotalAmount", ((int)order.CashAmount).ToString() },
            { "TradeDesc", "平台點數儲值" },
            { "ItemName", $"儲值 {order.CashAmount} 元 獲得 {order.PointsGranted} 點" },
            { "ReturnURL", ReturnUrl },
            { "ClientBackURL", "https://localhost:5173/recharge-result" },
            { "ChoosePayment", "ALL" },
            { "EncryptType", "1" }
        };

            data.Add("CheckMacValue", GenerateCheckMacValue(data));
            return data;
        }

        public string GenerateCheckMacValue(Dictionary<string, string> data)
        {
            // 1. 排序：按鍵名字典序排序（忽略大小寫）
            var ordered = data.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                              .Select(x => $"{x.Key}={x.Value}");

            // 2. 串接：HashKey + 參數 + HashIV
            var raw = $"HashKey={HashKey}&{string.Join("&", ordered)}&HashIV={HashIV}";

            // 3. URL Encode
            var encoded = System.Web.HttpUtility.UrlEncode(raw);

            // 4. 特殊字元替換（綠界文件要求）
            // 請確保替換順序和方式與綠界一致
            encoded = encoded.ToLower()
                .Replace("%2d", "-")
                .Replace("%5f", "_")
                .Replace("%2e", ".")
                .Replace("%21", "!")
                .Replace("%2a", "*")
                .Replace("%28", "(")
                .Replace("%29", ")")
                .Replace("%20", "+"); // 注意：有些版本不需替換 %20，這裡以綠界文件為主

            // 5. SHA256
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(encoded));
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }

        // 在 EcpayService.cs 裡面新增這個方法
        public bool ValidateCheckMacValue(IFormCollection form)
        {
            // 取得收到的 CheckMacValue
            string receivedCheckMacValue = form["CheckMacValue"].ToString() ?? "";

            if (string.IsNullOrEmpty(receivedCheckMacValue))
            {
                return false;
            }

            // 收集所有參數（排除 CheckMacValue 本身）
            var parameters = new Dictionary<string, string?>();

            foreach (var key in form.Keys)
            {
                if (key != "CheckMacValue")
                {
                    parameters[key] = form[key].ToString();
                }
            }

            // 按鍵名字典序排序（忽略大小寫）
            var orderedParams = parameters
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{x.Key}={x.Value ?? ""}");

            // 串接 HashKey & 參數 & HashIV
            var rawString = $"HashKey={HashKey}&{string.Join("&", orderedParams)}&HashIV={HashIV}";

            // URL Encode + 綠界指定的特殊替換
            var encoded = System.Web.HttpUtility.UrlEncode(rawString)
                .ToLower()
                .Replace("%21", "!")
                .Replace("%2a", "*")
                .Replace("%28", "(")
                .Replace("%29", ")")
                .Replace("%20", "+");

            // SHA256 雜湊
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encoded));

            // 轉成大寫十六進位
            var computedCheckMacValue = BitConverter.ToString(hashBytes)
                .Replace("-", "")
                .ToUpper();

            // 比對（忽略大小寫）
            return string.Equals(computedCheckMacValue, receivedCheckMacValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}
