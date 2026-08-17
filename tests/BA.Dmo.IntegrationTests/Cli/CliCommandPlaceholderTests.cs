using BA.Dmo.Web.Cli;

namespace BA.Dmo.IntegrationTests.Cli;

/// <summary>
/// CLI contract tests across roadmap units (GLM-ARCH-15).
/// U-02 replaced the migrate placeholder with the real runner; bootstrap-admin
/// remains a placeholder until U-05 (PV-08). Placeholders must never pretend
/// success: non-zero exit keeps Render pre-deploy semantics honest.
/// </summary>
public class CliCommandContractTests
{
    [Fact]
    public void BootstrapAdmin_Placeholder_FailsExplicitly_UntilU05()
    {
        var exitCode = BootstrapAdminCommand.Run();

        Assert.NotEqual(0, exitCode);
    }
}
