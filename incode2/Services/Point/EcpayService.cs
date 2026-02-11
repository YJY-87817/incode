using incode2.Models;
using System.Security.Cryptography;
using System.Text;

namespace incode2.Services.Point
{
    public class EcpayService
    {
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
            { "MerchantID", "2000132" },
            { "MerchantTradeNo", order.OrderNo },
            { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
            { "PaymentType", "aio" },
            { "TotalAmount", ((int)order.CashAmount).ToString() },
            { "TradeDesc", "點數儲值" },
            { "ItemName", $"儲值 {order.PointsGranted} 點" },
            { "ReturnURL", "  https://separative-learnable-immanuel.ngrok-free.dev/api/PointRechargeApi/ecpay/return" },
            { "ChoosePayment", "ALL" },
            { "EncryptType", "1" }
        };

            data.Add("CheckMacValue", GenerateCheckMacValue(data));
            return data;
        }

        private string GenerateCheckMacValue(Dictionary<string, string> data)
        {
            var hashKey = "5294y06JbISpM5x9";
            var hashIV = "v77hoKGq4kWxNNIS";

            var raw = $"HashKey={hashKey}&" +
                      string.Join("&", data.OrderBy(x => x.Key)
                      .Select(x => $"{x.Key}={x.Value}")) +
                      $"&HashIV={hashIV}";

            var encoded = System.Web.HttpUtility.UrlEncode(raw).ToLower();

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(encoded));
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }
    }

}
