using BA.Dmo.Application.Shared.Access;
using BA.Dmo.Application.Shared.Identity;
using BA.Dmo.Application.Shared.Persistence;
using BA.Dmo.Domain.Shared.Access;
using BA.Dmo.Domain.Shared.Kernel;
using BA.Dmo.Infrastructure.Auth;
using BA.Dmo.Infrastructure.Identity;
using BA.Dmo.Infrastructure.Persistence;
using BA.Dmo.Web.Authorization;
using BA.Dmo.Web.Cli;
using BA.Dmo.Web.Identity;
using Microsoft.AspNetCore.Authorization;

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

// Persistence foundation (U-03): snake_case ↔ PascalCase mapping conventions
// for Dapper. CLI verbs exit above and never reach this point.
PersistenceMappings.Configure();

// Canonical catalog validation (U-04, GLM-ACC-03): an invalid canonical
// configuration fails explicitly at startup — it is never silently repaired.
CatalogValidator.Validate(
    CanonicalModuleCatalog.Instance,
    CanonicalPageCatalog.Instance,
    CanonicalModuleCatalog.AreaChildren);

builder.Services.AddRazorPages();

// Identity/authentication foundation (U-05, GLM-ACC-01): session cookie
// bridge carrying ONLY the Supabase auth user id; grants are resolved
// server-side per request and never stored in the cookie. The privileged
// provisioning adapter is intentionally NOT registered here — it exists
// only inside the bootstrap-admin CLI path (PV-07).
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddAuthentication(SessionClaims.AuthenticationScheme)
    .AddCookie(SessionClaims.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(SessionClaims.AuthenticationScheme)
        .AddRequirements(new AuthenticatedSessionRequirement())
        .Build();
});
builder.Services.AddSingleton<IAuthorizationHandler, AuthenticatedSessionHandler>();

builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton<IDbConnectionFactory>(
    new LazyDbConnectionFactory(Environment.GetEnvironmentVariable));
builder.Services.AddSingleton<IInternalUserRepository, DapperInternalUserRepository>();
builder.Services.AddSingleton(
    new AccessResolver(
        CanonicalModuleCatalog.Instance,
        CanonicalPageCatalog.Instance,
        CanonicalModuleCatalog.AreaChildren));
builder.Services.AddScoped<IdentityResolutionService>();
builder.Services.AddScoped<ICurrentUserAccessor, RequestCurrentUserAccessor>();
builder.Services.AddScoped<IPersistenceAuthorshipAccessor, CurrentUserAuthorshipAccessor>();
builder.Services.AddSingleton<ISupabaseAuthAdapter>(_ => new SupabaseAuthAdapter(
    new HttpClient(),
    SupabaseSettings.ResolveUrl(Environment.GetEnvironmentVariable),
    SupabaseSettings.ResolveAnonKey(Environment.GetEnvironmentVariable)));

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
return 0;

// Exposes the generated entry point to the integration test project (tests/* only).
public partial class Program;
