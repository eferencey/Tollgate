// A real SQLite file in a temp folder, wired through the production AddPersistence call. Disposing it deletes the folder;
// building a second instance on the same folder is how the tests "restart the server".
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tollgate.Application;
using Tollgate.Infrastructure.Persistence;

namespace Tollgate.Infrastructure.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider provider;
    private readonly bool ownsDirectory;

    private TestDatabase(string directory, bool ownsDirectory, FakeClock clock, int retentionDays)
    {
        Directory = directory;
        this.ownsDirectory = ownsDirectory;
        Clock = clock;

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(Options.Create(new ScanOptions { CacheDuration = TimeSpan.FromMinutes(60), PollInterval = TimeSpan.FromSeconds(20) }));
        services.AddSingleton(Options.Create(new HistoryOptions { RetentionDays = retentionDays, DefaultPageSize = 20, MaxPageSize = 100, TopHostsCount = 2 }));
        services.AddPersistence(_ => Path.Combine(directory, "data", "test.db"));
        provider = services.BuildServiceProvider();
    }

    public string Directory { get; }

    public FakeClock Clock { get; }

    public IEvidenceCache Cache => provider.GetRequiredService<IEvidenceCache>();

    public IScanHistory History => provider.GetRequiredService<IScanHistory>();

    public IScanStatistics Statistics => provider.GetRequiredService<IScanStatistics>();

    public TollgateDbContext NewContext() => provider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<TollgateDbContext>>().CreateDbContext();

    public static async Task<TestDatabase> CreateAsync(string? existingDirectory = null, FakeClock? clock = null, int retentionDays = 90)
    {
        var directory = existingDirectory ?? System.IO.Directory.CreateTempSubdirectory("tollgate-infra-").FullName;
        var database = new TestDatabase(directory, ownsDirectory: existingDirectory is null, clock ?? new FakeClock(Start), retentionDays);
        await database.provider.MigrateDatabaseAsync();
        return database;
    }

    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
        if (ownsDirectory)
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
