using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceGroup = System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<Dashboard.Models.ServiceEntry>>;

namespace Dashboard.Pages;

/// <summary>
/// Page model for the Chrome new-tab page, showing a status summary and service links.
/// </summary>
/// <param name="statusStore">The singleton status store.</param>
/// <param name="configuration">The app configuration.</param>
public class NewTabModel(StatusStore statusStore, IConfiguration configuration) : PageModel
{
    /// <summary>
    /// Stores the current status of all services.
    /// </summary>
    private readonly StatusStore StatusStore = statusStore;

    /// <summary>
    /// Contains the list of hosts and services to show.
    /// </summary>
    private readonly IConfiguration Configuration = configuration;

    /// <summary>
    /// Services grouped by type, ordered by type name, populated on GET.
    /// </summary>
    public List<ServiceGroup> Groups { get; set; } = [];

    /// <summary>
    /// Latest cached status for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

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
    /// Bootstrap background class for the overall status pill.
    /// </summary>
    public string OverallStatusClass { get; set; } = string.Empty;

    /// <summary>
    /// Headline text for the overall status pill.
    /// </summary>
    public string OverallStatusText { get; set; } = string.Empty;

    /// <summary>
    /// Loads services from config and reads cached statuses.
    /// </summary>
    public void OnGet()
    {
        Response.Headers.CacheControl = "no-store";
        List<HostEntry> hosts = Configuration.GetSection("Hosts").Get<List<HostEntry>>() ?? [];
        List<ServiceEntry> services = [.. hosts.SelectMany(h => h.Services).OrderBy(s => s.Name)];
        Groups = [.. services.GroupBy(s => s.Type).OrderBy(g => g.Key).Select(g => new ServiceGroup(g.Key, [.. g]))];
        Statuses = StatusStore.GetAll();
        OnlineCount = Statuses.Values.Count(status => status.IsOnline);
        OfflineCount = Statuses.Values.Count(status => !status.IsOnline);
        PendingCount = services.Count - Statuses.Count;
        OverallStatusClass = OfflineCount == 0 ? "bg-success" : "bg-danger";
        OverallStatusText = OfflineCount == 0 ? "All Online" : $"{OfflineCount} Offline";
    }
}