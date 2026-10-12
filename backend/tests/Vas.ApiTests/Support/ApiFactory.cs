using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Vas.ApiTests.Support;

/// <summary>
/// Runs the real API with one substitution: the token issuer and signing key are the test ones,
/// so no call is made to Entra ID. Every other check stays as production configures it.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("AzureAd:TenantId", TestTokens.TenantId);
        builder.UseSetting("AzureAd:ClientId", TestTokens.ApiClientId);
        builder.UseSetting("AzureAd:SpaClientId", TestTokens.SpaClientId);

        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
                configuration.SigningKeys.Add(TestTokens.SigningKey);

                options.Configuration = configuration;
                options.TokenValidationParameters.ValidIssuer = TestTokens.Issuer;
                options.TokenValidationParameters.IssuerValidator = null;
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.SigningKey;
            }));
    }

    /// <summary>
    /// The same API as it must be configured outside Development: Fabric as the data source.
    /// Nothing connects to Fabric unless a report is requested.
    /// </summary>
    public WebApplicationFactory<Program> InEnvironment(string environment, Action<IWebHostBuilder>? configure = null) =>
        WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("Reporting:DataSource", "Fabric");
            builder.UseSetting("Fabric:SqlEndpoint", "example.datawarehouse.fabric.microsoft.com");
            builder.UseSetting("Fabric:Database", "gold_lakehouse");
            configure?.Invoke(builder);
        });

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
