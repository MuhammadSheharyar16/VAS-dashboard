using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Vas.Api.Startup;

public static class OpenApiExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Microsoft Entra ID access token for the dashboard API.",
                };

                return Task.CompletedTask;
            });

            // The API is closed by default, so every operation needs the token unless it opts out.
            options.AddOperationTransformer((operation, context, _) =>
            {
                var isAnonymous = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<IAllowAnonymous>()
                    .Any();

                if (!isAnonymous)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [],
                    });

                    operation.Responses ??= [];
                    operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No valid token." });
                    operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Signed in but not allowed." });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapApiOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        return app;
    }
}
