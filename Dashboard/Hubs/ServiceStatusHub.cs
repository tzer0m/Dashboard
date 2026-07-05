using Dashboard.Services;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Hubs;

/// <summary>
/// Hub used to push live service status updates to connected dashboard clients.
/// </summary>
public class ServiceStatusHub(HealthCheckService healthCheckService) : Hub
{
    private readonly HealthCheckService HealthCheckService = healthCheckService;

    /// <summary>
    /// Triggers an immediate, out-of-band health check for a single service,
    /// bypassing the normal 60-second cycle. Result is sent back to the caller only.
    /// </summary>
    /// <param name="serviceName">The name of the service to check.</param>
    public async Task RequestRefresh(string serviceName)
    {
        await HealthCheckService.CheckSingleServiceAsync(serviceName);
    }
}