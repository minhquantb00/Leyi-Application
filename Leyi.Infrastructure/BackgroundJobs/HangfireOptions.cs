namespace Leyi.Infrastructure.BackgroundJobs;

/// <summary>
/// Bound to the <c>Hangfire</c> section. Hangfire uses the SQL Server connection string and manages its own schema.
/// </summary>
public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public string SchemaName { get; set; } = "hangfire";

    /// <summary>
    /// Lets Hangfire create/upgrade its schema at startup. Hangfire serializes the install with a database lock.
    /// </summary>
    public bool PrepareSchemaIfNecessary { get; set; } = true;

    /// <summary>Null uses the Hangfire default.</summary>
    public int? WorkerCount { get; set; }
}
