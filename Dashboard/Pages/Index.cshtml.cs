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
/// <param name="dnsUpdateService">The service for reading DNS update records.</param>
public class IndexModel(StatusStore statusStore, IConfiguration configuration, DnsUpdateService dnsUpdateService) : PageModel
{
    private readonly StatusStore StatusStore = statusStore;
    private readonly IConfiguration Configuration = configuration;
    private readonly DnsUpdateService DnsUpdateService = dnsUpdateService;

    /// <summary>
    /// Services grouped by device name, populated on GET.
    /// </summary>
    public Dictionary<string, List<ServiceEntry>> ServicesByDevice { get; set; } = [];

    /// <summary>
    /// Latest cached status for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

    /// <summary>
    /// The most recent DNS update records.
    /// </summary>
    public List<DnsUpdate> DnsUpdates { get; set; } = [];

    /// <summary>
    /// Loads services from config, reads cached statuses and fetches DNS update records.
    /// </summary>
    public async Task OnGetAsync()
    {
        List<ServiceEntry> services = Configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];
        ServicesByDevice = services.GroupBy(s => s.Device).ToDictionary(g => g.Key, g => g.ToList());
        Statuses = StatusStore.GetAll();
        DnsUpdates = await DnsUpdateService.GetRecentAsync();
    }
}