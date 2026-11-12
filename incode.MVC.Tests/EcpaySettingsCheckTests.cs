using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace incode.MVC.Tests;

/// <summary>正式環境不能用綠界公開的測試商店（第二次檢查 37）。</summary>
public class EcpaySettingsCheckTests
{
    private sealed class Env : IHostEnvironment
    {
        public Env(string name) => EnvironmentName = name;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "incode.MVC";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(string merchantId, bool? allowTest = null)
    {
        var values = new Dictionary<string, string?> { ["ECPay:MerchantID"] = merchantId };
        if (allowTest.HasValue) values["ECPay:AllowTestMerchant"] = allowTest.Value ? "true" : "false";
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Test_merchant_outside_development_is_rejected(string environment)
    {
        var error = EcpayService.CheckSettings(Config(EcpayService.PublicTestMerchantId), new Env(environment));

        Assert.NotNull(error);
        Assert.Contains("ECPay__AllowTestMerchant", error);
    }

    [Fact]
    public void Test_merchant_is_allowed_in_development()
        => Assert.Null(EcpayService.CheckSettings(Config(EcpayService.PublicTestMerchantId), new Env("Development")));

    [Fact]
    public void Test_merchant_can_be_explicitly_allowed_for_a_demo_environment()
        => Assert.Null(EcpayService.CheckSettings(Config(EcpayService.PublicTestMerchantId, allowTest: true), new Env("Production")));

    [Theory]
    [InlineData("1234567")]
    [InlineData("")] // 沒有設定綠界：儲值功能無法使用，但網站照常啟動
    public void Real_or_missing_merchant_is_fine_in_production(string merchantId)
        => Assert.Null(EcpayService.CheckSettings(Config(merchantId, allowTest: false), new Env("Production")));
}
