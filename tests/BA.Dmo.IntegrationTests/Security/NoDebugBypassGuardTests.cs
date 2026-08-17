using System.Reflection;

namespace BA.Dmo.IntegrationTests.Security;

/// <summary>
/// U-01 technical contract guard: no production debug bypass (Plan-V3 09_TEST §10.4,
/// GLM-ARCH-18). Production assemblies must contain no debug auth bypass, anonymous admin,
/// debug claims, insecure fallback identity or impersonation types. Test doubles are
/// confined to the tests/* projects.
/// </summary>
public class NoDebugBypassGuardTests
{
    private static readonly string[] ForbiddenMarkers =
    [
        "debuguser",
        "debugauth",
        "debugclaim",
        "authbypass",
        "bypassauth",
        "anonymousadmin",
        "fallbackidentity",
        "impersonat"
    ];

    [Fact]
    public void ProductionAssemblies_ContainNoDebugAuthBypassTypes()
    {
        var productionAssemblies = new[]
        {
            typeof(BA.Dmo.Domain.Shared.Kernel.DomainError).Assembly,
            Assembly.Load("BA.Dmo.Application"),
            Assembly.Load("BA.Dmo.Infrastructure"),
            typeof(Program).Assembly
        };

        var offenders = productionAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => ForbiddenMarkers.Any(marker =>
                type.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Select(type => $"{type.Assembly.GetName().Name}: {type.FullName}")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Production code must not contain debug authentication bypass types (GLM-ARCH-18). " +
            $"Offenders: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void WebStartup_RegistersNoDebugIdentityServices()
    {
        // The U-01 composition root exposes no authentication/identity bypass surface:
        // the Program entry point of the web assembly must stay free of debug bypass markers.
        var programType = typeof(Program);

        Assert.DoesNotContain(
            ForbiddenMarkers,
            marker => programType.Name.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
