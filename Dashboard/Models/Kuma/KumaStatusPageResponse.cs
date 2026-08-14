namespace Dashboard.Models.Kuma;

/// <summary>
/// The response from Kuma's public status page config endpoint (<c>/api/status-page/{slug}</c>), used to discover monitor IDs and names.
/// </summary>
public sealed class KumaStatusPageResponse
{
    /// <summary>
    /// The groups of monitors configured on the status page.
    /// </summary>
    public List<KumaGroup> PublicGroupList { get; init; } = [];
}