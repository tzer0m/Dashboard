using Dashboard.Models;
using Dashboard.Models.Uptime;
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
/// <param name="uptimeSummaryStore">The singleton uptime summary store.</param>
public class IndexModel(StatusStore statusStore, IConfiguration configuration, UptimeSummaryStore uptimeSummaryStore) : PageModel
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
    /// Stores the latest computed 30-day uptime summary for each service.
    /// </summary>
    private readonly UptimeSummaryStore UptimeSummaryStore = uptimeSummaryStore;

    /// <summary>
    /// Services grouped by device name, populated on GET.
    /// </summary>
    public Dictionary<string, List<ServiceEntry>> ServicesByDevice { get; set; } = [];

    /// <summary>
    /// Latest cached status for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

    /// <summary>
    /// 30-day uptime summaries for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, UptimeSummary> UptimeSummaries { get; set; } = new Dictionary<string, UptimeSummary>();

    /// <summary>
    /// Loads services from config and reads cached statuses and uptime summaries.
    /// </summary>
    public void OnGet()
    {
        // Load the list of services from config, grouped by device.
        Response.Headers.CacheControl = "no-store";
        List<ServiceEntry> services = Configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];
        ServicesByDevice = services.GroupBy(s => s.LocalIp).ToDictionary(g => g.Key, g => g.ToList());
        Statuses = StatusStore.GetAll();
        UptimeSummaries = UptimeSummaryStore.GetAll();
    }
}