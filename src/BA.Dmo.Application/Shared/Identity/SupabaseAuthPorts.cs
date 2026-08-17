using BA.Dmo.Domain.Shared.Kernel;

namespace BA.Dmo.Application.Shared.Identity;

/// <summary>
/// External authentication identity from Supabase Auth (Plan-V3 GLM-ACC-01):
/// the Supabase Auth user UUID. Application authorization is NEVER derived
/// from Supabase role names — only from internal_users → access templates →
/// catalog (GLM-ACC-02/03).
/// </summary>
public sealed record AuthUser(Guid AuthUserId, string Email);

/// <summary>
/// Supabase authentication boundary (Plan-V3 GLM-ARCH-14, PV-06, 06_DATA §14).
/// Application/Web never depend on provider SDK/HTTP types; the concrete
/// implementation lives in Infrastructure behind this port. The normal
/// request pipeline never uses service_role credentials (PV-07).
/// </summary>
public interface ISupabaseAuthAdapter
{
    /// <summary>
    /// Verifies email/password credentials against Supabase Auth. Failures are
    /// generic (never reveal whether an email exists) and fail closed.
    /// </summary>
    Task<Result<AuthUser, DomainError>> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Privileged provisioning boundary (Plan-V3 GLM-ARCH-14, PV-07, 06_DATA §14–15).
/// The ONLY component allowed to use service_role credentials; exclusively
/// for explicit privileged operations (bootstrap-admin). Isolated from the
/// normal authentication pipeline; never reachable from pages/handlers.
/// </summary>
public interface IAdminProvisioningAdapter
{
    /// <summary>
    /// Ensures a Supabase Auth user exists for the email (created when
    /// absent). Idempotent. Service_role stays server-side and never appears
    /// in messages, claims or browser assets.
    /// </summary>
    Task<Result<AuthUser, DomainError>> EnsureAuthUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
