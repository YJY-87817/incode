using incode2.Models;
using System.Security.Cryptography;
using System.Text;

namespace incode2.Services.Point
{
    public class EcpayService
    {
        private const string MerchantID = "3002599";
        private const string HashKey = "spPjZn66i0OhqJsQ";
        private const string HashIV = "hT5OJckN45isQTTs";
        private const string ReturnUrl = "https://separative-learnable-immanuel.ngrok-free.dev/api/PointRechargeApi/ecpay/return";

        private readonly IConfiguration _config;

        public EcpayService(IConfiguration config)
        {
            _config = config;
        }

        public Dictionary<string, string> CreatePayment(
            PointRecharge order)
        {
            var data = new Dictionary<string, string>
        {
            { "MerchantID", "3002599" },
            { "MerchantTradeNo", order.OrderNo },
            { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
            { "PaymentType", "aio" },
            { "TotalAmount", ((int)order.CashAmount).ToString() },
            { "TradeDesc", "PointRecharge" },
            { "ItemName", $"Point {order.PointsGranted}" },
            { "ReturnURL", "https://separative-learnable-immanuel.ngrok-free.dev/api/PointRechargeApi/ecpay/return" },
            { "ChoosePayment", "ALL" },
            { "EncryptType", "1" }
        };

            data.Add("CheckMacValue", GenerateCheckMacValue(data));
            return data;
        }

        private string GenerateCheckMacValue(Dictionary<string, string> data)
        {
            var hashKey = "spPjZn66i0OhqJsQ";
            var hashIV = "hT5OJckN45isQTTs";

            var ordered = data
        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        .Select(x => $"{x.Key}={x.Value}");

            var raw = $"HashKey={hashKey}&{string.Join("&", ordered)}&HashIV={hashIV}";

            var encoded = System.Web.HttpUtility.UrlEncode(raw).ToLower()
                .Replace("%21", "!")
                .Replace("%2a", "*")
                .Replace("%28", "(")
                .Replace("%29", ")");

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(encoded));
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }
    }

}
