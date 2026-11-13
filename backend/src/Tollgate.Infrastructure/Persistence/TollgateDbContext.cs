// The EF Core model of the SQLite database: the evidence cache and the scan history.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tollgate.Infrastructure.Persistence;

public sealed class TollgateDbContext(DbContextOptions<TollgateDbContext> options) : DbContext(options)
{
    public DbSet<CachedEvidenceRow> CachedEvidence => Set<CachedEvidenceRow>();

    public DbSet<ScanHistoryRow> ScanHistory => Set<ScanHistoryRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedEvidenceRow>(cache =>
        {
            cache.ToTable("CachedEvidence");
            cache.HasKey(row => row.Url);
            cache.Property(row => row.ScannedAt).HasConversion(UtcTicks);
            cache.HasIndex(row => row.ScannedAt); // the cleanup of expired rows scans by age
        });

        modelBuilder.Entity<ScanHistoryRow>(history =>
        {
            history.ToTable("ScanHistory");
            history.Property(row => row.ScannedAt).HasConversion(UtcTicks);
            history.Property(row => row.Verdict).HasConversion<string>(); // readable in a DB browser, safe against enum reordering
            history.Property(row => row.Confidence).HasConversion<string>();
            history.HasIndex(row => row.ScannedAt); // newest-first listing and retention cleanup
            history.HasIndex(row => row.Host); // "most scanned hosts"
        });
    }

    // SQLite cannot compare or sort DateTimeOffset values, but it can do both on integers.
    private static readonly ValueConverter<DateTimeOffset, long> UtcTicks =
        new(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
}
