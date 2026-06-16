namespace Dashboard.Models;

/// <summary>
/// Represents a single DNS IP update record from the database.
/// </summary>
public class DnsUpdate
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the IP address that was set.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this IP was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; }
}