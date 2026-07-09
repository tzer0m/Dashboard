using Microsoft.EntityFrameworkCore;
using Dashboard.Models;

namespace Dashboard.Data;

/// <summary>
/// The Entity Framework Core database context for the Dashboard service, backed by the shared Robert1 PostgreSQL database.
/// </summary>
/// <param name="options">The context configuration options.</param>
public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Health check results used to compute rolling uptime percentages and outage history.
    /// </summary>
    public DbSet<UptimeRecord> UptimeRecords => Set<UptimeRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UptimeRecord>(entity =>
        {
            entity.HasIndex(e => new { e.ServiceName, e.CheckedAtUtc });
        });
    }
}