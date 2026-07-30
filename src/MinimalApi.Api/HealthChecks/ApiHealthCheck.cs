using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MinimalApi.Api.HealthChecks;

public sealed class ApiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("API is available."));
}
