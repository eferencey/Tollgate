// Wires the SQLite database and the three stores built on it, and applies migrations at startup.
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tollgate.Application;

namespace Tollgate.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <param name="databasePath">Resolved lazily so it can come from validated options.</param>
    public static IServiceCollection AddPersistence(this IServiceCollection services, Func<IServiceProvider, string> databasePath)
    {
        // A factory (short-lived context per operation) because the stores are singletons.
        services.AddDbContextFactory<TollgateDbContext>((provider, options) =>
            options.UseSqlite(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath(provider),
                Pooling = false, // release the file when a context closes, so it can be deleted or synced (OneDrive!)
            }.ToString()));

        services.AddSingleton<IEvidenceCache, SqliteEvidenceCache>();
        services.AddSingleton<IScanHistory, SqliteScanHistory>();
        services.AddSingleton<IScanStatistics, SqliteScanStatistics>();
        return services;
    }

    /// <summary>Creates the database file and brings its schema up to date.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<TollgateDbContext>>().CreateDbContextAsync(cancellationToken);

        var file = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);

        await db.Database.MigrateAsync(cancellationToken);
    }
}
