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
            MerchantID = config["ECPay:MerchantID"];
            HashKey = config["ECPay:HashKey"];
            HashIV = config["ECPay:HashIV"];
            ReturnUrl = config["ECPay:ReturnUrl"];
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
            { "TradeDesc", "PointRecharge" },
            { "ItemName", $"Point {order.PointsGranted}" },
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
            var ordered = data.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                              .Select(x => $"{x.Key}={x.Value}");
            var raw = $"HashKey={HashKey}&{string.Join("&", ordered)}&HashIV={HashIV}";
            var encoded = System.Web.HttpUtility.UrlEncode(raw).ToLower()
                .Replace("%21", "!").Replace("%2a", "*")
                .Replace("%28", "(").Replace("%29", ")")
                .Replace("%20", "+");

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(encoded));
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }
    }

}
