using System.Net;
using Microsoft.Extensions.Configuration;

namespace incode.MVC.Tests;

/// <summary>以「正式商店」（不是綠界公開的測試商店）設定啟動網站。</summary>
public class RealMerchantApiFactory : ApiFactory
{
    public static readonly Dictionary<string, string?> Ecpay = new()
    {
        ["ECPay:MerchantID"] = "1234567",
        ["ECPay:HashKey"] = "RealHashKey00001",
        ["ECPay:HashIV"] = "RealHashIV000001",
        ["ECPay:ReturnUrl"] = "https://example.com/api/PointRechargeApi/ecpay/return",
    };

    protected override Dictionary<string, string?> ExtraConfiguration => Ecpay;
}

/// <summary>綠界付款通知（第二次檢查 37）。</summary>
public class EcpayCallbackTests : IClassFixture<RealMerchantApiFactory>
{
    private readonly RealMerchantApiFactory _factory;

    public EcpayCallbackTests(RealMerchantApiFactory factory) => _factory = factory;

    private static EcpayService SigningService()
        => new(new ConfigurationBuilder().AddInMemoryCollection(RealMerchantApiFactory.Ecpay).Build());

    private async Task<string> CreateOrder(int userId)
    {
        await using var context = _factory.Db.NewContext();
        return (await new PointRechargeService(context).CreateOrderAsync(userId, 100, "ecpay")).OrderNo;
    }

    private async Task<HttpResponseMessage> Notify(string orderNo, string simulatePaid)
    {
        var fields = new Dictionary<string, string>
        {
            ["MerchantID"] = RealMerchantApiFactory.Ecpay["ECPay:MerchantID"]!,
            ["MerchantTradeNo"] = orderNo,
            ["RtnCode"] = "1",
            ["RtnMsg"] = "交易成功",
            ["TradeNo"] = "T" + orderNo,
            ["TradeAmt"] = "100",
            ["PaymentType"] = "Credit_CreditCard",
            ["SimulatePaid"] = simulatePaid,
        };
        fields["CheckMacValue"] = SigningService().GenerateCheckMacValue(fields);
        return await _factory.CreateAnonymousClient()
            .PostAsync("/api/PointRechargeApi/ecpay/return", new FormUrlEncodedContent(fields));
    }

    private async Task<(string Status, int Balance)> State(int userId, string orderNo)
    {
        await using var context = _factory.Db.NewContext();
        var status = (await context.PointRecharge.AsNoTracking().SingleAsync(r => r.OrderNo == orderNo)).Status;
        return (status, await new PointLogService(context).GetCurrentBalanceAsync(userId));
    }

    [Fact]
    public async Task Real_payment_notification_grants_points()
    {
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var orderNo = await CreateOrder(userId);

        var response = await Notify(orderNo, simulatePaid: "0");

        Assert.Equal("1|OK", await response.Content.ReadAsStringAsync());
        Assert.Equal(("success", 1000), await State(userId, orderNo));
    }

    [Fact]
    public async Task Simulated_payment_on_a_real_merchant_does_not_grant_points()
    {
        // 綠界後台的「模擬付款」會送出 SimulatePaid=1 的成功通知，實際沒有收到錢
        var userId = _factory.Db.SeedUser($"{Guid.NewGuid():N}@example.com");
        var orderNo = await CreateOrder(userId);

        var response = await Notify(orderNo, simulatePaid: "1");

        Assert.Equal("1|OK", await response.Content.ReadAsStringAsync()); // 回 OK，綠界才不會一直重送
        Assert.Equal(("pending", 0), await State(userId, orderNo));
    }
}
