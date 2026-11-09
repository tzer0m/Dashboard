using Dashboard.Hubs;
using Dashboard.Models;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Services;

/// <summary>
/// Background service that checks every configured repo's deploy status once a minute and pushes any changes to connected dashboard clients.
/// </summary>
/// <param name="scopeFactory">Factory used to resolve the badge service, a typed HTTP client, once per cycle.</param>
/// <param name="badgeStore">The singleton store for the latest badge statuses.</param>
/// <param name="hubContext">The hub context used to broadcast badge updates.</param>
/// <param name="hostStore">The store holding the hosts and their services.</param>
/// <param name="logger">The logger instance.</param>
public class GitHubBadgeRefreshService(IServiceScopeFactory scopeFactory, BadgeStore badgeStore, IHubContext<ServiceStatusHub> hubContext, HostStore hostStore, ILogger<GitHubBadgeRefreshService> logger) : BackgroundService
{
    /// <summary>
    /// Interval between deploy status checks, matching the badge service's cache duration.
    /// </summary>
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Runs the refresh loop until the host shuts down.
    /// </summary>
    /// <param name="stoppingToken">Token that signals when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAllAsync(stoppingToken);
            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Fetches the deploy status for every configured repo, stores it, and broadcasts only the repos whose status changed.
    /// </summary>
    /// <param name="cancellationToken">Token that signals when the operation should stop.</param>
    private async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        // Read the repos fresh each cycle so edits take effect without a restart.
        List<string> repos = [.. hostStore.GetAll().SelectMany(h => h.Services).Select(s => s.RepoName ?? string.Empty).Where(r => r.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
        Dictionary<string, GitHubBadgeStatus> previous = badgeStore.GetAll();
        Dictionary<string, GitHubBadgeStatus> changed = new(StringComparer.OrdinalIgnoreCase);
        using IServiceScope scope = scopeFactory.CreateScope();
        GitHubBadgeService gitHubBadgeService = scope.ServiceProvider.GetRequiredService<GitHubBadgeService>();

        // Check each repo on its own, so one failing repo doesn't stop the rest from updating.
        foreach (string repo in repos)
        {
            try
            {
                GitHubBadgeStatus status = await gitHubBadgeService.GetStatusAsync(repo, "deploy.yml", "master", cancellationToken);
                badgeStore.Set(repo, status);
                if (!previous.TryGetValue(repo, out GitHubBadgeStatus? last) || last.Message != status.Message || last.Color != status.Color) changed[repo] = status;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Failed to refresh deploy status for {Repo}: {Message}", repo, ex.Message);
            }
        }

        // Drop repos that are no longer configured, then push the changes in a single message.
        badgeStore.RemoveExcept(repos);
        if (changed.Count > 0) await hubContext.Clients.All.SendAsync("RepoBadgesUpdated", changed, cancellationToken);
    }
}