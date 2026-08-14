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
    /// Triggers an immediate refresh from Kuma for every configured service, bypassing the normal polling interval. Broadcasts to all connected clients.
    /// </summary>
    public async Task RequestRefresh()
    {
        await KumaStatusService.RefreshAllAsync();
    }
}