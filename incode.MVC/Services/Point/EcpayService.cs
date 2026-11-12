using System.Security.Cryptography;
using System.Text;

namespace incode.MVC.Services.Point
{
    public class EcpayService
    {
        /// <summary>
        /// 綠界公開的測試商店。它的 HashKey / HashIV 也是公開的，任何人都能算出正確的 CheckMacValue 偽造付款成功通知，
        /// 測試環境也能用公開的測試信用卡付款，所以只能用在開發；正式環境用到會拒絕啟動（見 <see cref="CheckSettings"/>）
        /// </summary>
        public const string PublicTestMerchantId = "3002607";

        private readonly string MerchantID;
        private readonly string HashKey;
        private readonly string HashIV;
        private readonly string ReturnUrl;     // 綠界伺服器端付款結果通知 (需公開網址，開發時用 ngrok)
        private readonly string ClientBackUrl; // 付款完成後使用者被導回的前端頁面
        private readonly IConfiguration _config;

        public EcpayService(IConfiguration config)
        {
            // appsettings.json 為空字串（測試金鑰只放在 appsettings.Development.json），空字串視同未設定
            MerchantID = Required(config, "ECPay:MerchantID");
            HashKey = Required(config, "ECPay:HashKey");
            HashIV = Required(config, "ECPay:HashIV");
            ReturnUrl = Required(config, "ECPay:ReturnUrl");
            ClientBackUrl = $"{config.GetFrontendUrl()}/recharge-result";
        }

        /// <summary>目前使用的是綠界公開的測試商店（付款通知可能是偽造或模擬的）</summary>
        public bool IsTestMerchant => MerchantID == PublicTestMerchantId;

        private static string Required(IConfiguration config, string key)
        {
            var value = config[key];
            return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{key} 未設定") : value;
        }

        /// <summary>
        /// 啟動時檢查綠界設定，有問題回傳錯誤訊息（Program.cs 會拒絕啟動）。
        /// 非 Development 環境不能使用公開的測試商店，除非明確設定 ECPay:AllowTestMerchant=true（例如對外展示用的環境）
        /// </summary>
        public static string? CheckSettings(IConfiguration config, IHostEnvironment environment)
        {
            if (environment.IsDevelopment()) return null;
            if (config["ECPay:MerchantID"] != PublicTestMerchantId) return null;
            if (config.GetValue<bool>("ECPay:AllowTestMerchant")) return null;

            return $"{environment.EnvironmentName} 環境不可使用綠界公開的測試商店（{PublicTestMerchantId}）：測試的 HashKey / HashIV 是公開的，"
                + "任何人都能偽造付款成功通知替自己加點。請以環境變數設定正式商店（ECPay__MerchantID、ECPay__HashKey、ECPay__HashIV、ECPay__ReturnUrl）；"
                + "若是展示用的環境、確定要用測試金流，設定 ECPay__AllowTestMerchant=true";
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
            { "ClientBackURL", ClientBackUrl }, // 付款完成後導回前端的頁面
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
