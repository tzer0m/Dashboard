using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Hubs;

/// <summary>
/// Hub used to push live service status and deploy badge updates to connected dashboard clients.
/// </summary>
/// <param name="kumaStatusService">The service that pulls monitor status from Kuma.</param>
/// <param name="badgeStore">The store holding the latest deploy badge statuses.</param>
public class ServiceStatusHub(KumaStatusService kumaStatusService, BadgeStore badgeStore) : Hub
{
    private readonly KumaStatusService KumaStatusService = kumaStatusService;

    /// <summary>
    /// Triggers an immediate refresh from Kuma for every configured service, bypassing the normal polling interval. Broadcasts to all connected clients.
    /// </summary>
    public async Task RequestRefresh()
    {
        await KumaStatusService.RefreshAllAsync();
    }

    /// <summary>
    /// Returns the latest deploy badge status for every repo, so a newly connected client can fill its badges straight away.
    /// </summary>
    public Dictionary<string, GitHubBadgeStatus> GetRepoBadges()
    {
        return badgeStore.GetAll();
    }
}