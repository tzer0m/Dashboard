namespace Dashboard.Models;

/// <summary>
/// One host's counts in the Tidgets Dashboard widget summary.
/// </summary>
public class WidgetHost
{
    /// <summary>
    /// The shortest start of the host's name that no other host shares, such as Ty for Tyrion.
    /// </summary>
    public string Short { get; set; } = string.Empty;

    /// <summary>
    /// The number of the host's monitors that are online.
    /// </summary>
    public int Online { get; set; }

    /// <summary>
    /// The number of the host's monitors that are offline.
    /// </summary>
    public int Offline { get; set; }
}