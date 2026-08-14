using Dashboard.Services;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Hubs;

/// <summary>
/// Hub used to push live service status updates to connected dashboard clients.
/// </summary>
public class ServiceStatusHub(KumaStatusService kumaStatusService) : Hub
{
    private readonly KumaStatusService KumaStatusService = kumaStatusService;

    /// <summary>
    /// Triggers an immediate refresh from Kuma, bypassing the normal polling interval. Broadcasts to all connected clients since Kuma's API returns every monitor's status in one response.
    /// </summary>
    /// <param name="serviceName">The name of the service that requested the refresh.</param>
    public async Task RequestRefresh(string serviceName)
    {
        await KumaStatusService.RefreshServiceAsync(serviceName);
    }
}