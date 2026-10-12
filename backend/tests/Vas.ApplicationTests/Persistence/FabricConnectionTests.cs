using Azure.Core;
using Microsoft.Data.SqlClient;
using Vas.Application.Common.Options;
using Vas.Infrastructure;
using Vas.Infrastructure.Persistence.Interceptors;

namespace Vas.ApplicationTests.Persistence;

public class FabricConnectionTests
{
    private sealed class FakeCredential : TokenCredential
    {
        public List<string> RequestedScopes { get; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            RequestedScopes.AddRange(requestContext.Scopes);
            return new AccessToken("token-from-the-credential", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private static readonly FabricOptions Fabric = new()
    {
        SqlEndpoint = "example.datawarehouse.fabric.microsoft.com",
        Database = "gold_lakehouse",
    };

    [Fact]
    public async Task Each_connection_gets_a_token_for_the_sql_resource_from_the_credential()
    {
        var credential = new FakeCredential();
        var interceptor = new FabricAccessTokenInterceptor(credential);
        await using var connection = new SqlConnection(DependencyInjection.BuildConnectionString(Fabric));

        await interceptor.ConnectionOpeningAsync(connection, null!, default);

        Assert.Equal("token-from-the-credential", connection.AccessToken);
        Assert.Equal([FabricAccessTokenInterceptor.SqlScope], credential.RequestedScopes);
    }

    [Fact]
    public void Synchronous_open_gets_the_token_too()
    {
        var interceptor = new FabricAccessTokenInterceptor(new FakeCredential());
        using var connection = new SqlConnection(DependencyInjection.BuildConnectionString(Fabric));

        interceptor.ConnectionOpening(connection, null!, default);

        Assert.Equal("token-from-the-credential", connection.AccessToken);
    }

    [Fact]
    public void Connection_string_is_encrypted_and_carries_no_user_password_or_secret()
    {
        var connectionString = new SqlConnectionStringBuilder(DependencyInjection.BuildConnectionString(Fabric));

        Assert.Equal("tcp:example.datawarehouse.fabric.microsoft.com,1433", connectionString.DataSource);
        Assert.Equal("gold_lakehouse", connectionString.InitialCatalog);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, connectionString.Encrypt);
        Assert.False(connectionString.TrustServerCertificate);
        Assert.Equal(string.Empty, connectionString.UserID);
        Assert.Equal(string.Empty, connectionString.Password);
        Assert.Equal(SqlAuthenticationMethod.NotSpecified, connectionString.Authentication);
    }
}
