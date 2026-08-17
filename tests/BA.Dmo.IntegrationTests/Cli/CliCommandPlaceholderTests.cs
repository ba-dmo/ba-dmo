using BA.Dmo.Web.Cli;

namespace BA.Dmo.IntegrationTests.Cli;

/// <summary>
/// U-01 technical contract test: CLI-only placeholders must never pretend success.
/// The real migrate runner arrives in U-02 and bootstrap-admin in U-05; until then the
/// verbs must fail explicitly (non-zero exit) instead of silently doing nothing
/// (GLM-ARCH-15; Render pre-deploy relies on exit codes — 06_DATA/U-22).
/// These assertions are expected to be replaced by the real command tests in U-02/U-05.
/// </summary>
public class CliCommandPlaceholderTests
{
    [Fact]
    public void Migrate_Placeholder_FailsExplicitly()
    {
        var exitCode = MigrateCommand.Run();

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void BootstrapAdmin_Placeholder_FailsExplicitly()
    {
        var exitCode = BootstrapAdminCommand.Run();

        Assert.NotEqual(0, exitCode);
    }
}
