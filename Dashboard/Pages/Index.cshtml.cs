using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dashboard.Pages;

/// <summary>
/// Page model for the main dashboard, grouped by device.
/// </summary>
/// <remarks>
/// Initialises a new instance of <see cref="IndexModel"/>.
/// </remarks>
/// <param name="statusStore">The singleton status store.</param>
/// <param name="configuration">The app configuration.</param>
public class IndexModel(StatusStore statusStore, IConfiguration configuration) : PageModel
{
    /// <summary>
    /// Stores the current status of all services, grouped by device.
    /// </summary>
    private readonly StatusStore StatusStore = statusStore;

    /// <summary>
    /// Contains the configuration for the dashboard, including the list of devices and services to monitor.
    /// </summary>
    private readonly IConfiguration Configuration = configuration;

    /// <summary>
    /// Services grouped by device name, populated on GET.
    /// </summary>
    public Dictionary<string, List<ServiceEntry>> ServicesByDevice { get; set; } = [];

    /// <summary>
    /// Latest cached status for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

    /// <summary>
    /// The live diagrams.net viewer URL for the rack cabling diagram, read from config.
    /// </summary>
    public string DiagramUrl { get; set; } = string.Empty;

    /// <summary>
    /// Count of services currently reporting online.
    /// </summary>
    public int OnlineCount { get; set; }

    /// <summary>
    /// Count of services currently reporting offline.
    /// </summary>
    public int OfflineCount { get; set; }

    /// <summary>
    /// Count of services with no cached status yet.
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Bootstrap background class for the overall status summary card: green when every service is online, red otherwise.
    /// </summary>
    public string OverallStatusClass { get; set; } = string.Empty;

    /// <summary>
    /// Headline text for the overall status summary card.
    /// </summary>
    public string OverallStatusText { get; set; } = string.Empty;

    /// <summary>
    /// The most recent timestamp across all cached service statuses, shown at the top of the page rather than per-card since a refresh now always updates everything at once. Null if no statuses are cached yet.
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Loads services from config and reads cached statuses.
    /// </summary>
    public void OnGet()
    {
        // Load the list of services from config, grouped by device.
        Response.Headers.CacheControl = "no-store";
        List<ServiceEntry> services = Configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];
        ServicesByDevice = services.GroupBy(s => s.LocalIp).ToDictionary(g => g.Key, g => g.ToList());
        Statuses = StatusStore.GetAll();
        DiagramUrl = Configuration.GetSection("Diagram")["Url"] ?? string.Empty;
        OnlineCount = Statuses.Values.Count(status => status.IsOnline);
        OfflineCount = Statuses.Values.Count(status => !status.IsOnline);
        PendingCount = services.Count - Statuses.Count;
        OverallStatusClass = OfflineCount == 0 ? "bg-success" : "bg-danger";
        OverallStatusText = OfflineCount == 0 ? "All Services Online" : $"{OfflineCount} Service{(OfflineCount == 1 ? string.Empty : "s")} Offline";
        LastUpdated = Statuses.Count > 0 ? Statuses.Values.Max(status => status.LastChecked) : null;
    }
}