using BA.Dmo.Web.Cli;

// BA DMO — single composition root (Plan-V3 GLM-ARCH-07).
//
// Operational CLI verbs are distinguished by process arguments; there is no separate CLI
// project (GLM-ARCH-15):
//   migrate            → dotnet BA.Dmo.Web.dll migrate
//   bootstrap-admin    → dotnet BA.Dmo.Web.dll bootstrap-admin
//   (omission)         → normal web startup
// CLI verbs are CLI ONLY: no HTTP migration endpoint, no hosted-service automation,
// no privileged action on normal production web startup.
var mode = CliModeResolver.Resolve(args);
switch (mode)
{
    case CliMode.Migrate:
        return MigrateCommand.Run();
    case CliMode.BootstrapAdmin:
        return BootstrapAdminCommand.Run();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRouting();
app.MapRazorPages();

app.Run();
return 0;

// Exposes the generated entry point to the integration test project (tests/* only).
public partial class Program;
