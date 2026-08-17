namespace BA.Dmo.Web.Cli;

/// <summary>
/// CLI entry for the one-shot privileged bootstrap of the first Admin
/// (GLM-ARCH-15, CLI ONLY; 06_DATA §15).
/// U-01 provides only the routing placeholder; the operation itself is delivered by
/// roadmap unit U-05. There is no anonymous admin, no startup bootstrap and no HTTP
/// equivalent of this command.
/// </summary>
public static class BootstrapAdminCommand
{
    public static int Run()
    {
        Console.Error.WriteLine(
            "BA DMO bootstrap-admin: not available yet (bootstrap operation is delivered by roadmap unit U-05).");
        return 1;
    }
}
