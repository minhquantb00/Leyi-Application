using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Leyi.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Used only by <c>dotnet ef</c> (migrations add / script / database update).
/// <c>migrations add</c> needs no database. For <c>database update</c>, set the
/// <c>ConnectionStrings__SqlServer</c> environment variable; no credential is stored in source.
/// </summary>
internal sealed class LeyiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LeyiDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__SqlServer";
    private const string PlaceholderConnectionString = "Server=localhost;Database=Leyi;Integrated Security=false;TrustServerCertificate=True";

    public LeyiDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        var options = new DbContextOptionsBuilder<LeyiDbContext>()
            .UseSqlServer(
                string.IsNullOrWhiteSpace(connectionString) ? PlaceholderConnectionString : connectionString,
                PersistenceDependencyInjection.ConfigureSqlServer)
            .Options;

        return new LeyiDbContext(options);
    }
}
