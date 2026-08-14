using Dashboard.Hubs;
using Dashboard.Models;
using Dashboard.Models.Kuma;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Dashboard.Services;

/// <summary>
/// Background service that periodically pulls live monitor status from Kuma's public status page API and stores the results in <see cref="StatusStore"/>, replacing the dashboard's own direct HTTP health checks now that Kuma owns monitoring.
/// </summary>
/// <param name="httpClientFactory">Factory used to create the named HTTP client for calling Kuma's API.</param>
/// <param name="statusStore">The singleton store for caching results.</param>
/// <param name="hubContext">The hub context used to broadcast status updates.</param>
/// <param name="configuration">The app configuration containing service entries and Kuma connection settings.</param>
/// <param name="logger">The logger instance.</param>
public class KumaStatusService(IHttpClientFactory httpClientFactory, StatusStore statusStore, IHubContext<ServiceStatusHub> hubContext, IConfiguration configuration, ILogger<KumaStatusService> logger) : BackgroundService
{
    /// <summary>
    /// Options used to deserialise Kuma's camelCase JSON responses into this project's PascalCase models.
    /// </summary>
    private static readonly JsonSerializerOptions KumaJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Http client used to call Kuma's public status page API.
    /// </summary>
    private readonly HttpClient HttpClient = httpClientFactory.CreateClient("KumaStatusService");

    /// <summary>
    /// Store used to cache the latest known status for each service.
    /// </summary>
    private readonly StatusStore StatusStore = statusStore;

    /// <summary>
    /// Hub context used to broadcast status updates to connected dashboard clients.
    /// </summary>
    private readonly IHubContext<ServiceStatusHub> HubContext = hubContext;

    /// <summary>
    /// List of services to monitor, loaded from configuration.
    /// </summary>
    private readonly List<ServiceEntry> Services = configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];

    /// <summary>
    /// The configured service entries being monitored.
    /// </summary>
    public IReadOnlyList<ServiceEntry> ServiceEntries => Services;

    /// <summary>
    /// Logger used to log information and errors.
    /// </summary>
    private readonly ILogger<KumaStatusService> Logger = logger;

    /// <summary>
    /// The base URL of the Kuma instance to pull status from.
    /// </summary>
    private readonly string BaseUrl = (configuration["Kuma:BaseUrl"] ?? string.Empty).TrimEnd('/');

    /// <summary>
    /// The slug of the Kuma status page whose monitors should be reflected on the dashboard.
    /// </summary>
    private readonly string StatusPageSlug = configuration["Kuma:StatusPageSlug"] ?? "default";

    /// <summary>
    /// Interval between polls of Kuma's status API, in seconds.
    /// </summary>
    private readonly int PollIntervalSeconds = configuration.GetValue("Kuma:PollIntervalSeconds", 30);

    /// <summary>
    /// Names of configured services that couldn't be matched to a Kuma monitor, tracked so the warning is only logged once per service rather than on every poll cycle.
    /// </summary>
    private readonly HashSet<string> UnmatchedServicesWarned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the polling loop, refreshing every configured service's status from Kuma on each interval.
    /// </summary>
    /// <param name="stoppingToken">Token that signals when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Services.Count == 0 || string.IsNullOrEmpty(BaseUrl))
        {
            Logger.LogWarning("KumaStatusService is not starting: no services configured or Kuma:BaseUrl is missing.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAllAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken);
        }
    }

    /// <summary>
    /// Triggers an immediate refresh, used by a manual refresh button. Kuma's public API only exposes all monitors in one response, so a "single service" refresh re-fetches everything and re-applies it — cheap, since the payload is small.
    /// </summary>
    /// <param name="serviceName">The name of the service that requested the refresh, used only for logging context.</param>
    public async Task RefreshServiceAsync(string serviceName)
    {
        Logger.LogInformation("Manual refresh requested for {ServiceName}", serviceName);
        await RefreshAllAsync(CancellationToken.None);
    }

    /// <summary>
    /// Fetches the current monitor list and latest heartbeats from Kuma, matches them against the configured services by name, and updates and broadcasts each match.
    /// </summary>
    /// <param name="cancellationToken">Token that signals when the operation should stop.</param>
    private async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            Dictionary<string, int> monitorIdsByName = await FetchMonitorIdsByNameAsync(cancellationToken);
            KumaHeartbeatPageResponse heartbeatPage = await HttpClient.GetFromJsonAsync<KumaHeartbeatPageResponse>($"{BaseUrl}/api/status-page/heartbeat/{StatusPageSlug}", KumaJsonOptions, cancellationToken) ?? new();

            foreach (ServiceEntry service in Services)
            {
                if (!monitorIdsByName.TryGetValue(service.Name, out int monitorId))
                {
                    if (UnmatchedServicesWarned.Add(service.Name))
                    {
                        Logger.LogWarning("No Kuma monitor matches the name of configured service {ServiceName}. Rename the monitor in Kuma to match, or update the service config.", service.Name);
                    }
                    continue;
                }

                if (!heartbeatPage.HeartbeatList.TryGetValue(monitorId.ToString(), out List<KumaHeartbeat>? beats) || beats.Count == 0)
                {
                    continue;
                }

                KumaHeartbeat latest = beats[^1];
                bool isOnline = latest.Status == 1;
                ServiceStatus status = new()
                {
                    IsOnline = isOnline,
                    ResponseTimeMs = latest.Ping ?? 0,
                    LastChecked = DateTime.UtcNow,
                    Error = isOnline ? null : (string.IsNullOrWhiteSpace(latest.Msg) ? "Service is down." : latest.Msg)
                };

                StatusStore.Set(service.Name, status);
                await HubContext.Clients.All.SendAsync("ServiceStatusUpdated", new
                {
                    service.Name,
                    status.IsOnline,
                    status.ResponseTimeMs,
                    status.LastChecked,
                    status.Error
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to refresh status from Kuma: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Fetches the status page's monitor list and builds a lookup of monitor ID by name, so heartbeats can be matched back to configured services.
    /// </summary>
    /// <param name="cancellationToken">Token that signals when the operation should stop.</param>
    private async Task<Dictionary<string, int>> FetchMonitorIdsByNameAsync(CancellationToken cancellationToken)
    {
        KumaStatusPageResponse? page = await HttpClient.GetFromJsonAsync<KumaStatusPageResponse>($"{BaseUrl}/api/status-page/{StatusPageSlug}", KumaJsonOptions, cancellationToken);
        Dictionary<string, int> result = new(StringComparer.OrdinalIgnoreCase);

        if (page == null)
        {
            return result;
        }

        foreach (KumaGroup group in page.PublicGroupList)
        {
            foreach (KumaMonitorInfo monitor in group.MonitorList)
            {
                result[monitor.Name] = monitor.Id;
            }
        }

        return result;
    }
}
