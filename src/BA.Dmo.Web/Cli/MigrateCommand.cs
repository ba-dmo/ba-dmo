namespace BA.Dmo.Web.Cli;

/// <summary>
/// CLI entry for forward-only schema migrations (GLM-ARCH-15, CLI ONLY).
/// U-01 provides only the routing placeholder; the Npgsql full-script runner,
/// schema_migrations tracking and SHA-256 checksums are implemented in U-02.
/// Migrations never run from HTTP endpoints, hosted services or production web startup.
/// </summary>
public static class MigrateCommand
{
    public static int Run()
    {
        Console.Error.WriteLine(
            "BA DMO migrate: not available yet (migration runner is delivered by roadmap unit U-02).");
        return 1;
    }
}
