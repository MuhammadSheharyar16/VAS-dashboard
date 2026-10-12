using System.Data.Common;
using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vas.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Gives each connection an Entra token for the SQL resource. In Azure the credential is the
/// App Service managed identity; on a developer machine it is the developer's own az login.
/// There is no client secret, and this token is never the executive's API token.
/// </summary>
public sealed class FabricAccessTokenInterceptor(TokenCredential credential) : DbConnectionInterceptor
{
    public const string SqlScope = "https://database.windows.net/.default";

    private static readonly TokenRequestContext TokenRequest = new([SqlScope]);

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        if (connection is SqlConnection sqlConnection)
        {
            sqlConnection.AccessToken = credential.GetToken(TokenRequest, CancellationToken.None).Token;
        }

        return result;
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection sqlConnection)
        {
            var token = await credential.GetTokenAsync(TokenRequest, cancellationToken);
            sqlConnection.AccessToken = token.Token;
        }

        return result;
    }
}
