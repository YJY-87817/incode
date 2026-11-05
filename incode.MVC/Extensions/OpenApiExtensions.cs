using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace incode.MVC.Extensions
{
    /// <summary>
    /// API 文件（OpenAPI + Scalar）。只在開發環境開放，只收錄前台 REST API（api/ 開頭），不含後台 MVC。
    /// 文件：/openapi/v1.json　介面：/scalar/v1
    /// </summary>
    public static class OpenApiExtensions
    {
        private const string MemberCookieScheme = "MemberCookie";

        public static IServiceCollection AddApiDocs(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                // 後台 Razor Controller 不放進文件
                options.ShouldInclude = description =>
                    description.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;

                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "incode API";
                    document.Info.Version = "v1";
                    document.Info.Description =
                        "前台 REST API。登入為 Cookie 驗證：先呼叫登入 API，瀏覽器會自動帶 Incode.Member Cookie。" +
                        "SignalR（/chatHub）不在此文件中。";

                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    document.Components.SecuritySchemes[MemberCookieScheme] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Cookie,
                        Name = "Incode.Member",
                        Description = "會員登入後由伺服器設定的 Cookie"
                    };
                    return Task.CompletedTask;
                });
            });
            return services;
        }

        /// <summary>僅在開發環境對應 /openapi/v1.json 與 /scalar/v1（需放在 MapControllers 之後）</summary>
        public static WebApplication MapApiDocs(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.WithTitle("incode API");
                });
            }
            return app;
        }
    }
}
