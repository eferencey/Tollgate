// The migrations are part of the code: they must build the schema the model expects and not lag behind it.
using Microsoft.EntityFrameworkCore;
using static Tollgate.Infrastructure.Tests.TestDatabase;

namespace Tollgate.Infrastructure.Tests;

public class MigrationTests
{
    [Fact]
    public async Task The_migrations_are_in_sync_with_the_model()
    {
        await using var db = await CreateAsync();
        await using var context = db.NewContext();

        // Fails after someone changes a row class without running "dotnet ef migrations add".
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task A_fresh_database_gets_both_tables_and_applying_the_migrations_again_is_harmless()
    {
        await using var db = await CreateAsync();
        await using var context = db.NewContext();

        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await context.CachedEvidence.CountAsync());
        Assert.Equal(0, await context.ScanHistory.CountAsync());
    }
}
