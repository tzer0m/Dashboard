using Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Data;

/// <summary>
/// EF Core database context for the dashboard.
/// </summary>
/// <remarks>
/// Initialises a new instance of <see cref="DashboardDbContext"/>.
/// </remarks>
/// <param name="options">The database context options.</param>
public class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{

    /// <summary>
    /// Gets or sets the DNS update records.
    /// </summary>
    public DbSet<DnsUpdate> DnsUpdates { get; set; }
}