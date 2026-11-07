using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dashboard.Pages;

/// <summary>
/// Page model for the services editor, which edits the hosts and services in the services file.
/// </summary>
/// <param name="hostStore">The store holding the hosts and their services.</param>
/// <param name="statusStore">The singleton status store.</param>
/// <param name="kumaStatusService">The Kuma status service, refreshed after a save.</param>
public class EditModel(HostStore hostStore, StatusStore statusStore, KumaStatusService kumaStatusService) : PageModel
{
    /// <summary>
    /// Holds the hosts and their services.
    /// </summary>
    private readonly HostStore HostStore = hostStore;

    /// <summary>
    /// Stores the current status of all services.
    /// </summary>
    private readonly StatusStore StatusStore = statusStore;

    /// <summary>
    /// Pulls fresh statuses from Kuma after a save.
    /// </summary>
    private readonly KumaStatusService KumaStatusService = kumaStatusService;

    /// <summary>
    /// The hosts and their services being edited.
    /// </summary>
    [BindProperty]
    public List<HostEntry> Hosts { get; set; } = [];

    /// <summary>
    /// Every service type currently in use, offered as suggestions in the Type field.
    /// </summary>
    public List<string> Types { get; set; } = [];

    /// <summary>
    /// Whether the page is showing straight after a successful save.
    /// </summary>
    public bool Saved { get; set; }

    /// <summary>
    /// Loads the hosts and their services for editing.
    /// </summary>
    /// <param name="saved">Whether the page was reached by redirect after a successful save.</param>
    public void OnGet(bool saved)
    {
        Response.Headers.CacheControl = "no-store";
        Hosts = HostStore.GetAll();
        Saved = saved;
        LoadTypes();
    }

    /// <summary>
    /// Validates and saves the edited hosts, clears statuses for removed services, then refreshes from Kuma.
    /// </summary>
    /// <param name="cancellationToken">Token that signals when the request is aborted.</param>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        // Trim every text field, since Kuma monitors are matched by exact service name.
        foreach (HostEntry host in Hosts)
        {
            host.Host = host.Host?.Trim() ?? string.Empty;
            host.Ip = host.Ip?.Trim() ?? string.Empty;
            foreach (ServiceEntry service in host.Services)
            {
                service.Name = service.Name?.Trim() ?? string.Empty;
                service.Url = service.Url?.Trim() ?? string.Empty;
                service.Type = service.Type?.Trim() ?? string.Empty;
                service.RepoName = string.IsNullOrWhiteSpace(service.RepoName) ? null : service.RepoName.Trim();
                service.FaviconUrl = string.IsNullOrWhiteSpace(service.FaviconUrl) ? null : service.FaviconUrl.Trim();

                // Extra monitors come in as one comma-separated field, so split them into the list, leaving it empty rather than blank.
                List<string> extras = [.. (service.ExtraMonitors ?? []).SelectMany(m => (m ?? string.Empty).Split(',')).Select(m => m.Trim()).Where(m => m.Length > 0)];
                service.ExtraMonitors = extras.Count > 0 ? extras : null;
            }
        }

        // Required fields are flagged by model binding; add one summary line for them, plus the checks binding can't do.
        if (!ModelState.IsValid) ModelState.AddModelError(string.Empty, "Fill in the highlighted fields.");
        if (Hosts.Count == 0) ModelState.AddModelError(string.Empty, "There must be at least one host.");
        foreach (string name in Hosts.SelectMany(h => h.Services).SelectMany(s => s.MonitorNames).Where(n => n.Length > 0).GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key)) ModelState.AddModelError(string.Empty, $"{name} is used more than once. Service and monitor names must be unique, as they're matched to Kuma monitors by name.");
        if (!ModelState.IsValid)
        {
            LoadTypes();
            return Page();
        }

        HostStore.Save(Hosts);
        StatusStore.RemoveExcept(Hosts.SelectMany(h => h.Services).SelectMany(s => s.MonitorNames));
        await KumaStatusService.RefreshAllAsync(cancellationToken);
        return RedirectToPage(new { saved = true });
    }

    /// <summary>
    /// Signs out of the editor, then returns to the dashboard.
    /// </summary>
    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    /// <summary>
    /// Fills the list of service type suggestions from the saved services.
    /// </summary>
    private void LoadTypes()
    {
        Types = [.. HostStore.GetAll().SelectMany(h => h.Services).Select(s => s.Type).Where(t => t.Length > 0).Distinct().Order()];
    }
}