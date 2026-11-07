namespace Dashboard.Models;

/// <summary>
/// The compact status summary shown by the Tidgets Dashboard widget.
/// </summary>
public class WidgetSummary
{
    /// <summary>
    /// The number of monitors online across every host.
    /// </summary>
    public int Online { get; set; }

    /// <summary>
    /// The number of monitors offline across every host.
    /// </summary>
    public int Offline { get; set; }

    /// <summary>
    /// Each host's counts, ordered by host name.
    /// </summary>
    public List<WidgetHost> Hosts { get; set; } = [];
}