using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Api.HealthChecks;

namespace MinimalApi.Api.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddHealthChecks()
            .AddCheck<ApiHealthCheck>("api")
            .AddNpgSql(sp =>
                sp.GetRequiredService<MinimalApi.Infrastructure.Persistence.IDbConnectionFactory>().CreateConnection()
                    .ConnectionString, name: "postgresql", tags: ["ready"]);
        return services;
    }
}